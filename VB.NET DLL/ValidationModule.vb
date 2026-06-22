' =========================================================================
' ValidationModule.vb
' Validation and extraction helpers used by the DLL and available to any
' consuming program that references the DLL.
'
' Functions in this module are grouped by concern:
'   - Connection state validation
'   - String, integer, and method-name input validation
'   - Named-parameter JSON parsing
'   - Raw-stream availability check and message framing
'   - Error code comparison
'   - Echo response verification
'   - error.data extraction from RemoteInvocationException
'   - Batch result arithmetic verification
' =========================================================================
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports StreamJsonRpc

Public Module ValidationModule

    ' =========================================================================
    ' CONNECTION VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Returns (IsValid=True) when the JsonRpc instance exists and has not been
    ''' disposed.  Call this before any RPC operation to provide a clear error
    ''' message to the user rather than letting an ObjectDisposedException surface
    ''' from inside StreamJsonRpc.
    ''' </summary>
    Public Function ValidateConnection(jsonRpc As JsonRpc) As (IsValid As Boolean, ErrorMessage As String)
        If jsonRpc Is Nothing Then
            Return (False, "Not connected to server. JsonRpc is Nothing.")
        End If

        If jsonRpc.IsDisposed Then
            Return (False, "Connection is disposed. Please reconnect to server.")
        End If

        Return (True, String.Empty)
    End Function

    ' =========================================================================
    ' STRING VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Returns (IsValid=False) when text is null, empty, or whitespace-only.
    ''' fieldName is included in the error message for context.
    ''' </summary>
    Public Function ValidateNonEmptyString(text As String, fieldName As String) As (IsValid As Boolean, ErrorMessage As String)
        If String.IsNullOrWhiteSpace(text) Then
            Return (False, $"{fieldName} cannot be empty or whitespace")
        End If

        Return (True, String.Empty)
    End Function

    ' =========================================================================
    ' INTEGER VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Attempts to parse text as an Integer and stores the result in value.
    ''' Returns (IsValid=False) with a descriptive message if parsing fails.
    ''' </summary>
    Public Function ValidateIntegerParameter(text As String, paramName As String, ByRef value As Integer) As (IsValid As Boolean, ErrorMessage As String)
        If String.IsNullOrWhiteSpace(text) Then
            Return (False, $"{paramName} cannot be empty")
        End If

        If Not Integer.TryParse(text.Trim(), value) Then
            Return (False, $"{paramName} ""{text}"" is not a valid integer")
        End If

        Return (True, String.Empty)
    End Function

    ''' <summary>
    ''' Validates and parses two integer parameters, stopping at the first failure.
    ''' </summary>
    Public Function ValidateTwoIntegerParameters(param1Text As String, param2Text As String, ByRef int1 As Integer, ByRef int2 As Integer) As (IsValid As Boolean, ErrorMessage As String)
        If String.IsNullOrWhiteSpace(param1Text) OrElse String.IsNullOrWhiteSpace(param2Text) Then
            Return (False, "Both parameters must be provided")
        End If

        Dim result1 = ValidateIntegerParameter(param1Text, "Parameter 1", int1)
        If Not result1.IsValid Then
            Return (False, result1.ErrorMessage)
        End If

        Dim result2 = ValidateIntegerParameter(param2Text, "Parameter 2", int2)
        If Not result2.IsValid Then
            Return (False, result2.ErrorMessage)
        End If

        Return (True, String.Empty)
    End Function

    ' =========================================================================
    ' METHOD VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Checks that a method name string is not null or empty.
    ''' The caller is responsible for converting any UI selection object to a
    ''' String before passing — this function must not receive UI control items.
    ''' </summary>
    Public Function ValidateMethodSelection(selectedItem As String) As (IsValid As Boolean, ErrorMessage As String)
        If selectedItem Is Nothing Then
            Return (False, "No method selected. Please select a method from the dropdown.")
        End If
        Return ValidateNonEmptyString(selectedItem, "Selected method name")
    End Function

    ' =========================================================================
    ' NAMED PARAMETERS VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Parses a user-supplied named-parameter string into a typed dictionary.
    '''
    ''' Input format: key:value pairs without surrounding braces, e.g.
    '''   "x":10, "y":43
    ''' The function wraps the input in { } before parsing as JSON, then
    ''' converts each JSON value to a native .NET type (Integer, Double,
    ''' String, Boolean, or Nothing for null).  JSON TRUE/FALSE (any case)
    ''' are normalised to lowercase before parsing.
    ''' </summary>
    Public Function ValidateNamedParameters(input As String, ByRef paramsObject As Dictionary(Of String, Object)) As (IsValid As Boolean, ErrorMessage As String)
        Try
            input = input.Trim()

            If String.IsNullOrWhiteSpace(input) Then
                Return (False, "Parameters cannot be empty")
            End If

            ' Normalise boolean literals so JSON.NET accepts any casing
            Dim normalizedInput As String = input
            normalizedInput = Regex.Replace(normalizedInput, "\bTRUE\b", "true", RegexOptions.IgnoreCase)
            normalizedInput = Regex.Replace(normalizedInput, "\bFALSE\b", "false", RegexOptions.IgnoreCase)

            Dim jsonString As String = "{" & normalizedInput & "}"

            Try
                Using jsonDoc As JsonDocument = JsonDocument.Parse(jsonString)

                    If jsonDoc.RootElement.ValueKind <> JsonValueKind.Object Then
                        Return (False, "Parameters must be in object format: ""key"":value")
                    End If

                    Dim paramDict As New Dictionary(Of String, Object)
                    For Each prop In jsonDoc.RootElement.EnumerateObject()
                        Select Case prop.Value.ValueKind
                            Case JsonValueKind.Number
                                Dim intValue As Integer
                                If prop.Value.TryGetInt32(intValue) Then
                                    paramDict(prop.Name) = intValue
                                Else
                                    paramDict(prop.Name) = prop.Value.GetDouble()
                                End If
                            Case JsonValueKind.String
                                paramDict(prop.Name) = prop.Value.GetString()
                            Case JsonValueKind.True
                                paramDict(prop.Name) = True
                            Case JsonValueKind.False
                                paramDict(prop.Name) = False
                            Case JsonValueKind.Null
                                paramDict(prop.Name) = Nothing
                            Case Else
                                paramDict(prop.Name) = prop.Value.GetRawText()
                        End Select
                    Next

                    If paramDict.Count = 0 Then
                        Return (False, "At least one named parameter is required")
                    End If

                    paramsObject = paramDict
                    Return (True, String.Empty)

                End Using

            Catch jsonEx As JsonException
                Return (False, $"Invalid JSON format: {jsonEx.Message}")
            End Try

        Catch ex As Exception
            Return (False, $"Validation error: {ex.Message}")
        End Try
    End Function

    ' =========================================================================
    ' RAW MESSAGE VALIDATION AND FRAMING
    ' =========================================================================

    ''' <summary>
    ''' Checks that a pipe stream is available and writable before a raw
    ''' protocol test writes directly to it.  Used by ErrorTestingModule for
    ''' the -32700 and -32600 tests which bypass JsonRpc intentionally.
    ''' </summary>
    Public Function ValidateStreamForTest(stream As System.IO.Stream) As (IsValid As Boolean, ErrorMessage As String)
        If stream Is Nothing Then
            Return (False, "Pipe stream is not available")
        End If
        If Not stream.CanWrite Then
            Return (False, "Pipe stream is not writable")
        End If
        Return (True, String.Empty)
    End Function

    ''' <summary>
    ''' Wraps a JSON string in a Content-Length framed message suitable for
    ''' writing directly to the pipe stream.  This is the same framing that
    ''' HeaderDelimitedMessageHandler uses, but applied manually so that
    ''' malformed JSON payloads can be sent without StreamJsonRpc intercepting
    ''' or rejecting them first — required for the -32700 and -32600 error tests.
    ''' </summary>
    Public Function BuildRawMessage(jsonBody As String) As Byte()
        Dim contentLength As Integer = System.Text.Encoding.UTF8.GetByteCount(jsonBody)
        Dim CRLF As String = Convert.ToChar(13).ToString() & Convert.ToChar(10).ToString()
        Dim fullMessage As String = $"Content-Length: {contentLength}{CRLF}{CRLF}{jsonBody}"
        Return System.Text.Encoding.UTF8.GetBytes(fullMessage)
    End Function

    ' =========================================================================
    ' ERROR CODE VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Compares a received RPC error code against the expected value and
    ''' returns a human-readable result message.  Used by ErrorTestingModule
    ''' to confirm that the server returned the correct standard error code.
    ''' </summary>
    Public Function ValidateErrorCode(receivedCode As Integer, expectedCode As Integer) As (IsMatch As Boolean, Message As String)
        If receivedCode = expectedCode Then
            Return (True, $"ERROR {expectedCode} VERIFIED!")
        Else
            Return (False, $"Expected error {expectedCode}, received: {receivedCode}")
        End If
    End Function

    ' =========================================================================
    ' ECHO RESPONSE VALIDATION
    ' =========================================================================

    Public Enum EchoResult
        ExactMatch
        PartialMatch
        Mismatch
        EmptyResponse
        NullResponse
    End Enum

    ''' <summary>
    ''' Compares a server's echo response against the original message.
    ''' Returns ExactMatch when the response equals the sent message,
    ''' PartialMatch when the sent message appears within a longer response
    ''' (e.g. the server prefixes or suffixes it), and Mismatch otherwise.
    ''' </summary>
    Public Function ValidateEchoResponse(sentMessage As String, response As String) As (Result As EchoResult, Message As String)
        If response Is Nothing Then
            Return (EchoResult.NullResponse, "Server returned NULL")
        End If
        If String.IsNullOrEmpty(response) Then
            Return (EchoResult.EmptyResponse, "Server returned empty string")
        End If
        If response = sentMessage Then
            Return (EchoResult.ExactMatch, "ECHO VERIFIED - Message matches perfectly!")
        End If
        If response.Contains(sentMessage) Then
            Return (EchoResult.PartialMatch, "Echo received with prefix/suffix")
        End If
        Return (EchoResult.Mismatch, "ECHO MISMATCH DETECTED!")
    End Function

    ' =========================================================================
    ' ERROR DATA EXTRACTION
    ' =========================================================================

    ''' <summary>
    ''' Extracts and formats the optional error.data member from a JSON-RPC
    ''' error response surfaced as an exception by StreamJsonRpc.
    '''
    ''' The JSON-RPC 2.0 specification allows a server to include an optional
    ''' "data" member in an error response for additional diagnostic detail.
    ''' StreamJsonRpc exposes this as RemoteInvocationException.ErrorData.
    '''
    ''' The parameter is typed as Exception rather than RemoteInvocationException
    ''' because StreamJsonRpc can throw RemoteMethodNotFoundException and
    ''' RemoteSerializationException for certain error codes, and those types
    ''' are not direct subclasses of RemoteInvocationException in this version
    ''' of the library.  TryCast is used internally to reach ErrorData only when
    ''' the exception is actually a RemoteInvocationException.
    '''
    ''' Returns HasData=False and empty strings when no error data is present,
    ''' so callers can always check HasData before logging without null guards.
    ''' </summary>
    Public Function ExtractErrorData(ex As Exception) As (HasData As Boolean, Summary As String, RawJson As String)
        Dim rpcEx = TryCast(ex, RemoteInvocationException)
        If rpcEx Is Nothing OrElse rpcEx.ErrorData Is Nothing Then
            Return (False, String.Empty, String.Empty)
        End If

        Try
            Dim raw As String = rpcEx.ErrorData.ToString()
            If String.IsNullOrWhiteSpace(raw) OrElse raw = "null" Then
                Return (False, String.Empty, String.Empty)
            End If
            Return (True, $"error.data: {raw}", raw)
        Catch
            Return (False, String.Empty, String.Empty)
        End Try
    End Function

    ' =========================================================================
    ' BATCH REQUEST VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Verifies that an AddInt result equals the arithmetic sum of its inputs.
    ''' Used after a batch request to confirm the server computed the correct
    ''' value rather than returning a cached or default result.
    ''' </summary>
    Public Function ValidateBatchAddResult(param1 As Integer, param2 As Integer, result As Integer) As (IsValid As Boolean, Message As String)
        Dim expected As Integer = param1 + param2
        If result = expected Then
            Return (True, $"Calculation verified: {param1} + {param2} = {result}")
        End If
        Return (False, $"Calculation mismatch: expected {expected}, received {result}")
    End Function

End Module

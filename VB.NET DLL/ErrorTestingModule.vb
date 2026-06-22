' =========================================================================
' ErrorTestingModule.vb
' Tests for the five standard JSON-RPC 2.0 error codes.
'
' Two different test strategies are used depending on the error type:
'
' Raw-stream tests  (-32700, -32600):
'   These errors are detected by the server BEFORE it can form a valid
'   JSON-RPC response.  The server closes the connection instead of sending
'   an error object.  To trigger them the test bypasses StreamJsonRpc and
'   writes a malformed payload directly to the underlying pipe stream.
'   Success is confirmed by waiting for the JsonRpc Disconnected event rather
'   than by inspecting a returned error code.  Because the connection is
'   destroyed, an automatic reconnect is performed after each test.
'
' Normal RPC tests  (-32601, -32602, -32603):
'   The server receives a syntactically valid JSON-RPC request but cannot
'   fulfil it (unknown method, wrong params, or internal fault).  It returns
'   a well-formed error response object.  StreamJsonRpc deserialises the
'   response and throws an exception whose ErrorCode property carries the
'   standard code.  The connection remains intact after these tests.
' =========================================================================
Imports System.IO
Imports System.IO.Pipes
Imports System.Threading.Tasks
Imports StreamJsonRpc
Imports StreamJsonRpc.Protocol

Public Module ErrorTestingModule

    ' =========================================================================
    ' TEST RESULT TYPE
    ' =========================================================================

    ''' <summary>
    ''' Returned by every test function.
    ''' NeedsReconnect is True for raw-stream tests (-32700, -32600) that
    ''' destroy the connection; the caller should reconnect before the next
    ''' RPC operation.
    ''' </summary>
    Public Class TestResult
        Public Property Passed As Boolean
        Public Property Message As String
        Public Property ErrorCode As Integer
        Public Property NeedsReconnect As Boolean
    End Class

    ' =========================================================================
    ' SEND RAW MALFORMED PAYLOAD
    ' =========================================================================

    ''' <summary>
    ''' Writes a Content-Length framed payload directly to the pipe stream,
    ''' bypassing StreamJsonRpc entirely.  This is the only way to send
    ''' malformed JSON or structurally invalid requests — StreamJsonRpc would
    ''' refuse to serialise them through its normal API.
    ''' </summary>
    Public Async Function SendRawMalformedJson(
        stream As Stream,
        malformedJson As String) As Task

        Dim messageBytes As Byte() = ValidationModule.BuildRawMessage(malformedJson)
        Await stream.WriteAsync(messageBytes, 0, messageBytes.Length)
        Await stream.FlushAsync()
    End Function

    ' =========================================================================
    ' DISCONNECT VERIFICATION HELPER
    ' =========================================================================

    ''' <summary>
    ''' Waits up to AppConstants.DisconnectVerificationTimeoutMs for the server
    ''' to close the connection after a malformed payload is received.
    ''' Returns True  — server disconnected within the timeout (error confirmed).
    ''' Returns False — connection still open after timeout (error unconfirmed).
    '''
    ''' Uses TaskCompletionSource so the wait is event-driven rather than
    ''' polling.  The temporary Disconnected handler removes itself as soon as
    ''' the event fires to avoid duplicate notifications.
    ''' </summary>
    Private Async Function WaitForDisconnectAsync(
        rpc As JsonRpc,
        logCallback As Action(Of String)) As Task(Of Boolean)

        Dim tcs As New TaskCompletionSource(Of Boolean)()

        Dim handler As EventHandler(Of JsonRpcDisconnectedEventArgs) = Nothing
        handler = Sub(sender, e)
                      logCallback($"   Disconnect reason: {e.Reason}")
                      RemoveHandler rpc.Disconnected, handler
                      tcs.TrySetResult(True)
                  End Sub

        AddHandler rpc.Disconnected, handler

        Dim timeoutTask As Task = Task.Delay(AppConstants.DisconnectVerificationTimeoutMs)
        Dim completed As Task = Await Task.WhenAny(tcs.Task, timeoutTask)

        If completed Is timeoutTask Then
            RemoveHandler rpc.Disconnected, handler
            tcs.TrySetResult(False)
            Return False
        End If

        Return tcs.Task.Result
    End Function

    ' =========================================================================
    ' RAW DISCONNECT ERROR TEST  (shared implementation for -32700 and -32600)
    ' =========================================================================

    ''' <summary>
    ''' Shared implementation used by TestParseErrorAsync (-32700) and
    ''' TestInvalidRequestAsync (-32600).
    '''
    ''' Both tests follow the same sequence:
    '''   1. Validate that the pipe stream is available and writable.
    '''   2. Send the caller-supplied raw payload directly to the pipe.
    '''   3. Wait for the server to disconnect (confirms the payload was rejected).
    '''   4. Pause briefly (ReconnectPreDelayMs) to let OS buffers drain.
    '''   5. Reconnect via the caller-supplied reconnectCallback.
    '''
    ''' All error-specific strings (type name, description, labels) are
    ''' supplied by the caller so this function stays generic.
    ''' </summary>
    Private Async Function TestRawDisconnectErrorAsync(
        pipeClient As NamedPipeClientStream,
        rpc As JsonRpc,
        logCallback As Action(Of String),
        reconnectCallback As Func(Of Task),
        errorCode As Integer,
        errorTypeName As String,
        errorDescription As String,
        payload As String,
        sendingNote As String,
        payloadLabel As String,
        sentNote As String,
        rejectedDescription As String) As Task(Of TestResult)

        Dim result = New TestResult()
        Try
            Dim streamResult = ValidationModule.ValidateStreamForTest(pipeClient)
            If Not streamResult.IsValid Then
                logCallback(streamResult.ErrorMessage)
                result.Passed = False
                result.Message = streamResult.ErrorMessage
                Return result
            End If

            logCallback("=========================================")
            logCallback($"TESTING JSON-RPC 2.0 ERROR {errorCode}")
            logCallback($"   Error Type: {errorTypeName}")
            logCallback($"   Description: {errorDescription}")
            logCallback("=========================================")

            logCallback(sendingNote)
            logCallback($"   {payloadLabel}: {payload}")

            Await SendRawMalformedJson(pipeClient, payload)
            logCallback($"{sentNote} — waiting for server disconnect to verify {errorCode}...")

            Dim disconnected As Boolean = Await WaitForDisconnectAsync(rpc, logCallback)

            If disconnected Then
                logCallback($"SERVER DISCONNECT VERIFIED — server rejected {rejectedDescription}")
                logCallback($"ERROR {errorCode} CONFIRMED")
                result.Passed = True
                result.ErrorCode = errorCode
                result.Message = $"Server disconnected after {rejectedDescription} — error {errorCode} confirmed"
            Else
                logCallback($"WARNING: Server did not disconnect within {AppConstants.DisconnectVerificationTimeoutMs \ 1000} seconds")
                logCallback($"ERROR {errorCode} UNCONFIRMED — server may not have rejected the payload")
                result.Passed = False
                result.ErrorCode = 0
                result.Message = $"Server did not disconnect — error {errorCode} unconfirmed"
            End If

            result.NeedsReconnect = True

        Catch ex As IOException
            logCallback($"IO ERROR during test: {ex.Message}")
            result.Passed = False
            result.NeedsReconnect = True
            result.Message = ex.Message
        Catch ex As Exception
            logCallback($"TEST ERROR: {ex.Message}")
            result.Passed = False
            result.NeedsReconnect = True
            result.Message = ex.Message
        End Try

        If result.NeedsReconnect Then
            ' Brief pause to allow pipe buffers to drain before reconnecting
            Await Task.Delay(AppConstants.ReconnectPreDelayMs)
            logCallback("Automatically reconnecting to server...")
            Await reconnectCallback()
        End If

        Return result
    End Function

    ' =========================================================================
    ' TEST -32700: PARSE ERROR
    ' =========================================================================

    ''' <summary>
    ''' Sends malformed JSON (a request with a missing closing brace) directly
    ''' to the pipe stream.  The server cannot parse the JSON and should close
    ''' the connection with error -32700.  Success is confirmed by the
    ''' Disconnected event rather than an error response.
    ''' </summary>
    Public Async Function TestParseErrorAsync(
        pipeClient As NamedPipeClientStream,
        rpc As JsonRpc,
        logCallback As Action(Of String),
        reconnectCallback As Func(Of Task)) As Task(Of TestResult)

        Return Await TestRawDisconnectErrorAsync(
            pipeClient, rpc, logCallback, reconnectCallback,
            errorCode:=-32700,
            errorTypeName:="Parse error",
            errorDescription:="Invalid JSON was received by the server",
            payload:="{""jsonrpc"":""2.0"",""method"":""test"",""params"":[],""id"":1",
            sendingNote:="-> Sending malformed JSON (missing closing brace)",
            payloadLabel:="Malformed JSON",
            sentNote:="Malformed JSON sent",
            rejectedDescription:="malformed JSON")
    End Function

    ' =========================================================================
    ' TEST -32600: INVALID REQUEST
    ' =========================================================================

    ''' <summary>
    ''' Sends a syntactically valid JSON object that is missing the required
    ''' "jsonrpc" field, making it an invalid JSON-RPC 2.0 request object.
    ''' The server should close the connection with error -32600.
    ''' </summary>
    Public Async Function TestInvalidRequestAsync(
        pipeClient As NamedPipeClientStream,
        rpc As JsonRpc,
        logCallback As Action(Of String),
        reconnectCallback As Func(Of Task)) As Task(Of TestResult)

        Return Await TestRawDisconnectErrorAsync(
            pipeClient, rpc, logCallback, reconnectCallback,
            errorCode:=-32600,
            errorTypeName:="Invalid Request",
            errorDescription:="The JSON sent is not a valid Request object",
            payload:="{""method"":""test"",""params"":[],""id"":100}",
            sendingNote:="-> Sending invalid JSON-RPC request (missing 'jsonrpc' field)",
            payloadLabel:="Invalid request",
            sentNote:="Invalid request sent",
            rejectedDescription:="invalid request")
    End Function

    ' =========================================================================
    ' TEST -32601: METHOD NOT FOUND
    ' =========================================================================

    ''' <summary>
    ''' Calls a method name that does not exist on the server.  The server
    ''' should return a -32601 error response.  StreamJsonRpc typically surfaces
    ''' this as RemoteMethodNotFoundException, but some server implementations
    ''' return a plain RemoteInvocationException with ErrorCode=-32601 instead,
    ''' so both catch blocks are present.
    ''' </summary>
    Public Async Function TestMethodNotFoundAsync(
        rpc As JsonRpc,
        logCallback As Action(Of String)) As Task(Of TestResult)

        Dim result = New TestResult()
        Try
            logCallback("=========================================")
            logCallback("TESTING JSON-RPC 2.0 ERROR -32601")
            logCallback("   Error Type: Method not found")
            logCallback("=========================================")
            logCallback("-> Calling non-existent method: 'NonExistentMethod'")

            Dim response As String = Await rpc.InvokeAsync(Of String)("NonExistentMethod", "test_parameter")

            logCallback($"WARNING: Unexpectedly received response: ""{response}""")
            result.Passed = False
            result.Message = $"Unexpected response: {response}"

        Catch ex As RemoteMethodNotFoundException
            logCallback("ERROR -32601 VERIFIED!")
            logCallback($"   Exception type: {ex.GetType().Name}")
            logCallback($"   Error message: {ex.Message}")
            result.Passed = True
            result.ErrorCode = -32601
            result.Message = ex.Message

        Catch ex As RemoteInvocationException
            Dim codeResult = ValidationModule.ValidateErrorCode(ex.ErrorCode, -32601)
            logCallback(codeResult.Message)
            logCallback($"   Error message: {ex.Message}")
            Dim errData = ValidationModule.ExtractErrorData(ex)
            If errData.HasData Then logCallback($"   {errData.Summary}")
            result.Passed = codeResult.IsMatch
            result.ErrorCode = ex.ErrorCode
            result.Message = ex.Message
        End Try

        Return result
    End Function

    ' =========================================================================
    ' TEST -32602: INVALID PARAMS
    ' =========================================================================

    ''' <summary>
    ''' Calls a valid server method with the wrong number of parameters.
    ''' The server should return a -32602 error response.
    '''
    ''' NOTE: The method name "AddInt" is hard-coded here because this module
    ''' is method-neutral and cannot reference the consuming program's
    ''' AppConstants.  If the server uses case-sensitive method dispatch,
    ''' this name must match the server's registered name exactly — verify
    ''' against the server if this test returns -32601 instead of -32602.
    '''
    ''' Two catch blocks handle the same error because StreamJsonRpc may throw
    ''' either RemoteMethodNotFoundException or RemoteInvocationException
    ''' depending on the server's error response structure.
    ''' </summary>
    Public Async Function TestInvalidParamsAsync(
        rpc As JsonRpc,
        logCallback As Action(Of String)) As Task(Of TestResult)

        Dim result = New TestResult()
        Try
            logCallback("=========================================")
            logCallback("TESTING JSON-RPC 2.0 ERROR -32602")
            logCallback("   Error Type: Invalid params")
            logCallback("=========================================")
            logCallback("-> Calling 'AddInt' with wrong parameter count")

            Dim response As Integer = Await rpc.InvokeAsync(Of Integer)("AddInt", 10)

            logCallback($"WARNING: Unexpectedly received response: {response}")
            result.Passed = False
            result.Message = $"Unexpected response: {response}"

        Catch ex As RemoteMethodNotFoundException
            Dim codeResult = ValidationModule.ValidateErrorCode(ex.ErrorCode, -32602)
            logCallback(codeResult.Message)
            logCallback($"   Exception type: {ex.GetType().Name}")
            logCallback($"   Error message: {ex.Message}")
            Dim errData1 = ValidationModule.ExtractErrorData(ex)
            If errData1.HasData Then logCallback($"   {errData1.Summary}")
            result.Passed = codeResult.IsMatch
            result.ErrorCode = ex.ErrorCode
            result.Message = ex.Message

        Catch ex As RemoteInvocationException
            Dim codeResult = ValidationModule.ValidateErrorCode(ex.ErrorCode, -32602)
            logCallback(codeResult.Message)
            logCallback($"   Error message: {ex.Message}")
            Dim errData2 = ValidationModule.ExtractErrorData(ex)
            If errData2.HasData Then logCallback($"   {errData2.Summary}")
            result.Passed = codeResult.IsMatch
            result.ErrorCode = ex.ErrorCode
            result.Message = ex.Message
        End Try

        Return result
    End Function

    ' =========================================================================
    ' TEST -32603: INTERNAL ERROR
    ' =========================================================================

    ''' <summary>
    ''' Triggers a server-side internal error by calling DivideInt with a
    ''' divisor of zero.  The server should detect the fault and return -32603.
    '''
    ''' NOTE: The method name "DivideInt" is hard-coded here for the same
    ''' reason as in TestInvalidParamsAsync — verify casing against the server
    ''' if the test returns -32601 instead of -32603.
    '''
    ''' Three catch blocks are required:
    '''   RemoteMethodNotFoundException — server reports method not found
    '''     (would indicate the method name is wrong).
    '''   RemoteSerializationException  — StreamJsonRpc throws this for some
    '''     server error responses where the error code is not carried in the
    '''     standard ErrorCode property; a DirectCast is needed to read it.
    '''   RemoteInvocationException     — the general fallback for all other
    '''     server error responses.
    ''' </summary>
    Public Async Function TestInternalErrorAsync(
        rpc As JsonRpc,
        logCallback As Action(Of String)) As Task(Of TestResult)

        Dim result = New TestResult()
        Try
            logCallback("=========================================")
            logCallback("TESTING JSON-RPC 2.0 ERROR -32603")
            logCallback("   Error Type: Internal error")
            logCallback("=========================================")
            logCallback("-> Calling 'DivideInt' with division by zero")

            Dim response As Integer = Await rpc.InvokeAsync(Of Integer)("DivideInt", 10, 0)

            logCallback($"WARNING: Operation completed with result: {response}")
            result.Passed = False
            result.Message = $"Unexpected result: {response}"

        Catch ex As RemoteMethodNotFoundException
            ' Method name mismatch — server does not recognise "DivideInt"
            logCallback("Method 'DivideInt' not found on server.")
            result.Passed = False
            result.ErrorCode = -32601
            result.Message = "DivideInt method does not exist on server"

        Catch ex As RemoteSerializationException
            ' StreamJsonRpc wraps the error code differently for some responses
            Dim errorCodeValue As Integer = CInt(DirectCast(ex.ErrorCode, Object))
            Dim codeResult = ValidationModule.ValidateErrorCode(errorCodeValue, -32603)
            logCallback(codeResult.Message)
            logCallback($"   Exception type: {ex.GetType().Name}")
            logCallback($"   Error message: {ex.Message}")
            Dim errData3 = ValidationModule.ExtractErrorData(ex)
            If errData3.HasData Then logCallback($"   {errData3.Summary}")
            result.Passed = codeResult.IsMatch
            result.ErrorCode = errorCodeValue
            result.Message = ex.Message

        Catch ex As RemoteInvocationException
            Dim errorCodeValue As Integer = CInt(DirectCast(ex.ErrorCode, Object))
            Dim codeResult = ValidationModule.ValidateErrorCode(errorCodeValue, -32603)
            logCallback(codeResult.Message)
            logCallback($"   Error message: {ex.Message}")
            Dim errData4 = ValidationModule.ExtractErrorData(ex)
            If errData4.HasData Then logCallback($"   {errData4.Summary}")
            result.Passed = codeResult.IsMatch
            result.ErrorCode = errorCodeValue
            result.Message = ex.Message

        End Try

        Return result
    End Function

End Module

' =========================================================================
' RPCOperations.vb
' Wrapper functions for outbound JSON-RPC 2.0 calls to the server.
'
' Design — method-neutral wrappers:
'   Every public function accepts a methodName parameter supplied by the
'   caller.  No server method names are hard-coded here.  This keeps the DLL
'   reusable with any JSON-RPC server; the consuming program defines its own
'   method name constants and passes them in.
'
' Timeout:
'   All calls are raced against AppConstants.RequestTimeoutMs via the private
'   WithTimeout helpers.  A TimeoutException is thrown if the server does not
'   respond in time, allowing the caller to handle it without waiting forever.
'
' Error handling:
'   Server-side errors (wrong method name, invalid params, internal errors)
'   are surfaced as RemoteInvocationException by StreamJsonRpc and propagate
'   to the caller unchanged — no swallowing or wrapping is done here.
' =========================================================================
Imports StreamJsonRpc

Public Module RPCOperations

    ' =========================================================================
    ' TIMEOUT HELPERS  (private — used only within this module)
    ' =========================================================================

    ' Two overloads are required: one for Task(Of T) (functions that return a
    ' value) and one for Task (procedures that return nothing).  VB.NET cannot
    ' unify them with a single generic because Await on a plain Task does not
    ' produce a value, so the return handling differs.

    Private Async Function WithTimeout(Of T)(taskToRun As Task(Of T)) As Task(Of T)
        Dim timeoutTask As Task = Task.Delay(AppConstants.RequestTimeoutMs)
        Dim completed As Task = Await Task.WhenAny(taskToRun, timeoutTask)
        If completed Is timeoutTask Then
            Throw New TimeoutException($"Request timed out after {AppConstants.RequestTimeoutMs \ 1000} seconds")
        End If
        Return Await taskToRun
    End Function

    Private Async Function WithTimeout(task As Task) As Task
        Dim timeoutTask As Task = Task.Delay(AppConstants.RequestTimeoutMs)
        Dim completed As Task = Await Task.WhenAny(task, timeoutTask)
        If completed Is timeoutTask Then
            Throw New TimeoutException($"Request timed out after {AppConstants.RequestTimeoutMs \ 1000} seconds")
        End If
        Await task
    End Function

    ' =========================================================================
    ' SEND MESSAGE  (request — expects a string response)
    ' =========================================================================

    ''' <summary>
    ''' Sends a single string parameter to the server using the supplied method
    ''' name and returns the server's string response.
    ''' Typical use: echo / message round-trip tests.
    ''' </summary>
    Public Async Function SendMessageAsync(
        rpc As JsonRpc,
        methodName As String,
        message As String) As Task(Of String)

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Return Await WithTimeout(rpc.InvokeAsync(Of String)(methodName, message))
    End Function

    ' =========================================================================
    ' INTEGER OPERATION  (request — expects an Integer response)
    ' =========================================================================

    ''' <summary>
    ''' Calls a two-parameter integer operation on the server and returns the
    ''' integer result.  Suitable for add, subtract, multiply, divide, etc.
    ''' </summary>
    Public Async Function ExecuteOperationAsync(
        rpc As JsonRpc,
        methodName As String,
        param1 As Integer,
        param2 As Integer) As Task(Of Integer)

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Return Await WithTimeout(rpc.InvokeAsync(Of Integer)(methodName, param1, param2))
    End Function

    ' =========================================================================
    ' REAL (DOUBLE) OPERATIONS  (request — expects a Double response)
    ' =========================================================================

    ''' <summary>
    ''' Calls a two-parameter real (Double) operation on the server and returns
    ''' the Double result.  Suitable for add, subtract, multiply, divide, etc.
    ''' </summary>
    Public Async Function ExecuteRealOperationAsync(
        rpc As JsonRpc,
        methodName As String,
        param1 As Double,
        param2 As Double) As Task(Of Double)

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Return Await WithTimeout(rpc.InvokeAsync(Of Double)(methodName, param1, param2))
    End Function

    ''' <summary>
    ''' Single-parameter overload of ExecuteRealOperationAsync.
    ''' Used for operations such as benchmarks that take one scalar input.
    ''' </summary>
    Public Async Function ExecuteRealOperationAsync(
        rpc As JsonRpc,
        methodName As String,
        param1 As Double) As Task(Of Double)

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Return Await WithTimeout(rpc.InvokeAsync(Of Double)(methodName, param1))
    End Function

    ' =========================================================================
    ' STRING OPERATION  (request — expects a String response)
    ' =========================================================================

    ''' <summary>
    ''' Calls a two-parameter string operation on the server and returns the
    ''' string result.
    '''
    ''' This wrapper is used for operations where the caller encodes parameter
    ''' values as strings before sending — for example, complex numbers passed
    ''' as canonical strings (e.g. "43 + 16i") because JSON-RPC 2.0 has no
    ''' native complex number type.  All encoding and decoding of the string
    ''' content is the caller's responsibility; this function transfers the
    ''' strings as-is.
    ''' </summary>
    Public Async Function ExecuteStringOperationAsync(
        rpc As JsonRpc,
        methodName As String,
        param1 As String,
        param2 As String) As Task(Of String)

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Return Await WithTimeout(rpc.InvokeAsync(Of String)(methodName, param1, param2))
    End Function

    ' =========================================================================
    ' NOTIFICATIONS  (fire-and-forget — no response expected)
    ' =========================================================================

    ''' <summary>
    ''' Sends a JSON-RPC 2.0 notification carrying a single string message.
    ''' No response is expected or awaited.
    '''
    ''' StreamJsonRpc's NotifyAsync omits the "id" field from the serialised
    ''' JSON, which is the correct wire format for notifications per the
    ''' JSON-RPC 2.0 specification, and distinguishes them from requests on
    ''' the server side.
    ''' </summary>
    Public Async Function SendNotificationAsync(
        rpc As JsonRpc,
        methodName As String,
        message As String) As Task

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Await WithTimeout(rpc.NotifyAsync(methodName, message))
    End Function

    ''' <summary>
    ''' Parameterless overload of SendNotificationAsync.
    ''' Use for server-control signals such as "stop", "pause", or "resume"
    ''' where no message payload is needed.
    ''' </summary>
    Public Async Function SendNotificationAsync(
        rpc As JsonRpc,
        methodName As String) As Task

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Await WithTimeout(rpc.NotifyAsync(methodName))
    End Function

    ' =========================================================================
    ' NAMED PARAMETERS  (request — expects an Object response)
    ' =========================================================================

    ''' <summary>
    ''' Sends a request using JSON-RPC 2.0 named (by-name) parameters and
    ''' returns the server response as Object.
    '''
    ''' Named parameters are sent as a JSON object  { "key": value, ... }
    ''' rather than a positional array [ v1, v2, ... ].  The server must
    ''' support by-name parameter binding for the target method.
    ''' The caller builds the parameter dictionary; this function sends it.
    ''' </summary>
    Public Async Function SendNamedParametersAsync(
        rpc As JsonRpc,
        methodName As String,
        paramsDict As Dictionary(Of String, Object)) As Task(Of Object)

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If
        Return Await WithTimeout(rpc.InvokeWithParameterObjectAsync(Of Object)(methodName, paramsDict))
    End Function

End Module

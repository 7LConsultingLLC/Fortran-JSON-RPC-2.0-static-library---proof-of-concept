' =========================================================================
' ConnectionModule.vb
' Manages the lifecycle of the named-pipe / JSON-RPC connection.
'
' Responsibilities:
'   - Create and connect the NamedPipeClientStream (CreatePipeClientAsync)
'   - Initialise and configure the JsonRpc instance (InitializeJsonRpc)
'   - Provide a single convenience entry point (ConnectToServerAsync)
'   - Clean up all three resources in the correct tear-down order (CleanupConnection)
'   - Reconnect with retry on connection loss (ReconnectWithRetryAsync)
' =========================================================================
Imports System.IO
Imports System.IO.Pipes
Imports System.Threading
Imports System.Threading.Tasks
Imports StreamJsonRpc

Public Module ConnectionModule

    ' =========================================================================
    ' PIPE CLIENT CREATION
    ' =========================================================================

    ''' <summary>
    ''' Creates a NamedPipeClientStream and connects it to the server.
    ''' "." means the local machine; PipeDirection.InOut is required because
    ''' StreamJsonRpc reads responses and writes requests on the same stream.
    ''' PipeOptions.Asynchronous enables non-blocking IO used by StreamJsonRpc.
    ''' </summary>
    Public Async Function CreatePipeClientAsync(
        pipeName As String,
        cts As CancellationTokenSource) As Task(Of NamedPipeClientStream)

        Dim pipeClient = New NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        )
        Await pipeClient.ConnectAsync(cts.Token)
        Return pipeClient
    End Function

    ' =========================================================================
    ' JSON-RPC INITIALISATION
    ' =========================================================================

    ''' <summary>
    ''' Builds and starts a JsonRpc instance wired to the supplied pipe stream.
    '''
    ''' Transport stack:
    '''   JsonMessageFormatter    — serialises/deserialises JSON and enforces
    '''                             the "jsonrpc":"2.0" version field on every
    '''                             incoming message.  Any response that is
    '''                             missing the field or carries a wrong version
    '''                             is rejected and surfaces as a
    '''                             RemoteInvocationException or a Disconnected
    '''                             event — no manual version checking is needed.
    '''   HeaderDelimitedMessageHandler — frames each JSON object with an HTTP-
    '''                             style Content-Length header so the receiver
    '''                             knows exactly how many bytes to read.
    '''
    ''' Request IDs:
    '''   StreamJsonRpc assigns a sequential integer id (1, 2, 3 ...) to every
    '''   InvokeAsync / InvokeWithParameterObjectAsync call.  Null is never
    '''   used, so requests cannot be mistaken for notifications by the server.
    '''   NotifyAsync / NotifyWithParameterObjectAsync correctly omit the id
    '''   field entirely per the JSON-RPC 2.0 specification.
    '''
    ''' SynchronizationContext = Nothing:
    '''   Forces incoming messages (responses, server-initiated requests, and
    '''   notifications) to be dispatched on thread-pool threads rather than
    '''   marshalled back to the UI thread.  This is essential — if the UI
    '''   thread is blocked awaiting a response, a server notification that
    '''   arrives before the response would deadlock if it tried to marshal to
    '''   the same thread.  Target methods that update UI controls must
    '''   explicitly marshal via Control.BeginInvoke.
    '''
    ''' rpcTarget:
    '''   The object whose public methods the server may call.  Implemented by
    '''   RpcTargetHandler.  Pass Nothing if the server never initiates calls.
    ''' </summary>
    Public Function InitializeJsonRpc(
        pipeClient As NamedPipeClientStream,
        rpcTarget As Object,
        disconnectCallback As EventHandler(Of JsonRpcDisconnectedEventArgs)) As JsonRpc

        Dim formatter = New JsonMessageFormatter()
        Dim messageHandler = New HeaderDelimitedMessageHandler(pipeClient, pipeClient, formatter)
        Dim rpc = New JsonRpc(messageHandler, rpcTarget)

        ' Dispatch all incoming messages on thread-pool threads, not the UI thread.
        ' See summary above for why this is required.
        rpc.SynchronizationContext = Nothing

        AddHandler rpc.Disconnected, disconnectCallback
        rpc.StartListening()
        Return rpc
    End Function

    ' =========================================================================
    ' FULL CONNECTION
    ' =========================================================================

    ''' <summary>
    ''' Convenience wrapper: creates the pipe, initialises JsonRpc, and returns
    ''' all three objects the caller needs to hold for later cleanup.
    ''' Callers should store the returned tuple and pass it to CleanupConnection
    ''' when disconnecting.
    ''' </summary>
    Public Async Function ConnectToServerAsync(
        pipeName As String,
        rpcTarget As Object,
        disconnectCallback As EventHandler(Of JsonRpcDisconnectedEventArgs)) As Task(Of (Rpc As JsonRpc, PipeClient As NamedPipeClientStream, Cts As CancellationTokenSource))

        Dim newCts = New CancellationTokenSource()
        Dim newClient = Await CreatePipeClientAsync(pipeName, newCts)
        Dim newRpc = InitializeJsonRpc(newClient, rpcTarget, disconnectCallback)
        Return (newRpc, newClient, newCts)
    End Function

    ' =========================================================================
    ' CLEANUP
    ' =========================================================================

    ''' <summary>
    ''' Disposes JsonRpc, NamedPipeClientStream, and CancellationTokenSource in
    ''' the correct tear-down order.
    '''
    ''' Order matters:
    '''   1. JsonRpc first  — stops message processing and flushes pending work.
    '''   2. PipeClient     — closes the OS pipe handle after JsonRpc is done.
    '''   3. Cts last       — cancels and releases the token only after the pipe
    '''                       is closed, because outstanding async pipe operations
    '''                       may still be observing the token as it drains.
    '''
    ''' Returns True if all three disposed without error.  Returns False if any
    ''' step threw (exception is suppressed but the flag lets the caller log a
    ''' warning).  The caller is responsible for nulling its own field references
    ''' after this call returns.
    ''' </summary>
    Public Function CleanupConnection(
        pipeClient As NamedPipeClientStream,
        rpc As JsonRpc,
        cts As CancellationTokenSource) As Boolean

        Dim cleanExit As Boolean = True

        ' Step 1 — dispose JsonRpc to stop message processing
        If rpc IsNot Nothing Then
            Try
                If Not rpc.IsDisposed Then rpc.Dispose()
            Catch ex As Exception
                cleanExit = False
            End Try
        End If

        ' Step 2 — close and dispose the pipe after JsonRpc is torn down
        If pipeClient IsNot Nothing Then
            Try
                If pipeClient.IsConnected Then pipeClient.Close()
                pipeClient.Dispose()
            Catch ex As Exception
                cleanExit = False
            End Try
        End If

        ' Step 3 — cancel and dispose the CancellationTokenSource last
        If cts IsNot Nothing Then
            Try
                cts.Cancel()
                cts.Dispose()
            Catch ex As Exception
                cleanExit = False
            End Try
        End If

        Return cleanExit
    End Function

    ' =========================================================================
    ' RECONNECT WITH RETRY
    ' =========================================================================

    ''' <summary>
    ''' Cleans up the existing connection then attempts to reconnect up to
    ''' AppConstants.ReconnectMaxAttempts times.  A delay of
    ''' AppConstants.ReconnectDelayMs is inserted between attempts (but not
    ''' before the first attempt).  Throws InvalidOperationException wrapping
    ''' the last caught exception if all attempts fail.
    '''
    ''' If CleanupConnection reports a partial failure the caller is notified
    ''' via logCallback before reconnection proceeds — orphaned resources may
    ''' exist but reconnection is still attempted.
    ''' </summary>
    Public Async Function ReconnectWithRetryAsync(
        pipeName As String,
        rpcTarget As Object,
        disconnectCallback As EventHandler(Of JsonRpcDisconnectedEventArgs),
        pipeClient As NamedPipeClientStream,
        rpc As JsonRpc,
        cts As CancellationTokenSource,
        logCallback As Action(Of String)) As Task(Of (Rpc As JsonRpc, PipeClient As NamedPipeClientStream, Cts As CancellationTokenSource))

        Dim cleanedUp As Boolean = CleanupConnection(pipeClient, rpc, cts)

        If Not cleanedUp Then
            logCallback("WARNING: One or more resources did not dispose cleanly before reconnect — orphaned resources may exist")
        End If

        Dim lastException As Exception = Nothing
        Dim applyDelay As Boolean = False

        For attempt As Integer = 1 To AppConstants.ReconnectMaxAttempts

            ' No delay before the first attempt; delay between subsequent attempts
            If applyDelay Then
                Await Task.Delay(AppConstants.ReconnectDelayMs)
            End If

            Try
                Return Await ConnectToServerAsync(pipeName, rpcTarget, disconnectCallback)
            Catch ex As Exception
                lastException = ex
                applyDelay = attempt < AppConstants.ReconnectMaxAttempts
            End Try

        Next

        Throw New InvalidOperationException(
            $"Failed to reconnect after {AppConstants.ReconnectMaxAttempts} attempts. " &
            $"Last error: {lastException?.Message}", lastException)
    End Function

End Module

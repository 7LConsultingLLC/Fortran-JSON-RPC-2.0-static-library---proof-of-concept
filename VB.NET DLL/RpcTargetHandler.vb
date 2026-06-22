' =========================================================================
' RpcTargetHandler.vb
' Handles server-initiated JSON-RPC calls — the server-to-client direction.
'
' StreamJsonRpc requires a "target object" whose public methods the server
' may call.  An instance of this class is passed to InitializeJsonRpc and
' registered with JsonRpc at startup.  When the server sends a request or
' notification whose method name matches a public method on this class,
' StreamJsonRpc dispatches the call automatically.
'
' Important: method names on this class must match the server's method names
' exactly (case-sensitive).  StreamJsonRpc silently discards any incoming
' server notification whose method name does not match — there is no catch-
' all hook.  Add a new public method with the exact matching name to handle
' any additional server notification.
'
' Thread safety: StreamJsonRpc dispatches these methods on thread-pool
' threads (because SynchronizationContext = Nothing in InitializeJsonRpc).
' The _log callback marshals UI updates via Control.BeginInvoke, and the
' _scrollCallback is wrapped in UI(...) by the caller for the same reason.
' =========================================================================
Public Class RpcTargetHandler

    Private ReadOnly _log As Action(Of String)
    Private ReadOnly _scrollCallback As Action

    ''' <summary>
    ''' Creates a new RpcTargetHandler.
    ''' logCallback     — called to write a line to the application log.
    ''' scrollCallback  — called after notifications to scroll the log view.
    ''' Both callbacks must be safe to call from a background thread (i.e.
    ''' they must marshal to the UI thread internally if needed).
    ''' </summary>
    Public Sub New(logCallback As Action(Of String), scrollCallback As Action)
        _log = logCallback
        _scrollCallback = scrollCallback
    End Sub

    ' ---------------------------------------------------------
    ' Server sends a message and expects a string acknowledgement
    ' ---------------------------------------------------------
    Public Function OnServerMessage(message As String) As String
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log($"← Server message: ""{message}""")
        _log("- - - - - - - - - - - - - - - - - - - - -")
        Return "Message acknowledged by VB.NET client"
    End Function

    ' ---------------------------------------------------------
    ' Server sends a fire-and-forget notification (no response)
    ' ---------------------------------------------------------
    Public Sub OnServerNotification(message As String)
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log($"← SERVER NOTIFICATION: ""{message}""")
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log("")
        _scrollCallback?.Invoke()
    End Sub

    ' ---------------------------------------------------------
    ' Server sends progress notifications during long operations.
    ' Method name is lowercase "progress" — must match the server exactly.
    ' ---------------------------------------------------------
    Public Sub progress(percent As Integer)
        Try
            _log($"→ Progress: {percent}% complete")
            _scrollCallback?.Invoke()
        Catch ex As Exception
            ' Fallback: if UI marshalling fails, log to console so progress
            ' notifications do not crash the background dispatch thread.
            Console.WriteLine($"[ERROR in progress handler] {ex.Message}")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Server sends a status notification to acknowledge client commands
    ' such as pause, resume, and stop.
    ' Method name is lowercase "status" — must match the server exactly.
    ' ---------------------------------------------------------
    Public Sub status(message As String)
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log($"← Server status: ""{message}""")
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log("")
        _scrollCallback?.Invoke()
    End Sub

    ' ---------------------------------------------------------
    ' Server sends data and expects a Boolean acknowledgement
    ' ---------------------------------------------------------
    Public Function ReceiveData(data As String) As Task(Of Boolean)
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log($"← Server data: ""{data}""")
        _log("- - - - - - - - - - - - - - - - - - - - -")
        Return Task.FromResult(True)
    End Function

    ' ---------------------------------------------------------
    ' Server requests a one-line status string from the client
    ' ---------------------------------------------------------
    Public Function GetClientStatus() As String
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log("← Server requesting client status")
        Dim status As String = "VB.NET client is running and ready"
        _log($"→ Responding with: ""{status}""")
        _log("- - - - - - - - - - - - - - - - - - - - -")
        Return status
    End Function

    ' ---------------------------------------------------------
    ' Server requests structured client information (ClientInfo object)
    ' ---------------------------------------------------------
    Public Function GetClientInfo() As ClientInfo
        _log("- - - - - - - - - - - - - - - - - - - - -")
        _log("← Server requesting client info")
        Dim info As New ClientInfo() With {
            .Name = "VB.NET Client",
            .Version = "1.0.0",
            .Platform = Environment.OSVersion.ToString(),
            .Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        }
        _log("→ Sending client info")
        _log("- - - - - - - - - - - - - - - - - - - - -")
        Return info
    End Function

End Class

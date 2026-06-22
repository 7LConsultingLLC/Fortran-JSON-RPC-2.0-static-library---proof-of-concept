' =========================================================================
' AppConstants.vb
' Operational constants for the JSON-RPC client DLL.
'
' This module holds DLL-level behaviour constants only (timeouts, retry
' policy, pipe name).  Server method name strings are deliberately NOT
' defined here — they are application-specific and belong in the consuming
' program's own AppConstants module so the DLL remains method-neutral and
' reusable with any JSON-RPC server.
'
' NOTE: PipeName is duplicated in the client program's AppConstants.vb.
' Both must match the Fortran server's hard-coded pipe name.  Hard-coding
' the pipe name in both places is a deliberate POC simplification; a
' production implementation would supply it via configuration.
' =========================================================================

Public Module AppConstants

    ' ---------------------------------------------------------
    ' Named pipe identifier — must match the Fortran server exactly.
    ' POC exception: hard-coded for simplicity (see file header note).
    ' ---------------------------------------------------------
    Public Const PipeName As String = "MyTestPipe"

    ' ---------------------------------------------------------
    ' Reconnection retry policy used by ReconnectWithRetryAsync.
    ' After a connection is lost the DLL waits ReconnectDelayMs between
    ' each attempt and gives up after ReconnectMaxAttempts failures.
    ' ---------------------------------------------------------
    Public Const ReconnectMaxAttempts As Integer = 3
    Public Const ReconnectDelayMs As Integer = 2000

    ' ---------------------------------------------------------
    ' Stabilisation delay before reconnecting after a raw error test.
    ' Tests for -32700 (Parse Error) and -32600 (Invalid Request) send
    ' malformed payloads directly to the pipe, which causes the server to
    ' close the connection.  This brief pause allows the OS pipe buffers to
    ' drain before a new connection is attempted.
    ' ---------------------------------------------------------
    Public Const ReconnectPreDelayMs As Integer = 1000

    ' ---------------------------------------------------------
    ' Per-request timeout applied by the WithTimeout helpers in
    ' RPCOperations.vb.  Each InvokeAsync / NotifyAsync call is raced
    ' against this deadline; a TimeoutException is thrown if the server
    ' does not respond in time.
    ' ---------------------------------------------------------
    Public Const RequestTimeoutMs As Integer = 300000  ' 5 minutes

    ' ---------------------------------------------------------
    ' How long WaitForDisconnectAsync waits for the server to close the
    ' connection after a malformed payload is sent (-32700 / -32600 tests).
    ' If the server has not disconnected within this window the test is
    ' marked as unconfirmed.
    ' ---------------------------------------------------------
    Public Const DisconnectVerificationTimeoutMs As Integer = 5000

End Module

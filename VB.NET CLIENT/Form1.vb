Imports System.Text
Imports System.Threading
Imports System.IO
Imports System.IO.Pipes
Imports System.Threading.Tasks
Imports StreamJsonRpc
Imports System.Runtime.Versioning
Imports JSONRPCClientLibrary
Imports Newtonsoft.Json.Linq  ' JArray — used directly in ExecuteRealOperation to convert matrix response
Imports System.Linq            ' Take() — used in matrix data preview log lines

<SupportedOSPlatform("windows")>
Public Class frmMain
    Private client As NamedPipeClientStream
    Private jsonRpc As JsonRpc
    Private cts As CancellationTokenSource
    Private rpcTarget As RpcTargetHandler

    ' Define a class-level variable for the labels
    Private lblOM(3, 3) As Label
    Private lblMM(3, 3) As Label  ' Modified Matrix labels

    ' Seeded once at startup; shared across all matrix data generation calls
    Private rng As New Random()

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        btnExitDemo.Enabled = False
        btnPauseDemo.Enabled = False
        btnResumeDemo.Enabled = False
        Log("Client program started.")
        Log("Ready to connect to Fortran JSON-RPC server...")

        ' Initialize both matrix labels
        InitializeMatrixLabels()
        InitializeModifiedMatrixLabels()

    End Sub

    ' ---------------------------------------------------------
    ' Initialize Matrix Labels
    ' ---------------------------------------------------------
    Private Sub InitializeMatrixLabels()
        ' Use MatrixModule to initialize the original matrix
        MatrixModule.InitializeMatrixLabels(tlpOriginalMatrix, lblOM, "lblOM", AddressOf Log)
    End Sub

    ' ---------------------------------------------------------
    ' Initialize Modified Matrix Labels
    ' ---------------------------------------------------------
    Private Sub InitializeModifiedMatrixLabels()
        ' Use MatrixModule to initialize the modified matrix
        MatrixModule.InitializeMatrixLabels(tlpModifiedMatrix, lblMM, "lblMM", AddressOf Log)
    End Sub

    ' ---------------------------------------------------------
    ' Form Closing Event - Cleanup and Stop Server
    ' ---------------------------------------------------------
    Private Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Log("Client program closing...")

        ' Disconnect from server if connected
        If jsonRpc IsNot Nothing Then
            Try
                DisconnectPipe()
            Catch ex As Exception
                Log($"Error during disconnect: {ex.Message}")
            End Try
        End If

        Log("Client program stopped.")
    End Sub

    ' ---------------------------------------------------------
    ' UI Thread Marshal Helper
    ' ---------------------------------------------------------
    Private Sub UI(action As Action)
        UIHelperModule.RunOnUIThread(Me, action)
    End Sub

    ' ---------------------------------------------------------
    ' Logging Helper with Timestamp
    ' ---------------------------------------------------------
    Private Sub Log(msg As String)
        UI(Sub()
               Dim timestamp As String = DateTime.Now.ToString("HH:mm:ss.fff")
               txbVBLog.AppendText($"{timestamp}  {msg}{Environment.NewLine}")
               txbVBLog.SelectionStart = txbVBLog.Text.Length
               txbVBLog.ScrollToCaret()
           End Sub)
    End Sub

    ' ---------------------------------------------------------
    ' Connect to Fortran Server
    ' ---------------------------------------------------------
    Private Async Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        UI(Sub() btnConnect.Enabled = False)
        Log("Connecting to Fortran server...")
        Try
            rpcTarget = New RpcTargetHandler(
                AddressOf Log,
                Sub() UI(Sub()
                             txbVBLog.SelectionStart = txbVBLog.Text.Length
                             txbVBLog.ScrollToCaret()
                         End Sub))

            Dim disconnectCallback As EventHandler(Of JsonRpcDisconnectedEventArgs) =
                Sub(sender2, e2)
                    Log($"JSON-RPC disconnected: {e2.Reason}")
                    Log($"  Description: {e2.Description}")
                    If e2.Exception IsNot Nothing Then
                        Log($"  Disconnect exception: {e2.Exception.Message}")
                    End If
                    ' DO NOT call DisconnectPipe() here - let it happen in exception handlers
                    ' or when user explicitly disconnects. Calling it here causes premature
                    ' pipe closure during message parsing.
                    SetConnectionStatus(False)
                    UI(Sub() btnConnect.Enabled = True)
                End Sub

            Dim conn = Await ConnectionModule.ConnectToServerAsync(AppConstants.PipeName, rpcTarget, disconnectCallback)
            jsonRpc = conn.Rpc
            client = conn.PipeClient
            cts = conn.Cts

            Log("Connected to named pipe.")
            Log("JSON-RPC initialized with Content-Length protocol.")
            Log("Full duplex communication ready.")
            SetConnectionStatus(True)

        Catch ex As Exception
            Log($"Connect error: {ex.Message}")
            Log($"Exception type: {ex.GetType().Name}")
            If ex.InnerException IsNot Nothing Then
                Log($"Inner exception: {ex.InnerException.Message}")
            End If
            UI(Sub() btnConnect.Enabled = True)
            SetConnectionStatus(False)
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Update Connection Status Label
    ' ---------------------------------------------------------
    Private Sub SetConnectionStatus(isConnected As Boolean)
        UIHelperModule.UpdateConnectionStatus(
            lblConnectionStatus, isConnected, Color.Green, Color.Red)
    End Sub

    ' ---------------------------------------------------------
    ' Send Message to Server with Echo Response
    ' ---------------------------------------------------------
    Private Async Sub btnSendMessage_Click(sender As Object, e As EventArgs) Handles btnSendMessage.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot send — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Dim msg = txbMessage.Text.Trim
            Dim msgResult = ValidateNonEmptyString(msg, "Message")
            If Not msgResult.IsValid Then
                Log($"Cannot send — {msgResult.ErrorMessage}")
                Return
            End If

            Log("=========================================")
            Log($"-> SENDING to server: ""{msg}""")
            Log("   Method: sendmessage")

            ' JSON-RPC method names are case-sensitive; the Fortran server registers all methods lowercase.
            Dim response = Await RPCOperations.SendMessageAsync(jsonRpc, AppConstants.MethodSendMessage, msg)

            Log("=========================================")
            Dim echoResult = ValidateEchoResponse(msg, response)
            Select Case echoResult.Result
                Case JSONRPCClientLibrary.ValidationModule.EchoResult.NullResponse, JSONRPCClientLibrary.ValidationModule.EchoResult.EmptyResponse
                    Log(echoResult.Message)
                Case JSONRPCClientLibrary.ValidationModule.EchoResult.ExactMatch, JSONRPCClientLibrary.ValidationModule.EchoResult.PartialMatch
                    Log($"<- ECHO RESPONSE from server: ""{response}""")
                    Log("=========================================")
                    Log(echoResult.Message)
                    Log($"    Sent:     ""{msg}""")
                    Log($"    Received: ""{response}""")
                Case JSONRPCClientLibrary.ValidationModule.EchoResult.Mismatch
                    Log($"<- ECHO RESPONSE from server: ""{response}""")
                    Log("=========================================")
                    Log(echoResult.Message)
                    Log($"    Sent:     ""{msg}""")
                    Log($"    Received: ""{response}""")
            End Select
            Log("=========================================")
            Log("")

        Catch ex As RemoteInvocationException
            Log("=========================================")
            Log($"SERVER ERROR: {ex.Message}")
            If ex.ErrorCode <> 0 Then Log($"   Error code: {ex.ErrorCode}")
            If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Server error: {ex.Message}", "Server Error")
        Catch ex As ConnectionLostException
            Log("=========================================")
            Log($"CONNECTION LOST: {ex.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage("Connection to server lost.", "Connection Error")
            DisconnectPipe()
        Catch ex As TaskCanceledException
            Log($"Request timed out after {AppConstants.RequestTimeoutMs \ 1000} seconds")
            UIHelperModule.ShowWarningMessage($"Request timed out after {AppConstants.RequestTimeoutMs \ 1000} seconds.", "Timeout")
        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            If ex.InnerException IsNot Nothing Then Log($"   Inner exception: {ex.InnerException.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Error: {ex.Message}", "Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Send Notification to Server (No Response Expected)
    ' JSON-RPC 2.0 Notifications are requests without an "id"
    ' ---------------------------------------------------------
    Private Async Sub btnSendNotification_Click(sender As Object, e As EventArgs) Handles btnSendNotification.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot send notification — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Dim notification As String = txbNotification.Text.Trim()
            Dim notifResult = ValidateNonEmptyString(notification, "Notification")
            If Not notifResult.IsValid Then
                Log($"Cannot send — {notifResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage("Please enter a notification message.", "Empty Notification")
                Return
            End If

            Log("=========================================")
            Log($"SENDING NOTIFICATION to server: ""{notification}""")
            Log("   (No response expected - JSON-RPC 2.0 Notification)")

            Await RPCOperations.SendNotificationAsync(jsonRpc, AppConstants.MethodClientNotify, notification)

            Log("Notification dispatched to server")
            Log("   Delivery unconfirmed — no acknowledgment per JSON-RPC 2.0 spec")
            Log("=========================================")
            Log("")
            txbNotification.Clear()

        Catch ex As ConnectionLostException
            Log("=========================================")
            Log($"CONNECTION LOST: {ex.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage("Connection to server lost.", "Connection Error")
            DisconnectPipe()
        Catch ex As Exception
            Log("=========================================")
            Log($"NOTIFICATION ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            If ex.InnerException IsNot Nothing Then Log($"   Inner exception: {ex.InnerException.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Error sending notification: {ex.Message}", "Notification Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Execute Calculation
    ' ---------------------------------------------------------
    Private Async Sub btnExecute_Click(sender As Object, e As EventArgs) Handles btnExecute.Click
        Try
            Dim connResult = ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot execute — {connResult.ErrorMessage}")
                ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Dim methodResult = ValidateMethodSelection(
                If(cbMethods.SelectedItem IsNot Nothing, cbMethods.SelectedItem.ToString, Nothing))
            If Not methodResult.IsValid Then
                Log($"ERROR: {methodResult.ErrorMessage}")
                ShowWarningMessage(methodResult.ErrorMessage, "No Method Selected")
                Return
            End If

            Dim displayName = cbMethods.SelectedItem.ToString
            Dim selectedMethod = GetServerMethodName(displayName)

            Dim param1Text = txbParameter1.Text.Trim
            Dim param2Text = txbParameter2.Text.Trim

            ' Route to the correct handler based on method type
            Dim methodType As String = CalculationModule.DetermineMethodType(selectedMethod)

            Select Case methodType
                Case "integer"
                    Dim int1, int2 As Integer
                    Dim paramResult = ValidateTwoIntegerParameters(param1Text, param2Text, int1, int2)
                    If Not paramResult.IsValid Then
                        Log($"ERROR: {paramResult.ErrorMessage}")
                        ShowErrorMessage(paramResult.ErrorMessage, "Invalid Input")
                        Return
                    End If

                    Dim opInfo = GetOperationInfo(selectedMethod)
                    Log("=========================================")
                    Log($"CALLING {selectedMethod} on Fortran server")
                    Log($"   Operation: {opInfo.Name}")
                    Log($"   Parameter 1: {int1:N0}")
                    Log($"   Parameter 2: {int2:N0}")
                    Log($"   Expression: {int1:N0} {opInfo.Symbol} {int2:N0}")

                    Dim result = Await ExecuteOperationAsync(jsonRpc, selectedMethod, int1, int2)

                    Log("=========================================")
                    Log($"<- RESULT from Fortran server: {result:N0}")
                    Log("=========================================")

                    Dim expected = CalculationModule.CalculateExpectedResult(selectedMethod, int1, int2)
                    Dim verification = CalculationModule.VerifyCalculationResult(expected, result, selectedMethod, int1, int2)
                    If verification.IsMatch Then
                        Log(verification.SuccessMessage)
                        ShowSuccessMessage($"Result: {int1:N0} {opInfo.Symbol} {int2:N0} = {result:N0}", $"{opInfo.Name} Successful")
                    Else
                        Log(verification.ErrorMessage)
                        Log($"    Expected: {expected:N0}")
                        Log($"    Received: {result:N0}")
                        ShowWarningMessage($"Warning: Expected {expected:N0} but received {result:N0}", "Calculation Mismatch")
                    End If
                    Log("=========================================")
                    Log("")

                Case "real"
                    Await ExecuteRealCalculation(selectedMethod, displayName, param1Text, param2Text)

                Case "complex"
                    Await ExecuteComplexCalculation(selectedMethod, displayName, param1Text, param2Text)

                Case Else
                    Log($"ERROR: Unknown method type for '{selectedMethod}'")
                    ShowWarningMessage($"Method '{displayName}' is not recognized.", "Unknown Method")
                    Return

            End Select

        Catch ex As RemoteInvocationException
            Log("=========================================")
            Log($"SERVER ERROR: {ex.Message}")
            If ex.ErrorCode <> 0 Then Log($"   Error code: {ex.ErrorCode}")
            If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
            Log("=========================================")
            ShowErrorMessage($"Server error: {ex.Message}", "Server Error")
        Catch ex As ConnectionLostException
            Log("=========================================")
            Log($"CONNECTION LOST: {ex.Message}")
            Log("=========================================")
            ShowErrorMessage("Connection to server lost.", "Connection Error")
            DisconnectPipe()
        Catch ex As TaskCanceledException
            Log($"Request timed out after {RequestTimeoutMs \ 1000} seconds")
            ShowWarningMessage($"Request timed out after {RequestTimeoutMs \ 1000} seconds.", "Timeout")
        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType.Name}")
            If ex.InnerException IsNot Nothing Then Log($"   Inner exception: {ex.InnerException.Message}")
            Log("=========================================")
            ShowErrorMessage($"Error: {ex.Message}", "Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Send Named Parameters to Server
    ' ---------------------------------------------------------
    Private Async Sub btnSendNamedParameters_Click(sender As Object, e As EventArgs) Handles btnSendNamedParameters.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot send — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Dim namedParamsText As String = txbNamedParameters.Text.Trim()
            Dim emptyResult = ValidateNonEmptyString(namedParamsText, "Named parameters")
            If Not emptyResult.IsValid Then
                Log($"Cannot send — {emptyResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage("Please enter named parameters in the format:" & vbCrLf &
                               """x"":10, ""y"":43", "Empty Parameters")
                Return
            End If

            Dim paramsObject As Object = Nothing
            Dim validationResult = ValidationModule.ValidateNamedParameters(namedParamsText, paramsObject)
            If Not validationResult.IsValid Then
                Log($"ERROR: {validationResult.ErrorMessage}")
                UIHelperModule.ShowErrorMessage("Invalid named parameter format:" & vbCrLf & vbCrLf &
                               validationResult.ErrorMessage & vbCrLf & vbCrLf &
                               "Expected format: ""x"":10, ""y"":43", "Validation Error")
                Return
            End If

            Log("=========================================")
            Log("-> SENDING NAMED PARAMETERS to server")
            Log("   Method: namedparameters")
            Log($"   Parameters: {{{namedParamsText}}}")

            Dim result = Await RPCOperations.SendNamedParametersAsync(jsonRpc, AppConstants.MethodNamedParameters, paramsObject)

            Log("=========================================")
            Log("<- RESPONSE from server:")
            Log($"   {If(result IsNot Nothing, result.ToString(), "(null)")}")
            Log("=========================================")
            Log("")
            UIHelperModule.ShowSuccessMessage($"Named parameters sent successfully!" & vbCrLf & vbCrLf &
                           $"Response: {If(result IsNot Nothing, result.ToString(), "(null)")}", "Success")

        Catch ex As RemoteInvocationException
            Log("=========================================")
            Log($"SERVER ERROR: {ex.Message}")
            If ex.ErrorCode <> 0 Then Log($"   Error code: {ex.ErrorCode}")
            If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Server error: {ex.Message}", "Server Error")
        Catch ex As ConnectionLostException
            Log("=========================================")
            Log($"CONNECTION LOST: {ex.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage("Connection to server lost.", "Connection Error")
            DisconnectPipe()
        Catch ex As Exception
            Log("=========================================")
            Log($"NAMED PARAMETERS ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            If ex.InnerException IsNot Nothing Then Log($"   Inner exception: {ex.InnerException.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Error sending named parameters: {ex.Message}", "Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Clear Log Button
    ' ---------------------------------------------------------
    Private Sub btnClearLog_Click(sender As Object, e As EventArgs) Handles btnClearVBLog.Click
        ClearTextBox(txbVBLog)
    End Sub

    ' ---------------------------------------------------------
    ' Cleanup and Disconnect
    ' ---------------------------------------------------------
    Private Sub DisconnectPipe()
        Log("DisconnectPipe() called")

        UIHelperModule.RunOnUIThread(Me, Sub()
                                             Try
                                                 ConnectionModule.CleanupConnection(client, jsonRpc, cts)
                                                 client = Nothing
                                                 jsonRpc = Nothing
                                                 cts = Nothing
                                                 rpcTarget = Nothing
                                                 Log("Disconnected from server.")
                                                 btnConnect.Enabled = True
                                                 SetConnectionStatus(False)
                                             Catch ex As Exception
                                                 Log($"Disconnect error: {ex.Message}")
                                             End Try
                                         End Sub)
    End Sub

    ' ---------------------------------------------------------
    ' Reconnect to Server Helper
    ' ---------------------------------------------------------
    Private Async Function ReconnectToServer() As Task
        Try
            rpcTarget = New RpcTargetHandler(
                AddressOf Log,
                Sub()
                    ' Ensure scroll callback runs on UI thread
                    If Me.InvokeRequired Then
                        Me.BeginInvoke(Sub()
                                           txbVBLog.SelectionStart = txbVBLog.Text.Length
                                           txbVBLog.ScrollToCaret()
                                       End Sub)
                    Else
                        txbVBLog.SelectionStart = txbVBLog.Text.Length
                        txbVBLog.ScrollToCaret()
                    End If
                End Sub)
            Dim disconnectCallback As EventHandler(Of JsonRpcDisconnectedEventArgs) =
                Sub(sender2, e2)
                    Log($"JSON-RPC disconnected: {e2.Reason}")
                    SetConnectionStatus(False)
                    UI(Sub() btnConnect.Enabled = True)
                End Sub

            Dim conn = Await ConnectionModule.ReconnectWithRetryAsync(
                AppConstants.PipeName, rpcTarget, disconnectCallback, client, jsonRpc, cts,
                AddressOf Log)

            jsonRpc = conn.Rpc
            client = conn.PipeClient
            cts = conn.Cts

            Log("Reconnected to named pipe.")
            Log("JSON-RPC reinitialized. Connection restored.")
            Log("=========================================")
            SetConnectionStatus(True)

        Catch ex As Exception
            Log($"Reconnect error: {ex.Message}")
            SetConnectionStatus(False)
            UI(Sub() btnConnect.Enabled = True)
            UIHelperModule.ShowWarningMessage($"Failed to reconnect. Please use the Connect button.{vbCrLf}Error: {ex.Message}",
                           "Reconnection Failed")
        End Try
    End Function

    ' ---------------------------------------------------------
    ' Execute Mandelbrot Benchmark Demo
    ' ---------------------------------------------------------
    Private Async Sub btnStartDemo_Click(sender As Object, e As EventArgs) Handles btnStartDemo.Click
        Try
            ' Validate connection
            Dim connResult = ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot execute benchmark — {connResult.ErrorMessage}")
                ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            ' Get and validate seed input
            Dim seedText = txbSeed.Text.Trim
            Dim seed = 0
            Dim seedResult = ValidateIntegerParameter(seedText, "Seed", seed)
            If Not seedResult.IsValid Then
                Log($"ERROR: {seedResult.ErrorMessage}")
                ShowErrorMessage(seedResult.ErrorMessage, "Invalid Seed")
                Return
            End If

            ' Validate seed is between 1 and 10
            If seed < 1 OrElse seed > 10 Then
                Log("ERROR: Seed must be between 1 and 10")
                ShowErrorMessage("Seed must be between 1 and 10", "Invalid Seed Range")
                Return
            End If

            Log("=========================================")
            Log($"CALLING mandelbrotbenchmark on Fortran server")
            Log($"   Seed: {seed}")
            Log("Listening for progress notifications...")

            btnStartDemo.Enabled = False
            btnExitDemo.Enabled = True
            btnPauseDemo.Enabled = True
            btnResumeDemo.Enabled = False

            ' Use InvokeAsync - this keeps StreamJsonRpc actively reading
            ' Progress notifications will be processed automatically by RpcTargetHandler    
            Dim checksum = Await RPCOperations.ExecuteRealOperationAsync(jsonRpc, "mandelbrotbenchmark", seed)

            btnExitDemo.Enabled = False
            btnStartDemo.Enabled = True
            btnPauseDemo.Enabled = False
            btnResumeDemo.Enabled = False

            Log("=========================================")
            ' Format large numbers with thousand separators and 2 decimal places
            Log($"<- CHECKSUM from Fortran server: {checksum:N2}")
            Log("=========================================")

            ' Display result in message box with formatted checksum
            ShowSuccessMessage($"Mandelbrot Benchmark Complete{vbCrLf}{vbCrLf}Seed: {seed}{vbCrLf}Checksum: {checksum:N2}", "Benchmark Result")
            Log("")

        Catch ex As RemoteInvocationException
            btnExitDemo.Enabled = False
            btnStartDemo.Enabled = True
            btnPauseDemo.Enabled = False
            btnResumeDemo.Enabled = False
            Log("=========================================")
            Log($"SERVER ERROR: {ex.Message}")
            If ex.ErrorCode <> 0 Then Log($"   Error code: {ex.ErrorCode}")
            If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
            Log("=========================================")
            ShowErrorMessage($"Server error: {ex.Message}", "Server Error")
        Catch ex As ConnectionLostException
            btnExitDemo.Enabled = False
            btnStartDemo.Enabled = True
            btnPauseDemo.Enabled = False
            btnResumeDemo.Enabled = False
            Log("=========================================")
            Log($"CONNECTION LOST: {ex.Message}")
            Log("=========================================")
            ShowErrorMessage("Connection to server lost.", "Connection Error")
            DisconnectPipe()
        Catch ex As TimeoutException
            btnExitDemo.Enabled = False
            btnStartDemo.Enabled = True
            btnPauseDemo.Enabled = False
            btnResumeDemo.Enabled = False
            Log($"Request timed out after {RequestTimeoutMs \ 1000} seconds")
            ShowWarningMessage($"Request timed out after {RequestTimeoutMs \ 1000} seconds.", "Timeout")
        Catch ex As Exception
            btnExitDemo.Enabled = False
            btnStartDemo.Enabled = True
            btnPauseDemo.Enabled = False
            btnResumeDemo.Enabled = False
            Log("=========================================")
            Log($"ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType.Name}")
            If ex.InnerException IsNot Nothing Then Log($"   Inner exception: {ex.InnerException.Message}")
            Log("=========================================")
            ShowErrorMessage($"Error: {ex.Message}", "Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Exit Demo Button - Send stop notification to Fortran server
    ' ---------------------------------------------------------
    Private Async Sub btnExitDemo_Click(sender As Object, e As EventArgs) Handles btnExitDemo.Click
        Try
            btnExitDemo.Enabled = False
            Log("=========================================")
            Log("-> Sending stop notification to Fortran server")
            Await RPCOperations.SendNotificationAsync(jsonRpc, "stop")
            Log("   Stop notification sent")
            Log("=========================================")
        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR sending stop notification: {ex.Message}")
            Log($"   Exception type: {ex.GetType.Name}")
            Log("=========================================")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Pause Demo Button - Send pause notification to Fortran server
    ' ---------------------------------------------------------
    Private Async Sub btnPauseDemo_Click(sender As Object, e As EventArgs) Handles btnPauseDemo.Click
        Try
            btnStartDemo.Enabled = False
            btnExitDemo.Enabled = False
            btnPauseDemo.Enabled = False
            btnResumeDemo.Enabled = True
            Log("=========================================")
            Log("-> Sending pause notification to Fortran server")
            Await RPCOperations.SendNotificationAsync(jsonRpc, "pause")
            Log("   Pause notification sent")
            Log("=========================================")
        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR sending pause notification: {ex.Message}")
            Log($"   Exception type: {ex.GetType.Name}")
            Log("=========================================")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Resume Demo Button - Send resume notification to Fortran server
    ' ---------------------------------------------------------
    Private Async Sub btnResumeDemo_Click(sender As Object, e As EventArgs) Handles btnResumeDemo.Click
        Try
            btnStartDemo.Enabled = False
            btnExitDemo.Enabled = True
            btnPauseDemo.Enabled = True
            btnResumeDemo.Enabled = False
            Log("=========================================")
            Log("-> Sending resume notification to Fortran server")
            Await RPCOperations.SendNotificationAsync(jsonRpc, "resume")
            Log("   Resume notification sent")
            Log("=========================================")
        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR sending resume notification: {ex.Message}")
            Log($"   Exception type: {ex.GetType.Name}")
            Log("=========================================")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Disconnect Button - Manually Disconnect from Server
    ' ---------------------------------------------------------
    Private Sub btnDisconnect_Click(sender As Object, e As EventArgs) Handles btnDisconnect.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log(connResult.ErrorMessage)
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Log("=========================================")
            Log("User requested manual disconnect")

            ' Send disconnect notification to server (optional - only if server supports it)
            ' Comment this out if your Fortran server doesn't have a disconnect method
            ' Try
            '     If jsonRpc IsNot Nothing Then
            '         jsonRpc.NotifyAsync(AppConstants.MethodDisconnect).Wait(2000)
            '         Log("Disconnect notification sent to server")
            '     End If
            ' Catch ex As Exception
            '     Log($"Could not send disconnect notification: {ex.Message}")
            ' End Try

            ' Perform cleanup
            DisconnectPipe()

            Log("Manual disconnect complete")
            Log("=========================================")

        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR during disconnect: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Error during disconnect:{vbCrLf}{vbCrLf}{ex.Message}", "Disconnect Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Close Button - Shutdown Client and Server
    ' ---------------------------------------------------------
    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Log("Close button clicked - initiating shutdown...")

        ' The FormClosing event will handle cleanup
        Me.Close()
    End Sub

    ' ---------------------------------------------------------
    ' Test -32700 Parse Error (Malformed JSON)
    ' ---------------------------------------------------------
    Private Async Sub btnTest32700_Click(sender As Object, e As EventArgs) Handles btnTest32700.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot test — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            ' Warn user that this test will disconnect
            Dim result = MessageBox.Show(
                "NOTE: This test will cause the server to disconnect." & vbCrLf & vbCrLf &
                "This is by design and in compliance with the JSON-RPC 2.0 standard." & vbCrLf & vbCrLf &
                "The client will automatically attempt to reconnect after the test." & vbCrLf & vbCrLf &
                "Do you want to proceed?",
                "Disconnect Warning - Test -32700",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning)

            If result <> DialogResult.OK Then
                Log("Test -32700 cancelled by user")
                Return
            End If

            Log("=========================================")
            Log("TEST: JSON-RPC Error -32700 (Parse Error)")
            Log("   Sending malformed JSON to trigger parse error...")

            Await ErrorTestingModule.TestParseErrorAsync(client, jsonRpc, AddressOf Log, AddressOf ReconnectToServer)

        Catch ex As Exception
            Log("=========================================")
            Log($"TEST ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Test error:{vbCrLf}{vbCrLf}{ex.Message}", "Test Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Test -32600 Invalid Request
    ' ---------------------------------------------------------
    Private Async Sub btnTest32600_Click(sender As Object, e As EventArgs) Handles btnTest32600.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot test — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            ' Warn user that this test will disconnect
            Dim result = MessageBox.Show(
                "NOTE: This test will cause the server to disconnect." & vbCrLf & vbCrLf &
                "This is by design and in compliance with the JSON-RPC 2.0 standard." & vbCrLf & vbCrLf &
                "The client will automatically attempt to reconnect after the test." & vbCrLf & vbCrLf &
                "Do you want to proceed?",
                "Disconnect Warning - Test -32600",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning)

            If result <> DialogResult.OK Then
                Log("Test -32600 cancelled by user")
                Return
            End If

            Log("=========================================")
            Log("TEST: JSON-RPC Error -32600 (Invalid Request)")
            Log("   Sending invalid JSON-RPC structure...")

            Await ErrorTestingModule.TestInvalidRequestAsync(client, jsonRpc, AddressOf Log, AddressOf ReconnectToServer)

        Catch ex As Exception
            Log("=========================================")
            Log($"TEST ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Test error:{vbCrLf}{vbCrLf}{ex.Message}", "Test Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Test Batch Request
    ' ---------------------------------------------------------
    Private Async Sub btnTestBatchRequest_Click(sender As Object, e As EventArgs) Handles btnTestBatchRequest.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot execute batch request — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Log("=========================================")
            Log("TEST: Batch Request")
            Log("   Sending multiple concurrent requests to server...")
            Log("   - subtractint(100, 25)")
            Log("   - multiplyint(6, 7)")
            Log("   - addint(100, 456)")
            Log("   - Notification: 'Batch test notification'")

            Try
                Dim batchResult = Await ExecuteBatchRequestAsync(jsonRpc, "Batch test notification")

                Log("=========================================")
                Log("BATCH REQUEST RESULTS:")
                If batchResult.SubtractIntError Is Nothing Then
                    Log($"   subtractint(100, 25) = {batchResult.SubtractIntResult}  (expected 75)")
                Else
                    Log($"   subtractint ERROR: {batchResult.SubtractIntError}")
                End If
                If batchResult.MultiplyIntError Is Nothing Then
                    Log($"   multiplyint(6, 7)   = {batchResult.MultiplyIntResult}  (expected 42)")
                Else
                    Log($"   multiplyint ERROR: {batchResult.MultiplyIntError}")
                End If
                If batchResult.AddIntError Is Nothing Then
                    Log($"   addint(100, 456)    = {batchResult.AddIntResult}  (expected 556)")
                    Log($"   {batchResult.AddIntVerification.Message}")
                Else
                    Log($"   addint ERROR: {batchResult.AddIntError}")
                End If
                Log($"   Notification sent: {batchResult.NotificationSent}")
                Log("=========================================")

                Dim allPassed = batchResult.SubtractIntResult = 75 AndAlso
                                batchResult.MultiplyIntResult = 42 AndAlso
                                batchResult.AddIntVerification.IsValid

                If allPassed Then
                    Log("✓ All batch results verified successfully!")
                    UIHelperModule.ShowSuccessMessage(
                        "Batch Request Successful!" & vbCrLf & vbCrLf &
                        $"subtractint(100,25) = {batchResult.SubtractIntResult}" & vbCrLf &
                        $"multiplyint(6,7)    = {batchResult.MultiplyIntResult}" & vbCrLf &
                        $"addint(100,456)     = {batchResult.AddIntResult}",
                        "Batch Request Success")
                Else
                    Log("⚠ One or more batch results were unexpected.")
                    UIHelperModule.ShowWarningMessage(
                        "Batch request completed but one or more results were unexpected. Check the log.",
                        "Batch Request Warning")
                End If

            Catch ex As TaskCanceledException
                Log($"Batch request timed out after {AppConstants.RequestTimeoutMs \ 1000} seconds")
                UIHelperModule.ShowWarningMessage($"Batch request timed out after {AppConstants.RequestTimeoutMs \ 1000} seconds.", "Timeout")
            Catch ex As AggregateException
                Log("=========================================")
                Log("BATCH REQUEST ERRORS:")
                For Each innerEx In ex.InnerExceptions
                    If TypeOf innerEx Is RemoteInvocationException Then
                        Dim rpcEx = DirectCast(innerEx, RemoteInvocationException)
                        Log($"   Error code: {rpcEx.ErrorCode} - {rpcEx.Message}")
                        If rpcEx.ErrorData IsNot Nothing Then Log($"   Error data: {rpcEx.ErrorData}")
                    Else
                        Log($"   Error: {innerEx.Message}")
                    End If
                Next
                Log("=========================================")
                UIHelperModule.ShowWarningMessage("Batch request completed with errors. Check the log.", "Batch Request Errors")
            End Try

            Log("")

        Catch ex As RemoteInvocationException
            Log("=========================================")
            Log($"SERVER ERROR: {ex.Message}")
            If ex.ErrorCode <> 0 Then Log($"   Error code: {ex.ErrorCode}")
            If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Server error: {ex.Message}", "Server Error")
        Catch ex As ConnectionLostException
            Log("=========================================")
            Log($"CONNECTION LOST: {ex.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage("Connection to server lost.", "Connection Error")
            DisconnectPipe()
        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            If ex.InnerException IsNot Nothing Then Log($"   Inner exception: {ex.InnerException.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Error: {ex.Message}", "Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Test -32601 Method Not Found
    ' ---------------------------------------------------------
    Private Async Sub btnTest32601_Click(sender As Object, e As EventArgs) Handles btnTest32601.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot test — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Log("=========================================")
            Log("TEST: JSON-RPC Error -32601 (Method Not Found)")
            Log("   Calling non-existent method 'nonexistentmethod'...")

            Try
                Dim result = Await jsonRpc.InvokeAsync(Of String)("nonexistentmethod", "test")
                Log($"Unexpected success: {result}")
            Catch ex As RemoteInvocationException
                Log("=========================================")
                Log($"✓ Received expected error from server:")
                Log($"   Error code: {ex.ErrorCode} (expected -32601)")
                Log($"   Message: {ex.Message}")
                If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
                Log("=========================================")

                If ex.ErrorCode = -32601 Then
                    UIHelperModule.ShowSuccessMessage("Test passed!" & vbCrLf & vbCrLf &
                        $"Server correctly returned error -32601 (Method Not Found)", "Test -32601 Success")
                Else
                    UIHelperModule.ShowWarningMessage($"Server returned error code {ex.ErrorCode} instead of -32601", "Unexpected Error Code")
                End If
            End Try

        Catch ex As Exception
            Log("=========================================")
            Log($"TEST ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Test error:{vbCrLf}{vbCrLf}{ex.Message}", "Test Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Test -32602 Invalid Params
    ' ---------------------------------------------------------
    Private Async Sub btnTest32602_Click(sender As Object, e As EventArgs) Handles btnTest32602.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot test — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Log("=========================================")
            Log("TEST: JSON-RPC Error -32602 (Invalid Params)")
            Log("   Calling 'addint' with wrong number of parameters...")

            Try
                ' Send only 1 parameter when server expects 2
                Dim result = Await jsonRpc.InvokeAsync(Of Integer)("addint", 42)
                Log($"Unexpected success: {result}")
            Catch ex As RemoteInvocationException
                Log("=========================================")
                Log($"✓ Received expected error from server:")
                Log($"   Error code: {ex.ErrorCode} (expected -32602)")
                Log($"   Message: {ex.Message}")
                If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
                Log("=========================================")

                If ex.ErrorCode = -32602 Then
                    UIHelperModule.ShowSuccessMessage("Test passed!" & vbCrLf & vbCrLf &
                        $"Server correctly returned error -32602 (Invalid Params)", "Test -32602 Success")
                Else
                    UIHelperModule.ShowWarningMessage($"Server returned error code {ex.ErrorCode} instead of -32602", "Unexpected Error Code")
                End If
            End Try

        Catch ex As Exception
            Log("=========================================")
            Log($"TEST ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Test error:{vbCrLf}{vbCrLf}{ex.Message}", "Test Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Test -32603 Internal Error
    ' ---------------------------------------------------------
    Private Async Sub btnTest32603_Click(sender As Object, e As EventArgs) Handles btnTest32603.Click
        Try
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot test — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If

            Log("=========================================")
            Log("TEST: JSON-RPC Error -32603 (Internal Error)")
            Log("   Attempting to trigger server internal error...")
            Log("   (This test depends on server implementation)")

            Try
                ' This depends on what causes internal errors in your Fortran server
                ' Common triggers: division by zero, overflow, etc.
                Dim result = Await jsonRpc.InvokeAsync(Of Integer)("divideint", 10, 0)
                Log($"Result: {result}")
                UIHelperModule.ShowWarningMessage("Server did not return an error for division by zero." & vbCrLf &
                    "The server may handle this differently than expected.", "No Error Returned")
            Catch ex As RemoteInvocationException
                Log("=========================================")
                Log($"✓ Received error from server:")
                Log($"   Error code: {ex.ErrorCode}")
                Log($"   Message: {ex.Message}")
                If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
                Log("=========================================")

                If ex.ErrorCode = -32603 Then
                    UIHelperModule.ShowSuccessMessage("Test passed!" & vbCrLf & vbCrLf &
                        $"Server correctly returned error -32603 (Internal Error)", "Test -32603 Success")
                Else
                    UIHelperModule.ShowWarningMessage($"Server returned error code {ex.ErrorCode} instead of -32603" & vbCrLf & vbCrLf &
                        "The server may use a different error code for this condition.", "Different Error Code")
                End If
            End Try

        Catch ex As Exception
            Log("=========================================")
            Log($"TEST ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Test error:{vbCrLf}{vbCrLf}{ex.Message}", "Test Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Helper: Get Server Method Name
    ' ---------------------------------------------------------
    Private Function GetServerMethodName(displayName As String) As String
        Return CalculationModule.GetServerMethodName(displayName)
    End Function

    ' ---------------------------------------------------------
    ' Execute Real Number Calculation
    ' ---------------------------------------------------------
    Private Async Function ExecuteRealCalculation(selectedMethod As String, displayName As String,
                                                   param1Text As String, param2Text As String) As Task
        Dim real1, real2 As Double
        Dim paramResult = ValidationModule.ValidateTwoRealParameters(param1Text, param2Text, real1, real2)
        If Not paramResult.IsValid Then
            Log($"ERROR: {paramResult.ErrorMessage}")
            ShowErrorMessage(paramResult.ErrorMessage, "Invalid Input")
            Return
        End If

        Dim opInfo = GetOperationInfo(selectedMethod)
        Dim r1Display = CalculationModule.FormatRealResult(real1)
        Dim r2Display = CalculationModule.FormatRealResult(real2)

        Log("=========================================")
        Log($"CALLING {selectedMethod} on Fortran server")
        Log($"   Operation: {opInfo.Name}")
        Log($"   Parameter 1: {r1Display}")
        Log($"   Parameter 2: {r2Display}")
        Log($"   Expression: {r1Display} {opInfo.Symbol} {r2Display}")

        Dim result As Double = Await RPCOperations.ExecuteRealOperationAsync(jsonRpc, selectedMethod, real1, real2)

        Dim resultDisplay = CalculationModule.FormatRealResult(result)
        Log("=========================================")
        Log($"<- RESULT from Fortran server: {resultDisplay}")
        Log("=========================================")

        Dim expected As Double = CalculationModule.CalculateExpectedRealResult(selectedMethod, real1, real2)
        Dim verification = CalculationModule.VerifyRealCalculationResult(expected, result, selectedMethod, real1, real2)
        If verification.IsMatch Then
            Log(verification.SuccessMessage)
            ShowSuccessMessage($"Result: {r1Display} {opInfo.Symbol} {r2Display} = {resultDisplay}", $"{opInfo.Name} Successful")
        Else
            Log(verification.ErrorMessage)
            Log($"    Expected: {CalculationModule.FormatRealResult(expected)}")
            Log($"    Received: {resultDisplay}")
            ShowWarningMessage($"Warning: Expected {CalculationModule.FormatRealResult(expected)} but received {resultDisplay}", "Calculation Mismatch")
        End If
        Log("=========================================")
        Log("")
    End Function

    ' ---------------------------------------------------------
    ' Execute Complex Number Calculation
    ' ---------------------------------------------------------
    Private Async Function ExecuteComplexCalculation(selectedMethod As String, displayName As String,
                                                      param1Text As String, param2Text As String) As Task
        Dim c1Real, c1Imag, c2Real, c2Imag As Double
        Dim c1Canonical As String
        Dim c2Canonical As String = ""
        Dim realFormat As String
        Dim imagFormat As String = ""

        Dim paramResult = ValidationModule.ValidateTwoComplexParameters(
            param1Text, param2Text,
            c1Real, c1Imag, c2Real, c2Imag,
            c1Canonical, c2Canonical,
            realFormat, imagFormat)

        If Not paramResult.IsValid Then
            Log($"ERROR: {paramResult.ErrorMessage}")
            ShowErrorMessage(paramResult.ErrorMessage, "Invalid Input")
            Return
        End If

        ' Division-by-zero guard: reject (0 + 0i) as Parameter 2 for dividecomplex only.
        ' All other methods accept (0 + 0i) in either position.
        If selectedMethod = "dividecomplex" AndAlso c2Real = 0.0 AndAlso c2Imag = 0.0 Then
            Log("ERROR: Division by zero — Parameter 2 is (0 + 0i).")
            ShowErrorMessage(
                "Parameter 2 is (0 + 0i). Division by zero is not allowed." & vbCrLf &
                "Please enter a non-zero complex number for the divisor.",
                "Invalid Parameter")
            Return
        End If

        Dim opInfo = GetOperationInfo(selectedMethod)
        Log("=========================================")
        Log($"CALLING {selectedMethod} on Fortran server")
        Log($"   Operation : {opInfo.Name}")
        Log($"   Parameter 1: {c1Canonical}")
        Log($"   Parameter 2: {c2Canonical}")

        ' Send as strings per JSON-RPC 2.0 (no native complex type)
        Dim serverResultStr As String = Await RPCOperations.ExecuteStringOperationAsync(
            jsonRpc, selectedMethod, c1Canonical, c2Canonical)

        Log("=========================================")
        Log($"<- RAW RESULT from Fortran server: {serverResultStr}")

        ' Parse the server's returned string to extract real and imaginary doubles
        Dim parsed = CalculationModule.ParseServerComplexResult(serverResultStr)
        If Not parsed.Success Then
            Log("ERROR: Could not parse server response as a complex number.")
            ShowWarningMessage(
                $"Server returned a result that could not be parsed:{vbCrLf}{serverResultStr}",
                "Unexpected Response")
            Return
        End If

        ' Reformat using promoted format tags derived from client input (realFormat/imagFormat).
        ' Zero-suppression and sign normalisation are applied by FormatComplexResult.
        Dim resultDisplay As String = CalculationModule.FormatComplexResult(
            parsed.RealVal, parsed.ImagVal, realFormat, imagFormat)

        Log($"<- FORMATTED RESULT: {resultDisplay}")
        Log("=========================================")

        ' Verify against local calculation
        Dim expected = CalculationModule.CalculateExpectedComplexResult(
            selectedMethod, c1Real, c1Imag, c2Real, c2Imag)

        Const Epsilon As Double = 0.0001
        Dim realMatch As Boolean = RelativeMatch(expected.Real, parsed.RealVal, Epsilon)
        Dim imagMatch As Boolean = RelativeMatch(expected.Imag, parsed.ImagVal, Epsilon)

        If realMatch AndAlso imagMatch Then
            Log($"Verification OK: ({c1Canonical}) {opInfo.Symbol} ({c2Canonical}) = {resultDisplay}")
            ShowSuccessMessage(
                $"Result: ({c1Canonical}) {opInfo.Symbol} ({c2Canonical}){vbCrLf}= {resultDisplay}",
                $"{opInfo.Name} Successful")
        Else
            Dim expectedDisplay As String = CalculationModule.FormatComplexResult(
                expected.Real, expected.Imag, realFormat, imagFormat)
            Log($"WARNING: Expected {expectedDisplay}, received {resultDisplay}")
            Log($"    Expected real: {expected.Real},  received: {parsed.RealVal}")
            Log($"    Expected imag: {expected.Imag},  received: {parsed.ImagVal}")
            ShowWarningMessage(
                $"Warning: Expected {expectedDisplay} but received {resultDisplay}",
                "Calculation Mismatch")
        End If
        Log("=========================================")
        Log("")
    End Function

    ' Relative epsilon for values > 1.0 so tolerance scales with magnitude.
    ' Absolute epsilon for small values where division by near-zero gives false failures.
    Private Function RelativeMatch(expected As Double, received As Double, epsilon As Double) As Boolean
        Dim diff As Double = Math.Abs(expected - received)
        If Math.Abs(expected) > 1.0 Then
            Return diff / Math.Abs(expected) <= epsilon
        Else
            Return diff <= epsilon
        End If
    End Function

    ' ---------------------------------------------------------
    ' Matrix Data Type Selection Handler
    ' ---------------------------------------------------------
    Private Sub cbxMatrixDataType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbxMatrixDataType.SelectedIndexChanged
        Try
            Dim selectedType As String = cbxMatrixDataType.SelectedItem?.ToString()

            If String.IsNullOrEmpty(selectedType) Then
                Return
            End If

            ' Use MatrixModule to generate matrix data
            MatrixModule.GenerateMatrixData(selectedType, lblOM, rng, AddressOf Log)

            ' Update available operations based on data type
            UpdateAvailableOperations(selectedType)

        Catch ex As Exception
            Log($"ERROR generating matrix data: {ex.Message}")
            UIHelperModule.ShowErrorMessage($"Error generating matrix data:{vbCrLf}{vbCrLf}{ex.Message}", "Matrix Generation Error")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Update Available Operations Based on Data Type
    ' ---------------------------------------------------------
    Private Sub UpdateAvailableOperations(dataType As String)
        Try
            ' Store the currently selected operation (if any)
            Dim currentSelection As String = cbxMatrixOperation.SelectedItem?.ToString()

            ' Determine if "square" should be available
            Dim allowSquare As Boolean = True
            Dim normalizedType As String = dataType.ToLower()

            ' Disable "square" for character/text and logical/boolean
            If normalizedType = "character/text" OrElse normalizedType = "logical/boolean" Then
                allowSquare = False
            End If

            ' Get current items
            Dim currentItems As New List(Of String)
            For Each item As Object In cbxMatrixOperation.Items
                currentItems.Add(item.ToString())
            Next

            ' Define the full set of operations
            Dim allOperations As New List(Of String) From {"transpose", "copy", "square"}
            Dim filteredOperations As New List(Of String)

            ' Build filtered list
            For Each op As String In allOperations
                If op.ToLower() = "square" AndAlso Not allowSquare Then
                    ' Skip "square" if not allowed
                    Continue For
                End If
                filteredOperations.Add(op)
            Next

            ' Check if update is needed
            Dim needsUpdate As Boolean = False
            If currentItems.Count <> filteredOperations.Count Then
                needsUpdate = True
            Else
                For i As Integer = 0 To currentItems.Count - 1
                    If Not currentItems(i).Equals(filteredOperations(i), StringComparison.OrdinalIgnoreCase) Then
                        needsUpdate = True
                        Exit For
                    End If
                Next
            End If

            ' Update ComboBox if needed
            If needsUpdate Then
                cbxMatrixOperation.Items.Clear()
                For Each op As String In filteredOperations
                    cbxMatrixOperation.Items.Add(op)
                Next

                ' Try to restore previous selection if it's still available
                If Not String.IsNullOrEmpty(currentSelection) Then
                    Dim foundIndex As Integer = -1
                    For i As Integer = 0 To cbxMatrixOperation.Items.Count - 1
                        If cbxMatrixOperation.Items(i).ToString().Equals(currentSelection, StringComparison.OrdinalIgnoreCase) Then
                            foundIndex = i
                            Exit For
                        End If
                    Next

                    If foundIndex >= 0 Then
                        cbxMatrixOperation.SelectedIndex = foundIndex
                        Log($"   Operation '{currentSelection}' remains selected")
                    Else
                        ' Previous selection not available, clear selection
                        cbxMatrixOperation.SelectedIndex = -1
                        Log($"   Operation '{currentSelection}' not available for {dataType}, selection cleared")
                    End If
                Else
                    cbxMatrixOperation.SelectedIndex = -1
                End If

                Log($"   Available operations updated for '{dataType}': {String.Join(", ", filteredOperations)}")
            End If

        Catch ex As Exception
            Log($"ERROR updating available operations: {ex.Message}")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Execute Matrix Operation
    ' ---------------------------------------------------------
    Private Async Sub btnMatrixExecute_Click(sender As Object, e As EventArgs) Handles btnMatrixExecute.Click
        Try
            ' Disable button during operation
            btnMatrixExecute.Enabled = False

            Log("=========================================")
            Log("MATRIX OPERATION REQUESTED")

            ' === STEP 1: Validate Connection ===
            Dim connResult = JSONRPCClientLibrary.ValidationModule.ValidateConnection(jsonRpc)
            If Not connResult.IsValid Then
                Log($"Cannot execute matrix operation — {connResult.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(connResult.ErrorMessage, "Not Connected")
                Return
            End If
            Log("✓ Connection validated")

            ' === STEP 2: Validate ComboBox Selections ===
            Dim comboValidation = MatrixModule.ValidateComboBoxSelections(cbxMatrixDataType, cbxMatrixOperation, cbxMatrixOrdering)
            If Not comboValidation.IsValid Then
                Log($"Selection validation failed: {comboValidation.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(comboValidation.ErrorMessage, "Selection Required")
                Return
            End If

            Dim dataType As String = cbxMatrixDataType.SelectedItem.ToString().ToLower()
            Dim operation As String = cbxMatrixOperation.SelectedItem.ToString().ToLower()
            Dim ordering As String = cbxMatrixOrdering.SelectedItem.ToString().ToLower()

            Log($"   Data Type: {dataType}")
            Log($"   Operation: {operation}")
            Log($"   Ordering: {ordering}")
            Log("✓ Selections validated")

            ' === STEP 3: Route to Appropriate Handler ===
            Select Case dataType
                Case "integer"
                    Select Case operation
                        Case "transpose"
                            Await ExecuteIntegerOperation("transpose", ordering)
                        Case "copy"
                            Await ExecuteIntegerOperation("copy", ordering)
                        Case "square"
                            Await ExecuteIntegerOperation("square", ordering)
                        Case Else
                            Log($"Operation '{operation}' not yet implemented for integer matrices")
                            UIHelperModule.ShowWarningMessage($"Operation '{operation}' is not yet implemented for integer matrices.", "Not Implemented")
                    End Select

                Case "real"
                    Select Case operation
                        Case "copy"
                            Await ExecuteRealOperation("copy", ordering)
                        Case "transpose"
                            Await ExecuteRealOperation("transpose", ordering)
                        Case "square"
                            Await ExecuteRealOperation("square", ordering)
                        Case Else
                            Log($"Operation '{operation}' not yet implemented for real matrices")
                            UIHelperModule.ShowWarningMessage($"Operation '{operation}' is not yet implemented for real matrices.", "Not Implemented")
                    End Select

                Case "character/text"
                    Select Case operation
                        Case "copy"
                            Await ExecuteTextOperation("copy", ordering)
                        Case "transpose"
                            Await ExecuteTextOperation("transpose", ordering)
                        Case Else
                            Log($"Operation '{operation}' not yet implemented for text matrices")
                            UIHelperModule.ShowWarningMessage($"Operation '{operation}' is not yet implemented for text matrices.", "Not Implemented")
                    End Select

                Case "logical/boolean"
                    Select Case operation
                        Case "copy"
                            Await ExecuteLogicalOperation("copy", ordering)
                        Case "transpose"
                            Await ExecuteLogicalOperation("transpose", ordering)
                        Case Else
                            Log($"Operation '{operation}' not yet implemented for logical matrices")
                            UIHelperModule.ShowWarningMessage($"Operation '{operation}' is not yet implemented for logical matrices.", "Not Implemented")
                    End Select

                Case "complex"
                    Select Case operation
                        Case "copy"
                            Await ExecuteComplexOperation("copy", ordering)
                        Case "transpose"
                            Await ExecuteComplexOperation("transpose", ordering)
                        Case "square"
                            Await ExecuteComplexOperation("square", ordering)
                        Case Else
                            Log($"Operation '{operation}' not yet implemented for complex matrices")
                            UIHelperModule.ShowWarningMessage($"Operation '{operation}' is not yet implemented for complex matrices.", "Not Implemented")
                    End Select

                Case "double"
                    Log($"Data type '{dataType}' not yet implemented")
                    UIHelperModule.ShowWarningMessage($"Data type '{dataType}' is not yet implemented.", "Not Implemented")

                Case Else
                    Log($"Unknown data type: {dataType}")
                    UIHelperModule.ShowWarningMessage($"Unknown data type: {dataType}", "Unknown Data Type")
            End Select

        Catch ex As RemoteInvocationException
            Log("=========================================")
            Log($"SERVER ERROR: {ex.Message}")
            If ex.ErrorCode <> 0 Then Log($"   Error code: {ex.ErrorCode}")
            If ex.ErrorData IsNot Nothing Then Log($"   Error data: {ex.ErrorData}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Matrix operation error:{vbCrLf}{vbCrLf}{ex.Message}", "Server Error")
        Catch ex As ConnectionLostException
            Log("=========================================")
            Log($"CONNECTION LOST: {ex.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage("Connection to server lost during matrix operation.", "Connection Error")
            DisconnectPipe()
        Catch ex As Exception
            Log("=========================================")
            Log($"ERROR: {ex.Message}")
            Log($"   Exception type: {ex.GetType().Name}")
            If ex.InnerException IsNot Nothing Then Log($"   Inner exception: {ex.InnerException.Message}")
            Log("=========================================")
            UIHelperModule.ShowErrorMessage($"Matrix operation error:{vbCrLf}{vbCrLf}{ex.Message}", "Error")

        Finally
            ' Re-enable button
            btnMatrixExecute.Enabled = True
            Log("")
        End Try
    End Sub

    ' ---------------------------------------------------------
    ' Execute Integer Matrix Operation (Generic Handler)
    ' ---------------------------------------------------------
    Private Async Function ExecuteIntegerOperation(operation As String, ordering As String) As Task
        Try
            Dim operationDisplayName As String = GetOperationDisplayName(operation)

            Log("-------------------------------------------")
            Log($"EXECUTING: Integer Matrix {operationDisplayName}")

            Dim emptyValidation = MatrixModule.ValidateMatrixNotEmpty(lblOM)
            If Not emptyValidation.IsValid Then
                Log($"Matrix validation failed: {emptyValidation.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(emptyValidation.ErrorMessage, "Empty Matrix")
                Return
            End If
            Log("✓ Matrix is not empty")

            Dim dimValidation = MatrixModule.ValidateMatrixDimensions(lblOM, 4, 4)
            If Not dimValidation.IsValid Then
                Log($"Dimension validation failed: {dimValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dimValidation.ErrorMessage, "Dimension Error")
                Return
            End If
            Log("✓ Matrix dimensions validated: 4×4")

            Dim dataValidation = MatrixModule.ValidateMatrixIntegerData(lblOM)
            If Not dataValidation.IsValid Then
                Log($"Data validation failed: {dataValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dataValidation.ErrorMessage, "Invalid Data")
                Return
            End If
            Log("✓ All cells contain valid integer data")

            Dim matrixData() As Integer = MatrixModule.ExtractIntegerMatrix(lblOM)
            Log($"✓ Matrix extracted: {matrixData.Length} values")
            Log($"   Matrix values: [{String.Join(", ", matrixData.Take(8))}...]")

            Log("   Sending to Fortran server...")
            Log($"   Method: matrixinteger{operation.ToLower()}")
            Log($"   Parameters: datatype=integer, rows=4, columns=4, operation={operation}, ordering={ordering}")

            Dim response As MatrixOperationResponse = Await MatrixModule.MatrixIntegerOperationAsync(
                jsonRpc, operation, matrixData, 4, 4, ordering, AddressOf Log)

            Log("✓ Response received from server")

            Dim responseValidation = MatrixModule.ValidateMatrixResponse(response, "integer", 4, 4)
            If Not responseValidation.IsValid Then
                Log($"Response validation failed: {responseValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(responseValidation.ErrorMessage, "Invalid Response")
                Return
            End If
            Log("✓ Response validated successfully")

            Dim resultMatrix() As Integer = DirectCast(response.matrix, Integer())
            Log($"   Result matrix: [{String.Join(", ", resultMatrix.Take(8))}...]")

            MatrixModule.DisplayIntegerMatrix(lblMM, resultMatrix, response.rows, response.columns, AddressOf Log)

            Log("✓ Result displayed in modified matrix (lblMM)")
            Log("-------------------------------------------")
            Log($"INTEGER MATRIX {operation.ToUpper()} COMPLETED SUCCESSFULLY")
            Log("=========================================")

            Dim successMessage As String = GetSuccessMessage(operation, ordering)
            MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Log($"ERROR in ExecuteIntegerOperation: {ex.Message}")
            Throw
        End Try
    End Function

    ' ---------------------------------------------------------
    ' Execute Real Matrix Operation (Generic Handler)
    ' ---------------------------------------------------------
    Private Async Function ExecuteRealOperation(operation As String, ordering As String) As Task
        Try
            Dim operationDisplayName As String = GetOperationDisplayName(operation)

            Log("-------------------------------------------")
            Log($"EXECUTING: Real Matrix {operationDisplayName}")

            Dim emptyValidation = MatrixModule.ValidateMatrixNotEmpty(lblOM)
            If Not emptyValidation.IsValid Then
                Log($"Matrix validation failed: {emptyValidation.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(emptyValidation.ErrorMessage, "Empty Matrix")
                Return
            End If
            Log("✓ Matrix is not empty")

            Dim dimValidation = MatrixModule.ValidateMatrixDimensions(lblOM, 4, 4)
            If Not dimValidation.IsValid Then
                Log($"Dimension validation failed: {dimValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dimValidation.ErrorMessage, "Dimension Error")
                Return
            End If
            Log("✓ Matrix dimensions validated: 4×4")

            Dim dataValidation = MatrixModule.ValidateMatrixRealData(lblOM)
            If Not dataValidation.IsValid Then
                Log($"Data validation failed: {dataValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dataValidation.ErrorMessage, "Invalid Data")
                Return
            End If
            Log("✓ All cells contain valid real number data")

            Dim matrixData() As Double = MatrixModule.ExtractRealMatrix(lblOM)
            Log($"✓ Matrix extracted: {matrixData.Length} values")
            Log($"   Matrix values: [{String.Join(", ", matrixData.Take(8).Select(Function(x) x.ToString("F2")))}...]")

            Log("   Sending to Fortran server...")
            Log($"   Method: matrixreal{operation.ToLower()}")
            Log($"   Parameters: datatype=real, rows=4, columns=4, operation={operation}, ordering={ordering}")

            Dim response As MatrixOperationResponse = Await MatrixModule.MatrixRealOperationAsync(
                jsonRpc, operation, matrixData, 4, 4, ordering, AddressOf Log)

            Log("✓ Response received from server")

            Dim responseValidation = MatrixModule.ValidateMatrixRealResponse(response, "real", 4, 4)
            If Not responseValidation.IsValid Then
                Log($"Response validation failed: {responseValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(responseValidation.ErrorMessage, "Invalid Response")
                Return
            End If
            Log("✓ Response validated successfully")

            Dim resultMatrix() As Double = Nothing

            ' response.matrix runtime type depends on whether Newtonsoft.Json or StreamJsonRpc
            ' deserialized the payload — JArray, Double(), or List(Of Double) are all possible.
            Try
                If TypeOf response.matrix Is JArray Then
                    resultMatrix = DirectCast(response.matrix, JArray).ToObject(Of Double())()
                ElseIf TypeOf response.matrix Is Double() Then
                    resultMatrix = DirectCast(response.matrix, Double())
                ElseIf TypeOf response.matrix Is List(Of Double) Then
                    resultMatrix = DirectCast(response.matrix, List(Of Double)).ToArray()
                Else
                    Dim tempList As New List(Of Double)
                    For Each item In DirectCast(response.matrix, System.Collections.IEnumerable)
                        tempList.Add(Convert.ToDouble(item))
                    Next
                    resultMatrix = tempList.ToArray()
                End If
            Catch convEx As Exception
                Log($"ERROR converting response matrix: {convEx.Message}")
                UIHelperModule.ShowErrorMessage($"Failed to parse response matrix: {convEx.Message}", "Conversion Error")
                Return
            End Try

            If resultMatrix Is Nothing Then
                Log("ERROR: Could not convert response matrix to Double array")
                UIHelperModule.ShowErrorMessage("Failed to parse response matrix data", "Conversion Error")
                Return
            End If

            Log($"   Result matrix: [{String.Join(", ", resultMatrix.Take(8).Select(Function(x) x.ToString("F2")))}...]")

            MatrixModule.DisplayRealMatrix(lblMM, resultMatrix, response.rows, response.columns, AddressOf Log)

            Log("✓ Result displayed in modified matrix (lblMM)")
            Log("-------------------------------------------")
            Log($"REAL MATRIX {operation.ToUpper()} COMPLETED SUCCESSFULLY")
            Log("=========================================")

            Dim successMessage As String = GetSuccessMessageForReal(operation, ordering)
            MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Log($"ERROR in ExecuteRealOperation: {ex.Message}")
            Throw
        End Try
    End Function

    ' ---------------------------------------------------------
    ' Execute Text/Character Matrix Operation (Generic Handler)
    ' ---------------------------------------------------------
    Private Async Function ExecuteTextOperation(operation As String, ordering As String) As Task
        Try
            Dim operationDisplayName As String = GetOperationDisplayName(operation)

            Log("-------------------------------------------")
            Log($"EXECUTING: Text Matrix {operationDisplayName}")

            Dim emptyValidation = MatrixModule.ValidateMatrixNotEmpty(lblOM)
            If Not emptyValidation.IsValid Then
                Log($"Matrix validation failed: {emptyValidation.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(emptyValidation.ErrorMessage, "Empty Matrix")
                Return
            End If
            Log("✓ Matrix is not empty")

            Dim dimValidation = MatrixModule.ValidateMatrixDimensions(lblOM, 4, 4)
            If Not dimValidation.IsValid Then
                Log($"Dimension validation failed: {dimValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dimValidation.ErrorMessage, "Dimension Error")
                Return
            End If
            Log("✓ Matrix dimensions validated: 4×4")

            Dim dataValidation = MatrixModule.ValidateMatrixTextData(lblOM)
            If Not dataValidation.IsValid Then
                Log($"Data validation failed: {dataValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dataValidation.ErrorMessage, "Invalid Data")
                Return
            End If
            Log("✓ All cells contain valid text/character data")

            Dim matrixData() As String = MatrixModule.ExtractTextMatrix(lblOM)
            Log($"✓ Matrix extracted: {matrixData.Length} strings")
            Log($"   First 4 strings: [{String.Join(", ", matrixData.Take(4).Select(Function(s) $"""{s}"""))}...]")

            Log("   Sending to Fortran server...")
            Log($"   Method: matrixtext{operation.ToLower()}")
            Log($"   Parameters: datatype=string, rows=4, columns=4, operation={operation}, ordering={ordering}")

            Dim response As MatrixOperationResponse = Await MatrixModule.MatrixTextOperationAsync(
                jsonRpc, operation, matrixData, 4, 4, ordering, AddressOf Log)

            Log("✓ Response received from server")

            Dim responseValidation = MatrixModule.ValidateMatrixTextResponse(response, "string", 4, 4)
            If Not responseValidation.IsValid Then
                Log($"Response validation failed: {responseValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(responseValidation.ErrorMessage, "Invalid Response")
                Return
            End If
            Log("✓ Response validated successfully")

            Dim resultMatrix() As String = Nothing

            Try
                If TypeOf response.matrix Is JArray Then
                    resultMatrix = DirectCast(response.matrix, JArray).ToObject(Of String())()
                ElseIf TypeOf response.matrix Is String() Then
                    resultMatrix = DirectCast(response.matrix, String())
                ElseIf TypeOf response.matrix Is List(Of String) Then
                    resultMatrix = DirectCast(response.matrix, List(Of String)).ToArray()
                Else
                    Dim tempList As New List(Of String)
                    For Each item In DirectCast(response.matrix, System.Collections.IEnumerable)
                        tempList.Add(item.ToString())
                    Next
                    resultMatrix = tempList.ToArray()
                End If
            Catch convEx As Exception
                Log($"ERROR converting response matrix: {convEx.Message}")
                UIHelperModule.ShowErrorMessage($"Failed to parse response matrix: {convEx.Message}", "Conversion Error")
                Return
            End Try

            If resultMatrix Is Nothing Then
                Log("ERROR: Could not convert response matrix to String array")
                UIHelperModule.ShowErrorMessage("Failed to parse response matrix data", "Conversion Error")
                Return
            End If

            Log($"   First 4 result strings: [{String.Join(", ", resultMatrix.Take(4).Select(Function(s) $"""{s}"""))}...]")

            MatrixModule.DisplayTextMatrix(lblMM, resultMatrix, response.rows, response.columns, AddressOf Log)

            Log("✓ Result displayed in modified matrix (lblMM)")
            Log("-------------------------------------------")
            Log($"TEXT MATRIX {operation.ToUpper()} COMPLETED SUCCESSFULLY")
            Log("=========================================")

            Dim successMessage As String = GetSuccessMessageForText(operation, ordering)
            MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Log($"ERROR in ExecuteTextOperation: {ex.Message}")
            Throw
        End Try
    End Function

    ' ---------------------------------------------------------
    ' Execute Logical/Boolean Matrix Operation (Generic Handler)
    ' ---------------------------------------------------------
    Private Async Function ExecuteLogicalOperation(operation As String, ordering As String) As Task
        Try
            Dim operationDisplayName As String = GetOperationDisplayName(operation)

            Log("-------------------------------------------")
            Log($"EXECUTING: Logical Matrix {operationDisplayName}")

            Dim emptyValidation = MatrixModule.ValidateMatrixNotEmpty(lblOM)
            If Not emptyValidation.IsValid Then
                Log($"Matrix validation failed: {emptyValidation.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(emptyValidation.ErrorMessage, "Empty Matrix")
                Return
            End If
            Log("✓ Matrix is not empty")

            Dim dimValidation = MatrixModule.ValidateMatrixDimensions(lblOM, 4, 4)
            If Not dimValidation.IsValid Then
                Log($"Dimension validation failed: {dimValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dimValidation.ErrorMessage, "Dimension Error")
                Return
            End If
            Log("✓ Matrix dimensions validated: 4×4")

            Dim dataValidation = MatrixModule.ValidateMatrixLogicalData(lblOM)
            If Not dataValidation.IsValid Then
                Log($"Data validation failed: {dataValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dataValidation.ErrorMessage, "Invalid Data")
                Return
            End If
            Log("✓ All cells contain valid logical/boolean data")

            Dim matrixData() As Boolean = MatrixModule.ExtractLogicalMatrix(lblOM)
            Log($"✓ Matrix extracted: {matrixData.Length} boolean values")
            Log($"   First 4 values: [{String.Join(", ", matrixData.Take(4))}...]")

            Log("   Sending to Fortran server...")
            Log($"   Method: matrixlogical{operation.ToLower()}")
            Log($"   Parameters: datatype=logical, rows=4, columns=4, operation={operation}, ordering={ordering}")

            Dim response As MatrixOperationResponse = Await MatrixModule.MatrixLogicalOperationAsync(
                jsonRpc, operation, matrixData, 4, 4, ordering, AddressOf Log)

            Log("✓ Response received from server")

            Dim responseValidation = MatrixModule.ValidateMatrixLogicalResponse(response, "logical", 4, 4)
            If Not responseValidation.IsValid Then
                Log($"Response validation failed: {responseValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(responseValidation.ErrorMessage, "Invalid Response")
                Return
            End If
            Log("✓ Response validated successfully")

            If TypeOf response.matrix Is String() Then
                Dim resultMatrix() As String = DirectCast(response.matrix, String())
                Log($"   First 4 result values: [{String.Join(", ", resultMatrix.Take(4))}]")
                MatrixModule.DisplayLogicalMatrix(lblMM, resultMatrix, response.rows, response.columns, AddressOf Log)

            ElseIf TypeOf response.matrix Is Boolean() Then
                Dim resultMatrix() As Boolean = DirectCast(response.matrix, Boolean())
                Log($"   First 4 result values: [{String.Join(", ", resultMatrix.Take(4))}]")
                MatrixModule.DisplayLogicalMatrix(lblMM, resultMatrix, response.rows, response.columns, AddressOf Log)

            Else
                Log($"ERROR: Unexpected response matrix type: {response.matrix.GetType().Name}")
                UIHelperModule.ShowErrorMessage($"Unexpected response format: {response.matrix.GetType().Name}", "Format Error")
                Return
            End If

            Log("✓ Result displayed in modified matrix (lblMM)")
            Log("-------------------------------------------")
            Log($"LOGICAL MATRIX {operation.ToUpper()} COMPLETED SUCCESSFULLY")
            Log("=========================================")

            Dim successMessage As String = GetSuccessMessageForLogical(operation, ordering)
            MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Log($"ERROR in ExecuteLogicalOperation: {ex.Message}")
            Throw
        End Try
    End Function

    ' ---------------------------------------------------------
    ' Execute Complex Matrix Operation (Generic Handler)
    ' ---------------------------------------------------------
    Private Async Function ExecuteComplexOperation(operation As String, ordering As String) As Task
        Try
            Dim operationDisplayName As String = GetOperationDisplayName(operation)

            Log("-------------------------------------------")
            Log($"EXECUTING: Complex Matrix {operationDisplayName}")

            Dim emptyValidation = MatrixModule.ValidateMatrixNotEmpty(lblOM)
            If Not emptyValidation.IsValid Then
                Log($"Matrix validation failed: {emptyValidation.ErrorMessage}")
                UIHelperModule.ShowWarningMessage(emptyValidation.ErrorMessage, "Empty Matrix")
                Return
            End If
            Log("✓ Matrix is not empty")

            Dim dimValidation = MatrixModule.ValidateMatrixDimensions(lblOM, 4, 4)
            If Not dimValidation.IsValid Then
                Log($"Dimension validation failed: {dimValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dimValidation.ErrorMessage, "Dimension Error")
                Return
            End If
            Log("✓ Matrix dimensions validated: 4×4")

            Dim dataValidation = MatrixModule.ValidateMatrixComplexData(lblOM)
            If Not dataValidation.IsValid Then
                Log($"Data validation failed: {dataValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(dataValidation.ErrorMessage, "Invalid Data")
                Return
            End If
            Log("✓ All cells contain valid complex data")

            Dim matrixData() As String = MatrixModule.ExtractComplexMatrix(lblOM)
            Log($"✓ Matrix extracted: {matrixData.Length} complex values")
            Log($"   First 4 values: [{String.Join(", ", matrixData.Take(4))}...]")

            Log("   Sending to Fortran server...")
            Log($"   Method: matrixcomplex{operation.ToLower()}")
            Log($"   Parameters: datatype=complex, rows=4, columns=4, operation={operation}, ordering={ordering}")

            Dim response As MatrixOperationResponse = Await MatrixModule.MatrixComplexOperationAsync(
                jsonRpc, operation, matrixData, 4, 4, ordering, AddressOf Log)

            Log("✓ Response received from server")

            Dim responseValidation = MatrixModule.ValidateMatrixComplexResponse(response, "complex", 4, 4)
            If Not responseValidation.IsValid Then
                Log($"Response validation failed: {responseValidation.ErrorMessage}")
                UIHelperModule.ShowErrorMessage(responseValidation.ErrorMessage, "Invalid Response")
                Return
            End If
            Log("✓ Response validated successfully")

            If TypeOf response.matrix Is String() Then
                Dim resultMatrix() As String = DirectCast(response.matrix, String())
                Log($"   First 4 result values: [{String.Join(", ", resultMatrix.Take(4))}]")
                MatrixModule.DisplayComplexMatrix(lblMM, resultMatrix, response.rows, response.columns, AddressOf Log)
            Else
                Log($"ERROR: Unexpected response matrix type: {response.matrix.GetType().Name}")
                UIHelperModule.ShowErrorMessage($"Unexpected response format: {response.matrix.GetType().Name}", "Format Error")
                Return
            End If

            Log("✓ Result displayed in modified matrix (lblMM)")
            Log("-------------------------------------------")
            Log($"COMPLEX MATRIX {operation.ToUpper()} COMPLETED SUCCESSFULLY")
            Log("=========================================")

            Dim successMessage As String = GetSuccessMessageForComplex(operation, ordering)
            MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Catch ex As Exception
            Log($"ERROR in ExecuteComplexOperation: {ex.Message}")
            Throw
        End Try
    End Function

    ' ---------------------------------------------------------
    ' Get Success Message for Complex Operations
    ' ---------------------------------------------------------
    Private Function GetSuccessMessageForComplex(operation As String, ordering As String) As String
        Dim operationName As String = GetOperationDisplayName(operation)
        Dim baseMessage As String = $"Operation: Complex {operationName}{vbCrLf}" &
                                $"Dimensions: 4×4{vbCrLf}" &
                                $"Ordering: {ordering}{vbCrLf}" &
                                $"Format: (real+imagi) numbers"

        Select Case operation.ToLower()
            Case "copy"
                Return $"Complex matrix copied successfully!{vbCrLf}{vbCrLf}" &
                   $"All 16 complex values have been preserved{vbCrLf}{vbCrLf}{baseMessage}"

            Case "transpose"
                Return $"Complex matrix transposed successfully!{vbCrLf}{vbCrLf}{baseMessage}"

            Case "square"
                Return $"Complex matrix squared successfully!{vbCrLf}{vbCrLf}" &
                   $"Each cell value has been squared{vbCrLf}{vbCrLf}{baseMessage}"

            Case Else
                Return $"Complex matrix {operation} completed successfully!{vbCrLf}{vbCrLf}{baseMessage}"
        End Select
    End Function

    ' ---------------------------------------------------------
    ' Get Operation Display Name
    ' ---------------------------------------------------------
    Private Function GetOperationDisplayName(operation As String) As String
        Select Case operation.ToLower()
            Case "transpose"
                Return "Transpose"
            Case "copy"
                Return "Copy"
            Case "square"
                Return "Square"
            Case Else
                Return operation
        End Select
    End Function

    ' ---------------------------------------------------------
    ' Get Success Message for Integer Operations
    ' ---------------------------------------------------------
    Private Function GetSuccessMessage(operation As String, ordering As String) As String
        Dim operationName As String = GetOperationDisplayName(operation)
        Dim baseMessage As String = $"Operation: Integer {operationName}{vbCrLf}" &
                                $"Dimensions: 4×4{vbCrLf}" &
                                $"Ordering: {ordering}"

        Select Case operation.ToLower()
            Case "transpose"
                Return $"Matrix transposed successfully!{vbCrLf}{vbCrLf}{baseMessage}"

            Case "copy"
                Return $"Matrix copied successfully!{vbCrLf}{vbCrLf}{baseMessage}"

            Case "square"
                Return $"Matrix squared successfully!{vbCrLf}{vbCrLf}" &
                   $"Each cell value has been squared{vbCrLf}{baseMessage}"

            Case Else
                Return $"Matrix {operation} completed successfully!{vbCrLf}{vbCrLf}{baseMessage}"
        End Select
    End Function

    ' ---------------------------------------------------------
    ' Get Success Message for Real Operations
    ' ---------------------------------------------------------
    Private Function GetSuccessMessageForReal(operation As String, ordering As String) As String
        Dim operationName As String = GetOperationDisplayName(operation)
        Dim baseMessage As String = $"Operation: Real {operationName}{vbCrLf}" &
                                $"Dimensions: 4×4{vbCrLf}" &
                                $"Ordering: {ordering}{vbCrLf}" &
                                $"Precision: 2 decimal places"

        Select Case operation.ToLower()
            Case "copy"
                Return $"Real matrix copied successfully!{vbCrLf}{vbCrLf}" &
                   $"Each cell value has been preserved{vbCrLf}{baseMessage}"

            Case "transpose"
                Return $"Real matrix transposed successfully!{vbCrLf}{vbCrLf}{baseMessage}"

            Case "square"
                Return $"Real matrix squared successfully!{vbCrLf}{vbCrLf}" &
                   $"Each cell value has been squared{vbCrLf}{baseMessage}"

            Case Else
                Return $"Real matrix {operation} completed successfully!{vbCrLf}{vbCrLf}{baseMessage}"
        End Select
    End Function

    ' ---------------------------------------------------------
    ' Get Success Message for Text Operations
    ' ---------------------------------------------------------
    Private Function GetSuccessMessageForText(operation As String, ordering As String) As String
        Dim operationName As String = GetOperationDisplayName(operation)
        Dim baseMessage As String = $"Operation: Text {operationName}{vbCrLf}" &
                                $"Dimensions: 4×4{vbCrLf}" &
                                $"Ordering: {ordering}{vbCrLf}" &
                                $"Format: Alphanumeric strings (1-6 chars)"

        Select Case operation.ToLower()
            Case "copy"
                Return $"Text matrix copied successfully!{vbCrLf}{vbCrLf}" &
                   $"All 16 text strings have been preserved{vbCrLf}{vbCrLf}{baseMessage}"

            Case "transpose"
                Return $"Text matrix transposed successfully!{vbCrLf}{vbCrLf}{baseMessage}"

            Case "square"
                Return $"Text matrix squared successfully!{vbCrLf}{vbCrLf}{baseMessage}"

            Case Else
                Return $"Text matrix {operation} completed successfully!{vbCrLf}{vbCrLf}{baseMessage}"
        End Select
    End Function

    ' ---------------------------------------------------------
    ' Get Success Message for Logical Operations
    ' ---------------------------------------------------------
    Private Function GetSuccessMessageForLogical(operation As String, ordering As String) As String
        Dim operationName As String = GetOperationDisplayName(operation)
        Dim baseMessage As String = $"Operation: Logical {operationName}{vbCrLf}" &
                                $"Dimensions: 4×4{vbCrLf}" &
                                $"Ordering: {ordering}{vbCrLf}" &
                                $"Format: TRUE/FALSE values"

        Select Case operation.ToLower()
            Case "copy"
                Return $"Logical matrix copied successfully!{vbCrLf}{vbCrLf}" &
                   $"All 16 boolean values have been preserved{vbCrLf}{vbCrLf}{baseMessage}"

            Case "transpose"
                Return $"Logical matrix transposed successfully!{vbCrLf}{vbCrLf}{baseMessage}"

            Case Else
                Return $"Logical matrix {operation} completed successfully!{vbCrLf}{vbCrLf}{baseMessage}"
        End Select
    End Function

    ' ---------------------------------------------------------
    ' Method Selection Changed - Show Usage Instructions
    ' ---------------------------------------------------------
    Private Sub cbMethods_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cbMethods.SelectedIndexChanged
        Try
            If cbMethods.SelectedItem Is Nothing Then
                Return
            End If

            Dim selectedMethod As String = cbMethods.SelectedItem.ToString()
            Dim message As String = ""

            Select Case selectedMethod
                Case "Add real", "Subtract real", "Multiply real", "Divide real"
                    message = "Enter a real number, with 0 to 2 decimal places (no scientific notation or complex numbers) " &
                              "in Param 1 and Param 2 textboxes, then click the Execute button."

                Case "Add complex", "Subtract complex", "Multiply complex", "Divide complex"
                    message = "Enter a complex number enclosed in parentheses in the Param 1 and Param 2 text boxes." & vbCrLf & vbCrLf &
                              "Format:  (X + Yi)  or  (X - Yi)" & vbCrLf &
                              "where X is the real part and Y is the imaginary part." & vbCrLf & vbCrLf &
                              "Examples:" & vbCrLf &
                              "  Integer      : (43 + 16i),  (-4 - 23i)" & vbCrLf &
                              "  Real         : (12.34 + 5.67i),  (-99.5 - 0.25i)" & vbCrLf &
                              "  Scientific   : (1.5E+03 - 2.0E-01i),  (12.34E+10 + 44.04E+04i)" & vbCrLf &
                              "  Mixed        : (14 + 5.6i),  (43.21 + 16.02E+13i)" & vbCrLf & vbCrLf &
                              "Spaces around + or - are optional and will be normalized automatically." & vbCrLf &
                              "Then click the Execute button."

                Case Else
                    message = "Enter an integer (no decimals, no scientific notation, no complex numbers) " &
                              "in the Param 1 and Param 2 text boxes, then click the Execute button."
            End Select

            MessageBox.Show(message, "Parameter Input Instructions", MessageBoxButtons.OK, MessageBoxIcon.Information)

            txbParameter1.Focus()

        Catch ex As Exception
            Log($"ERROR in cbMethods_SelectedIndexChanged: {ex.Message}")
        End Try
    End Sub

    ' =========================================================================
    ' BATCH RESULT TYPE
    ' Holds per-operation results from ExecuteBatchRequestAsync. Defined here
    ' because the batch parameters and method set are specific to this client.
    ' =========================================================================

    Private Class BatchResult
        Public Property SubtractIntResult As Integer
        Public Property SubtractIntError As String
        Public Property MultiplyIntResult As Integer
        Public Property MultiplyIntError As String
        Public Property AddIntResult As Integer
        Public Property AddIntError As String
        Public Property AddIntVerification As (IsValid As Boolean, Message As String)
        Public Property NotificationSent As Boolean
    End Class

    ' =========================================================================
    ' EXECUTE BATCH REQUEST
    ' Sends subtractint(100,25), multiplyint(6,7), addint(100,456), and a
    ' notification sequentially. Requests are sent one at a time to ensure
    ' reliable delivery — the Fortran server's named-pipe read loop processes
    ' one framed message per cycle and cannot handle concurrent sends that
    ' arrive bundled together in a single pipe buffer.
    ' Application-specific to this client/server combination.
    ' =========================================================================

    ''' <summary>
    ''' Executes three sequential integer operations and one notification as a batch.
    ''' Per-operation errors are captured in individual error fields rather than thrown.
    ''' </summary>
    Private Async Function ExecuteBatchRequestAsync(
        rpc As JsonRpc,
        notif As String) As Task(Of BatchResult)

        If rpc Is Nothing OrElse rpc.IsDisposed Then
            Throw New InvalidOperationException("Not connected to server")
        End If

        Dim result = New BatchResult()

        Try
            result.SubtractIntResult = Await RPCOperations.ExecuteOperationAsync(rpc, AppConstants.MethodSubtractInt, 100, 25)
        Catch ex As Exception
            result.SubtractIntError = ex.Message
        End Try

        Try
            result.MultiplyIntResult = Await RPCOperations.ExecuteOperationAsync(rpc, AppConstants.MethodMultiplyInt, 6, 7)
        Catch ex As Exception
            result.MultiplyIntError = ex.Message
        End Try

        Try
            result.AddIntResult = Await RPCOperations.ExecuteOperationAsync(rpc, AppConstants.MethodAddInt, 100, 456)
            result.AddIntVerification = ValidateBatchAddResult(100, 456, result.AddIntResult)
        Catch ex As Exception
            result.AddIntError = ex.Message
            result.AddIntVerification = (False, $"AddInt failed: {ex.Message}")
        End Try

        Try
            Await RPCOperations.SendNotificationAsync(rpc, AppConstants.MethodClientNotify, notif)
            result.NotificationSent = True
        Catch ex As Exception
            result.NotificationSent = False
        End Try

        Return result
    End Function

End Class
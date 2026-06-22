' =========================================================================
' UIHelperModule.vb
' Centralized UI helper utilities for Fortran JSON-RPC Client
' =========================================================================
Imports System.Windows.Forms

Public Module UIHelperModule

    ' =========================================================================
    ' UI THREAD MARSHALING
    ' =========================================================================

    ''' <summary>
    ''' Marshals an action to the UI thread without blocking the caller.
    ''' BeginInvoke is used instead of Invoke to prevent deadlock: async
    ''' disconnect callbacks arrive on a thread-pool thread that may already
    ''' hold StreamJsonRpc internal locks; using synchronous Invoke would
    ''' block until the UI thread completes, while the UI thread may be
    ''' waiting on the same locks — causing a permanent deadlock.
    ''' </summary>
    Public Sub RunOnUIThread(control As Control, action As Action)
        If control.InvokeRequired Then
            control.BeginInvoke(action)
        Else
            action()
        End If
    End Sub

    ' =========================================================================
    ' CONNECTION STATUS
    ' =========================================================================

    ''' <summary>
    ''' Updates a label to reflect current connection state with color feedback
    ''' </summary>
    Public Sub UpdateConnectionStatus(
        label As Label,
        isConnected As Boolean,
        connectedColor As Color,
        disconnectedColor As Color)

        RunOnUIThread(label, Sub()
                                 If isConnected Then
                                     label.Text = "Connected"
                                     label.BackColor = connectedColor
                                     label.ForeColor = Color.White
                                 Else
                                     label.Text = "Disconnected"
                                     label.BackColor = disconnectedColor
                                     label.ForeColor = Color.White
                                 End If
                             End Sub)
    End Sub

    ' =========================================================================
    ' MESSAGE BOXES
    ' =========================================================================

    ''' <summary>
    ''' Displays a modal error message box
    ''' </summary>
    Public Sub ShowErrorMessage(message As String, title As String)
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

    ''' <summary>
    ''' Displays a modal success (information) message box
    ''' </summary>
    Public Sub ShowSuccessMessage(message As String, title As String)
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ''' <summary>
    ''' Displays a modal warning message box
    ''' </summary>
    Public Sub ShowWarningMessage(message As String, title As String)
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub

    ' =========================================================================
    ' TEXTBOX HELPERS
    ' =========================================================================

    ''' <summary>
    ''' Clears a TextBox and sets focus to it
    ''' </summary>
    Public Sub ClearAndFocusTextBox(textBox As TextBox)
        textBox.Clear()
        textBox.Focus()
    End Sub

    ''' <summary>
    ''' Clears a TextBox without changing focus
    ''' </summary>
    Public Sub ClearTextBox(textBox As TextBox)
        textBox.Clear()
    End Sub

End Module

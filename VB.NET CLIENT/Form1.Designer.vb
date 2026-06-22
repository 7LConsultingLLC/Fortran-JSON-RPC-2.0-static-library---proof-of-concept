<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        txbMessage = New TextBox()
        txbVBLog = New TextBox()
        Label1 = New Label()
        Label2 = New Label()
        btnClose = New Button()
        btnSendMessage = New Button()
        btnClearMessage = New Button()
        btnConnect = New Button()
        btnDisconnect = New Button()
        lblConnectionStatus = New Label()
        txbParameter1 = New TextBox()
        txbParameter2 = New TextBox()
        Label3 = New Label()
        Label4 = New Label()
        cbMethods = New ComboBox()
        btnExecute = New Button()
        Label5 = New Label()
        btnClearNotification = New Button()
        btnSendNotification = New Button()
        Label6 = New Label()
        txbNotification = New TextBox()
        btnTest32600 = New Button()
        btnTest32601 = New Button()
        btnTest32603 = New Button()
        btnTest32602 = New Button()
        btnTest32700 = New Button()
        btnClearVBLog = New Button()
        Label7 = New Label()
        Label8 = New Label()
        Label9 = New Label()
        Label10 = New Label()
        Label11 = New Label()
        Label12 = New Label()
        txbNamedParameters = New TextBox()
        Label13 = New Label()
        btnSendNamedParameters = New Button()
        btnTestBatchRequest = New Button()
        Label14 = New Label()
        Label15 = New Label()
        txbSeed = New TextBox()
        Label16 = New Label()
        btnStartDemo = New Button()
        btnPauseDemo = New Button()
        btnResumeDemo = New Button()
        btnExitDemo = New Button()
        tlpOriginalMatrix = New TableLayoutPanel()
        Label17 = New Label()
        cbxMatrixDataType = New ComboBox()
        cbxMatrixOperation = New ComboBox()
        Label18 = New Label()
        Label19 = New Label()
        Label20 = New Label()
        tlpModifiedMatrix = New TableLayoutPanel()
        Label21 = New Label()
        btnMatrixExecute = New Button()
        cbxMatrixOrdering = New ComboBox()
        Label23 = New Label()
        SuspendLayout()
        ' 
        ' txbMessage
        ' 
        txbMessage.Location = New Point(190, 143)
        txbMessage.Name = "txbMessage"
        txbMessage.Size = New Size(201, 23)
        txbMessage.TabIndex = 0
        ' 
        ' txbVBLog
        ' 
        txbVBLog.BackColor = Color.Red
        txbVBLog.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        txbVBLog.ForeColor = Color.White
        txbVBLog.Location = New Point(24, 427)
        txbVBLog.Multiline = True
        txbVBLog.Name = "txbVBLog"
        txbVBLog.ScrollBars = ScrollBars.Vertical
        txbVBLog.Size = New Size(700, 137)
        txbVBLog.TabIndex = 1
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(190, 125)
        Label1.Name = "Label1"
        Label1.Size = New Size(95, 15)
        Label1.TabIndex = 2
        Label1.Text = "Message to send"
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(24, 409)
        Label2.Name = "Label2"
        Label2.Size = New Size(123, 15)
        Label2.TabIndex = 3
        Label2.Text = "VB.NET Client console"
        ' 
        ' btnClose
        ' 
        btnClose.Location = New Point(1062, 604)
        btnClose.Name = "btnClose"
        btnClose.Size = New Size(142, 38)
        btnClose.TabIndex = 4
        btnClose.Text = "Close"
        btnClose.UseVisualStyleBackColor = True
        ' 
        ' btnSendMessage
        ' 
        btnSendMessage.Location = New Point(29, 128)
        btnSendMessage.Name = "btnSendMessage"
        btnSendMessage.Size = New Size(142, 38)
        btnSendMessage.TabIndex = 5
        btnSendMessage.Text = "Send message"
        btnSendMessage.UseVisualStyleBackColor = True
        ' 
        ' btnClearMessage
        ' 
        btnClearMessage.Location = New Point(410, 134)
        btnClearMessage.Name = "btnClearMessage"
        btnClearMessage.Size = New Size(142, 38)
        btnClearMessage.TabIndex = 6
        btnClearMessage.Text = "Clear message"
        btnClearMessage.UseVisualStyleBackColor = True
        ' 
        ' btnConnect
        ' 
        btnConnect.Location = New Point(29, 42)
        btnConnect.Name = "btnConnect"
        btnConnect.Size = New Size(142, 38)
        btnConnect.TabIndex = 7
        btnConnect.Text = "Connect"
        btnConnect.UseVisualStyleBackColor = True
        ' 
        ' btnDisconnect
        ' 
        btnDisconnect.Location = New Point(410, 42)
        btnDisconnect.Name = "btnDisconnect"
        btnDisconnect.Size = New Size(142, 38)
        btnDisconnect.TabIndex = 8
        btnDisconnect.Text = "Disconnect"
        btnDisconnect.UseVisualStyleBackColor = True
        ' 
        ' lblConnectionStatus
        ' 
        lblConnectionStatus.BackColor = Color.Red
        lblConnectionStatus.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblConnectionStatus.ForeColor = Color.White
        lblConnectionStatus.Location = New Point(190, 42)
        lblConnectionStatus.Name = "lblConnectionStatus"
        lblConnectionStatus.Size = New Size(201, 38)
        lblConnectionStatus.TabIndex = 9
        lblConnectionStatus.Text = "Disconnected"
        lblConnectionStatus.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' txbParameter1
        ' 
        txbParameter1.Location = New Point(190, 300)
        txbParameter1.Name = "txbParameter1"
        txbParameter1.Size = New Size(97, 23)
        txbParameter1.TabIndex = 11
        txbParameter1.TextAlign = HorizontalAlignment.Center
        ' 
        ' txbParameter2
        ' 
        txbParameter2.Location = New Point(289, 300)
        txbParameter2.Name = "txbParameter2"
        txbParameter2.Size = New Size(97, 23)
        txbParameter2.TabIndex = 12
        txbParameter2.TextAlign = HorizontalAlignment.Center
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(188, 282)
        Label3.Name = "Label3"
        Label3.Size = New Size(50, 15)
        Label3.TabIndex = 13
        Label3.Text = "Param 1"
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(289, 282)
        Label4.Name = "Label4"
        Label4.Size = New Size(50, 15)
        Label4.TabIndex = 14
        Label4.Text = "Param 2"
        ' 
        ' cbMethods
        ' 
        cbMethods.FormattingEnabled = True
        cbMethods.Items.AddRange(New Object() {"Add integer", "Subtract integer", "Multiply integer", "Divide integer", "Add real", "Subtract real", "Multiply real", "Divide real", "Add complex", "Subtract complex", "Multiply complex", "Divide complex"})
        cbMethods.Location = New Point(26, 300)
        cbMethods.Name = "cbMethods"
        cbMethods.Size = New Size(121, 23)
        cbMethods.TabIndex = 15
        ' 
        ' btnExecute
        ' 
        btnExecute.Location = New Point(407, 291)
        btnExecute.Name = "btnExecute"
        btnExecute.Size = New Size(142, 38)
        btnExecute.TabIndex = 16
        btnExecute.Text = "Execute"
        btnExecute.UseVisualStyleBackColor = True
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(26, 282)
        Label5.Name = "Label5"
        Label5.Size = New Size(86, 15)
        Label5.TabIndex = 17
        Label5.Text = "Select method:"
        ' 
        ' btnClearNotification
        ' 
        btnClearNotification.Location = New Point(408, 208)
        btnClearNotification.Name = "btnClearNotification"
        btnClearNotification.Size = New Size(142, 38)
        btnClearNotification.TabIndex = 21
        btnClearNotification.Text = "Clear notification"
        btnClearNotification.UseVisualStyleBackColor = True
        ' 
        ' btnSendNotification
        ' 
        btnSendNotification.Location = New Point(27, 202)
        btnSendNotification.Name = "btnSendNotification"
        btnSendNotification.Size = New Size(142, 38)
        btnSendNotification.TabIndex = 20
        btnSendNotification.Text = "Send notification"
        btnSendNotification.UseVisualStyleBackColor = True
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(188, 199)
        Label6.Name = "Label6"
        Label6.Size = New Size(112, 15)
        Label6.TabIndex = 19
        Label6.Text = "Notification to send"
        ' 
        ' txbNotification
        ' 
        txbNotification.Location = New Point(188, 217)
        txbNotification.Name = "txbNotification"
        txbNotification.Size = New Size(201, 23)
        txbNotification.TabIndex = 18
        ' 
        ' btnTest32600
        ' 
        btnTest32600.Location = New Point(584, 86)
        btnTest32600.Name = "btnTest32600"
        btnTest32600.Size = New Size(142, 38)
        btnTest32600.TabIndex = 22
        btnTest32600.Text = "Test error 32600"
        btnTest32600.UseVisualStyleBackColor = True
        ' 
        ' btnTest32601
        ' 
        btnTest32601.Location = New Point(584, 135)
        btnTest32601.Name = "btnTest32601"
        btnTest32601.Size = New Size(142, 38)
        btnTest32601.TabIndex = 23
        btnTest32601.Text = "Test error 32601"
        btnTest32601.UseVisualStyleBackColor = True
        ' 
        ' btnTest32603
        ' 
        btnTest32603.Location = New Point(584, 223)
        btnTest32603.Name = "btnTest32603"
        btnTest32603.Size = New Size(142, 38)
        btnTest32603.TabIndex = 24
        btnTest32603.Text = "Test error 32603"
        btnTest32603.UseVisualStyleBackColor = True
        ' 
        ' btnTest32602
        ' 
        btnTest32602.Location = New Point(582, 179)
        btnTest32602.Name = "btnTest32602"
        btnTest32602.Size = New Size(142, 38)
        btnTest32602.TabIndex = 25
        btnTest32602.Text = "Test error 332602"
        btnTest32602.UseVisualStyleBackColor = True
        ' 
        ' btnTest32700
        ' 
        btnTest32700.Location = New Point(584, 40)
        btnTest32700.Name = "btnTest32700"
        btnTest32700.Size = New Size(142, 38)
        btnTest32700.TabIndex = 26
        btnTest32700.Text = "Test error 32700"
        btnTest32700.UseVisualStyleBackColor = True
        ' 
        ' btnClearVBLog
        ' 
        btnClearVBLog.Location = New Point(24, 570)
        btnClearVBLog.Name = "btnClearVBLog"
        btnClearVBLog.Size = New Size(140, 38)
        btnClearVBLog.TabIndex = 28
        btnClearVBLog.Text = "Clear log"
        btnClearVBLog.UseVisualStyleBackColor = True
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label7.Location = New Point(27, 258)
        Label7.Name = "Label7"
        Label7.Size = New Size(127, 15)
        Label7.TabIndex = 29
        Label7.Text = "Positional parameters"
        ' 
        ' Label8
        ' 
        Label8.AutoSize = True
        Label8.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label8.Location = New Point(30, 24)
        Label8.Name = "Label8"
        Label8.Size = New Size(107, 15)
        Label8.TabIndex = 30
        Label8.Text = "Connect to server"
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label9.Location = New Point(30, 103)
        Label9.Name = "Label9"
        Label9.Size = New Size(86, 15)
        Label9.TabIndex = 31
        Label9.Text = "Send message"
        ' 
        ' Label10
        ' 
        Label10.AutoSize = True
        Label10.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label10.Location = New Point(29, 180)
        Label10.Name = "Label10"
        Label10.Size = New Size(102, 15)
        Label10.TabIndex = 32
        Label10.Text = "Send notification"
        ' 
        ' Label11
        ' 
        Label11.AutoSize = True
        Label11.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label11.Location = New Point(584, 21)
        Label11.Name = "Label11"
        Label11.Size = New Size(117, 15)
        Label11.TabIndex = 33
        Label11.Text = "Test for error codes"
        ' 
        ' Label12
        ' 
        Label12.AutoSize = True
        Label12.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label12.Location = New Point(30, 341)
        Label12.Name = "Label12"
        Label12.Size = New Size(114, 15)
        Label12.TabIndex = 34
        Label12.Text = "Named parameters"
        ' 
        ' txbNamedParameters
        ' 
        txbNamedParameters.Location = New Point(188, 365)
        txbNamedParameters.Name = "txbNamedParameters"
        txbNamedParameters.Size = New Size(201, 23)
        txbNamedParameters.TabIndex = 35
        ' 
        ' Label13
        ' 
        Label13.AutoSize = True
        Label13.Location = New Point(30, 365)
        Label13.Name = "Label13"
        Label13.Size = New Size(139, 15)
        Label13.TabIndex = 36
        Label13.Text = "Enter named parameters:"
        ' 
        ' btnSendNamedParameters
        ' 
        btnSendNamedParameters.Location = New Point(407, 356)
        btnSendNamedParameters.Name = "btnSendNamedParameters"
        btnSendNamedParameters.Size = New Size(142, 38)
        btnSendNamedParameters.TabIndex = 37
        btnSendNamedParameters.Text = "Send named parameters"
        btnSendNamedParameters.UseVisualStyleBackColor = True
        ' 
        ' btnTestBatchRequest
        ' 
        btnTestBatchRequest.Location = New Point(582, 352)
        btnTestBatchRequest.Name = "btnTestBatchRequest"
        btnTestBatchRequest.Size = New Size(142, 38)
        btnTestBatchRequest.TabIndex = 38
        btnTestBatchRequest.Text = "Test batch request"
        btnTestBatchRequest.UseVisualStyleBackColor = True
        ' 
        ' Label14
        ' 
        Label14.AutoSize = True
        Label14.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label14.Location = New Point(584, 326)
        Label14.Name = "Label14"
        Label14.Size = New Size(130, 15)
        Label14.TabIndex = 39
        Label14.Text = "Test for batch request"
        ' 
        ' Label15
        ' 
        Label15.AutoSize = True
        Label15.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label15.Location = New Point(775, 432)
        Label15.Name = "Label15"
        Label15.Size = New Size(104, 15)
        Label15.TabIndex = 40
        Label15.Text = "Interactive demo"
        ' 
        ' txbSeed
        ' 
        txbSeed.Location = New Point(915, 453)
        txbSeed.Name = "txbSeed"
        txbSeed.Size = New Size(62, 23)
        txbSeed.TabIndex = 41
        txbSeed.TextAlign = HorizontalAlignment.Center
        ' 
        ' Label16
        ' 
        Label16.AutoSize = True
        Label16.Location = New Point(775, 456)
        Label16.Name = "Label16"
        Label16.Size = New Size(133, 15)
        Label16.TabIndex = 42
        Label16.Text = "Enter an integer (1 - 10):"
        ' 
        ' btnStartDemo
        ' 
        btnStartDemo.Location = New Point(1002, 427)
        btnStartDemo.Name = "btnStartDemo"
        btnStartDemo.Size = New Size(142, 38)
        btnStartDemo.TabIndex = 43
        btnStartDemo.Text = "Start demo program"
        btnStartDemo.UseVisualStyleBackColor = True
        ' 
        ' btnPauseDemo
        ' 
        btnPauseDemo.Enabled = False
        btnPauseDemo.Location = New Point(1002, 471)
        btnPauseDemo.Name = "btnPauseDemo"
        btnPauseDemo.Size = New Size(142, 38)
        btnPauseDemo.TabIndex = 44
        btnPauseDemo.Text = "Pause demo"
        btnPauseDemo.UseVisualStyleBackColor = True
        ' 
        ' btnResumeDemo
        ' 
        btnResumeDemo.Enabled = False
        btnResumeDemo.Location = New Point(1002, 515)
        btnResumeDemo.Name = "btnResumeDemo"
        btnResumeDemo.Size = New Size(142, 38)
        btnResumeDemo.TabIndex = 45
        btnResumeDemo.Text = "Resume demo"
        btnResumeDemo.UseVisualStyleBackColor = True
        ' 
        ' btnExitDemo
        ' 
        btnExitDemo.Enabled = False
        btnExitDemo.Location = New Point(1002, 559)
        btnExitDemo.Name = "btnExitDemo"
        btnExitDemo.Size = New Size(142, 38)
        btnExitDemo.TabIndex = 46
        btnExitDemo.Text = "Exit demo"
        btnExitDemo.UseVisualStyleBackColor = True
        ' 
        ' tlpOriginalMatrix
        ' 
        tlpOriginalMatrix.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        tlpOriginalMatrix.ColumnCount = 4
        tlpOriginalMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.Location = New Point(770, 144)
        tlpOriginalMatrix.Name = "tlpOriginalMatrix"
        tlpOriginalMatrix.RowCount = 4
        tlpOriginalMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpOriginalMatrix.Size = New Size(434, 100)
        tlpOriginalMatrix.TabIndex = 47
        ' 
        ' Label17
        ' 
        Label17.AutoSize = True
        Label17.Location = New Point(773, 120)
        Label17.Name = "Label17"
        Label17.Size = New Size(86, 15)
        Label17.TabIndex = 48
        Label17.Text = "Original matrix"
        ' 
        ' cbxMatrixDataType
        ' 
        cbxMatrixDataType.FormattingEnabled = True
        cbxMatrixDataType.Items.AddRange(New Object() {"integer", "real", "complex", "logical/boolean", "character/text"})
        cbxMatrixDataType.Location = New Point(773, 66)
        cbxMatrixDataType.Name = "cbxMatrixDataType"
        cbxMatrixDataType.Size = New Size(121, 23)
        cbxMatrixDataType.TabIndex = 49
        ' 
        ' cbxMatrixOperation
        ' 
        cbxMatrixOperation.FormattingEnabled = True
        cbxMatrixOperation.Items.AddRange(New Object() {"copy", "transpose", "square"})
        cbxMatrixOperation.Location = New Point(906, 66)
        cbxMatrixOperation.Name = "cbxMatrixOperation"
        cbxMatrixOperation.Size = New Size(121, 23)
        cbxMatrixOperation.TabIndex = 50
        ' 
        ' Label18
        ' 
        Label18.AutoSize = True
        Label18.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label18.Location = New Point(770, 22)
        Label18.Name = "Label18"
        Label18.Size = New Size(106, 15)
        Label18.TabIndex = 51
        Label18.Text = "Matrix operations"
        ' 
        ' Label19
        ' 
        Label19.AutoSize = True
        Label19.Location = New Point(773, 48)
        Label19.Name = "Label19"
        Label19.Size = New Size(93, 15)
        Label19.TabIndex = 52
        Label19.Text = "Select data type:"
        ' 
        ' Label20
        ' 
        Label20.AutoSize = True
        Label20.Location = New Point(906, 48)
        Label20.Name = "Label20"
        Label20.Size = New Size(95, 15)
        Label20.TabIndex = 53
        Label20.Text = "Select operation:"
        ' 
        ' tlpModifiedMatrix
        ' 
        tlpModifiedMatrix.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        tlpModifiedMatrix.ColumnCount = 4
        tlpModifiedMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.Location = New Point(773, 286)
        tlpModifiedMatrix.Name = "tlpModifiedMatrix"
        tlpModifiedMatrix.RowCount = 4
        tlpModifiedMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.RowStyles.Add(New RowStyle(SizeType.Percent, 25F))
        tlpModifiedMatrix.Size = New Size(431, 100)
        tlpModifiedMatrix.TabIndex = 54
        ' 
        ' Label21
        ' 
        Label21.AutoSize = True
        Label21.Location = New Point(773, 260)
        Label21.Name = "Label21"
        Label21.Size = New Size(92, 15)
        Label21.TabIndex = 55
        Label21.Text = "Modified matrix"
        ' 
        ' btnMatrixExecute
        ' 
        btnMatrixExecute.Location = New Point(1062, 97)
        btnMatrixExecute.Name = "btnMatrixExecute"
        btnMatrixExecute.Size = New Size(142, 38)
        btnMatrixExecute.TabIndex = 59
        btnMatrixExecute.Text = "Execute"
        btnMatrixExecute.UseVisualStyleBackColor = True
        ' 
        ' cbxMatrixOrdering
        ' 
        cbxMatrixOrdering.FormattingEnabled = True
        cbxMatrixOrdering.Items.AddRange(New Object() {"row-major", "column-major"})
        cbxMatrixOrdering.Location = New Point(1040, 68)
        cbxMatrixOrdering.Name = "cbxMatrixOrdering"
        cbxMatrixOrdering.Size = New Size(121, 23)
        cbxMatrixOrdering.TabIndex = 60
        ' 
        ' Label23
        ' 
        Label23.AutoSize = True
        Label23.Location = New Point(1040, 50)
        Label23.Name = "Label23"
        Label23.Size = New Size(158, 15)
        Label23.TabIndex = 61
        Label23.Text = "Select row/column ordering:"
        ' 
        ' frmMain
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1230, 654)
        Controls.Add(Label23)
        Controls.Add(cbxMatrixOrdering)
        Controls.Add(btnMatrixExecute)
        Controls.Add(Label21)
        Controls.Add(tlpModifiedMatrix)
        Controls.Add(Label20)
        Controls.Add(Label19)
        Controls.Add(Label18)
        Controls.Add(cbxMatrixOperation)
        Controls.Add(cbxMatrixDataType)
        Controls.Add(Label17)
        Controls.Add(tlpOriginalMatrix)
        Controls.Add(btnExitDemo)
        Controls.Add(btnResumeDemo)
        Controls.Add(btnPauseDemo)
        Controls.Add(btnStartDemo)
        Controls.Add(Label16)
        Controls.Add(txbSeed)
        Controls.Add(Label15)
        Controls.Add(Label14)
        Controls.Add(btnTestBatchRequest)
        Controls.Add(btnSendNamedParameters)
        Controls.Add(Label13)
        Controls.Add(txbNamedParameters)
        Controls.Add(Label12)
        Controls.Add(Label11)
        Controls.Add(Label10)
        Controls.Add(Label9)
        Controls.Add(Label8)
        Controls.Add(Label7)
        Controls.Add(btnClearVBLog)
        Controls.Add(btnTest32700)
        Controls.Add(btnTest32602)
        Controls.Add(btnTest32603)
        Controls.Add(btnTest32601)
        Controls.Add(btnTest32600)
        Controls.Add(btnClearNotification)
        Controls.Add(btnSendNotification)
        Controls.Add(Label6)
        Controls.Add(txbNotification)
        Controls.Add(Label5)
        Controls.Add(btnExecute)
        Controls.Add(cbMethods)
        Controls.Add(Label4)
        Controls.Add(Label3)
        Controls.Add(txbParameter2)
        Controls.Add(txbParameter1)
        Controls.Add(lblConnectionStatus)
        Controls.Add(btnDisconnect)
        Controls.Add(btnConnect)
        Controls.Add(btnClearMessage)
        Controls.Add(btnSendMessage)
        Controls.Add(btnClose)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Controls.Add(txbVBLog)
        Controls.Add(txbMessage)
        Name = "frmMain"
        StartPosition = FormStartPosition.CenterScreen
        Text = "VB Client Program for Fortran Server RPC "
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents txbMessage As TextBox
    Friend WithEvents txbVBLog As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents btnClose As Button
    Friend WithEvents btnSendMessage As Button
    Friend WithEvents btnClearMessage As Button
    Friend WithEvents btnConnect As Button
    Friend WithEvents btnDisconnect As Button
    Friend WithEvents lblConnectionStatus As Label
    Friend WithEvents txbParameter1 As TextBox
    Friend WithEvents txbParameter2 As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents cbMethods As ComboBox
    Friend WithEvents btnExecute As Button
    Friend WithEvents Label5 As Label
    Friend WithEvents btnClearNotification As Button
    Friend WithEvents btnSendNotification As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents txbNotification As TextBox
    Friend WithEvents btnTest32600 As Button
    Friend WithEvents btnTest32601 As Button
    Friend WithEvents btnTest32603 As Button
    Friend WithEvents btnTest32602 As Button
    Friend WithEvents btnTest32700 As Button
    Friend WithEvents btnClearVBLog As Button
    Friend WithEvents Label7 As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents Label9 As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents Label11 As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents txbNamedParameters As TextBox
    Friend WithEvents Label13 As Label
    Friend WithEvents btnSendNamedParameters As Button
    Friend WithEvents btnTestBatchRequest As Button
    Friend WithEvents Label14 As Label
    Friend WithEvents Label15 As Label
    Friend WithEvents txbSeed As TextBox
    Friend WithEvents Label16 As Label
    Friend WithEvents btnStartDemo As Button
    Friend WithEvents btnPauseDemo As Button
    Friend WithEvents btnResumeDemo As Button
    Friend WithEvents btnExitDemo As Button
    Friend WithEvents tlpOriginalMatrix As TableLayoutPanel
    Friend WithEvents Label17 As Label
    Friend WithEvents cbxMatrixDataType As ComboBox
    Friend WithEvents cbxMatrixOperation As ComboBox
    Friend WithEvents Label18 As Label
    Friend WithEvents Label19 As Label
    Friend WithEvents Label20 As Label
    Friend WithEvents tlpModifiedMatrix As TableLayoutPanel
    Friend WithEvents Label21 As Label
    Friend WithEvents btnMatrixExecute As Button
    Friend WithEvents cbxMatrixOrdering As ComboBox
    Friend WithEvents Label23 As Label

End Class

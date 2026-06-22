' =========================================================================
' AppConstants.vb
' Application-specific constants for the Fortran JSON-RPC Client program.
' Method name strings define the server-side contract with the Fortran server.
' =========================================================================

Public Module AppConstants

    ' ---------------------------------------------------------
    ' Named pipe identifier shared between client and Fortran server.
    ' NOTE: Hard-coded pipe name is a deliberate POC simplification.
    ' A production implementation would supply this via configuration.
    ' ---------------------------------------------------------
    Public Const PipeName As String = "MyTestPipe"

    ' ---------------------------------------------------------
    ' Global RPC timeout for all client-side calls (individual and batch).
    ' Set generously for the Mandelbrot benchmark, which can run for several
    ' minutes. Adjust here to change policy for all operations at once.
    ' ---------------------------------------------------------
    Public Const RequestTimeoutMs As Integer = 300000  ' 5 minutes

    ' ---------------------------------------------------------
    ' Outbound server method names - INTEGER OPERATIONS
    ' ---------------------------------------------------------
    Public Const MethodAddInt As String = "addint"
    Public Const MethodSubtractInt As String = "subtractint"
    Public Const MethodMultiplyInt As String = "multiplyint"
    Public Const MethodDivideInt As String = "divideint"

    ' ---------------------------------------------------------
    ' Outbound server method names - REAL/FLOATING-POINT OPERATIONS
    ' ---------------------------------------------------------
    Public Const MethodAddReal As String = "addreal"
    Public Const MethodSubtractReal As String = "subtractreal"
    Public Const MethodMultiplyReal As String = "multiplyreal"
    Public Const MethodDivideReal As String = "dividereal"

    ' ---------------------------------------------------------
    ' Outbound server method names - COMPLEX NUMBER OPERATIONS
    ' ---------------------------------------------------------
    Public Const MethodAddComplex As String = "addcomplex"
    Public Const MethodSubtractComplex As String = "subtractcomplex"
    Public Const MethodMultiplyComplex As String = "multiplycomplex"
    Public Const MethodDivideComplex As String = "dividecomplex"

    ' ---------------------------------------------------------
    ' Outbound server method names - GENERAL PURPOSE
    ' NOTE: MethodClientNotify and MethodDisconnect are notification method names
    ' sent to the Fortran server. The server accepts any notification method name
    ' silently except "disconnect", which triggers a graceful server shutdown.
    ' ---------------------------------------------------------
    Public Const MethodSendMessage As String = "sendmessage"
    Public Const MethodNamedParameters As String = "namedparameters"
    Public Const MethodClientNotify As String = "clientevent"
    Public Const MethodDisconnect As String = "disconnect"

End Module

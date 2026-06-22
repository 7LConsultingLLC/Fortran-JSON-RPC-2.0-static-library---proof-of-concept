' =========================================================================
' CalculationModule.vb
' Centralized calculation logic for Fortran JSON-RPC Client
' =========================================================================

Public Module CalculationModule

    ' =========================================================================
    ' GET OPERATION INFO
    ' =========================================================================

    ''' <summary>
    ''' Returns the display name and symbol for a given RPC method name
    ''' </summary>
    Public Function GetOperationInfo(methodName As String) As (Name As String, Symbol As String)
        Select Case methodName.ToLower()
            Case "addint"
                Return ("Addition", "+")
            Case "subtractint"
                Return ("Subtraction", "-")
            Case "multiplyint"
                Return ("Multiplication", "*")
            Case "divideint"
                Return ("Division", "\")
            Case "addreal"
                Return ("Real Addition", "+")
            Case "subtractreal"
                Return ("Real Subtraction", "-")
            Case "multiplyreal"
                Return ("Real Multiplication", "*")
            Case "dividereal"
                Return ("Real Division", "/")
            Case "addcomplex"
                Return ("Complex Addition", "+")
            Case "subtractcomplex"
                Return ("Complex Subtraction", "-")
            Case "multiplycomplex"
                Return ("Complex Multiplication", "*")
            Case "dividecomplex"
                Return ("Complex Division", "/")
            Case Else
                Return ("Unknown Operation", "?")
        End Select
    End Function

    ' =========================================================================
    ' CALCULATE EXPECTED RESULT (INTEGER)
    ' =========================================================================

    ''' <summary>
    ''' Calculates the locally expected result for integer methods
    ''' </summary>
    Public Function CalculateExpectedResult(methodName As String, param1 As Integer, param2 As Integer) As Integer
        Select Case methodName.ToLower()
            Case "addint"
                Return param1 + param2
            Case "subtractint"
                Return param1 - param2
            Case "multiplyint"
                Return param1 * param2
            Case "divideint"
                Return param1 \ param2
            Case Else
                Return 0
        End Select
    End Function

    ' =========================================================================
    ' CALCULATE EXPECTED RESULT (REAL)
    ' =========================================================================

    ''' <summary>
    ''' Calculates the locally expected result for real number methods
    ''' </summary>
    Public Function CalculateExpectedRealResult(methodName As String, param1 As Double, param2 As Double) As Double
        Select Case methodName.ToLower()
            Case "addreal"
                Return param1 + param2
            Case "subtractreal"
                Return param1 - param2
            Case "multiplyreal"
                Return param1 * param2
            Case "dividereal"
                If param2 = 0.0 Then Return 0.0
                Return param1 / param2
            Case Else
                Return 0.0
        End Select
    End Function

    ' =========================================================================
    ' VERIFY CALCULATION RESULT (INTEGER)
    ' =========================================================================

    ''' <summary>
    ''' Verifies server result against locally calculated expected integer value.
    ''' Returns (IsMatch, SuccessMessage, ErrorMessage)
    ''' </summary>
    Public Function VerifyCalculationResult(
        expected As Integer,
        actual As Integer,
        methodName As String,
        param1 As Integer,
        param2 As Integer) As (IsMatch As Boolean, SuccessMessage As String, ErrorMessage As String)

        If expected = actual Then
            Select Case methodName.ToLower()
                Case "addint"
                    Return (True, $"Addition verified: {param1} + {param2} = {actual}", "")
                Case "subtractint"
                    Return (True, $"Subtraction verified: {param1} - {param2} = {actual}", "")
                Case "multiplyint"
                    Return (True, $"Multiplication verified: {param1} * {param2} = {actual}", "")
                Case "divideint"
                    Return (True, $"Division verified: {param1} \ {param2} = {actual}", "")
                Case Else
                    Return (True, $"Calculation verified: result = {actual}", "")
            End Select
        Else
            Return (False, "", $"Calculation mismatch: expected {expected}, got {actual}")
        End If
    End Function

    ' =========================================================================
    ' VERIFY CALCULATION RESULT (REAL)
    ' =========================================================================

    ''' <summary>
    ''' Verifies server result against locally calculated expected real value.
    ''' Uses relative epsilon (diff/expected) for values greater than 1.0 so
    ''' tolerance scales with magnitude; absolute epsilon for small values
    ''' where relative comparison breaks down near zero.
    ''' Returns (IsMatch, SuccessMessage, ErrorMessage)
    ''' </summary>
    Public Function VerifyRealCalculationResult(
        expected As Double,
        actual As Double,
        methodName As String,
        param1 As Double,
        param2 As Double) As (IsMatch As Boolean, SuccessMessage As String, ErrorMessage As String)

        Const Epsilon As Double = 0.0001
        Dim diff As Double = Math.Abs(expected - actual)
        Dim matchOk As Boolean = If(Math.Abs(expected) > 1.0,
                                    diff / Math.Abs(expected) <= Epsilon,
                                    diff <= Epsilon)
        If matchOk Then
            Select Case methodName.ToLower()
                Case "addreal"
                    Return (True, $"Real addition verified: {param1} + {param2} = {actual}", "")
                Case "subtractreal"
                    Return (True, $"Real subtraction verified: {param1} - {param2} = {actual}", "")
                Case "multiplyreal"
                    Return (True, $"Real multiplication verified: {param1} * {param2} = {actual}", "")
                Case "dividereal"
                    Return (True, $"Real division verified: {param1} / {param2} = {actual}", "")
                Case Else
                    Return (True, $"Real calculation verified: result = {actual}", "")
            End Select
        Else
            Return (False, "", $"Calculation mismatch: expected {expected}, got {actual}")
        End If
    End Function

    ' =========================================================================
    ' FORMAT REAL RESULT
    ' =========================================================================

    ''' <summary>
    ''' Formats a Double for display.
    ''' If forceScientific is True, always uses scientific notation format ####.##E##
    ''' (no + on positive exponent, exactly 2 exponent digits).
    ''' If forceScientific is False, uses plain decimal ####.## where possible,
    ''' falling back to scientific only when the value exceeds plain decimal range.
    ''' </summary>
    Public Function FormatRealResult(value As Double, Optional forceScientific As Boolean = False) As String
        If forceScientific Then
            ' Format as scientific notation: mantissa with up to 2 decimal places, 2-digit exponent, no + sign
            Dim raw As String = value.ToString("0.00E+00", System.Globalization.CultureInfo.InvariantCulture)
            ' Strip leading + from exponent: "1.23E+04" -> "1.23E04", "-1.23E-04" stays "-1.23E-04"
            Return System.Text.RegularExpressions.Regex.Replace(raw, "E\+", "E")
        Else
            ' Use plain decimal when value fits neatly within ####.## bounds
            If Math.Abs(value) <= 9999.99 Then
                Return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
            End If
            ' Fall back to scientific for large values
            Dim raw As String = value.ToString("0.00E+00", System.Globalization.CultureInfo.InvariantCulture)
            Return System.Text.RegularExpressions.Regex.Replace(raw, "E\+", "E")
        End If
    End Function

    ' =========================================================================
    ' GET SERVER METHOD NAME
    ' =========================================================================

    ''' <summary>
    ''' Maps the display name from cbMethods to the actual Fortran server method name
    ''' </summary>
    Public Function GetServerMethodName(displayName As String) As String
        Select Case displayName
            Case "Add integer"
                Return "addint"
            Case "Subtract integer"
                Return "subtractint"
            Case "Multiply integer"
                Return "multiplyint"
            Case "Divide integer"
                Return "divideint"
            Case "Add real"
                Return "addreal"
            Case "Subtract real"
                Return "subtractreal"
            Case "Multiply real"
                Return "multiplyreal"
            Case "Divide real"
                Return "dividereal"
            Case "Add complex"
                Return "addcomplex"
            Case "Subtract complex"
                Return "subtractcomplex"
            Case "Multiply complex"
                Return "multiplycomplex"
            Case "Divide complex"
                Return "dividecomplex"
            Case Else
                Return displayName
        End Select
    End Function

    ' =========================================================================
    ' DETERMINE METHOD TYPE
    ' =========================================================================

    ''' <summary>
    ''' Returns the parameter type category for a given server method name:
    ''' "integer", "real", "complex", or "unknown".
    ''' Used by btnExecute_Click to route to the correct validation and execution path.
    ''' </summary>
    Public Function DetermineMethodType(serverMethodName As String) As String
        Select Case serverMethodName.ToLower()
            Case "addint", "subtractint", "multiplyint", "divideint"
                Return "integer"
            Case "addreal", "subtractreal", "multiplyreal", "dividereal"
                Return "real"
            Case "addcomplex", "subtractcomplex", "multiplycomplex", "dividecomplex"
                Return "complex"
            Case Else
                Return "unknown"
        End Select
    End Function


    ' =========================================================================
    ' FORMAT COMPLEX COMPONENT
    ' =========================================================================

    ''' <summary>
    ''' Formats a single Double value for display in a complex number result.
    ''' format = "integer", "real", or "scientific".
    ''' Returns empty string if value is exactly zero (for zero suppression).
    ''' </summary>
    Public Function FormatComplexComponent(value As Double, format As String) As String
        If value = 0.0 Then Return ""

        Select Case format.ToLower()
            Case "integer"
                Return CInt(Math.Round(value)).ToString()
            Case "real"
                Return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
            Case "scientific"
                Dim raw As String = value.ToString("0.00E+00", System.Globalization.CultureInfo.InvariantCulture)
                ' Remove leading + from exponent; keep - sign
                Return System.Text.RegularExpressions.Regex.Replace(raw, "E\+", "E")
            Case Else
                Return value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        End Select
    End Function

    ' =========================================================================
    ' FORMAT COMPLEX RESULT
    ' =========================================================================

    ''' <summary>
    ''' Assembles a display string from real and imaginary result components.
    ''' Applies zero suppression: omits a component if its value is exactly 0.0.
    ''' Sign normalization: uses "- ####i" for negative imaginary (not "+ -####i").
    ''' </summary>
    Public Function FormatComplexResult(realVal As Double, imagVal As Double,
                                        realFormat As String, imagFormat As String) As String
        Dim realStr As String = FormatComplexComponent(realVal, realFormat)
        Dim imagStr As String = FormatComplexComponent(Math.Abs(imagVal), imagFormat)

        Dim realZero As Boolean = (realVal = 0.0)
        Dim imagZero As Boolean = (imagVal = 0.0)

        If realZero AndAlso imagZero Then Return "0"

        If realZero Then
            ' Only imaginary part
            If imagVal < 0 Then Return $"-{imagStr}i"
            Return $"{imagStr}i"
        End If

        If imagZero Then
            ' Only real part
            Return realStr
        End If

        ' Both parts present
        If imagVal < 0 Then
            Return $"{realStr} - {imagStr}i"
        Else
            Return $"{realStr} + {imagStr}i"
        End If
    End Function

    ' =========================================================================
    ' CALCULATE EXPECTED COMPLEX RESULT
    ' =========================================================================

    ''' <summary>
    ''' Calculates the locally expected result for complex arithmetic methods.
    ''' Used for verification against server response.
    ''' Returns (Real As Double, Imag As Double).
    ''' </summary>
    Public Function CalculateExpectedComplexResult(methodName As String,
                                                    c1Real As Double, c1Imag As Double,
                                                    c2Real As Double, c2Imag As Double) As (Real As Double, Imag As Double)
        Select Case methodName.ToLower()
            Case "addcomplex"
                Return (c1Real + c2Real, c1Imag + c2Imag)

            Case "subtractcomplex"
                Return (c1Real - c2Real, c1Imag - c2Imag)

            Case "multiplycomplex"
                ' (a+bi)(c+di) = (ac-bd) + (ad+bc)i
                Return (c1Real * c2Real - c1Imag * c2Imag,
                        c1Real * c2Imag + c1Imag * c2Real)

            Case "dividecomplex"
                ' (a+bi)/(c+di) = ((ac+bd)/(c^2+d^2)) + ((bc-ad)/(c^2+d^2))i
                Dim denom As Double = c2Real * c2Real + c2Imag * c2Imag
                If denom = 0.0 Then Return (0.0, 0.0)
                Return ((c1Real * c2Real + c1Imag * c2Imag) / denom,
                        (c1Imag * c2Real - c1Real * c2Imag) / denom)

            Case Else
                Return (0.0, 0.0)
        End Select
    End Function

    ' =========================================================================
    ' PARSE COMPLEX RESULT STRING (from Fortran server)
    ' =========================================================================

    ''' <summary>
    ''' Parses a complex number string returned by the Fortran server.
    ''' Handles: "real + imagi", "real - imagi", "imagi" (real=0), "real" (imag=0).
    ''' Returns (Success, RealVal, ImagVal).
    ''' </summary>
    Public Function ParseServerComplexResult(serverStr As String) As (Success As Boolean, RealVal As Double, ImagVal As Double)
        If String.IsNullOrWhiteSpace(serverStr) Then Return (False, 0, 0)

        Dim s As String = serverStr.Trim()

        ' Case: pure imaginary — ends with i and has no operator after position 0
        ' e.g. "45.67i" or "-45.67i"
        If s.EndsWith("i", StringComparison.OrdinalIgnoreCase) Then
            ' Try to find operator splitting real and imaginary parts
            ' Scan right-to-left for + or - not preceded by E/e
            Dim splitIdx As Integer = -1
            Dim splitOp As String = ""
            For idx As Integer = s.Length - 2 To 1 Step -1   ' -2 to skip trailing i
                Dim c As Char = s(idx)
                If c = "+"c OrElse c = "-"c Then
                    Dim prev As Char = s(idx - 1)
                    If prev <> "E"c AndAlso prev <> "e"c Then
                        splitIdx = idx
                        splitOp = c.ToString()
                        Exit For
                    End If
                End If
            Next

            If splitIdx < 0 Then
                ' Pure imaginary: no real part
                Dim imagOnly As String = s.Substring(0, s.Length - 1).Trim()
                Dim imagOnlyVal As Double
                If Double.TryParse(imagOnly, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, imagOnlyVal) Then
                    Return (True, 0.0, imagOnlyVal)
                End If
                Return (False, 0, 0)
            End If

            ' Has both real and imaginary parts
            Dim realStr As String = s.Substring(0, splitIdx).Trim()
            Dim imagStr As String = s.Substring(splitIdx + 1, s.Length - splitIdx - 2).Trim()
            If splitOp = "-" Then imagStr = "-" & imagStr

            Dim realVal As Double, imagVal As Double
            If Double.TryParse(realStr, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, realVal) AndAlso
               Double.TryParse(imagStr, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, imagVal) Then
                Return (True, realVal, imagVal)
            End If
            Return (False, 0, 0)
        End If

        ' Case: pure real — no imaginary component
        Dim pureReal As Double
        If Double.TryParse(s, System.Globalization.NumberStyles.Float,
                           System.Globalization.CultureInfo.InvariantCulture, pureReal) Then
            Return (True, pureReal, 0.0)
        End If

        Return (False, 0, 0)
    End Function


End Module

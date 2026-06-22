' =========================================================================
' ValidationModule.vb
' Client-specific validation logic for Fortran JSON-RPC Client.
' Shared validation functions (ValidateNonEmptyString, ValidateIntegerParameter,
' ValidateTwoIntegerParameters, ValidateMethodSelection, ValidateStreamForTest,
' ValidateErrorCode, EchoResult, ValidateEchoResponse, ValidateBatchAddResult)
' are provided by JSONRPCClientLibrary.ValidationModule.
' This module contains only the client-unique validation functions:
'   - Named parameters (Object return variant used by Form1)
'   - Scientific notation and real number pair validation
'   - Complex number parsing and validation
' =========================================================================
Imports System.Text.Json
Imports System.Text.RegularExpressions
Public Module ValidationModule

    ' =========================================================================
    ' NAMED PARAMETERS VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Validates and parses named parameters from user input.
    ''' Returns paramsObject typed as Object (not Dictionary) so Form1 can pass it
    ''' directly to RPCOperations.SendNamedParametersAsync without a cast — the DLL's
    ''' overload accepts Dictionary(Of String, Object); late binding handles the rest.
    ''' </summary>
    Public Function ValidateNamedParameters(input As String, ByRef paramsObject As Object) As (IsValid As Boolean, ErrorMessage As String)
        Try
            input = input.Trim()

            If String.IsNullOrWhiteSpace(input) Then
                Return (False, "Parameters cannot be empty")
            End If

            ' Normalize boolean values
            Dim normalizedInput As String = input
            normalizedInput = Regex.Replace(normalizedInput, "\bTRUE\b", "true", RegexOptions.IgnoreCase)
            normalizedInput = Regex.Replace(normalizedInput, "\bFALSE\b", "false", RegexOptions.IgnoreCase)

            Dim jsonString As String = "{" & normalizedInput & "}"

            Try
                Using jsonDoc As JsonDocument = JsonDocument.Parse(jsonString)

                    If jsonDoc.RootElement.ValueKind <> JsonValueKind.Object Then
                        Return (False, "Parameters must be in object format: ""key"":value")
                    End If

                    Dim paramDict As New Dictionary(Of String, Object)
                    For Each prop In jsonDoc.RootElement.EnumerateObject()
                        Select Case prop.Value.ValueKind
                            Case JsonValueKind.Number
                                Dim intValue As Integer
                                If prop.Value.TryGetInt32(intValue) Then
                                    paramDict(prop.Name) = intValue
                                Else
                                    paramDict(prop.Name) = prop.Value.GetDouble()
                                End If
                            Case JsonValueKind.String
                                paramDict(prop.Name) = prop.Value.GetString()
                            Case JsonValueKind.True
                                paramDict(prop.Name) = True
                            Case JsonValueKind.False
                                paramDict(prop.Name) = False
                            Case JsonValueKind.Null
                                paramDict(prop.Name) = Nothing
                            Case Else
                                paramDict(prop.Name) = prop.Value.GetRawText()
                        End Select
                    Next

                    If paramDict.Count = 0 Then
                        Return (False, "At least one named parameter is required")
                    End If

                    paramsObject = paramDict
                    Return (True, String.Empty)

                End Using

            Catch jsonEx As JsonException
                Return (False, $"Invalid JSON format: {jsonEx.Message}")
            End Try

        Catch ex As Exception
            Return (False, $"Validation error: {ex.Message}")
        End Try
    End Function

    ' =========================================================================
    ' REAL NUMBER VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Validates and parses a single real number parameter.
    ''' Accepts plain decimal [-]####.## or scientific [-]####.##E[-]##.
    ''' Returns (IsValid, Value, ErrorMessage).
    ''' </summary>
    Public Function ValidateScientificNotation(input As String, paramName As String) As (IsValid As Boolean, Value As Double, ErrorMessage As String)
        If String.IsNullOrWhiteSpace(input) Then
            Return (False, 0, $"{paramName} cannot be empty.")
        End If

        Dim s As String = input.Trim()
        Dim pattern As String = "^(-?)(0|[1-9][0-9]{0,3})\.([0-9]{1,2})(?:[Ee]([+-]?)([0-9]{2}))?$"
        Dim m As System.Text.RegularExpressions.Match =
            System.Text.RegularExpressions.Regex.Match(s, pattern)

        If Not m.Success Then
            Return (False, 0,
                $"{paramName} ""{input}"" is not a valid real number." & vbCrLf &
                "Accepted formats:" & vbCrLf &
                "  Plain decimal  : ####.# or ####.##  (e.g. 1234.5, -99.75, 0.1)" & vbCrLf &
                "  Scientific     : ####.#E## or ####.##E##  (e.g. 12.34E03, -9.9E-01)" & vbCrLf &
                "  Integer part   : 0 to 9999, no leading zeros" & vbCrLf &
                "  Fractional     : 1 or 2 digits (decimal point mandatory)" & vbCrLf &
                "  Exponent       : exactly 2 digits, range -99 to 99")
        End If

        Dim hasExponent As Boolean = m.Groups(4).Success OrElse m.Groups(5).Success
        Dim mantissaSign As String = m.Groups(1).Value
        Dim intPart      As String = m.Groups(2).Value
        Dim fracPart     As String = m.Groups(3).Value
        Dim expSign      As String = m.Groups(4).Value
        Dim expDigits    As String = m.Groups(5).Value

        Dim parseStr As String
        If hasExponent Then
            Dim expVal As Integer = Integer.Parse(expDigits)
            Dim expSignChar As String = If(expSign = "-", "-", "")
            parseStr = $"{mantissaSign}{intPart}.{fracPart}E{expSignChar}{expVal}"
        Else
            parseStr = $"{mantissaSign}{intPart}.{fracPart}"
        End If

        Dim value As Double
        If Not Double.TryParse(parseStr,
                               System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture,
                               value) Then
            Return (False, 0, $"{paramName} ""{input}"" could not be converted to a number.")
        End If

        Dim mantissaValue As Double
        Double.TryParse($"{mantissaSign}{intPart}.{fracPart}",
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        mantissaValue)

        If Math.Abs(mantissaValue) > 9999.99 Then
            Return (False, 0,
                $"{paramName} mantissa {mantissaValue} is out of range. " &
                "Mantissa must be between -9999.99 and 9999.99.")
        End If

        Return (True, value, String.Empty)
    End Function

    ''' <summary>
    ''' Validates and parses two real number parameters.
    ''' </summary>
    Public Function ValidateTwoRealParameters(param1Text As String, param2Text As String,
                                              ByRef real1 As Double, ByRef real2 As Double) As (IsValid As Boolean, ErrorMessage As String)
        If String.IsNullOrWhiteSpace(param1Text) OrElse String.IsNullOrWhiteSpace(param2Text) Then
            Return (False, "Both parameters must be provided.")
        End If

        Dim result1 = ValidateScientificNotation(param1Text, "Parameter 1")
        If Not result1.IsValid Then Return (False, result1.ErrorMessage)
        real1 = result1.Value

        Dim result2 = ValidateScientificNotation(param2Text, "Parameter 2")
        If Not result2.IsValid Then Return (False, result2.ErrorMessage)
        real2 = result2.Value

        Return (True, String.Empty)
    End Function

    ' =========================================================================
    ' COMPLEX NUMBER VALIDATION
    ' =========================================================================

    ''' <summary>
    ''' Strips parentheses from the input and normalizes internal whitespace.
    ''' Accepts "(X + Yi)" or "(X+Yi)" — parens required, spaces optional.
    ''' Returns the inner content trimmed, or empty string if parens are missing.
    ''' </summary>
    Private Function StripAndNormalizeParens(input As String, paramName As String) As (IsValid As Boolean, Inner As String, ErrorMessage As String)
        Dim s As String = input.Trim()

        If Not s.StartsWith("(") OrElse Not s.EndsWith(")") Then
            Return (False, "",
                $"{paramName} ""{input}"" is not a valid complex number." & vbCrLf &
                "Complex numbers must be enclosed in parentheses." & vbCrLf &
                "Examples: (43 + 16i),  (12.34 - 5.67i),  (-4 + 23i),  (1.5E+03 - 2.0E-01i)")
        End If

        ' Strip parens and trim inner whitespace
        Dim inner As String = s.Substring(1, s.Length - 2).Trim()
        Return (True, inner, String.Empty)
    End Function

    ''' <summary>
    ''' Normalizes a scientific exponent token to always include an explicit sign.
    ''' E04 -> E+04,  E-04 stays E-04,  E+04 stays E+04.
    ''' Operates on the unsigned magnitude token (sign already stripped).
    ''' </summary>
    Private Function NormalizeExponent(token As String) As String
        ' Match E or e followed by optional sign and exactly 2 digits
        Return Regex.Replace(token, "([Ee])([0-9]{2}$)", "$1+$2")
    End Function

    ''' <summary>
    ''' Classifies a numeric token as "integer", "real", or "scientific".
    ''' Token must be stripped of its leading sign before calling.
    ''' Accepts exponent with or without explicit + sign: E04 and E+04 both match scientific.
    ''' Returns empty string if unrecognized.
    ''' </summary>
    Private Function ClassifyNumericToken(token As String) As String
        ' Scientific: ####.##E##, ####.##E+##, ####.##E-##  (exponent sign optional for +)
        If Regex.IsMatch(token, "^(0|[1-9][0-9]{0,3})\.[0-9]{1,2}[Ee][+-]?[0-9]{2}$") Then
            Return "scientific"
        End If
        ' Real: ####.##
        If Regex.IsMatch(token, "^(0|[1-9][0-9]{0,3})\.[0-9]{1,2}$") Then
            Return "real"
        End If
        ' Integer: 0 to 9999, no leading zeros
        If Regex.IsMatch(token, "^(0|[1-9][0-9]{0,3})$") Then
            Return "integer"
        End If
        Return ""
    End Function

    ''' <summary>
    ''' Parses one component of a complex number (real or imaginary part).
    ''' expectImaginary=True strips and requires trailing i/I.
    ''' Returns (IsValid, Value, Format, NormalizedToken, ErrorMessage).
    ''' NormalizedToken is the unsigned magnitude with exponent sign normalized to explicit +/-,
    ''' suitable for assembling the canonical string sent to the Fortran server.
    ''' </summary>
    Private Function ParseComplexComponent(token As String, componentName As String,
                                           expectImaginary As Boolean) As (IsValid As Boolean, Value As Double, Format As String, NormalizedToken As String, ErrorMessage As String)
        Dim s As String = token.Trim()

        If expectImaginary Then
            If s.EndsWith("i", StringComparison.OrdinalIgnoreCase) Then
                s = s.Substring(0, s.Length - 1).TrimEnd()
            Else
                Return (False, 0, "", "", $"{componentName} is missing the required 'i' suffix.")
            End If
        End If

        Dim sign As String = ""
        If s.StartsWith("-") Then
            sign = "-"
            s = s.Substring(1).TrimStart()
        ElseIf s.StartsWith("+") Then
            s = s.Substring(1).TrimStart()
        End If

        Dim fmt As String = ClassifyNumericToken(s)
        If fmt = "" Then
            Return (False, 0, "", "",
                $"{componentName} ""{token.Trim()}"" is not valid." & vbCrLf &
                "  Integer   : 0 to 9999           (e.g. 42)" & vbCrLf &
                "  Real      : ####.##             (e.g. 12.34)" & vbCrLf &
                "  Scientific: ####.##E## or ####.##E+## or ####.##E-##  (e.g. 12.34E+03, 9.9E-01)")
        End If

        ' Normalize exponent sign to always explicit (E04 -> E+04)
        Dim normalizedMagnitude As String = If(fmt = "scientific", NormalizeExponent(s), s)

        Dim parseStr As String = sign & normalizedMagnitude
        Dim value As Double
        If Not Double.TryParse(parseStr,
                               System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture,
                               value) Then
            Return (False, 0, "", "", $"{componentName} ""{token.Trim()}"" could not be converted to a number.")
        End If

        Return (True, value, fmt, normalizedMagnitude, String.Empty)
    End Function

    ''' <summary>
    ''' Validates a single complex number string in the format (X + Yi) or (X - Yi).
    ''' Parentheses are required. Spaces around the operator are normalized internally.
    ''' Both components are required. Trailing i on the imaginary part is required.
    ''' Returns (IsValid, RealPart, ImagPart, RealFormat, ImagFormat, Canonical, ErrorMessage).
    ''' Canonical is the normalized string with parens stripped and exponent signs explicit,
    ''' ready to send to the Fortran server: "43 + 16i" or "12.34E+03 - 5.67E-01i".
    ''' </summary>
    Public Function ValidateComplexNumber(input As String, paramName As String) As (IsValid As Boolean, RealPart As Double, ImagPart As Double, RealFormat As String, ImagFormat As String, Canonical As String, ErrorMessage As String)

        If String.IsNullOrWhiteSpace(input) Then
            Return (False, 0, 0, "", "", "", $"{paramName} cannot be empty.")
        End If

        ' Step 1: strip and validate parentheses
        Dim parenResult = StripAndNormalizeParens(input, paramName)
        If Not parenResult.IsValid Then
            Return (False, 0, 0, "", "", "", parenResult.ErrorMessage)
        End If

        Dim s As String = parenResult.Inner

        ' Step 2: find the operator splitting real and imaginary parts.
        ' Scan right-to-left for + or - that is NOT an exponent sign (not preceded by E/e).
        ' Spaces have been preserved inside the parens so the scan works on the raw inner string.
        Dim splitIdx As Integer = -1
        Dim splitOp As String = ""
        For idx As Integer = s.Length - 1 To 1 Step -1
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
            Return (False, 0, 0, "", "", "",
                $"{paramName} ""{input}"" is not a valid complex number." & vbCrLf &
                "Format: (realPart + imagParti)  or  (realPart - imagParti)" & vbCrLf &
                "Both components and the +/- separator are required." & vbCrLf &
                "Examples: (43 + 16i),  (12.34 - 5.67i),  (-4 + 23i),  (1.5E+03 - 2.0E-01i)")
        End If

        Dim realToken As String = s.Substring(0, splitIdx).Trim()
        Dim imagToken As String = s.Substring(splitIdx + 1).Trim()

        ' Apply operator sign to imaginary token for parsing
        If splitOp = "-" Then
            imagToken = "-" & imagToken
        End If

        ' Step 3: parse and classify each component
        Dim realResult = ParseComplexComponent(realToken, $"{paramName} real part", False)
        If Not realResult.IsValid Then
            Return (False, 0, 0, "", "", "", realResult.ErrorMessage)
        End If

        Dim imagResult = ParseComplexComponent(imagToken, $"{paramName} imaginary part", True)
        If Not imagResult.IsValid Then
            Return (False, 0, 0, "", "", "", imagResult.ErrorMessage)
        End If

        ' Step 4: assemble canonical string (no parens, normalized exponents, explicit spaces).
        ' realResult.NormalizedToken and imagResult.NormalizedToken are unsigned magnitudes.
        ' The sign of the real part is recovered from realResult.Value.
        ' The operator between parts is determined by the sign of imagResult.Value.
        Dim realSign As String = If(realResult.Value < 0, "-", "")
        Dim imagOp As String = If(imagResult.Value < 0, " - ", " + ")
        Dim canonical As String = $"{realSign}{realResult.NormalizedToken}{imagOp}{imagResult.NormalizedToken}i"

        Return (True, realResult.Value, imagResult.Value,
                realResult.Format, imagResult.Format, canonical, String.Empty)
    End Function

    ''' <summary>
    ''' Validates two complex number parameters.
    ''' Format validation only — no method-awareness, no division-by-zero check.
    ''' Division-by-zero guard for dividecomplex must be applied by the caller (Form1.vb)
    ''' after this function returns, using c2Real and c2Imag.
    ''' Promotion hierarchy for result display format: scientific > real > integer.
    ''' </summary>
    Public Function ValidateTwoComplexParameters(
            param1Text As String, param2Text As String,
            ByRef c1Real As Double, ByRef c1Imag As Double,
            ByRef c2Real As Double, ByRef c2Imag As Double,
            ByRef c1Canonical As String, ByRef c2Canonical As String,
            ByRef realFormat As String, ByRef imagFormat As String) As (IsValid As Boolean, ErrorMessage As String)

        If String.IsNullOrWhiteSpace(param1Text) OrElse String.IsNullOrWhiteSpace(param2Text) Then
            Return (False, "Both parameters must be provided.")
        End If

        Dim r1 = ValidateComplexNumber(param1Text, "Parameter 1")
        If Not r1.IsValid Then Return (False, r1.ErrorMessage)

        Dim r2 = ValidateComplexNumber(param2Text, "Parameter 2")
        If Not r2.IsValid Then Return (False, r2.ErrorMessage)

        c1Real = r1.RealPart
        c1Imag = r1.ImagPart
        c2Real = r2.RealPart
        c2Imag = r2.ImagPart
        c1Canonical = r1.Canonical
        c2Canonical = r2.Canonical
        realFormat = PromoteFormat(r1.RealFormat, r2.RealFormat)
        imagFormat = PromoteFormat(r1.ImagFormat, r2.ImagFormat)

        Return (True, String.Empty)
    End Function

    ''' <summary>
    ''' Returns the promoted format: scientific > real > integer.
    ''' </summary>
    Private Function PromoteFormat(fmt1 As String, fmt2 As String) As String
        If fmt1 = "scientific" OrElse fmt2 = "scientific" Then Return "scientific"
        If fmt1 = "real" OrElse fmt2 = "real" Then Return "real"
        Return "integer"
    End Function


End Module

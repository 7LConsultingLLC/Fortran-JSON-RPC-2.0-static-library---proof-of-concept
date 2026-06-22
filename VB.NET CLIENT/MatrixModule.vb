Imports System.Text
Imports System.Linq
Imports StreamJsonRpc
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

''' <summary>
''' Module for handling matrix operations, data generation, and UI management
''' </summary>
Public Module MatrixModule

    ' ============================================================
    ' RESPONSE CLASSES
    ' ============================================================

    ''' <summary>
    ''' Generic response class for matrix operations
    ''' </summary>
    Public Class MatrixOperationResponse
        Public Property datatype As String
        Public Property rows As Integer
        Public Property columns As Integer
        Public Property ordering As String
        Public Property matrix As Object  ' Will be cast based on datatype
    End Class

    ''' <summary>
    ''' Validation result structure
    ''' </summary>
    Public Class ValidationResult
        Public Property IsValid As Boolean
        Public Property ErrorMessage As String

        Public Sub New(isValid As Boolean, Optional errorMessage As String = "")
            Me.IsValid = isValid
            Me.ErrorMessage = errorMessage
        End Sub
    End Class

    ' ============================================================
    ' INITIALIZATION METHODS
    ' ============================================================

    ''' <summary>
    ''' Initialize a matrix of labels in a TableLayoutPanel
    ''' </summary>
    Public Sub InitializeMatrixLabels(tableLayoutPanel As TableLayoutPanel,
                                      labelArray(,) As Label,
                                      labelPrefix As String,
                                      Optional logCallback As Action(Of String) = Nothing)
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)

            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    labelArray(row, col) = New Label() With {
                        .Name = $"{labelPrefix}{row}{col}",
                        .Text = $"({row},{col})",
                        .TextAlign = ContentAlignment.MiddleCenter,
                        .Dock = DockStyle.Fill,
                        .AutoSize = False,
                        .BorderStyle = BorderStyle.FixedSingle,
                        .BackColor = Color.White
                    }
                    tableLayoutPanel.Controls.Add(labelArray(row, col), col, row)
                Next
            Next

            logCallback?.Invoke($"Matrix labels initialized successfully ({labelPrefix})")

        Catch ex As Exception
            logCallback?.Invoke($"ERROR initializing matrix labels: {ex.Message}")
            Throw
        End Try
    End Sub

    ' ============================================================
    ' DATA GENERATION METHODS
    ' ============================================================

    ''' <summary>
    ''' Generate random matrix data based on the selected data type
    ''' </summary>
    Public Sub GenerateMatrixData(dataType As String,
                                  labelArray(,) As Label,
                                  rng As Random,
                                  Optional logCallback As Action(Of String) = Nothing)
        Try
            Dim selectedType As String = dataType?.ToLower()
            If String.IsNullOrEmpty(selectedType) Then Return

            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)

            Select Case selectedType
                Case "integer"
                    GenerateIntegerMatrix(labelArray, rows, cols, rng, logCallback)
                Case "real"
                    GenerateRealMatrix(labelArray, rows, cols, rng, logCallback)
                Case "complex"
                    GenerateComplexMatrix(labelArray, rows, cols, rng, logCallback)
                Case "logical/boolean"
                    GenerateLogicalMatrix(labelArray, rows, cols, rng, logCallback)
                Case "character/text"
                    GenerateCharacterMatrix(labelArray, rows, cols, rng, logCallback)
                Case "double"
                    logCallback?.Invoke($"Matrix data type '{selectedType}' selected")
                Case Else
                    logCallback?.Invoke($"Unknown matrix data type: {selectedType}")
            End Select

        Catch ex As Exception
            logCallback?.Invoke($"ERROR generating matrix data: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub GenerateIntegerMatrix(labelArray(,) As Label, rows As Integer, cols As Integer, rng As Random, logCallback As Action(Of String))
        logCallback?.Invoke("Generating random integer matrix (0-100)...")
        For row As Integer = 0 To rows
            For col As Integer = 0 To cols
                labelArray(row, col).Text = rng.Next(0, 101).ToString()
            Next
        Next
        logCallback?.Invoke("Random integer matrix generated successfully")
    End Sub

    Private Sub GenerateRealMatrix(labelArray(,) As Label, rows As Integer, cols As Integer, rng As Random, logCallback As Action(Of String))
        logCallback?.Invoke("Generating random real matrix (0.00-100.99)...")
        For row As Integer = 0 To rows
            For col As Integer = 0 To cols
                Dim integerPart As Integer = rng.Next(0, 101)
                Dim decimalPart As Double = rng.Next(0, 100) / 100.0
                Dim randomValue As Double = integerPart + decimalPart
                labelArray(row, col).Text = randomValue.ToString("F2")
            Next
        Next
        logCallback?.Invoke("Random real matrix generated successfully")
    End Sub

    Private Sub GenerateComplexMatrix(labelArray(,) As Label, rows As Integer, cols As Integer, rng As Random, logCallback As Action(Of String))
        logCallback?.Invoke("Generating random complex matrix (0.00-100.99 + 0.00-100.99i)...")
        For row As Integer = 0 To rows
            For col As Integer = 0 To cols
                Dim realIntPart As Integer = rng.Next(0, 101)
                Dim realDecPart As Double = rng.Next(0, 100) / 100.0
                Dim realPart As Double = realIntPart + realDecPart
                Dim imagIntPart As Integer = rng.Next(0, 101)
                Dim imagDecPart As Double = rng.Next(0, 100) / 100.0
                Dim imagPart As Double = imagIntPart + imagDecPart
                labelArray(row, col).Text = $"({realPart.ToString("F2")}+{imagPart.ToString("F2")}i)"
            Next
        Next
        logCallback?.Invoke("Random complex matrix generated successfully")
    End Sub

    Private Sub GenerateLogicalMatrix(labelArray(,) As Label, rows As Integer, cols As Integer, rng As Random, logCallback As Action(Of String))
        logCallback?.Invoke("Generating random logical matrix (true/false)...")
        For row As Integer = 0 To rows
            For col As Integer = 0 To cols
                Dim randomBoolean As Boolean = rng.Next(0, 2) = 1
                labelArray(row, col).Text = If(randomBoolean, "true", "false")
            Next
        Next
        logCallback?.Invoke("Random logical matrix generated successfully")
    End Sub

    Private Sub GenerateCharacterMatrix(labelArray(,) As Label, rows As Integer, cols As Integer, rng As Random, logCallback As Action(Of String))
        logCallback?.Invoke("Generating random character matrix (e.g., 'a45', 'dog4')...")
        For row As Integer = 0 To rows
            For col As Integer = 0 To cols
                Dim randomString As String = GenerateRandomCharacterString(rng)
                labelArray(row, col).Text = $"'{randomString}'"
            Next
        Next
        logCallback?.Invoke("Random character matrix generated successfully")
    End Sub

    Public Function GenerateRandomCharacterString(rng As Random) As String
        Dim result As New StringBuilder()
        Dim firstLetter As Char = Chr(rng.Next(97, 123))
        result.Append(firstLetter)
        Dim totalLength As Integer = rng.Next(1, 7)
        Dim remainingChars As Integer = totalLength - 1

        If remainingChars > 0 Then
            Dim maxLetters As Integer = Math.Min(3, remainingChars)
            Dim maxNumbers As Integer = Math.Min(3, remainingChars)
            Dim numLetters As Integer = rng.Next(0, Math.Min(maxLetters + 1, remainingChars + 1))
            Dim numNumbers As Integer = rng.Next(0, Math.Min(maxNumbers + 1, remainingChars - numLetters + 1))
            Dim chars As New List(Of Char)

            For i As Integer = 1 To numLetters
                chars.Add(Chr(rng.Next(97, 123)))
            Next
            For i As Integer = 1 To numNumbers
                chars.Add(Chr(rng.Next(48, 58)))
            Next

            For i As Integer = chars.Count - 1 To 1 Step -1
                Dim j As Integer = rng.Next(0, i + 1)
                Dim temp As Char = chars(i)
                chars(i) = chars(j)
                chars(j) = temp
            Next

            For Each c As Char In chars
                result.Append(c)
            Next
        End If

        Return result.ToString()
    End Function

    ' ============================================================
    ' VALIDATION METHODS
    ' ============================================================

    Public Function ValidateMatrixIntegerData(labelArray(,) As Label) As ValidationResult
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)

            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    Dim cellValue As String = labelArray(row, col).Text.Trim()
                    If String.IsNullOrWhiteSpace(cellValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) is empty")
                    End If
                    If cellValue.StartsWith("(") AndAlso cellValue.EndsWith(")") Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains placeholder text: {cellValue}")
                    End If
                    Dim intValue As Integer
                    If Not Integer.TryParse(cellValue, intValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains non-integer value: {cellValue}")
                    End If
                Next
            Next
            Return New ValidationResult(True)
        Catch ex As Exception
            Return New ValidationResult(False, $"Validation error: {ex.Message}")
        End Try
    End Function

    Public Function ValidateMatrixRealData(labelArray(,) As Label) As ValidationResult
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)

            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    Dim cellValue As String = labelArray(row, col).Text.Trim()

                    ' Check if empty
                    If String.IsNullOrWhiteSpace(cellValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) is empty")
                    End If

                    ' Check if placeholder text
                    If cellValue.StartsWith("(") AndAlso cellValue.EndsWith(")") Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains placeholder text: {cellValue}")
                    End If

                    ' Try to parse as double (allows integers too)
                    Dim doubleValue As Double
                    If Not Double.TryParse(cellValue, doubleValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains non-numeric value: {cellValue}")
                    End If
                Next
            Next

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Validation error: {ex.Message}")
        End Try
    End Function

    Public Function ValidateMatrixDimensions(labelArray(,) As Label, expectedRows As Integer, expectedCols As Integer) As ValidationResult
        Try
            Dim actualRows As Integer = labelArray.GetUpperBound(0) + 1
            Dim actualCols As Integer = labelArray.GetUpperBound(1) + 1
            If actualRows <> expectedRows OrElse actualCols <> expectedCols Then
                Return New ValidationResult(False, $"Matrix dimension mismatch. Expected: {expectedRows}×{expectedCols}, Actual: {actualRows}×{actualCols}")
            End If
            Return New ValidationResult(True)
        Catch ex As Exception
            Return New ValidationResult(False, $"Dimension validation error: {ex.Message}")
        End Try
    End Function

    Public Function ValidateComboBoxSelections(dataTypeCombo As ComboBox, operationCombo As ComboBox, orderingCombo As ComboBox) As ValidationResult
        If dataTypeCombo.SelectedItem Is Nothing Then
            Return New ValidationResult(False, "Please select a data type")
        End If
        If operationCombo.SelectedItem Is Nothing Then
            Return New ValidationResult(False, "Please select an operation")
        End If
        If orderingCombo.SelectedItem Is Nothing Then
            Return New ValidationResult(False, "Please select an ordering")
        End If
        Return New ValidationResult(True)
    End Function

    Public Function ValidateMatrixNotEmpty(labelArray(,) As Label) As ValidationResult
        Dim rows As Integer = labelArray.GetUpperBound(0)
        Dim cols As Integer = labelArray.GetUpperBound(1)
        Dim hasData As Boolean = False

        For row As Integer = 0 To rows
            For col As Integer = 0 To cols
                Dim cellValue As String = labelArray(row, col).Text.Trim()

                ' Skip empty cells
                If String.IsNullOrWhiteSpace(cellValue) Then
                    Continue For
                End If

                ' Check if it's a placeholder like "(0,0)" or "(1,2)"
                ' Placeholder has format: starts with "(", ends with ")", and contains a comma
                Dim isPlaceholder As Boolean = cellValue.StartsWith("(") AndAlso
                                               cellValue.EndsWith(")") AndAlso
                                               cellValue.Contains(",")

                ' If it's NOT a placeholder, then we have real data
                If Not isPlaceholder Then
                    hasData = True
                    Exit For
                End If
            Next
            If hasData Then Exit For
        Next

        If Not hasData Then
            Return New ValidationResult(False, "Matrix is empty. Please select a data type to generate values.")
        End If
        Return New ValidationResult(True)
    End Function

    Public Function ValidateMatrixResponse(response As MatrixOperationResponse, expectedDataType As String, expectedRows As Integer, expectedCols As Integer) As ValidationResult
        Try
            If Not response.datatype.Equals(expectedDataType, StringComparison.OrdinalIgnoreCase) Then
                Return New ValidationResult(False, $"Response data type mismatch. Expected: {expectedDataType}, Received: {response.datatype}")
            End If
            If response.rows <> expectedRows OrElse response.columns <> expectedCols Then
                Return New ValidationResult(False, $"Response dimension mismatch. Expected: {expectedRows}×{expectedCols}, Received: {response.rows}×{response.columns}")
            End If
            Dim matrixArray As Integer() = TryCast(response.matrix, Integer())
            If matrixArray Is Nothing Then
                Return New ValidationResult(False, "Response matrix data is not in integer array format")
            End If
            Dim expectedCount As Integer = expectedRows * expectedCols
            If matrixArray.Length <> expectedCount Then
                Return New ValidationResult(False, $"Response value count mismatch. Expected: {expectedCount}, Received: {matrixArray.Length}")
            End If
            Return New ValidationResult(True)
        Catch ex As Exception
            Return New ValidationResult(False, $"Response validation error: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Validate response for real matrix operations (no value validation, just structure)
    ''' </summary>
    Public Function ValidateMatrixRealResponse(response As MatrixOperationResponse,
                                           expectedDataType As String,
                                           expectedRows As Integer,
                                           expectedCols As Integer) As ValidationResult
        Try
            ' Check data type
            If Not response.datatype.Equals(expectedDataType, StringComparison.OrdinalIgnoreCase) Then
                Return New ValidationResult(False,
                    $"Response data type mismatch. Expected: {expectedDataType}, Received: {response.datatype}")
            End If

            ' Check dimensions
            If response.rows <> expectedRows OrElse response.columns <> expectedCols Then
                Return New ValidationResult(False,
                    $"Response dimension mismatch. Expected: {expectedRows}×{expectedCols}, Received: {response.rows}×{response.columns}")
            End If

            ' Check matrix data exists
            If response.matrix Is Nothing Then
                Return New ValidationResult(False, "Response matrix is null")
            End If

            ' Try to get count without strict type checking
            Dim count As Integer = 0
            If TypeOf response.matrix Is JArray Then
                count = DirectCast(response.matrix, JArray).Count
            ElseIf TypeOf response.matrix Is Double() Then
                count = DirectCast(response.matrix, Double()).Length
            ElseIf TypeOf response.matrix Is Array Then
                count = DirectCast(response.matrix, Array).Length
            Else
                Return New ValidationResult(False, "Response matrix is not in array format")
            End If

            ' Check value count
            Dim expectedCount As Integer = expectedRows * expectedCols
            If count <> expectedCount Then
                Return New ValidationResult(False,
                    $"Response value count mismatch. Expected: {expectedCount}, Received: {count}")
            End If

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Response validation error: {ex.Message}")
        End Try
    End Function

    ' ============================================================
    ' EXTRACTION METHODS
    ' ============================================================

    Public Function ExtractIntegerMatrix(labelArray(,) As Label) As Integer()
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)
            Dim totalElements As Integer = (rows + 1) * (cols + 1)
            Dim matrixData(totalElements - 1) As Integer
            Dim index As Integer = 0

            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    matrixData(index) = Integer.Parse(labelArray(row, col).Text.Trim())
                    index += 1
                Next
            Next
            Return matrixData
        Catch ex As Exception
            Throw New Exception($"Error extracting integer matrix: {ex.Message}", ex)
        End Try
    End Function

    ''' <summary>
    ''' Extract real (double) values from label array in row-major order
    ''' </summary>
    Public Function ExtractRealMatrix(labelArray(,) As Label) As Double()
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)
            Dim totalElements As Integer = (rows + 1) * (cols + 1)
            Dim matrixData(totalElements - 1) As Double
            Dim index As Integer = 0

            ' Extract in row-major order
            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    matrixData(index) = Double.Parse(labelArray(row, col).Text.Trim())
                    index += 1
                Next
            Next

            Return matrixData

        Catch ex As Exception
            Throw New Exception($"Error extracting real matrix: {ex.Message}", ex)
        End Try
    End Function

    ' ============================================================
    ' DISPLAY METHODS
    ' ============================================================

    Public Sub DisplayIntegerMatrix(labelArray(,) As Label, matrixData() As Integer, rows As Integer, cols As Integer, Optional logCallback As Action(Of String) = Nothing)
        Try
            Dim expectedLength As Integer = rows * cols
            If matrixData.Length <> expectedLength Then
                Throw New ArgumentException($"Matrix data length mismatch. Expected: {expectedLength}, Received: {matrixData.Length}")
            End If
            Dim index As Integer = 0
            For row As Integer = 0 To rows - 1
                For col As Integer = 0 To cols - 1
                    labelArray(row, col).Text = matrixData(index).ToString()
                    index += 1
                Next
            Next
            logCallback?.Invoke($"Matrix displayed: {rows}×{cols} = {matrixData.Length} integer values")
        Catch ex As Exception
            logCallback?.Invoke($"ERROR displaying matrix: {ex.Message}")
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' Display real (double) matrix values in label array (row-major order)
    ''' Formats with 2 decimal places
    ''' </summary>
    Public Sub DisplayRealMatrix(labelArray(,) As Label,
                             matrixData() As Double,
                             rows As Integer,
                             cols As Integer,
                             Optional logCallback As Action(Of String) = Nothing)
        Try
            Dim expectedLength As Integer = rows * cols

            If matrixData.Length <> expectedLength Then
                Throw New ArgumentException($"Matrix data length mismatch. Expected: {expectedLength}, Received: {matrixData.Length}")
            End If

            Dim index As Integer = 0

            ' Display in row-major order with 2 decimal places
            For row As Integer = 0 To rows - 1
                For col As Integer = 0 To cols - 1
                    labelArray(row, col).Text = matrixData(index).ToString("F2")
                    index += 1
                Next
            Next

            logCallback?.Invoke($"Matrix displayed: {rows}×{cols} = {matrixData.Length} real values")

        Catch ex As Exception
            logCallback?.Invoke($"ERROR displaying matrix: {ex.Message}")
            Throw
        End Try
    End Sub

    ' ============================================================
    ' RPC OPERATIONS
    ' ============================================================

    ' ============================================================
    ' RPC OPERATIONS - GENERIC IMPLEMENTATION
    ' ============================================================

    ''' <summary>
    ''' Execute matrix integer operation on Fortran server (generic for all single-matrix operations)
    ''' Supports: transpose, copy, inverse, etc.
    ''' </summary>
    Public Async Function MatrixIntegerOperationAsync(jsonRpc As JsonRpc,
                                                   operation As String,
                                                   matrixData() As Integer,
                                                   rows As Integer,
                                                   cols As Integer,
                                                   ordering As String,
                                                   Optional logCallback As Action(Of String) = Nothing) As Task(Of MatrixOperationResponse)
        ' Build the method name: matrix{datatype}{operation}
        Dim methodName As String = $"matrixinteger{operation.ToLower()}"

        ' Build parameters object
        Dim params As New With {
            .datatype = "integer",
            .rows = rows,
            .columns = cols,
            .operation = operation.ToLower(),
            .ordering = ordering,
            .matrix = matrixData
        }

        ' Use InvokeWithParameterObjectAsync to send params as an object (not array)
        Dim result = Await jsonRpc.InvokeWithParameterObjectAsync(Of Object)(methodName, params)

        ' Parse the response
        Dim jsonResult As JObject
        If TypeOf result Is JObject Then
            jsonResult = DirectCast(result, JObject)
        Else
            jsonResult = JObject.Parse(result.ToString())
        End If

        ' Create response object
        Dim response As New MatrixOperationResponse With {
            .datatype = jsonResult("datatype").ToString(),
            .rows = CInt(jsonResult("rows")),
            .columns = CInt(jsonResult("columns")),
            .ordering = jsonResult("ordering").ToString(),
            .matrix = jsonResult("matrix").ToObject(Of Integer())()
        }

        Return response
    End Function

    ' ============================================================
    ' RPC OPERATIONS - REAL MATRIX IMPLEMENTATION
    ' ============================================================

    ''' <summary>
    ''' Execute matrix real operation on Fortran server (generic for all single-matrix operations)
    ''' Supports: copy, transpose, square, etc.
    ''' </summary>
    Public Async Function MatrixRealOperationAsync(jsonRpc As JsonRpc,
                                               operation As String,
                                               matrixData() As Double,
                                               rows As Integer,
                                               cols As Integer,
                                               ordering As String,
                                               Optional logCallback As Action(Of String) = Nothing) As Task(Of MatrixOperationResponse)
        ' Build the method name: matrix{datatype}{operation}
        Dim methodName As String = $"matrixreal{operation.ToLower()}"

        ' Round matrix data to 2 decimal places for transmission
        Dim roundedMatrix(matrixData.Length - 1) As Double
        For i As Integer = 0 To matrixData.Length - 1
            roundedMatrix(i) = Math.Round(matrixData(i), 2)
        Next

        ' Build parameters object
        Dim params As New With {
            .datatype = "real",
            .rows = rows,
            .columns = cols,
            .operation = operation.ToLower(),
            .ordering = ordering,
            .matrix = roundedMatrix
        }

        ' Use InvokeWithParameterObjectAsync to send params as an object
        Dim result = Await jsonRpc.InvokeWithParameterObjectAsync(Of Object)(methodName, params)

        ' Parse the response
        Dim jsonResult As JObject
        If TypeOf result Is JObject Then
            jsonResult = DirectCast(result, JObject)
        Else
            jsonResult = JObject.Parse(result.ToString())
        End If

        ' Create response object
        Dim response As New MatrixOperationResponse With {
            .datatype = jsonResult("datatype").ToString(),
            .rows = CInt(jsonResult("rows")),
            .columns = CInt(jsonResult("columns")),
            .ordering = jsonResult("ordering").ToString(),
            .matrix = jsonResult("matrix").ToObject(Of Double())()
        }

        Return response
    End Function

    ' ============================================================
    ' TEXT/CHARACTER MATRIX VALIDATION
    ' ============================================================

    ''' <summary>
    ''' Validate that all cells contain valid character/text strings
    ''' </summary>
    Public Function ValidateMatrixTextData(labelArray(,) As Label) As ValidationResult
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)

            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    Dim cellValue As String = labelArray(row, col).Text.Trim()

                    ' Check if empty
                    If String.IsNullOrWhiteSpace(cellValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) is empty")
                    End If

                    ' Check if placeholder text
                    If cellValue.StartsWith("(") AndAlso cellValue.EndsWith(")") Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains placeholder text: {cellValue}")
                    End If

                    ' Strip single quotes for validation
                    Dim strippedValue As String = cellValue.Trim("'"c)

                    ' Validate: Not empty after stripping quotes
                    If String.IsNullOrWhiteSpace(strippedValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains only quotes with no text")
                    End If

                    ' Validate: Length 1-6 characters
                    If strippedValue.Length < 1 OrElse strippedValue.Length > 6 Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) text length must be 1-6 characters. Found: {strippedValue.Length} ('{strippedValue}')")
                    End If

                    ' Validate: Starts with a letter (a-z, case insensitive)
                    If Not Char.IsLetter(strippedValue(0)) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) must start with a letter. Found: '{strippedValue}'")
                    End If

                    ' Validate: Contains only alphanumeric characters
                    For Each ch As Char In strippedValue
                        If Not Char.IsLetterOrDigit(ch) Then
                            Return New ValidationResult(False, $"Cell ({row},{col}) contains invalid character '{ch}'. Only letters and numbers allowed. Found: '{strippedValue}'")
                        End If
                    Next
                Next
            Next

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Validation error: {ex.Message}")
        End Try
    End Function

    ' ============================================================
    ' TEXT/CHARACTER MATRIX EXTRACTION
    ' ============================================================

    ''' <summary>
    ''' Extract text/character strings from label array in row-major order
    ''' Strips single quotes from display format
    ''' </summary>
    Public Function ExtractTextMatrix(labelArray(,) As Label) As String()
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)
            Dim totalElements As Integer = (rows + 1) * (cols + 1)
            Dim matrixData(totalElements - 1) As String
            Dim index As Integer = 0

            ' Extract in row-major order, stripping quotes
            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    Dim cellText As String = labelArray(row, col).Text.Trim()
                    ' Strip single quotes
                    matrixData(index) = cellText.Trim("'"c)
                    index += 1
                Next
            Next

            Return matrixData

        Catch ex As Exception
            Throw New Exception($"Error extracting text matrix: {ex.Message}", ex)
        End Try
    End Function

    ' ============================================================
    ' TEXT/CHARACTER MATRIX DISPLAY
    ' ============================================================

    ''' <summary>
    ''' Display text/character matrix in label array with single quotes
    ''' </summary>
    Public Sub DisplayTextMatrix(labelArray(,) As Label,
                                  matrixData() As String,
                                  rows As Integer,
                                  cols As Integer,
                                  Optional logCallback As Action(Of String) = Nothing)
        Try
            If matrixData.Length <> rows * cols Then
                Throw New ArgumentException($"Matrix data length ({matrixData.Length}) does not match dimensions ({rows}×{cols})")
            End If

            Dim index As Integer = 0
            For row As Integer = 0 To rows - 1
                For col As Integer = 0 To cols - 1
                    ' Add single quotes for display consistency
                    labelArray(row, col).Text = $"'{matrixData(index)}'"
                    index += 1
                Next
            Next

            logCallback?.Invoke($"Text matrix displayed: {rows}×{cols} = {matrixData.Length} strings")

        Catch ex As Exception
            logCallback?.Invoke($"ERROR displaying text matrix: {ex.Message}")
            Throw
        End Try
    End Sub

    ' ============================================================
    ' TEXT/CHARACTER MATRIX RPC METHODS
    ' ============================================================

    ''' <summary>
    ''' Generic text/character matrix operation via JSON-RPC
    ''' </summary>
    Public Async Function MatrixTextOperationAsync(jsonRpc As JsonRpc,
                                                    operation As String,
                                                    matrixData() As String,
                                                    rows As Integer,
                                                    cols As Integer,
                                                    ordering As String,
                                                    Optional logCallback As Action(Of String) = Nothing) As Task(Of MatrixOperationResponse)
        ' Construct method name: matrixtext{operation}
        Dim methodName As String = $"matrixtext{operation.ToLower()}"

        ' Build parameters object
        Dim params As New With {
            .datatype = "string",
            .rows = rows,
            .columns = cols,
            .operation = operation.ToLower(),
            .ordering = ordering.ToLower(),
            .matrix = matrixData
        }

        ' Invoke with named parameters (object, not array)
        Dim result As Object = Await jsonRpc.InvokeWithParameterObjectAsync(Of Object)(methodName, params)

        ' Parse response
        Dim jsonResult As JObject
        If TypeOf result Is JObject Then
            jsonResult = DirectCast(result, JObject)
        Else
            jsonResult = JObject.Parse(result.ToString())
        End If

        ' Create response object
        Dim response As New MatrixOperationResponse With {
            .datatype = jsonResult("datatype").ToString(),
            .rows = CInt(jsonResult("rows")),
            .columns = CInt(jsonResult("columns")),
            .ordering = jsonResult("ordering").ToString(),
            .matrix = jsonResult("matrix").ToObject(Of String())()
        }

        Return response
    End Function

    ' ============================================================
    ' TEXT/CHARACTER MATRIX RESPONSE VALIDATION
    ' ============================================================

    ''' <summary>
    ''' Validate text/character matrix operation response structure only (not content)
    ''' </summary>
    Public Function ValidateMatrixTextResponse(response As MatrixOperationResponse,
                                                expectedDataType As String,
                                                expectedRows As Integer,
                                                expectedCols As Integer) As ValidationResult
        Try
            ' Check datatype
            If Not response.datatype.Equals(expectedDataType, StringComparison.OrdinalIgnoreCase) Then
                Return New ValidationResult(False,
                    $"Response data type mismatch. Expected: {expectedDataType}, Received: {response.datatype}")
            End If

            ' Check dimensions
            If response.rows <> expectedRows OrElse response.columns <> expectedCols Then
                Return New ValidationResult(False,
                    $"Response dimension mismatch. Expected: {expectedRows}×{expectedCols}, Received: {response.rows}×{response.columns}")
            End If

            ' Check matrix is string array and has correct count
            Dim count As Integer = 0
            If TypeOf response.matrix Is String() Then
                count = DirectCast(response.matrix, String()).Length
            ElseIf TypeOf response.matrix Is JArray Then
                count = DirectCast(response.matrix, JArray).Count
            ElseIf TypeOf response.matrix Is Array Then
                count = DirectCast(response.matrix, Array).Length
            Else
                Return New ValidationResult(False, "Response matrix is not in string array format")
            End If

            ' Check value count
            Dim expectedCount As Integer = expectedRows * expectedCols
            If count <> expectedCount Then
                Return New ValidationResult(False,
                    $"Response value count mismatch. Expected: {expectedCount}, Received: {count}")
            End If

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Response validation error: {ex.Message}")
        End Try
    End Function

    ' ============================================================
    ' COMPLEX MATRIX VALIDATION
    ' ============================================================

    ''' <summary>
    ''' Validate that all cells contain valid complex number format: (real±imagi)
    ''' Format: (12.34+56.78i), (12.34-56.78i), (12.34+0.00i), (0.00+56.78i)
    ''' Value ranges: real and imaginary parts must be 0.00-100.99
    ''' </summary>
    Public Function ValidateMatrixComplexData(labelArray(,) As Label) As ValidationResult
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)

            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    Dim cellValue As String = labelArray(row, col).Text.Trim()

                    ' Check if empty
                    If String.IsNullOrWhiteSpace(cellValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) is empty")
                    End If

                    ' Check if placeholder text
                    If cellValue.StartsWith("(") AndAlso cellValue.EndsWith(")") AndAlso cellValue.Contains(",") Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains placeholder text: {cellValue}")
                    End If

                    ' Validate complex number format
                    Dim validationResult = ValidateComplexNumberFormat(cellValue, row, col)
                    If Not validationResult.IsValid Then
                        Return validationResult
                    End If
                Next
            Next

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Validation error: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Validate a single complex number string format
    ''' </summary>
    Private Function ValidateComplexNumberFormat(value As String, row As Integer, col As Integer) As ValidationResult
        Try
            ' Must start with '(' and end with ')'
            If Not value.StartsWith("(") OrElse Not value.EndsWith(")") Then
                Return New ValidationResult(False, $"Cell ({row},{col}) must have format (real±imagi). Found: {value}")
            End If

            ' Must end with 'i)' for imaginary unit
            If Not value.EndsWith("i)") Then
                Return New ValidationResult(False, $"Cell ({row},{col}) must end with 'i)'. Found: {value}")
            End If

            ' Remove parentheses and 'i' suffix
            Dim inner As String = value.Substring(1, value.Length - 3) ' Remove '(' and 'i)'

            ' Find the operator position (+ or -) for the imaginary part
            ' Must search from position 1 to skip potential negative sign on real part
            Dim operatorPos As Integer = -1
            For i As Integer = 1 To inner.Length - 1
                If inner(i) = "+"c OrElse inner(i) = "-"c Then
                    operatorPos = i
                    Exit For
                End If
            Next

            If operatorPos = -1 Then
                Return New ValidationResult(False, $"Cell ({row},{col}) missing '+' or '-' operator. Found: {value}")
            End If

            ' Split into real and imaginary parts
            Dim realPart As String = inner.Substring(0, operatorPos).Trim()
            Dim imagPart As String = inner.Substring(operatorPos).Trim() ' Includes the sign

            ' Validate real part is a valid number
            Dim realValue As Double
            If Not Double.TryParse(realPart, realValue) Then
                Return New ValidationResult(False, $"Cell ({row},{col}) has invalid real part: '{realPart}'")
            End If

            ' Validate imaginary part is a valid number
            Dim imagValue As Double
            If Not Double.TryParse(imagPart, imagValue) Then
                Return New ValidationResult(False, $"Cell ({row},{col}) has invalid imaginary part: '{imagPart}'")
            End If

            ' Validate range: 0.00 to 100.99 for both real and imaginary parts
            If realValue < 0.0 OrElse realValue > 100.99 Then
                Return New ValidationResult(False, $"Cell ({row},{col}) real part must be 0.00-100.99. Found: {realValue:F2}")
            End If

            If Math.Abs(imagValue) > 100.99 Then
                Return New ValidationResult(False, $"Cell ({row},{col}) imaginary part must be -100.99 to +100.99. Found: {imagValue:F2}")
            End If

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Cell ({row},{col}) format error: {ex.Message}")
        End Try
    End Function

    ' ============================================================
    ' LOGICAL/BOOLEAN MATRIX VALIDATION
    ' ============================================================

    ''' <summary>
    ''' Validate that all cells contain valid logical/boolean values (true or false, case insensitive)
    ''' </summary>
    Public Function ValidateMatrixLogicalData(labelArray(,) As Label) As ValidationResult
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)

            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    Dim cellValue As String = labelArray(row, col).Text.Trim()

                    ' Check if empty
                    If String.IsNullOrWhiteSpace(cellValue) Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) is empty")
                    End If

                    ' Check if placeholder text
                    If cellValue.StartsWith("(") AndAlso cellValue.EndsWith(")") Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains placeholder text: {cellValue}")
                    End If

                    ' Validate: Must be "true" or "false" (case insensitive)
                    Dim upperValue As String = cellValue.ToUpper()
                    If upperValue <> "TRUE" AndAlso upperValue <> "FALSE" Then
                        Return New ValidationResult(False, $"Cell ({row},{col}) contains invalid boolean value. Expected true or false, found: '{cellValue}'")
                    End If
                Next
            Next

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Validation error: {ex.Message}")
        End Try
    End Function

    ' ============================================================
    ' LOGICAL/BOOLEAN MATRIX EXTRACTION
    ' ============================================================

    ''' <summary>
    ''' Extract logical/boolean values from label array in row-major order
    ''' Converts "TRUE"/"FALSE" strings to Boolean values
    ''' </summary>
    Public Function ExtractLogicalMatrix(labelArray(,) As Label) As Boolean()
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)
            Dim totalElements As Integer = (rows + 1) * (cols + 1)
            Dim matrixData(totalElements - 1) As Boolean
            Dim index As Integer = 0

            ' Extract in row-major order, converting to Boolean
            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    Dim cellText As String = labelArray(row, col).Text.Trim().ToUpper()
                    ' Parse TRUE/FALSE to Boolean (case insensitive)
                    matrixData(index) = (cellText = "TRUE")
                    index += 1
                Next
            Next

            Return matrixData

        Catch ex As Exception
            Throw New Exception($"Error extracting logical matrix: {ex.Message}", ex)
        End Try
    End Function

    ' ============================================================
    ' LOGICAL/BOOLEAN MATRIX DISPLAY
    ' ============================================================

    ''' <summary>
    ''' Display logical/boolean matrix in label array
    ''' Supports both Boolean arrays and Fortran string format (.true. / .false.)
    ''' </summary>
    Public Sub DisplayLogicalMatrix(labelArray(,) As Label,
                                     matrixData As Object,
                                     rows As Integer,
                                     cols As Integer,
                                     Optional logCallback As Action(Of String) = Nothing)
        Try
            Dim expectedLength As Integer = rows * cols
            Dim index As Integer = 0

            ' Check if matrixData is Boolean array or String array
            If TypeOf matrixData Is Boolean() Then
                ' Handle Boolean array - convert to lowercase "true"/"false"
                Dim boolArray() As Boolean = DirectCast(matrixData, Boolean())

                If boolArray.Length <> expectedLength Then
                    Throw New ArgumentException($"Matrix data length ({boolArray.Length}) does not match dimensions ({rows}×{cols})")
                End If

                For row As Integer = 0 To rows - 1
                    For col As Integer = 0 To cols - 1
                        labelArray(row, col).Text = If(boolArray(index), "true", "false")
                        index += 1
                    Next
                Next

                logCallback?.Invoke($"Logical matrix displayed: {rows}×{cols} = {boolArray.Length} boolean values")

            ElseIf TypeOf matrixData Is String() Then
                ' Handle String array - display Fortran format (.true. / .false.)
                Dim stringArray() As String = DirectCast(matrixData, String())

                If stringArray.Length <> expectedLength Then
                    Throw New ArgumentException($"Matrix data length ({stringArray.Length}) does not match dimensions ({rows}×{cols})")
                End If

                For row As Integer = 0 To rows - 1
                    For col As Integer = 0 To cols - 1
                        ' Display Fortran boolean string directly
                        labelArray(row, col).Text = stringArray(index)
                        index += 1
                    Next
                Next

                logCallback?.Invoke($"Logical matrix displayed: {rows}×{cols} = {stringArray.Length} Fortran boolean strings")

            Else
                Throw New ArgumentException($"Unsupported matrix data type: {matrixData.GetType().Name}. Expected Boolean() or String()")
            End If

        Catch ex As Exception
            logCallback?.Invoke($"ERROR displaying logical matrix: {ex.Message}")
            Throw
        End Try
    End Sub

    ' ============================================================
    ' LOGICAL/BOOLEAN MATRIX RPC METHODS
    ' ============================================================

    ''' <summary>
    ''' Generic logical/boolean matrix operation via JSON-RPC
    ''' Handles Fortran boolean format (.true. / .false.) in responses
    ''' </summary>
    Public Async Function MatrixLogicalOperationAsync(jsonRpc As JsonRpc,
                                                       operation As String,
                                                       matrixData() As Boolean,
                                                       rows As Integer,
                                                       cols As Integer,
                                                       ordering As String,
                                                       Optional logCallback As Action(Of String) = Nothing) As Task(Of MatrixOperationResponse)
        ' Construct method name: matrixlogical{operation}
        Dim methodName As String = $"matrixlogical{operation.ToLower()}"

        ' Build parameters object
        Dim params As New With {
            .datatype = "logical",
            .rows = rows,
            .columns = cols,
            .operation = operation.ToLower(),
            .ordering = ordering.ToLower(),
            .matrix = matrixData
        }

        ' Invoke with named parameters (object, not array)
        Dim result As Object = Await jsonRpc.InvokeWithParameterObjectAsync(Of Object)(methodName, params)

        ' Parse response
        Dim jsonResult As JObject
        If TypeOf result Is JObject Then
            jsonResult = DirectCast(result, JObject)
        Else
            jsonResult = JObject.Parse(result.ToString())
        End If

        ' Keep as String array to preserve Fortran boolean literals (.true. / .false.)
        ' rather than deserializing to VB.NET Boolean, which would lose the Fortran format.
        Dim matrixArray As JArray = DirectCast(jsonResult("matrix"), JArray)
        Dim stringMatrix(matrixArray.Count - 1) As String

        For i As Integer = 0 To matrixArray.Count - 1
            stringMatrix(i) = matrixArray(i).ToString().Trim()
        Next

        ' Create response object with String array instead of Boolean array
        Dim response As New MatrixOperationResponse With {
            .datatype = jsonResult("datatype").ToString(),
            .rows = CInt(jsonResult("rows")),
            .columns = CInt(jsonResult("columns")),
            .ordering = jsonResult("ordering").ToString(),
            .matrix = stringMatrix
        }

        Return response
    End Function

    ' ============================================================
    ' LOGICAL/BOOLEAN MATRIX RESPONSE VALIDATION
    ' ============================================================

    ''' <summary>
    ''' Validate logical/boolean matrix operation response structure only (not content)
    ''' </summary>
    Public Function ValidateMatrixLogicalResponse(response As MatrixOperationResponse,
                                                   expectedDataType As String,
                                                   expectedRows As Integer,
                                                   expectedCols As Integer) As ValidationResult
        Try
            ' Check datatype
            If Not response.datatype.Equals(expectedDataType, StringComparison.OrdinalIgnoreCase) Then
                Return New ValidationResult(False,
                    $"Response data type mismatch. Expected: {expectedDataType}, Received: {response.datatype}")
            End If

            ' Check dimensions
            If response.rows <> expectedRows OrElse response.columns <> expectedCols Then
                Return New ValidationResult(False,
                    $"Response dimension mismatch. Expected: {expectedRows}×{expectedCols}, Received: {response.rows}×{response.columns}")
            End If

            ' Check matrix is boolean array and has correct count
            Dim count As Integer = 0
            If TypeOf response.matrix Is Boolean() Then
                count = DirectCast(response.matrix, Boolean()).Length
            ElseIf TypeOf response.matrix Is JArray Then
                count = DirectCast(response.matrix, JArray).Count
            ElseIf TypeOf response.matrix Is Array Then
                count = DirectCast(response.matrix, Array).Length
            Else
                Return New ValidationResult(False, "Response matrix is not in boolean array format")
            End If

            ' Check value count
            Dim expectedCount As Integer = expectedRows * expectedCols
            If count <> expectedCount Then
                Return New ValidationResult(False,
                    $"Response value count mismatch. Expected: {expectedCount}, Received: {count}")
            End If

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Response validation error: {ex.Message}")
        End Try
    End Function

    ' ============================================================
    ' COMPLEX MATRIX EXTRACTION
    ' ============================================================

    ''' <summary>
    ''' Extract complex number strings from label array in row-major order
    ''' Keeps format as-is: (real±imagi)
    ''' </summary>
    Public Function ExtractComplexMatrix(labelArray(,) As Label) As String()
        Try
            Dim rows As Integer = labelArray.GetUpperBound(0)
            Dim cols As Integer = labelArray.GetUpperBound(1)
            Dim totalElements As Integer = (rows + 1) * (cols + 1)
            Dim matrixData(totalElements - 1) As String
            Dim index As Integer = 0

            ' Extract in row-major order, keeping complex format
            For row As Integer = 0 To rows
                For col As Integer = 0 To cols
                    matrixData(index) = labelArray(row, col).Text.Trim()
                    index += 1
                Next
            Next

            Return matrixData

        Catch ex As Exception
            Throw New Exception($"Error extracting complex matrix: {ex.Message}", ex)
        End Try
    End Function

    ' ============================================================
    ' COMPLEX MATRIX DISPLAY
    ' ============================================================

    ''' <summary>
    ''' Display complex matrix in label array as-is from server response
    ''' Format: (real±imagi)
    ''' </summary>
    Public Sub DisplayComplexMatrix(labelArray(,) As Label,
                                     matrixData() As String,
                                     rows As Integer,
                                     cols As Integer,
                                     Optional logCallback As Action(Of String) = Nothing)
        Try
            If matrixData.Length <> rows * cols Then
                Throw New ArgumentException($"Matrix data length ({matrixData.Length}) does not match dimensions ({rows}×{cols})")
            End If

            Dim index As Integer = 0
            For row As Integer = 0 To rows - 1
                For col As Integer = 0 To cols - 1
                    ' Display complex number string directly
                    labelArray(row, col).Text = matrixData(index)
                    index += 1
                Next
            Next

            logCallback?.Invoke($"Complex matrix displayed: {rows}×{cols} = {matrixData.Length} complex values")

        Catch ex As Exception
            logCallback?.Invoke($"ERROR displaying complex matrix: {ex.Message}")
            Throw
        End Try
    End Sub

    ' ============================================================
    ' COMPLEX MATRIX RPC METHODS
    ' ============================================================

    ''' <summary>
    ''' Generic complex matrix operation via JSON-RPC
    ''' Sends and receives complex numbers as strings: (real±imagi)
    ''' </summary>
    Public Async Function MatrixComplexOperationAsync(jsonRpc As JsonRpc,
                                                      operation As String,
                                                      matrixData() As String,
                                                      rows As Integer,
                                                      cols As Integer,
                                                      ordering As String,
                                                      Optional logCallback As Action(Of String) = Nothing) As Task(Of MatrixOperationResponse)
        ' Construct method name: matrixcomplex{operation}
        Dim methodName As String = $"matrixcomplex{operation.ToLower()}"

        ' Build parameters object
        Dim params As New With {
            .datatype = "complex",
            .rows = rows,
            .columns = cols,
            .operation = operation.ToLower(),
            .ordering = ordering.ToLower(),
            .matrix = matrixData
        }

        ' Invoke with named parameters (object, not array)
        Dim result As Object = Await jsonRpc.InvokeWithParameterObjectAsync(Of Object)(methodName, params)

        ' Parse response
        Dim jsonResult As JObject
        If TypeOf result Is JObject Then
            jsonResult = DirectCast(result, JObject)
        Else
            jsonResult = JObject.Parse(result.ToString())
        End If

        ' Create response object
        Dim response As New MatrixOperationResponse With {
            .datatype = jsonResult("datatype").ToString(),
            .rows = CInt(jsonResult("rows")),
            .columns = CInt(jsonResult("columns")),
            .ordering = jsonResult("ordering").ToString(),
            .matrix = jsonResult("matrix").ToObject(Of String())()
        }

        Return response
    End Function

    ' ============================================================
    ' COMPLEX MATRIX RESPONSE VALIDATION
    ' ============================================================

    ''' <summary>
    ''' Validate complex matrix operation response structure only (not content)
    ''' </summary>
    Public Function ValidateMatrixComplexResponse(response As MatrixOperationResponse,
                                                   expectedDataType As String,
                                                   expectedRows As Integer,
                                                   expectedCols As Integer) As ValidationResult
        Try
            ' Check datatype
            If Not response.datatype.Equals(expectedDataType, StringComparison.OrdinalIgnoreCase) Then
                Return New ValidationResult(False,
                    $"Response data type mismatch. Expected: {expectedDataType}, Received: {response.datatype}")
            End If

            ' Check dimensions
            If response.rows <> expectedRows OrElse response.columns <> expectedCols Then
                Return New ValidationResult(False,
                    $"Response dimension mismatch. Expected: {expectedRows}×{expectedCols}, Received: {response.rows}×{response.columns}")
            End If

            ' Check matrix is string array and has correct count
            Dim count As Integer = 0
            If TypeOf response.matrix Is String() Then
                count = DirectCast(response.matrix, String()).Length
            ElseIf TypeOf response.matrix Is JArray Then
                count = DirectCast(response.matrix, JArray).Count
            ElseIf TypeOf response.matrix Is Array Then
                count = DirectCast(response.matrix, Array).Length
            Else
                Return New ValidationResult(False, "Response matrix is not in string array format")
            End If

            ' Check value count
            Dim expectedCount As Integer = expectedRows * expectedCols
            If count <> expectedCount Then
                Return New ValidationResult(False,
                    $"Response value count mismatch. Expected: {expectedCount}, Received: {count}")
            End If

            Return New ValidationResult(True)

        Catch ex As Exception
            Return New ValidationResult(False, $"Response validation error: {ex.Message}")
        End Try
    End Function

End Module

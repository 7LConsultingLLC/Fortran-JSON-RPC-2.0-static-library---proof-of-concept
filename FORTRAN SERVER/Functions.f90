!****************************************************************************
!
!  MODULE: Functions (Parent Module)
!
!  PURPOSE:  Parent module containing common utilities and interfaces
!            Actual handler implementations are in submodules
!
!****************************************************************************
module Functions
    use json_module
    use jsonrpc_helpers
    use JsonRpcErrorCodes
    use jsonrpc_server_state
    use ifwin
    use ifwinty
    implicit none

    ! ========================================================================
    ! MODULE CONSTANTS
    ! ========================================================================
    integer, parameter :: BASE_RES  = 2000   ! Base grid resolution (points per axis)
    integer, parameter :: BASE_ITER = 5000   ! Base maximum iterations per point
    ! CRLF is used in LSP-style message framing: Content-Length header is
    ! followed by CRLF CRLF, then the JSON body.
    character(len=2), parameter :: CRLF = char(13)//char(10)

    ! ========================================================================
    ! INTERFACES - All handler subroutines
    ! ========================================================================
    interface
        module subroutine AddInt_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine AddInt_handler

        module subroutine SubtractInt_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine SubtractInt_handler

        module subroutine MultiplyInt_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MultiplyInt_handler

        module subroutine DivideInt_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine DivideInt_handler

        module subroutine AddReal_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine AddReal_handler

        module subroutine SubtractReal_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine SubtractReal_handler

        module subroutine MultiplyReal_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MultiplyReal_handler

        module subroutine DivideReal_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine DivideReal_handler

        module subroutine SendMessage_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine SendMessage_handler

        module subroutine NamedParameters_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine NamedParameters_handler

        module subroutine MatrixIntegerTranspose_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixIntegerTranspose_handler

        module subroutine MatrixIntegerCopy_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixIntegerCopy_handler

        module subroutine MatrixIntegerSquare_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixIntegerSquare_handler

        module subroutine MatrixRealCopy_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixRealCopy_handler

        module subroutine MatrixRealTranspose_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixRealTranspose_handler

        module subroutine MatrixRealSquare_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixRealSquare_handler

        module subroutine MatrixTextCopy_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixTextCopy_handler

        module subroutine MatrixTextTranspose_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixTextTranspose_handler

        module subroutine MatrixLogicalCopy_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixLogicalCopy_handler

        module subroutine MatrixLogicalTranspose_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixLogicalTranspose_handler

        module subroutine MatrixComplexCopy_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixComplexCopy_handler

        module subroutine MatrixComplexTranspose_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixComplexTranspose_handler

        module subroutine MatrixComplexSquare_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MatrixComplexSquare_handler

        module subroutine MandelbrotBenchmark_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MandelbrotBenchmark_handler

        module subroutine AddComplex_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine AddComplex_handler

        module subroutine SubtractComplex_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine SubtractComplex_handler

        module subroutine MultiplyComplex_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine MultiplyComplex_handler

        module subroutine DivideComplex_handler(json, params_array, result_value, error_code, error_msg)
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine DivideComplex_handler

    end interface

contains

    !****************************************************************************
    !  FUNCTION: DeterminePrimitiveType
    !  PURPOSE:  Determine the primitive type of a JSON value
    !****************************************************************************
    function DeterminePrimitiveType(json, param_item) result(primitive_type)
        use json_module, only: json_null, json_object, json_array, json_logical, &
                               json_integer, json_real, json_string
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: param_item
        character(len=20) :: primitive_type

        integer :: var_type
        character(len=:), allocatable :: string_value
        integer :: decimal_pos, exp_pos_lower, exp_pos_upper
        real(8) :: real_value
        integer :: int_value

        call json%info(param_item, var_type=var_type)

        select case (var_type)
            case (json_null)
                primitive_type = 'NULL'
            case (json_logical)
                primitive_type = 'BOOLEAN'
            case (json_integer)
                primitive_type = 'INTEGER'
            case (json_real)
                call json%get(param_item, string_value)
                if (allocated(string_value)) then
                    decimal_pos = index(string_value, '.')
                    exp_pos_lower = index(string_value, 'e')
                    exp_pos_upper = index(string_value, 'E')

                    if (decimal_pos > 0 .or. exp_pos_lower > 0 .or. exp_pos_upper > 0) then
                        primitive_type = 'REAL'
                    else
                        call json%get(param_item, real_value)
                        int_value = int(real_value)
                        if (abs(real_value - real(int_value, 8)) < 1.0d-10) then
                            primitive_type = 'INTEGER'
                        else
                            primitive_type = 'REAL'
                        end if
                    end if
                else
                    primitive_type = 'REAL'
                end if
            case (json_string)
                primitive_type = 'STRING'
            case default
                primitive_type = 'UNDETERMINED'
        end select

    end function DeterminePrimitiveType

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_get_real_param
    !  PURPOSE:  Get a real parameter from params array at specified index
    !****************************************************************************
    subroutine jsonrpc_get_real_param(json, params_array, index, value, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        integer, intent(in) :: index
        real(8), intent(out) :: value
        logical, intent(out) :: success

        type(json_value), pointer :: param_item
        integer :: var_type

        success = .false.
        value = 0.0d0

        call json%get_child(params_array, index, param_item, success)
        if (.not. success) return

        call json%info(param_item, var_type=var_type)

        ! json-fortran can read both integer and real as real
        call json%get(param_item, value)
        success = .true.

    end subroutine jsonrpc_get_real_param

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_set_real_result
    !  PURPOSE:  Set a real result value in the result JSON object
    !****************************************************************************
    subroutine jsonrpc_set_real_result(json, result_value, value)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(inout) :: result_value
        real(8), intent(in) :: value

        if (associated(result_value)) call json%destroy(result_value)
        call json%create_real(result_value, value, 'result')

    end subroutine jsonrpc_set_real_result

    !****************************************************************************
    !  SUBROUTINE: ValidateMatrixParams
    !  PURPOSE:  Validate all common fields of a 4x4 matrix JSON params object.
    !            Checks object type, datatype, rows=4, columns=4, operation,
    !            ordering="row-major", matrix array present with exactly 16 elements.
    !            Returns the matrix_array pointer on success.
    !****************************************************************************
    subroutine ValidateMatrixParams(json, params_array, expected_datatype, expected_operation, &
                                     matrix_array, error_code, error_msg, success)
        use json_module, only: json_object
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        character(len=*), intent(in) :: expected_datatype
        character(len=*), intent(in) :: expected_operation
        type(json_value), pointer, intent(out) :: matrix_array
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg
        logical, intent(out) :: success

        type(json_value), pointer :: field_ptr
        character(len=:), allocatable :: datatype, operation, ordering
        integer :: rows, cols, array_len, var_type
        logical :: found

        success = .false.
        error_code = 0
        nullify(matrix_array)

        if (.not. associated(params_array)) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: params object required'
            return
        end if

        call json%info(params_array, var_type=var_type)
        if (var_type /= json_object) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: params must be an object'
            return
        end if

        call json%get_child(params_array, 'datatype', field_ptr, found)
        if (.not. found) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: missing required field "datatype"'
            return
        end if
        call json%get(field_ptr, datatype)
        if (datatype /= expected_datatype) then
            error_code = JSONRPC_INVALID_DATA_TYPE
            error_msg = 'Invalid data type: expected "' // trim(expected_datatype) // &
                        '", received "' // trim(datatype) // '"'
            return
        end if

        call json%get_child(params_array, 'rows', field_ptr, found)
        if (.not. found) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: missing required field "rows"'
            return
        end if
        call json%get(field_ptr, rows)
        if (rows /= 4) then
            error_code = JSONRPC_MATRIX_DIMENSION_MISMATCH
            error_msg = 'Matrix dimension mismatch: expected rows=4, received rows='
            write(error_msg, '(A,I0)') trim(error_msg), rows
            return
        end if

        call json%get_child(params_array, 'columns', field_ptr, found)
        if (.not. found) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: missing required field "columns"'
            return
        end if
        call json%get(field_ptr, cols)
        if (cols /= 4) then
            error_code = JSONRPC_MATRIX_DIMENSION_MISMATCH
            error_msg = 'Matrix dimension mismatch: expected columns=4, received columns='
            write(error_msg, '(A,I0)') trim(error_msg), cols
            return
        end if

        call json%get_child(params_array, 'operation', field_ptr, found)
        if (.not. found) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: missing required field "operation"'
            return
        end if
        call json%get(field_ptr, operation)
        if (operation /= expected_operation) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: expected operation="' // trim(expected_operation) // &
                        '", received "' // trim(operation) // '"'
            return
        end if

        call json%get_child(params_array, 'ordering', field_ptr, found)
        if (.not. found) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: missing required field "ordering"'
            return
        end if
        call json%get(field_ptr, ordering)
        if (ordering /= 'row-major') then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: expected ordering="row-major", received "' // &
                        trim(ordering) // '"'
            return
        end if

        call json%get_child(params_array, 'matrix', matrix_array, found)
        if (.not. found) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: missing required field "matrix"'
            return
        end if

        call json%info(matrix_array, n_children=array_len)
        if (array_len /= 16) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: matrix array must contain exactly 16 ' // &
                        trim(expected_datatype) // ' values, '
            write(error_msg, '(A,I0,A)') trim(error_msg), array_len, ' received'
            return
        end if

        success = .true.

    end subroutine ValidateMatrixParams

    !****************************************************************************
    !  SUBROUTINE: BuildMatrixResultHeader
    !  PURPOSE:  Create JSON result object with standard 4x4 row-major matrix
    !            header fields (datatype, rows, columns, ordering).
    !****************************************************************************
    subroutine BuildMatrixResultHeader(json, result_value, datatype)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(inout) :: result_value
        character(len=*), intent(in) :: datatype

        call json%create_object(result_value, 'result')
        call json%add(result_value, 'datatype', datatype)
        call json%add(result_value, 'rows', 4)
        call json%add(result_value, 'columns', 4)
        call json%add(result_value, 'ordering', 'row-major')

    end subroutine BuildMatrixResultHeader


    !****************************************************************************
    !  SUBROUTINE: WriteJsonNotification
    !  PURPOSE:  Assemble an LSP-style framed message and write to the pipe.
    !            Frame format: "Content-Length: N\r\n\r\n<json>"
    !            Shared by SendProgressToClient and SendStatusToClient to
    !            avoid duplicating the WriteFile / FlushFileBuffers logic.
    !  NOTE:     On write failure, g_hPipe is zeroed to prevent further
    !            sends after the client has closed the connection.
    !****************************************************************************
    subroutine WriteJsonNotification(hPipe, notification_json)
        integer(HANDLE), intent(in) :: hPipe
        character(len=*), intent(in) :: notification_json

        character(len=2048), target :: message_buffer
        character(len=100) :: header_part
        integer(DWORD) :: bytesWritten
        integer(BOOL)  :: fSuccess
        integer :: total_msg_len, error_code, json_len, header_len
        integer(INT_PTR) :: buffer_ptr

        json_len   = len_trim(notification_json)

        write(header_part, '(A,I0)') 'Content-Length: ', json_len
        header_len = len_trim(header_part)

        message_buffer = ''
        message_buffer(1:header_len)              = trim(header_part)
        message_buffer(header_len+1:header_len+2) = CRLF
        message_buffer(header_len+3:header_len+4) = CRLF
        message_buffer(header_len+5:header_len+4+json_len) = trim(notification_json)

        total_msg_len = header_len + 4 + json_len

        print *, '  -> Notification: ', trim(notification_json)

        buffer_ptr = LOC(message_buffer)
        fSuccess = WriteFile(hPipe, buffer_ptr, total_msg_len, bytesWritten, NULL)

        if (fSuccess /= 0) then
            if (bytesWritten /= total_msg_len) then
                print *, '  [WARNING] Incomplete write - Expected:', total_msg_len, 'Written:', bytesWritten
            end if

            fSuccess = FlushFileBuffers(hPipe)

            if (fSuccess == 0) then
                print *, '  [WARNING] FlushFileBuffers failed (Error:', GetLastError(), ')'
            end if
        else
            error_code = GetLastError()
            print *, '  [ERROR] WriteFile failed with error:', error_code

            select case (error_code)
                case (232)
                    print *, '  ERROR_NO_DATA: Client closed pipe'
                case (109)
                    print *, '  ERROR_BROKEN_PIPE: Connection lost'
                case default
                    print *, '  Unknown error code'
            end select

            g_hPipe = 0
        end if

    end subroutine WriteJsonNotification

    !****************************************************************************
    !  SUBROUTINE: SendProgressToClient
    !  PURPOSE:  Send a progress percentage notification to the VB.NET client.
    !  WIRE FORMAT: {"jsonrpc":"2.0","method":"progress","params":[N]}
    !  NOTE:     Uses positional array params [N], not the object params
    !            {"percent":N} used by SendProgressNotification in the static
    !            library.  The VB.NET client expects the array format here.
    !****************************************************************************
    subroutine SendProgressToClient(hPipe, percent)
        integer(HANDLE), intent(in) :: hPipe
        integer, intent(in) :: percent

        character(len=100) :: percentStr, notification_json

        write(percentStr, '(I0)') percent
        notification_json = '{"jsonrpc":"2.0","method":"progress","params":[' &
                          // trim(percentStr) // ']}'

        call WriteJsonNotification(hPipe, notification_json)

    end subroutine SendProgressToClient

    !****************************************************************************
    !  SUBROUTINE: SendStatusToClient
    !  PURPOSE:  Send a status string notification to the VB.NET client.
    !  WIRE FORMAT: {"jsonrpc":"2.0","method":"status","params":["msg"]}
    !  NOTE:     Used by the Mandelbrot benchmark to report PAUSED / RESUMED /
    !            aborted state back to the client during long computations.
    !****************************************************************************
    subroutine SendStatusToClient(hPipe, status_msg)
        integer(HANDLE), intent(in) :: hPipe
        character(len=*), intent(in) :: status_msg

        character(len=512) :: notification_json

        notification_json = '{"jsonrpc":"2.0","method":"status","params":["' &
                          // trim(status_msg) // '"]}'

        call WriteJsonNotification(hPipe, notification_json)

    end subroutine SendStatusToClient

end module Functions

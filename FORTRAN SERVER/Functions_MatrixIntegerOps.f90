!****************************************************************************
!
!  SUBMODULE: Functions_MatrixIntegerOps
!
!  PURPOSE:  Integer matrix operation handlers (Transpose, Copy, Square)
!
!****************************************************************************
submodule (Functions) Functions_MatrixIntegerOps
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: MatrixIntegerTranspose_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 integer matrix transpose.
    !  NOTE:     Client sends and receives data in row-major order.  Fortran
    !            arrays are column-major, so the read loop fills matrix_fortran
    !            row by row (i outer, j inner), TRANSPOSE() is then applied,
    !            and the result is written back out in row-major order.
    !****************************************************************************
    module subroutine MatrixIntegerTranspose_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, j, idx
        integer :: matrix_fortran(4,4), transposed(4,4), output_rowmajor(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'integer', 'transpose', &
                                   matrix_array, error_code, error_msg, success)
        if (.not. success) return

        idx = 1
        do i = 1, 4
            do j = 1, 4
                call json%get_child(matrix_array, idx, field_ptr, found)
                if (.not. found) then
                    error_code = JSONRPC_INVALID_PARAMS
                    error_msg = 'Invalid params: failed to read matrix element'
                    return
                end if
                call json%get(field_ptr, matrix_fortran(i, j))
                idx = idx + 1
            end do
        end do

        transposed = transpose(matrix_fortran)

        idx = 1
        do i = 1, 4
            do j = 1, 4
                output_rowmajor(idx) = transposed(i, j)
                idx = idx + 1
            end do
        end do

        call BuildMatrixResultHeader(json, result_value, 'integer')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', output_rowmajor(i))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixIntegerTranspose: 4x4 matrix transposed successfully'

    end subroutine MatrixIntegerTranspose_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixIntegerCopy_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 integer matrix copy (identity operation)
    !****************************************************************************
    module subroutine MatrixIntegerCopy_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        integer :: matrix_values(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'integer', 'copy', &
                                   matrix_array, error_code, error_msg, success)
        if (.not. success) return

        do idx = 1, 16
            call json%get_child(matrix_array, idx, field_ptr, found)
            if (.not. found) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Invalid params: failed to read matrix element'
                return
            end if
            call json%get(field_ptr, matrix_values(idx))
        end do

        call BuildMatrixResultHeader(json, result_value, 'integer')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', matrix_values(i))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixIntegerCopy: 4x4 matrix copied successfully'

    end subroutine MatrixIntegerCopy_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixIntegerSquare_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 integer matrix element-wise squaring.
    !  NOTE:     Values are validated to [-100, +100] before squaring.  This
    !            keeps results within 32-bit signed integer range (max 10000),
    !            preventing silent overflow on the client side.
    !****************************************************************************
    module subroutine MatrixIntegerSquare_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        integer :: input_values(16), output_values(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'integer', 'square', &
                                   matrix_array, error_code, error_msg, success)
        if (.not. success) return

        do idx = 1, 16
            call json%get_child(matrix_array, idx, field_ptr, found)
            if (.not. found) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Invalid params: failed to read matrix element'
                return
            end if
            call json%get(field_ptr, input_values(idx))

            if (input_values(idx) < -100 .or. input_values(idx) > 100) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Invalid params: matrix value out of range [-100, +100] at index '
                write(error_msg, '(A,I0,A,I0)') trim(error_msg), idx, ', value=', input_values(idx)
                return
            end if
        end do

        do i = 1, 16
            output_values(i) = input_values(i) * input_values(i)
        end do

        call BuildMatrixResultHeader(json, result_value, 'integer')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', output_values(i))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixIntegerSquare: 4x4 matrix squared successfully'

    end subroutine MatrixIntegerSquare_handler

end submodule Functions_MatrixIntegerOps

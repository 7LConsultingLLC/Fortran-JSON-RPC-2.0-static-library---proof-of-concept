!****************************************************************************
!
!  SUBMODULE: Functions_MatrixRealOps
!
!  PURPOSE:  Real matrix operation handlers (Copy, Transpose, Square)
!
!****************************************************************************
submodule (Functions) Functions_MatrixRealOps
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: MatrixRealCopy_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 real matrix copy (identity operation).
    !  NOTE:     Values validated to [-100.0, +100.0] — client-enforced range
    !            matching the VB.NET UI spinner limits.
    !****************************************************************************
    module subroutine MatrixRealCopy_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        real(8) :: matrix_values(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'real', 'copy', &
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

            if (matrix_values(idx) < -100.0d0 .or. matrix_values(idx) > 100.0d0) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Invalid params: matrix value out of range [-100.0, +100.0] at index '
                write(error_msg, '(A,I0,A,F0.2)') trim(error_msg), idx, ', value=', matrix_values(idx)
                return
            end if
        end do

        call BuildMatrixResultHeader(json, result_value, 'real')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', matrix_values(i))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixRealCopy: 4x4 real matrix copied successfully'

    end subroutine MatrixRealCopy_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixRealTranspose_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 real matrix transpose.
    !  NOTE:     Values validated to [0.0, +100.0] — the VB.NET client uses a
    !            non-negative-only spinner for the transpose input.
    !            See MatrixRealCopy for the [-100.0, +100.0] signed range used
    !            by the copy operation.
    !****************************************************************************
    module subroutine MatrixRealTranspose_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, j, idx
        real(8) :: matrix_fortran(4,4), transposed(4,4), output_rowmajor(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'real', 'transpose', &
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

                if (matrix_fortran(i, j) < 0.0d0 .or. matrix_fortran(i, j) > 100.0d0) then
                    error_code = JSONRPC_INVALID_PARAMS
                    error_msg = 'Invalid params: matrix value out of range [0.0, +100.0] at index '
                    write(error_msg, '(A,I0,A,F0.2)') trim(error_msg), idx, ', value=', matrix_fortran(i, j)
                    return
                end if

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

        call BuildMatrixResultHeader(json, result_value, 'real')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', output_rowmajor(i))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixRealTranspose: 4x4 real matrix transposed successfully'

    end subroutine MatrixRealTranspose_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixRealSquare_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 real matrix element-wise squaring.
    !  NOTE:     No range validation performed — REAL(8) has sufficient range
    !            to square any client-supplied value without overflow risk.
    !****************************************************************************
    module subroutine MatrixRealSquare_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        real(8) :: input_values(16), output_values(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'real', 'square', &
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
        end do

        do i = 1, 16
            output_values(i) = input_values(i) * input_values(i)
        end do

        call BuildMatrixResultHeader(json, result_value, 'real')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', output_values(i))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixRealSquare: 4x4 real matrix squared successfully'

    end subroutine MatrixRealSquare_handler

end submodule Functions_MatrixRealOps

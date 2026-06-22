!****************************************************************************
!
!  SUBMODULE: Functions_MatrixTextOps
!
!  PURPOSE:  Text/String matrix operation handlers (Copy, Transpose)
!
!****************************************************************************
submodule (Functions) Functions_MatrixTextOps
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: MatrixTextCopy_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 text/string matrix copy (identity operation)
    !****************************************************************************
    module subroutine MatrixTextCopy_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        character(len=6) :: matrix_values(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'string', 'copy', &
                                   matrix_array, error_code, error_msg, success)
        if (.not. success) return

        do idx = 1, 16
            call json%get_child(matrix_array, idx, field_ptr, found)
            if (.not. found) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Invalid params: failed to read matrix element'
                return
            end if

            block
                character(len=:), allocatable :: temp_string
                call json%get(field_ptr, temp_string)
                matrix_values(idx) = temp_string
            end block
        end do

        call BuildMatrixResultHeader(json, result_value, 'string')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', trim(matrix_values(i)))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixTextCopy: 4x4 text matrix copied successfully'

    end subroutine MatrixTextCopy_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixTextTranspose_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 text/string matrix transpose
    !****************************************************************************
    module subroutine MatrixTextTranspose_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, j, idx
        character(len=6) :: matrix_fortran(4,4), transposed(4,4), output_rowmajor(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'string', 'transpose', &
                                   matrix_array, error_code, error_msg, success)
        if (.not. success) return

        ! Read strings and convert row-major to Fortran column-major
        idx = 1
        do i = 1, 4
            do j = 1, 4
                call json%get_child(matrix_array, idx, field_ptr, found)
                if (.not. found) then
                    error_code = JSONRPC_INVALID_PARAMS
                    error_msg = 'Invalid params: failed to read matrix element'
                    return
                end if

                ! Use allocatable string intermediate
                block
                    character(len=:), allocatable :: temp_string
                    call json%get(field_ptr, temp_string)
                    matrix_fortran(i, j) = temp_string
                end block

                idx = idx + 1
            end do
        end do

        ! Perform transpose using Fortran intrinsic
        transposed = transpose(matrix_fortran)

        ! Convert transposed matrix from column-major back to row-major
        idx = 1
        do i = 1, 4
            do j = 1, 4
                output_rowmajor(idx) = transposed(i, j)
                idx = idx + 1
            end do
        end do

        call BuildMatrixResultHeader(json, result_value, 'string')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call json%add(output_array, '', trim(output_rowmajor(i)))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixTextTranspose: 4x4 text matrix transposed successfully'

    end subroutine MatrixTextTranspose_handler

end submodule Functions_MatrixTextOps

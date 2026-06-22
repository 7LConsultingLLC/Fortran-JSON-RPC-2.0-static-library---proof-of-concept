!****************************************************************************
!
!  SUBMODULE: Functions_MatrixLogicalOps
!
!  PURPOSE:  Logical/Boolean matrix operation handlers (Copy, Transpose)
!
!****************************************************************************
submodule (Functions) Functions_MatrixLogicalOps
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: MatrixLogicalCopy_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 logical/boolean matrix copy (identity operation)
    !****************************************************************************
    module subroutine MatrixLogicalCopy_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        logical :: matrix_values(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'logical', 'copy', &
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

        call BuildMatrixResultHeader(json, result_value, 'logical')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            if (matrix_values(i)) then
                call json%add(output_array, '', 'true')
            else
                call json%add(output_array, '', 'false')
            end if
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixLogicalCopy: 4x4 logical matrix copied successfully'

    end subroutine MatrixLogicalCopy_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixLogicalTranspose_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 logical/boolean matrix transpose
    !
    !  NOTE: VB.NET sends uppercase True/False
    !        json-fortran converts to Fortran .TRUE./.FALSE. automatically
    !        Transposes using Fortran TRANSPOSE() intrinsic
    !        Returns lowercase "true" and "false" as strings
    !****************************************************************************
    module subroutine MatrixLogicalTranspose_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, j, idx
        logical :: matrix_fortran(4,4), transposed(4,4), output_rowmajor(16)
        logical :: found, success

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'logical', 'transpose', &
                                   matrix_array, error_code, error_msg, success)
        if (.not. success) return

        ! Read boolean values and convert row-major to Fortran column-major
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

        call BuildMatrixResultHeader(json, result_value, 'logical')

        ! Create output array - convert LOGICAL to "true" / "false" strings
        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            if (output_rowmajor(i)) then
                call json%add(output_array, '', 'true')
            else
                call json%add(output_array, '', 'false')
            end if
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixLogicalTranspose: 4x4 logical matrix transposed successfully'

    end subroutine MatrixLogicalTranspose_handler

end submodule Functions_MatrixLogicalOps

!****************************************************************************
!
!  SUBMODULE: Functions_MatrixComplexOps
!
!  PURPOSE:  Complex number matrix operation handlers (Copy, Transpose, Square)
!
!****************************************************************************
submodule (Functions) Functions_MatrixComplexOps
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: MatrixComplexCopy_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 complex matrix copy (identity operation)
    !****************************************************************************
    module subroutine MatrixComplexCopy_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        complex(8) :: matrix_values(16)
        character(len=50) :: complex_str, output_str
        logical :: found, parse_success, success
        real(8) :: real_part, imag_part

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'complex', 'copy', &
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
                complex_str = temp_string
            end block

            call parse_complex_string(complex_str, real_part, imag_part, parse_success)
            if (.not. parse_success) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Invalid complex number format at index '
                write(error_msg, '(A,I0,A)') trim(error_msg), idx, ': ' // trim(complex_str)
                return
            end if

            matrix_values(idx) = cmplx(real_part, imag_part, kind=8)
        end do

        call BuildMatrixResultHeader(json, result_value, 'complex')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call format_complex_string(matrix_values(i), output_str)
            call json%add(output_array, '', trim(output_str))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixComplexCopy: 4x4 complex matrix copied successfully'

    end subroutine MatrixComplexCopy_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixComplexTranspose_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 complex matrix transpose
    !****************************************************************************
    module subroutine MatrixComplexTranspose_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, j, idx
        complex(8) :: matrix_fortran(4,4), transposed(4,4), output_rowmajor(16)
        character(len=50) :: complex_str, output_str
        logical :: found, parse_success, success
        real(8) :: real_part, imag_part

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'complex', 'transpose', &
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

                block
                    character(len=:), allocatable :: temp_string
                    call json%get(field_ptr, temp_string)
                    complex_str = temp_string
                end block

                call parse_complex_string(complex_str, real_part, imag_part, parse_success)
                if (.not. parse_success) then
                    error_code = JSONRPC_INVALID_PARAMS
                    error_msg = 'Invalid complex number format at index '
                    write(error_msg, '(A,I0,A)') trim(error_msg), idx, ': ' // trim(complex_str)
                    return
                end if

                matrix_fortran(i, j) = cmplx(real_part, imag_part, kind=8)
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

        call BuildMatrixResultHeader(json, result_value, 'complex')

        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call format_complex_string(output_rowmajor(i), output_str)
            call json%add(output_array, '', trim(output_str))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixComplexTranspose: 4x4 complex matrix transposed successfully'

    end subroutine MatrixComplexTranspose_handler

    !****************************************************************************
    !  SUBROUTINE: MatrixComplexSquare_handler
    !  PURPOSE:  JSON-RPC handler for 4x4 complex matrix element-wise squaring
    !
    !  NOTE: Element-wise operation: output[i] = input[i]²
    !        Complex squaring: (a+bi)² = (a²-b²) + (2ab)i
    !        Fortran handles complex arithmetic automatically
    !        No range validation - client guarantees valid input
    !****************************************************************************
    module subroutine MatrixComplexSquare_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        type(json_value), pointer :: field_ptr, matrix_array, output_array
        integer :: i, idx
        complex(8) :: input_values(16), output_values(16)
        character(len=50) :: complex_str, output_str
        logical :: found, parse_success, success
        real(8) :: real_part, imag_part

        error_code = 0

        call ValidateMatrixParams(json, params_array, 'complex', 'square', &
                                   matrix_array, error_code, error_msg, success)
        if (.not. success) return

        ! Parse 16 complex number strings (no range validation)
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
                complex_str = temp_string
            end block

            call parse_complex_string(complex_str, real_part, imag_part, parse_success)
            if (.not. parse_success) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Invalid complex number format at index '
                write(error_msg, '(A,I0,A)') trim(error_msg), idx, ': ' // trim(complex_str)
                return
            end if

            input_values(idx) = cmplx(real_part, imag_part, kind=8)
        end do

        ! Square each complex number: c² = c × c (Fortran handles complex arithmetic)
        do i = 1, 16
            output_values(i) = input_values(i) * input_values(i)
        end do

        call BuildMatrixResultHeader(json, result_value, 'complex')

        ! Create output array - format COMPLEX(8) to strings
        call json%create_array(output_array, 'matrix')
        do i = 1, 16
            call format_complex_string(output_values(i), output_str)
            call json%add(output_array, '', trim(output_str))
        end do
        call json%add(result_value, output_array)

        print *, 'MatrixComplexSquare: 4x4 complex matrix squared successfully'

    end subroutine MatrixComplexSquare_handler

    !****************************************************************************
    !  SUBROUTINE: parse_complex_string
    !  PURPOSE:  Parse complex number string "(real±imagi)" into real and imaginary parts
    !****************************************************************************
    subroutine parse_complex_string(complex_str, real_part, imag_part, success)
        character(len=*), intent(in) :: complex_str
        real(8), intent(out) :: real_part, imag_part
        logical, intent(out) :: success

        character(len=:), allocatable :: work_str, real_str, imag_str
        integer :: len_str, pos_sep, i, iostat

        success = .false.
        real_part = 0.0d0
        imag_part = 0.0d0

        len_str = len_trim(complex_str)
        if (len_str < 5) return
        if (complex_str(1:1) /= '(' .or. complex_str(len_str:len_str) /= ')') return

        work_str = complex_str(2:len_str-1)
        len_str = len(work_str)

        if (work_str(len_str:len_str) /= 'i') return

        work_str = work_str(1:len_str-1)
        len_str = len(work_str)

        ! Scan forward from char 2, skipping +/- that are exponent signs (preceded by E/e)
        pos_sep = 0
        do i = 2, len_str
            if (work_str(i:i) == '+' .or. work_str(i:i) == '-') then
                if (work_str(i-1:i-1) /= 'E' .and. work_str(i-1:i-1) /= 'e') then
                    pos_sep = i
                    exit
                end if
            end if
        end do

        if (pos_sep == 0) return

        real_str = work_str(1:pos_sep-1)
        imag_str = work_str(pos_sep:len_str)

        read(real_str, *, iostat=iostat) real_part
        if (iostat /= 0) return

        read(imag_str, *, iostat=iostat) imag_part
        if (iostat /= 0) return

        success = .true.

    end subroutine parse_complex_string

    !****************************************************************************
    !  SUBROUTINE: format_complex_string
    !  PURPOSE:  Format COMPLEX(8) value to string "(real±imagi)" with 2 decimal places
    !****************************************************************************
    subroutine format_complex_string(c, output_str)
        complex(8), intent(in) :: c
        character(len=*), intent(out) :: output_str

        real(8) :: re, im
        character(len=30) :: temp_str

        re = real(c, kind=8)
        im = aimag(c)

        if (im >= 0.0d0) then
            write(temp_str, '(F0.2,A,F0.2,A)') re, '+', im, 'i'
        else
            write(temp_str, '(F0.2,F0.2,A)') re, im, 'i'
        end if

        output_str = '(' // trim(adjustl(temp_str)) // ')'

    end subroutine format_complex_string

end submodule Functions_MatrixComplexOps

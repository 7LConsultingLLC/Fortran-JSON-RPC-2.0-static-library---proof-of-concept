!****************************************************************************
!
!  SUBMODULE: Functions_ArithmeticOps
!
!  PURPOSE:  Arithmetic operation handlers (Integer and Real operations)
!
!****************************************************************************
submodule (Functions) Functions_ArithmeticOps
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: GetTwoIntParams
    !  PURPOSE:  Extract and validate two integer parameters from params array
    !****************************************************************************
    subroutine GetTwoIntParams(json, params_array, param1, param2, error_code, error_msg, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        integer, intent(out) :: param1, param2
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg
        logical, intent(out) :: success

        logical :: success1, success2

        success    = .false.
        error_code = 0
        param1     = 0
        param2     = 0

        if (.not. associated(params_array)) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: params must be an array'
            return
        end if

        call jsonrpc_get_int_param(json, params_array, 1, param1, success1)
        if (.not. success1) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param1 must be an integer'
            return
        end if

        call jsonrpc_get_int_param(json, params_array, 2, param2, success2)
        if (.not. success2) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param2 must be an integer'
            return
        end if

        success = .true.

    end subroutine GetTwoIntParams

    !****************************************************************************
    !  SUBROUTINE: PerformArithmeticOp
    !  PURPOSE:  Perform integer arithmetic (add/subtract/multiply) by op_code,
    !            with 64-bit intermediate to detect 32-bit overflow.
    !  NOTE:     Division is handled separately in DivideInt_handler because it
    !            requires a zero-check before the operation.
    !****************************************************************************
    subroutine PerformArithmeticOp(op_code, param1, param2, result_int, error_code, error_msg)
        integer, intent(in) :: op_code
        integer, intent(in) :: param1, param2
        integer, intent(out) :: result_int
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        integer(8) :: result_int64
        character(len=20) :: op_name
        character(len=1) :: op_symbol

        error_code = 0
        result_int = 0

        select case (op_code)
            case (OP_ADD)
                result_int64 = int(param1, 8) + int(param2, 8)
                op_name = 'AddInt'
                op_symbol = '+'
            case (OP_SUBTRACT)
                result_int64 = int(param1, 8) - int(param2, 8)
                op_name = 'SubtractInt'
                op_symbol = '-'
            case (OP_MULTIPLY)
                result_int64 = int(param1, 8) * int(param2, 8)
                op_name = 'MultiplyInt'
                op_symbol = '*'
            case default
                error_code = JSONRPC_INTERNAL_ERROR
                error_msg = 'Internal error: invalid operation code'
                return
        end select

        if (result_int64 > int(INT_MAX, 8) .or. result_int64 < int(INT_MIN, 8)) then
            error_code = JSONRPC_OVERFLOW_ERROR
            error_msg = 'Invalid params: result exceeds 32-bit integer range'
            return
        end if

        result_int = int(result_int64)
        print *, trim(op_name), ': ', param1, ' ', op_symbol, ' ', param2, ' = ', result_int

    end subroutine PerformArithmeticOp

    !****************************************************************************
    !  SUBROUTINE: AddInt_handler
    !  PURPOSE:  JSON-RPC handler for integer addition
    !****************************************************************************
    module subroutine AddInt_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        integer :: param1, param2, result_int
        logical :: success

        error_code = 0
        call GetTwoIntParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        call PerformArithmeticOp(OP_ADD, param1, param2, result_int, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_int_result(json, result_value, result_int)

    end subroutine AddInt_handler

    !****************************************************************************
    !  SUBROUTINE: SubtractInt_handler
    !  PURPOSE:  JSON-RPC handler for integer subtraction
    !****************************************************************************
    module subroutine SubtractInt_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        integer :: param1, param2, result_int
        logical :: success

        error_code = 0
        call GetTwoIntParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        call PerformArithmeticOp(OP_SUBTRACT, param1, param2, result_int, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_int_result(json, result_value, result_int)

    end subroutine SubtractInt_handler

    !****************************************************************************
    !  SUBROUTINE: MultiplyInt_handler
    !  PURPOSE:  JSON-RPC handler for integer multiplication
    !****************************************************************************
    module subroutine MultiplyInt_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        integer :: param1, param2, result_int
        logical :: success

        error_code = 0
        call GetTwoIntParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        call PerformArithmeticOp(OP_MULTIPLY, param1, param2, result_int, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_int_result(json, result_value, result_int)

    end subroutine MultiplyInt_handler

    !****************************************************************************
    !  SUBROUTINE: DivideInt_handler
    !  PURPOSE:  JSON-RPC handler for integer division with zero-check
    !****************************************************************************
    module subroutine DivideInt_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        integer :: param1, param2, result_int
        logical :: success

        error_code = 0
        call GetTwoIntParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        if (param2 == 0) then
            error_code = JSONRPC_DIVISION_BY_ZERO
            error_msg = 'Invalid params: division by zero is not allowed'
            return
        end if

        result_int = param1 / param2
        print *, 'DivideInt: ', param1, ' / ', param2, ' = ', result_int

        call jsonrpc_set_int_result(json, result_value, result_int)

    end subroutine DivideInt_handler

    !****************************************************************************
    !  SUBROUTINE: AddReal_handler
    !  PURPOSE:  JSON-RPC handler for real number addition
    !  
    !  INPUT: params array with 2 real numbers [param1, param2]
    !  OUTPUT: result = param1 + param2, rounded to 2 decimal places
    !  
    !  NOTE: Uses REAL(8) double precision
    !        No overflow validation (trust client)
    !        Rounds to 2 decimals to avoid floating-point display issues
    !****************************************************************************
    module subroutine AddReal_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        real(8) :: param1, param2, result_real
        logical :: success

        error_code = 0
        call GetTwoRealParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        call PerformRealArithmeticOp(OP_ADD, param1, param2, result_real, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_real_result(json, result_value, result_real)

    end subroutine AddReal_handler

    !****************************************************************************
    !  SUBROUTINE: SubtractReal_handler
    !  PURPOSE:  JSON-RPC handler for real number subtraction
    !
    !  INPUT: params array with 2 real numbers [param1, param2]
    !  OUTPUT: result = param1 - param2, rounded to 2 decimal places
    !
    !  NOTE: Uses REAL(8) double precision
    !        No overflow validation (trust client)
    !        Rounds to 2 decimals to avoid floating-point display issues
    !****************************************************************************
    module subroutine SubtractReal_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        real(8) :: param1, param2, result_real
        logical :: success

        error_code = 0
        call GetTwoRealParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        call PerformRealArithmeticOp(OP_SUBTRACT, param1, param2, result_real, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_real_result(json, result_value, result_real)

    end subroutine SubtractReal_handler

    !****************************************************************************
    !  SUBROUTINE: MultiplyReal_handler
    !  PURPOSE:  JSON-RPC handler for real number multiplication
    !
    !  INPUT: params array with 2 real numbers [param1, param2]
    !  OUTPUT: result = param1 * param2, rounded to 2 decimal places
    !
    !  NOTE: Uses REAL(8) double precision
    !        No overflow validation (trust client)
    !        Rounds to 2 decimals to avoid floating-point display issues
    !****************************************************************************
    module subroutine MultiplyReal_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        real(8) :: param1, param2, result_real
        logical :: success

        error_code = 0
        call GetTwoRealParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        call PerformRealArithmeticOp(OP_MULTIPLY, param1, param2, result_real, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_real_result(json, result_value, result_real)

    end subroutine MultiplyReal_handler

    !****************************************************************************
    !  SUBROUTINE: DivideReal_handler
    !  PURPOSE:  JSON-RPC handler for real number division with zero-check
    !
    !  INPUT: params array with 2 real numbers [param1, param2]
    !  OUTPUT: result = param1 / param2, rounded to 2 decimal places
    !
    !  NOTE: Uses REAL(8) double precision
    !        Checks for division by zero before dividing
    !        Rounds to 2 decimals to avoid floating-point display issues
    !****************************************************************************
    module subroutine DivideReal_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        real(8) :: param1, param2, result_real
        logical :: success

        error_code = 0
        call GetTwoRealParams(json, params_array, param1, param2, error_code, error_msg, success)
        if (.not. success) return

        if (param2 == 0.0d0) then
            error_code = JSONRPC_DIVISION_BY_ZERO
            error_msg = 'Invalid params: division by zero is not allowed'
            return
        end if

        result_real = param1 / param2
        result_real = anint(result_real * 100.0d0) / 100.0d0
        print *, 'DivideReal: ', param1, ' / ', param2, ' = ', result_real

        call jsonrpc_set_real_result(json, result_value, result_real)

    end subroutine DivideReal_handler

    !****************************************************************************
    !  SUBROUTINE: GetTwoRealParams
    !  PURPOSE:  Extract and validate two real number parameters from params array
    !****************************************************************************
    subroutine GetTwoRealParams(json, params_array, param1, param2, error_code, error_msg, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        real(8), intent(out) :: param1, param2
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg
        logical, intent(out) :: success

        logical :: success1, success2

        success    = .false.
        error_code = 0
        param1     = 0.0d0
        param2     = 0.0d0

        if (.not. associated(params_array)) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: params must be an array'
            return
        end if

        call jsonrpc_get_real_param(json, params_array, 1, param1, success1)
        if (.not. success1) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param1 must be a real number'
            return
        end if

        call jsonrpc_get_real_param(json, params_array, 2, param2, success2)
        if (.not. success2) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param2 must be a real number'
            return
        end if

        success = .true.

    end subroutine GetTwoRealParams

    !****************************************************************************
    !  SUBROUTINE: PerformRealArithmeticOp
    !  PURPOSE:  Perform real arithmetic operation with rounding to 2 decimals
    !  
    !  NOTE: No overflow checking (unlike integer version)
    !        Rounds result to 2 decimal places to avoid floating-point artifacts
    !****************************************************************************
    subroutine PerformRealArithmeticOp(op_code, param1, param2, result_real, error_code, error_msg)
        integer, intent(in) :: op_code
        real(8), intent(in) :: param1, param2
        real(8), intent(out) :: result_real
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        character(len=20) :: op_name
        character(len=1) :: op_symbol

        error_code = 0
        result_real = 0.0d0

        select case (op_code)
            case (OP_ADD)
                result_real = param1 + param2
                op_name = 'AddReal'
                op_symbol = '+'
            case (OP_SUBTRACT)
                result_real = param1 - param2
                op_name = 'SubtractReal'
                op_symbol = '-'
            case (OP_MULTIPLY)
                result_real = param1 * param2
                op_name = 'MultiplyReal'
                op_symbol = '*'
            case default
                error_code = JSONRPC_INTERNAL_ERROR
                error_msg = 'Internal error: invalid operation code'
                return
        end select

        ! Round to 2 decimal places
        result_real = anint(result_real * 100.0d0) / 100.0d0

        print *, trim(op_name), ': ', param1, ' ', op_symbol, ' ', param2, ' = ', result_real

    end subroutine PerformRealArithmeticOp

    !****************************************************************************
    !  FUNCTION: FormatComplexComponent
    !  PURPOSE:  Format a single real(8) value as a complex component string
    !
    !  Rules:
    !    |v| >= 1E6         -> scientific: e.g. 1.5E+06, 3E+06
    !    |v| < 0.01, v/=0   -> scientific: e.g. 3.14E-03, 3E-03
    !    otherwise          -> fixed:      e.g. 4, 4.25, 0
    !    Integer rule applies to scientific coefficient too (3E-03 not 3.0E-03)
    !    Exponent always 2 digits with explicit sign
    !****************************************************************************
    function FormatComplexComponent(v) result(str)
        real(8), intent(in) :: v
        character(len=:), allocatable :: str

        real(8) :: av, coeff
        integer :: exp_val
        character(len=40) :: buf
        character(len=10) :: exp_str
        integer :: dot_pos, last_nonzero, i

        av = abs(v)

        if (av >= 1.0d6 .or. (av < 0.01d0 .and. av /= 0.0d0)) then
            ! Scientific notation
            if (av > 0.0d0) then
                exp_val = floor(log10(av))
            else
                exp_val = 0
            end if
            coeff = v / (10.0d0 ** exp_val)

            ! Format coefficient: integer rule (no .0 if whole)
            if (coeff == dble(int(coeff))) then
                write(buf, '(I0)') int(coeff)
            else
                write(buf, '(F10.2)') coeff
                ! Strip trailing zeros after decimal
                buf = adjustl(buf)
                dot_pos = index(buf, '.')
                if (dot_pos > 0) then
                    last_nonzero = len_trim(buf)
                    do while (last_nonzero > dot_pos .and. buf(last_nonzero:last_nonzero) == '0')
                        last_nonzero = last_nonzero - 1
                    end do
                    if (last_nonzero == dot_pos) last_nonzero = dot_pos - 1
                    buf = buf(1:last_nonzero)
                end if
            end if

            ! Format exponent: always 2 digits with explicit sign
            if (exp_val >= 0) then
                write(exp_str, '(A,I2.2)') 'E+', exp_val
            else
                write(exp_str, '(A,I2.2)') 'E-', abs(exp_val)
            end if

            str = trim(adjustl(buf)) // trim(exp_str)

        else
            ! Fixed notation
            if (v == dble(int(v))) then
                ! Whole number — no decimal point
                write(buf, '(I0)') int(v)
                str = trim(adjustl(buf))
            else
                write(buf, '(F20.2)') v
                buf = adjustl(buf)
                ! Strip trailing zeros after decimal
                dot_pos = index(buf, '.')
                if (dot_pos > 0) then
                    last_nonzero = len_trim(buf)
                    do while (last_nonzero > dot_pos .and. buf(last_nonzero:last_nonzero) == '0')
                        last_nonzero = last_nonzero - 1
                    end do
                    if (last_nonzero == dot_pos) last_nonzero = dot_pos - 1
                    buf = buf(1:last_nonzero)
                end if
                str = trim(adjustl(buf))
            end if
        end if

    end function FormatComplexComponent

    !****************************************************************************
    !  SUBROUTINE: ParseComplexString
    !  PURPOSE:  Parse a complex number string into real(8) re and im components
    !
    !  Input format (no brackets): "3.5+2.0i", "-3.5-2.0i", "1.5E+10+3.2E-04i"
    !  Strategy: strip trailing 'i', scan from char 2 onward for + or - not
    !  preceded by 'E' — that is the split point between real and imaginary parts.
    !****************************************************************************
    subroutine ParseComplexString(str, re, im, success)
        character(len=*), intent(in) :: str
        real(8), intent(out) :: re, im
        logical, intent(out) :: success

        character(len=len(str)) :: work, nospace
        integer :: slen, i, split_pos, j
        character(len=1) :: ch, prev_ch

        success = .false.
        re = 0.0d0
        im = 0.0d0

        ! Strip all spaces so "1 + 2i" is treated identically to "1+2i"
        nospace = ''
        j = 0
        do i = 1, len_trim(str)
            if (str(i:i) /= ' ') then
                j = j + 1
                nospace(j:j) = str(i:i)
            end if
        end do

        work = nospace
        slen = j

        ! Strip trailing 'i'
        if (slen < 2) return
        if (work(slen:slen) /= 'i' .and. work(slen:slen) /= 'I') return
        slen = slen - 1

        ! Scan from character 2 onward for + or - not preceded by E
        split_pos = 0
        do i = 2, slen
            ch = work(i:i)
            prev_ch = work(i-1:i-1)
            if ((ch == '+' .or. ch == '-') .and. &
                (prev_ch /= 'E' .and. prev_ch /= 'e')) then
                split_pos = i
                exit
            end if
        end do

        if (split_pos == 0) return

        ! Read real part
        read(work(1:split_pos-1), *, iostat=i) re
        if (i /= 0) return

        ! Read imaginary part (includes its own sign)
        read(work(split_pos:slen), *, iostat=i) im
        if (i /= 0) return

        success = .true.

    end subroutine ParseComplexString

    !****************************************************************************
    !  SUBROUTINE: GetTwoComplexStringParams
    !  PURPOSE:  Extract two complex number strings from positional params array
    !****************************************************************************
    subroutine GetTwoComplexStringParams(json, params_array, str1, str2, &
                                         error_code, error_msg, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        character(len=:), allocatable, intent(out) :: str1, str2
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg
        logical, intent(out) :: success

        logical :: ok1, ok2

        success    = .false.
        error_code = 0

        if (.not. associated(params_array)) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: params must be an array'
            return
        end if

        call jsonrpc_get_string_param(json, params_array, 1, str1, ok1)
        if (.not. ok1) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param1 must be a complex number string'
            return
        end if

        call jsonrpc_get_string_param(json, params_array, 2, str2, ok2)
        if (.not. ok2) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param2 must be a complex number string'
            return
        end if

        success = .true.

    end subroutine GetTwoComplexStringParams

    !****************************************************************************
    !  FUNCTION: BuildComplexResultString
    !  PURPOSE:  Assemble re and im components into result string
    !
    !  Format: "re+imi" or "re-imi" — no spaces, lowercase i, no brackets
    !  The imaginary component string carries no leading sign; the separator does.
    !****************************************************************************
    function BuildComplexResultString(re, im) result(str)
        real(8), intent(in) :: re, im
        character(len=:), allocatable :: str

        character(len=:), allocatable :: re_str, im_str

        re_str = FormatComplexComponent(re)
        im_str = FormatComplexComponent(abs(im))

        if (im >= 0.0d0) then
            str = trim(re_str) // '+' // trim(im_str) // 'i'
        else
            str = trim(re_str) // '-' // trim(im_str) // 'i'
        end if

    end function BuildComplexResultString

    !****************************************************************************
    !  SUBROUTINE: PerformComplexArithmeticOp
    !  PURPOSE:  Perform complex arithmetic (add/subtract/multiply) by op_code
    !
    !  NOTE: DivideComplex is excluded — denominator zero-check requires
    !        separate handling not expressible as a generic op_code path.
    !****************************************************************************
    subroutine PerformComplexArithmeticOp(op_code, re_a, im_a, re_b, im_b, &
                                          re_r, im_r, error_code, error_msg)
        integer, intent(in) :: op_code
        real(8), intent(in) :: re_a, im_a, re_b, im_b
        real(8), intent(out) :: re_r, im_r
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        character(len=20) :: op_name
        character(len=1) :: op_symbol

        error_code = 0
        re_r = 0.0d0
        im_r = 0.0d0

        select case (op_code)
            case (OP_ADD)
                re_r = re_a + re_b
                im_r = im_a + im_b
                op_name   = 'AddComplex'
                op_symbol = '+'
            case (OP_SUBTRACT)
                re_r = re_a - re_b
                im_r = im_a - im_b
                op_name   = 'SubtractComplex'
                op_symbol = '-'
            case (OP_MULTIPLY)
                re_r = (re_a * re_b) - (im_a * im_b)
                im_r = (re_a * im_b) + (im_a * re_b)
                op_name   = 'MultiplyComplex'
                op_symbol = '*'
            case default
                error_code = JSONRPC_INTERNAL_ERROR
                error_msg  = 'Internal error: invalid operation code'
                return
        end select

        print *, trim(op_name), ': (', re_a, '+', im_a, 'i) ', op_symbol, &
                 ' (', re_b, '+', im_b, 'i) = (', re_r, '+', im_r, 'i)'

    end subroutine PerformComplexArithmeticOp

    !****************************************************************************
    !  SUBROUTINE: AddComplex_handler
    !  PURPOSE:  JSON-RPC handler for complex number addition
    !****************************************************************************
    module subroutine AddComplex_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        character(len=:), allocatable :: str1, str2
        real(8) :: re_a, im_a, re_b, im_b, re_r, im_r
        logical :: success, parse_ok

        error_code = 0
        call GetTwoComplexStringParams(json, params_array, str1, str2, &
                                       error_code, error_msg, success)
        if (.not. success) return

        call ParseComplexString(str1, re_a, im_a, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param1 is not a valid complex number'
            return
        end if

        call ParseComplexString(str2, re_b, im_b, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param2 is not a valid complex number'
            return
        end if

        call PerformComplexArithmeticOp(OP_ADD, re_a, im_a, re_b, im_b, &
                                        re_r, im_r, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_string_result(json, result_value, BuildComplexResultString(re_r, im_r))

    end subroutine AddComplex_handler

    !****************************************************************************
    !  SUBROUTINE: SubtractComplex_handler
    !  PURPOSE:  JSON-RPC handler for complex number subtraction
    !****************************************************************************
    module subroutine SubtractComplex_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        character(len=:), allocatable :: str1, str2
        real(8) :: re_a, im_a, re_b, im_b, re_r, im_r
        logical :: success, parse_ok

        error_code = 0
        call GetTwoComplexStringParams(json, params_array, str1, str2, &
                                       error_code, error_msg, success)
        if (.not. success) return

        call ParseComplexString(str1, re_a, im_a, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param1 is not a valid complex number'
            return
        end if

        call ParseComplexString(str2, re_b, im_b, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param2 is not a valid complex number'
            return
        end if

        call PerformComplexArithmeticOp(OP_SUBTRACT, re_a, im_a, re_b, im_b, &
                                        re_r, im_r, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_string_result(json, result_value, BuildComplexResultString(re_r, im_r))

    end subroutine SubtractComplex_handler

    !****************************************************************************
    !  SUBROUTINE: MultiplyComplex_handler
    !  PURPOSE:  JSON-RPC handler for complex number multiplication
    !  FORMULA: (re_a + im_a*i)(re_b + im_b*i) = (re_a*re_b - im_a*im_b)
    !                                           + (re_a*im_b + im_a*re_b)*i
    !****************************************************************************
    module subroutine MultiplyComplex_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        character(len=:), allocatable :: str1, str2
        real(8) :: re_a, im_a, re_b, im_b, re_r, im_r
        logical :: success, parse_ok

        error_code = 0
        call GetTwoComplexStringParams(json, params_array, str1, str2, &
                                       error_code, error_msg, success)
        if (.not. success) return

        call ParseComplexString(str1, re_a, im_a, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param1 is not a valid complex number'
            return
        end if

        call ParseComplexString(str2, re_b, im_b, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param2 is not a valid complex number'
            return
        end if

        call PerformComplexArithmeticOp(OP_MULTIPLY, re_a, im_a, re_b, im_b, &
                                        re_r, im_r, error_code, error_msg)
        if (error_code /= 0) return

        call jsonrpc_set_string_result(json, result_value, BuildComplexResultString(re_r, im_r))

    end subroutine MultiplyComplex_handler

    !****************************************************************************
    !  SUBROUTINE: DivideComplex_handler
    !  PURPOSE:  JSON-RPC handler for complex number division with zero-check
    !
    !  INPUT:  params[1] = complex string a, params[2] = complex string b
    !  OUTPUT: result string = a / b
    !  FORMULA: multiply numerator and denominator by conjugate of b:
    !           (re_a + im_a*i) / (re_b + im_b*i)
    !         = (re_a*re_b + im_a*im_b) / denom
    !         + (im_a*re_b - re_a*im_b) / denom * i
    !           where denom = re_b^2 + im_b^2
    !  ERROR: returns JSONRPC_DIVISION_BY_ZERO if denom == 0
    !****************************************************************************
    module subroutine DivideComplex_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        character(len=:), allocatable :: str1, str2
        real(8) :: re_a, im_a, re_b, im_b, re_r, im_r, denom
        logical :: success, parse_ok

        error_code = 0
        call GetTwoComplexStringParams(json, params_array, str1, str2, &
                                       error_code, error_msg, success)
        if (.not. success) return

        call ParseComplexString(str1, re_a, im_a, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param1 is not a valid complex number'
            return
        end if

        call ParseComplexString(str2, re_b, im_b, parse_ok)
        if (.not. parse_ok) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg  = 'Invalid params: param2 is not a valid complex number'
            return
        end if

        denom = (re_b * re_b) + (im_b * im_b)

        if (denom == 0.0d0) then
            error_code = JSONRPC_DIVISION_BY_ZERO
            error_msg  = 'Division by zero: divisor is 0+0i'
            return
        end if

        re_r = ((re_a * re_b) + (im_a * im_b)) / denom
        im_r = ((im_a * re_b) - (re_a * im_b)) / denom

        print *, 'DivideComplex: (', re_a, '+', im_a, 'i) / (', re_b, '+', im_b, 'i) = (', re_r, '+', im_r, 'i)'

        call jsonrpc_set_string_result(json, result_value, BuildComplexResultString(re_r, im_r))

    end subroutine DivideComplex_handler

end submodule Functions_ArithmeticOps
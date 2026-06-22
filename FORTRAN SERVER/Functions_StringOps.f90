!****************************************************************************
!
!  SUBMODULE: Functions_StringOps
!
!  PURPOSE:  String operation handlers (SendMessage, NamedParameters)
!
!****************************************************************************
submodule (Functions) Functions_StringOps
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: SendMessage_handler
    !  PURPOSE:  JSON-RPC handler for sending/echoing string messages
    !****************************************************************************
    module subroutine SendMessage_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        character(len=:), allocatable :: message
        logical :: success

        error_code = 0

        if (.not. associated(params_array)) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: params must be an array'
            return
        end if

        call jsonrpc_get_string_param(json, params_array, 1, message, success)
        if (.not. success) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: message must be a string'
            return
        end if

        if (len(message) == 0) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: message cannot be empty'
            return
        end if

        print *, 'SendMessage: ', trim(message)
        call jsonrpc_set_string_result(json, result_value, message)

    end subroutine SendMessage_handler

    !****************************************************************************
    !  SUBROUTINE: NamedParameters_handler
    !  PURPOSE:  JSON-RPC handler for demonstrating named parameter handling
    !****************************************************************************
    module subroutine NamedParameters_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        integer :: param_count, i
        character(len=:), allocatable :: param_name, param_value, response_message
        character(len=20) :: primitive_type
        character(len=10) :: count_str
        logical :: success
        type(json_value), pointer :: param_item

        error_code = 0
        param_count = 0

        if (.not. associated(params_array)) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: params must be provided'
            return
        end if

        call json%info(params_array, n_children=param_count)

        if (param_count == 0) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: at least one named parameter is required'
            return
        end if

        print *, 'NamedParameters: Received ', param_count, ' named parameter(s):'
        print *, ''

        do i = 1, param_count
            call json%get_child(params_array, i, param_item, success)

            if (.not. success) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Failed to retrieve parameter'
                return
            end if

            call json%info(param_item, name=param_name)

            if (.not. allocated(param_name)) then
                error_code = JSONRPC_INVALID_PARAMS
                error_msg = 'Failed to get parameter name'
                return
            end if

            call json%get(param_item, param_value)
            primitive_type = DeterminePrimitiveType(json, param_item)

            print *, '  KEY: ', trim(param_name), ', VALUE: ', trim(param_value), &
                     ', TYPE: ', trim(primitive_type)
        end do

        print *, ''
        print *, 'Total named parameters: ', param_count

        write(count_str, '(I0)') param_count
        response_message = 'The total number of named parameters received is: ' // trim(count_str)

        call jsonrpc_set_string_result(json, result_value, response_message)

    end subroutine NamedParameters_handler

end submodule Functions_StringOps
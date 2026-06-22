!****************************************************************************
!
!  MODULE: jsonrpc_types
!
!  PURPOSE:  Type definitions for JSON-RPC library
!
!****************************************************************************
module jsonrpc_types
    use json_module
    implicit none
    
    integer, parameter :: MAX_METHODS = 100
    
    abstract interface
        subroutine method_handler_interface(json, params_array, result_value, error_code, error_msg)
            use json_module
            type(json_core), intent(inout) :: json
            type(json_value), pointer, intent(in) :: params_array
            type(json_value), pointer, intent(inout) :: result_value
            integer, intent(out) :: error_code
            character(len=:), allocatable, intent(out) :: error_msg
        end subroutine method_handler_interface
    end interface
    
    type :: method_entry
        character(len=64) :: method_name
        procedure(method_handler_interface), pointer, nopass :: handler => null()
        logical :: is_registered = .false.
    end type method_entry
    
end module jsonrpc_types


!****************************************************************************
!
!  MODULE: jsonrpc_validation
!
!  PURPOSE:  JSON-RPC 2.0 protocol validation and utility functions
!
!****************************************************************************
module jsonrpc_validation
    implicit none

    private
    public :: JSONRPC_PARSE_ERROR, JSONRPC_INVALID_REQUEST, JSONRPC_METHOD_NOT_FOUND
    public :: JSONRPC_INVALID_PARAMS, JSONRPC_INTERNAL_ERROR
    public :: JSONRPC_SERVER_ERROR
    public :: PARAMS_ABSENT, PARAMS_ARRAY, PARAMS_OBJECT, PARAMS_INVALID
    public :: jsonrpc_skip_whitespace
    public :: jsonrpc_scan_to_value
    public :: jsonrpc_is_notification
    public :: jsonrpc_validate_version
    public :: jsonrpc_is_empty_batch
    public :: jsonrpc_validate_method_field
    public :: jsonrpc_get_params_type
    public :: jsonrpc_extract_id_token
    public :: jsonrpc_validate_id_field
    public :: jsonrpc_is_all_notification_batch
    public :: jsonrpc_build_error_response

    ! JSON-RPC 2.0 Standard Error Codes
    integer, parameter :: JSONRPC_PARSE_ERROR = -32700
    integer, parameter :: JSONRPC_INVALID_REQUEST = -32600
    integer, parameter :: JSONRPC_METHOD_NOT_FOUND = -32601
    integer, parameter :: JSONRPC_INVALID_PARAMS = -32602
    integer, parameter :: JSONRPC_INTERNAL_ERROR = -32603
    integer, parameter :: JSONRPC_SERVER_ERROR = -32000

    ! Params structure type constants
    integer, parameter :: PARAMS_ABSENT  =  0
    integer, parameter :: PARAMS_ARRAY   =  1
    integer, parameter :: PARAMS_OBJECT  =  2
    integer, parameter :: PARAMS_INVALID = -1

contains

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_skip_whitespace
    !  PURPOSE:  Skip over whitespace characters in a JSON string
    !****************************************************************************
    subroutine jsonrpc_skip_whitespace(jsonString, strLength, scan_pos)
        character(len=*), intent(in)    :: jsonString
        integer,          intent(in)    :: strLength
        integer,          intent(inout) :: scan_pos

        do while (scan_pos <= strLength)
            select case (jsonString(scan_pos:scan_pos))
                case (' ', char(9), char(10), char(13))
                    scan_pos = scan_pos + 1
                case default
                    exit
            end select
        end do

    end subroutine jsonrpc_skip_whitespace

    !****************************************************************************
    !  FUNCTION: jsonrpc_scan_to_value
    !  PURPOSE:  Locates the value portion of a JSON key:value pair
    !****************************************************************************
    function jsonrpc_scan_to_value(jsonString, strLength, key, scan_pos) result(found)
        character(len=*), intent(in)  :: jsonString
        integer,          intent(in)  :: strLength
        character(len=*), intent(in)  :: key
        integer,          intent(out) :: scan_pos
        logical                       :: found

        integer :: key_pos

        found    = .false.
        scan_pos = 0

        key_pos = index(jsonString(1:strLength), key)
        if (key_pos == 0) return

        scan_pos = key_pos + len(key)
        call jsonrpc_skip_whitespace(jsonString, strLength, scan_pos)

        if (scan_pos > strLength) return
        if (jsonString(scan_pos:scan_pos) /= ':') return

        scan_pos = scan_pos + 1
        call jsonrpc_skip_whitespace(jsonString, strLength, scan_pos)

        if (scan_pos > strLength) return

        found = .true.

    end function jsonrpc_scan_to_value

    !****************************************************************************
    !  FUNCTION: jsonrpc_is_notification
    !  PURPOSE:  Check if JSON-RPC message is a notification (missing "id" field)
    !****************************************************************************
    function jsonrpc_is_notification(jsonString, strLength) result(is_notif)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        logical :: is_notif

        character(len=:), allocatable :: id_token

        id_token = jsonrpc_extract_id_token(jsonString, strLength)

        if (trim(id_token) == 'null') then
            is_notif = .true.
        else
            is_notif = .false.
        end if

    end function jsonrpc_is_notification

    !****************************************************************************
    !  FUNCTION: jsonrpc_validate_version
    !  PURPOSE:  Validate that "jsonrpc" field is present and exactly "2.0"
    !****************************************************************************
    function jsonrpc_validate_version(jsonString, strLength) result(is_valid)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        logical :: is_valid

        integer :: scan_pos

        is_valid = .false.

        if (.not. jsonrpc_scan_to_value(jsonString, strLength, '"jsonrpc"', scan_pos)) return

        if (scan_pos + 4 > strLength) return
        if (jsonString(scan_pos:scan_pos + 4) == '"2.0"') then
            is_valid = .true.
        end if

    end function jsonrpc_validate_version

    !****************************************************************************
    !  FUNCTION: jsonrpc_is_empty_batch
    !  PURPOSE:  Check if a batch request contains zero items
    !****************************************************************************
    function jsonrpc_is_empty_batch(jsonString, strLength, startPos) result(is_empty)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        integer, intent(in) :: startPos
        logical :: is_empty

        integer :: scan_pos

        is_empty = .false.

        scan_pos = startPos + 1
        call jsonrpc_skip_whitespace(jsonString, strLength, scan_pos)

        if (scan_pos <= strLength) then
            if (jsonString(scan_pos:scan_pos) == ']') then
                is_empty = .true.
            end if
        end if

    end function jsonrpc_is_empty_batch

    !****************************************************************************
    !  FUNCTION: jsonrpc_validate_method_field
    !  PURPOSE:  Validate that "method" field is present and is a String
    !****************************************************************************
    function jsonrpc_validate_method_field(jsonString, strLength) result(is_valid)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        logical :: is_valid

        integer :: scan_pos

        is_valid = .false.

        if (.not. jsonrpc_scan_to_value(jsonString, strLength, '"method"', scan_pos)) return

        if (jsonString(scan_pos:scan_pos) == '"') then
            is_valid = .true.
        end if

    end function jsonrpc_validate_method_field

    !****************************************************************************
    !  FUNCTION: jsonrpc_get_params_type
    !  PURPOSE:  Inspect "params" field and return its type
    !****************************************************************************
    function jsonrpc_get_params_type(jsonString, strLength) result(params_type)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        integer :: params_type

        integer :: scan_pos

        params_type = PARAMS_ABSENT

        if (.not. jsonrpc_scan_to_value(jsonString, strLength, '"params"', scan_pos)) return

        select case (jsonString(scan_pos:scan_pos))
            case ('[')
                params_type = PARAMS_ARRAY
            case ('{')
                params_type = PARAMS_OBJECT
            case default
                params_type = PARAMS_INVALID
        end select

    end function jsonrpc_get_params_type

    !****************************************************************************
    !  FUNCTION: jsonrpc_extract_id_token
    !  PURPOSE:  Extract the raw "id" value token from a JSON-RPC request
    !****************************************************************************
    function jsonrpc_extract_id_token(jsonString, strLength) result(id_token)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        character(len=:), allocatable :: id_token

        integer :: scan_pos
        integer :: value_start
        integer :: value_end
        character(len=1) :: ch

        id_token = 'null'

        if (.not. jsonrpc_scan_to_value(jsonString, strLength, '"id"', scan_pos)) return

        value_start = scan_pos
        ch = jsonString(scan_pos:scan_pos)

        select case (ch)
            case ('"')
                scan_pos = scan_pos + 1
                do while (scan_pos <= strLength)
                    if (jsonString(scan_pos:scan_pos) == '\') then
                        scan_pos = scan_pos + 2
                    else if (jsonString(scan_pos:scan_pos) == '"') then
                        scan_pos = scan_pos + 1
                        exit
                    else
                        scan_pos = scan_pos + 1
                    end if
                end do
                value_end = scan_pos - 1

            case ('0':'9', '-')
                scan_pos = scan_pos + 1
                do while (scan_pos <= strLength)
                    ch = jsonString(scan_pos:scan_pos)
                    if (ch == ',' .or. ch == '}' .or. ch == ']' .or. &
                        ch == ' ' .or. ch == char(9) .or. &
                        ch == char(10) .or. ch == char(13)) then
                        exit
                    end if
                    scan_pos = scan_pos + 1
                end do
                value_end = scan_pos - 1

            case ('n')
                if (scan_pos + 3 <= strLength) then
                    if (jsonString(scan_pos:scan_pos + 3) == 'null') then
                        value_end = scan_pos + 3
                    else
                        return
                    end if
                else
                    return
                end if

            case default
                return

        end select

        if (value_end >= value_start) then
            id_token = jsonString(value_start:value_end)
        end if

    end function jsonrpc_extract_id_token

    !****************************************************************************
    !  FUNCTION: jsonrpc_validate_id_field
    !  PURPOSE:  Validate that "id" field, if present, is a valid type
    !            (string, number, or null) per JSON-RPC 2.0 specification
    !****************************************************************************
    function jsonrpc_validate_id_field(jsonString, strLength) result(is_valid)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        logical :: is_valid

        integer :: scan_pos
        character(len=1) :: first_char

        is_valid = .true.

        if (.not. jsonrpc_scan_to_value(jsonString, strLength, '"id"', scan_pos)) return

        first_char = jsonString(scan_pos:scan_pos)

        select case (first_char)
            case ('"')
                is_valid = .true.
            case ('0':'9', '-')
                is_valid = .true.
            case ('n')
                if (scan_pos + 3 <= strLength) then
                    if (jsonString(scan_pos:scan_pos + 3) == 'null') then
                        is_valid = .true.
                    else
                        is_valid = .false.
                    end if
                else
                    is_valid = .false.
                end if
            case ('t', 'f')
                is_valid = .false.
            case ('[', '{')
                is_valid = .false.
            case default
                is_valid = .false.
        end select

    end function jsonrpc_validate_id_field

    !****************************************************************************
    !  FUNCTION: jsonrpc_is_all_notification_batch
    !  PURPOSE:  Check if a batch request contains only notifications
    !            Per JSON-RPC 2.0: if all requests are notifications,
    !            the server MUST NOT return any response
    !****************************************************************************
    function jsonrpc_is_all_notification_batch(jsonString, strLength) result(all_notifications)
        character(len=*), intent(in) :: jsonString
        integer, intent(in) :: strLength
        logical :: all_notifications

        integer :: scan_pos
        integer :: brace_count
        integer :: bracket_count
        logical :: in_string
        logical :: found_id
        ! escaped tracks whether the previous character was an unescaped backslash.
        ! A simple prev_ch /= '\' check is wrong because \\" ends a string (the
        ! first \ escapes the second, leaving the " unescaped), whereas \" does not.
        logical :: escaped
        character(len=1) :: ch

        all_notifications = .true.
        scan_pos = 1
        brace_count = 0
        bracket_count = 0
        in_string = .false.
        found_id = .false.
        escaped = .false.

        do while (scan_pos <= strLength)
            if (jsonString(scan_pos:scan_pos) == '[') then
                bracket_count = 1
                scan_pos = scan_pos + 1
                exit
            end if
            scan_pos = scan_pos + 1
        end do

        do while (scan_pos <= strLength .and. bracket_count > 0)
            ch = jsonString(scan_pos:scan_pos)

            if (in_string) then
                if (escaped) then
                    escaped = .false.
                else if (ch == '\') then
                    escaped = .true.
                else if (ch == '"') then
                    in_string = .false.
                end if
            else
                select case (ch)
                    case ('"')
                        in_string = .true.

                    case ('{')
                        brace_count = brace_count + 1
                        found_id = .false.

                    case ('}')
                        brace_count = brace_count - 1
                        if (brace_count == 0 .and. found_id) then
                            all_notifications = .false.
                            return
                        end if

                    case ('[')
                        bracket_count = bracket_count + 1

                    case (']')
                        bracket_count = bracket_count - 1

                    case (':')
                        if (scan_pos >= 5) then
                            if (jsonString(scan_pos-4:scan_pos-1) == '"id"') then
                                found_id = .true.
                            end if
                        end if
                end select
            end if

            scan_pos = scan_pos + 1
        end do

    end function jsonrpc_is_all_notification_batch

    !****************************************************************************
    !  FUNCTION: jsonrpc_build_error_response
    !  PURPOSE:  Build a JSON-RPC 2.0 error response string without using json-fortran
    !            This is useful for generating error responses when json-fortran
    !            might not be available or when a simple string builder is needed
    !  RETURNS:  Allocatable string containing the complete JSON-RPC error response
    !  EXAMPLE:  response = jsonrpc_build_error_response(-32700, "Parse error", "null")
    !            Returns: {"jsonrpc":"2.0","error":{"code":-32700,"message":"Parse error"},"id":null}
    !****************************************************************************
    function jsonrpc_build_error_response(error_code, error_message, id_token) result(response)
        integer, intent(in) :: error_code
        character(len=*), intent(in) :: error_message
        character(len=*), intent(in) :: id_token
        character(len=:), allocatable :: response

        character(len=20) :: code_str
        character(len=:), allocatable :: escaped_message

        ! Convert error code to string
        write(code_str, '(I0)') error_code

        ! Escape special characters in error message for JSON
        escaped_message = escape_json_string(error_message)

        ! Build the JSON-RPC error response string
        response = '{"jsonrpc":"2.0","error":{"code":' // trim(code_str) // &
                   ',"message":"' // escaped_message // '"},"id":' // trim(id_token) // '}'

    end function jsonrpc_build_error_response

    !****************************************************************************
    !  FUNCTION: escape_json_string
    !  PURPOSE:  Escape special characters in a string for embedding in a JSON
    !            string literal.  Handles: \" \\ \b \t \n \f \r
    !
    !  Two-pass: Phase 1 calculates the exact output byte count so Phase 3 can
    !  write without bounds risk.  INVARIANT in both passes: out_len = number of
    !  characters committed so far; next write always goes to (out_len + 1).
    !****************************************************************************
    function escape_json_string(input_str) result(output_str)
        character(len=*), intent(in) :: input_str
        character(len=:), allocatable :: output_str
        integer :: i, out_len, required_len
        character(len=1) :: ch

        ! Phase 1: calculate exact output length
        required_len = 0
        do i = 1, len(input_str)
            ch = input_str(i:i)
            select case (ch)
                case ('"', '\')
                    required_len = required_len + 2
                case (char(8), char(9), char(10), char(12), char(13))
                    ! ASCII 8=BS 9=HT 10=LF 12=FF 13=CR  (each expands to 2-char \x sequence)
                    required_len = required_len + 2
                case default
                    required_len = required_len + 1
            end select
        end do

        ! Phase 2: allocate exact size
        allocate(character(len=required_len) :: output_str)

        ! Phase 3: build escaped string
        out_len = 0
        do i = 1, len(input_str)
            ch = input_str(i:i)
            select case (ch)
                case ('"')
                    output_str(out_len+1:out_len+2) = '\"'
                    out_len = out_len + 2
                case ('\')
                    output_str(out_len+1:out_len+2) = '\\'
                    out_len = out_len + 2
                case (char(8))   ! Backspace → \b
                    output_str(out_len+1:out_len+2) = '\b'
                    out_len = out_len + 2
                case (char(9))   ! Horizontal tab → \t
                    output_str(out_len+1:out_len+2) = '\t'
                    out_len = out_len + 2
                case (char(10))  ! Line feed → \n
                    output_str(out_len+1:out_len+2) = '\n'
                    out_len = out_len + 2
                case (char(12))  ! Form feed → \f
                    output_str(out_len+1:out_len+2) = '\f'
                    out_len = out_len + 2
                case (char(13))  ! Carriage return → \r
                    output_str(out_len+1:out_len+2) = '\r'
                    out_len = out_len + 2
                case default
                    output_str(out_len+1:out_len+1) = ch
                    out_len = out_len + 1
            end select
        end do

    end function escape_json_string
    
end module jsonrpc_validation


!****************************************************************************
!
!  MODULE: jsonrpc_utils
!
!  PURPOSE:  Utility functions for JSON-RPC library
!
!****************************************************************************
module jsonrpc_utils
    implicit none
    
contains

    !****************************************************************************
    !  FUNCTION: GetTimeStamp
    !  PURPOSE:  Get current time as HH:MM:SS string
    !****************************************************************************
    function GetTimeStamp() result(timestamp)
        character(len=8) :: timestamp
        integer :: values(8)
        
        call date_and_time(values=values)
        write(timestamp, '(I2.2,":",I2.2,":",I2.2)') values(5), values(6), values(7)
        
    end function GetTimeStamp
    
    !****************************************************************************
    !  FUNCTION: FindHeaderEnd
    !  PURPOSE:  Locate the end of HTTP-style headers (marked by CRLF CRLF)
    !****************************************************************************
    function FindHeaderEnd(buffer, buffer_len) result(header_end)
        character(len=*), intent(in) :: buffer
        integer, intent(in) :: buffer_len
        integer :: header_end
        integer :: i
        
        header_end = 0
        do i = 1, buffer_len - 3
            if (buffer(i:i+3) == char(13)//char(10)//char(13)//char(10)) then
                header_end = i - 1
                return
            end if
        end do
    end function FindHeaderEnd
    
    !****************************************************************************
    !  FUNCTION: ParseContentLength
    !  PURPOSE:  Extract Content-Length value from HTTP-style headers
    !****************************************************************************
    function ParseContentLength(buffer, header_end) result(content_length)
        character(len=*), intent(in) :: buffer
        integer, intent(in) :: header_end
        integer :: content_length
        integer :: i, j, read_status
        character(len=20) :: num_str
        
        content_length = 0
        
        do i = 1, header_end - 14  ! -14 ensures i+14 stays within header_end for the 15-char token
            if (buffer(i:i+14) == 'Content-Length:') then
                j = i + 15
                ! Skip whitespace after colon
                do while (j <= header_end .and. &
                         (buffer(j:j) == ' ' .or. buffer(j:j) == char(9)))
                    j = j + 1
                end do
                
                ! Extract numeric value
                num_str = ''
                do while (j <= header_end .and. &
                         buffer(j:j) /= char(13) .and. buffer(j:j) /= char(10))
                    num_str = trim(num_str) // buffer(j:j)
                    j = j + 1
                end do
                
                ! Convert to integer
                read(num_str, *, iostat=read_status) content_length
                if (read_status /= 0) content_length = 0
                return
            end if
        end do
    end function ParseContentLength

end module jsonrpc_utils


!****************************************************************************
!
!  MODULE: jsonrpc_server_state
!
!  PURPOSE:  Runtime state shared between the JSON-RPC transport layer
!            (ServerComm) and application handler code.
!            Holds the active named pipe handle and per-request flags.
!
!  NOTE:     This module uses Windows named pipe types (ifwin/ifwinty) and
!            is intentionally Windows-specific.  Consumers targeting a
!            different transport may replace g_hPipe with an equivalent
!            transport handle and modify StorePipeHandle accordingly.
!
!****************************************************************************
module jsonrpc_server_state
    use ifwin
    use ifwinty
    implicit none

    integer(HANDLE), save :: g_hPipe = 0
    logical,         save :: g_is_notification_mode = .false.

contains

    subroutine StorePipeHandle(hPipe)
        integer(HANDLE), intent(in) :: hPipe
        g_hPipe = hPipe
    end subroutine StorePipeHandle

    subroutine StoreNotificationMode(is_notification)
        logical, intent(in) :: is_notification
        g_is_notification_mode = is_notification
    end subroutine StoreNotificationMode

    function GetPipeHandle() result(hPipe)
        integer(HANDLE) :: hPipe
        hPipe = g_hPipe
    end function GetPipeHandle

    function GetNotificationMode() result(is_notification)
        logical :: is_notification
        is_notification = g_is_notification_mode
    end function GetNotificationMode

end module jsonrpc_server_state


!****************************************************************************
!
!  MODULE: jsonrpc_protocol
!
!  PURPOSE:  Core JSON-RPC 2.0 protocol implementation
!
!****************************************************************************
module jsonrpc_protocol
    use json_module
    use jsonrpc_types
    use jsonrpc_utils
    use jsonrpc_validation
    implicit none
    
    private
    public :: jsonrpc_init, jsonrpc_shutdown, jsonrpc_register_method, &
              jsonrpc_parse_message, jsonrpc_parse_batch, &
              jsonrpc_create_response, &
              jsonrpc_create_error_response, jsonrpc_extract_json
    
    type(method_entry), dimension(MAX_METHODS) :: method_registry
    integer :: num_registered_methods = 0
    logical :: library_initialized = .false.
    
contains

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_init
    !  PURPOSE:  Initialize the JSON-RPC library
    !****************************************************************************
    subroutine jsonrpc_init(success)
        logical, intent(out) :: success
        integer :: i
        
        num_registered_methods = 0
        method_registry(:)%is_registered = .false.

        ! Procedure pointers are not automatically nullified on re-initialization;
        ! explicit nullification prevents stale pointer dereferences on re-use
        do i = 1, MAX_METHODS
            method_registry(i)%handler => null()
        end do

        library_initialized = .true.
        success = .true.

    end subroutine jsonrpc_init
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_shutdown
    !  PURPOSE:  Shutdown the JSON-RPC library and clean up resources
    !****************************************************************************
    subroutine jsonrpc_shutdown()
        integer :: i
        
        num_registered_methods = 0
        method_registry(:)%is_registered = .false.
        
        do i = 1, MAX_METHODS
            method_registry(i)%handler => null()
        end do
        
        library_initialized = .false.
        
    end subroutine jsonrpc_shutdown
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_register_method
    !  PURPOSE:  Register a method handler for a specific JSON-RPC method name
    !****************************************************************************
    subroutine jsonrpc_register_method(method_name, handler, success)
        character(len=*), intent(in) :: method_name
        procedure(method_handler_interface) :: handler
        logical, intent(out) :: success
        
        success = .false.
        
        if (.not. library_initialized) return
        if (num_registered_methods >= MAX_METHODS) return
        
        num_registered_methods = num_registered_methods + 1
        method_registry(num_registered_methods)%method_name = trim(method_name)
        method_registry(num_registered_methods)%handler => handler
        method_registry(num_registered_methods)%is_registered = .true.
        
        success = .true.
        
    end subroutine jsonrpc_register_method
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_extract_json
    !  PURPOSE:  Extract JSON content from HTTP-style message with headers
    !****************************************************************************
    subroutine jsonrpc_extract_json(buffer, buffer_len, json_content, &
                                     json_length, success)
        character(len=*), intent(in) :: buffer
        integer, intent(in) :: buffer_len
        character(len=*), intent(out) :: json_content
        integer, intent(out) :: json_length
        logical, intent(out) :: success
        
        integer :: header_end, content_length, json_start
        
        success = .false.
        json_length = 0
        json_content = ''
        
        header_end = FindHeaderEnd(buffer, buffer_len)
        if (header_end == 0) return
        
        ! header_end is the last header character; positions +1..+4 are CRLF CRLF;
        ! +5 is therefore the first byte of the JSON content body
        json_start = header_end + 5
        
        content_length = ParseContentLength(buffer, header_end)
        if (content_length == 0) return
        
        if (json_start + content_length - 1 > buffer_len) return
        
        json_content = buffer(json_start:json_start+content_length-1)
        json_length = content_length
        success = .true.
        
    end subroutine jsonrpc_extract_json
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_parse_batch
    !  PURPOSE:  Parse and process a JSON-RPC batch request
    !****************************************************************************
    subroutine jsonrpc_parse_batch(json_content, response_json, success, error_code_out)
        character(len=*), intent(in) :: json_content
        character(len=:), allocatable, intent(out) :: response_json
        logical, intent(out) :: success
        integer, intent(out), optional :: error_code_out
        
        type(json_core) :: json
        type(json_value), pointer :: root, request_item
        character(len=:), allocatable :: single_request_json, single_response_json
        integer :: num_requests, i, rpc_error_code, final_error_code
        integer :: response_count
        integer :: var_type
        logical :: parse_success
        character(len=:), allocatable :: batch_response
        
        success = .false.
        final_error_code = 0
        response_count = 0
        batch_response = '['
        
        call json%initialize()
        call json%deserialize(root, trim(json_content))
        
        if (json%failed()) then
            final_error_code = JSONRPC_PARSE_ERROR
            call jsonrpc_create_error_response(json, JSONRPC_PARSE_ERROR, &
                'Parse error', 1, response_json)
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            return
        end if

        call json%info(root, var_type=var_type, n_children=num_requests)

        if (var_type /= json_array) then
            final_error_code = JSONRPC_INVALID_REQUEST
            call jsonrpc_create_error_response(json, JSONRPC_INVALID_REQUEST, &
                'Invalid Request - expected array for batch request', 1, response_json)
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            return
        end if

        if (num_requests == 0) then
            final_error_code = JSONRPC_INVALID_REQUEST
            call jsonrpc_create_error_response(json, JSONRPC_INVALID_REQUEST, &
                'Invalid Request - batch cannot be empty', 1, response_json)
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            return
        end if
        
        do i = 1, num_requests
            call json%get_child(root, i, request_item)
            
            if (.not. associated(request_item)) cycle
            
            call json%serialize(request_item, single_request_json)
            
            if (json%failed()) then
                single_response_json = '{"jsonrpc":"2.0","error":{"code":-32603,' // &
                    '"message":"Internal error serializing batch item"},"id":null}'
            else
                call jsonrpc_parse_message(single_request_json, single_response_json, &
                                          parse_success, rpc_error_code)
                
                if (.not. parse_success) then
                    single_response_json = '{"jsonrpc":"2.0","error":{"code":-32603,' // &
                        '"message":"Internal error processing batch item"},"id":null}'
                end if
            end if
            
            if (len(single_response_json) > 0) then
                if (response_count > 0) then
                    batch_response = batch_response // ','
                end if
                batch_response = batch_response // single_response_json
                response_count = response_count + 1
            end if
        end do
        
        batch_response = batch_response // ']'
        
        ! Empty response signals caller that every item in the batch was a
        ! notification; per JSON-RPC 2.0 the server must not send any reply
        if (response_count == 0) then
            response_json = ''
        else
            response_json = batch_response
        end if
        
        call json%destroy(root)
        success = .true.
        if (present(error_code_out)) error_code_out = final_error_code
        
    end subroutine jsonrpc_parse_batch
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_parse_message
    !  PURPOSE:  Parse and process a single JSON-RPC request
    !****************************************************************************
    subroutine jsonrpc_parse_message(json_content, response_json, success, error_code_out)
        character(len=*), intent(in) :: json_content
        character(len=:), allocatable, intent(out) :: response_json
        logical, intent(out) :: success
        integer, intent(out), optional :: error_code_out
        
        type(json_core) :: json
        type(json_value), pointer :: root, method_val, params_array, id_val, result_val
        type(json_value), pointer :: version_val
        character(len=:), allocatable :: method_name
        character(len=:), allocatable :: error_msg
        character(len=:), allocatable :: version_str
        integer :: request_id, error_code, i
        integer :: var_type
        integer :: final_error_code
        logical :: found, method_found
        logical :: is_notification

        success = .false.
        final_error_code = 0
        request_id = 1  ! Fallback used in error responses emitted before the id field is parsed

        call json%initialize()
        call json%deserialize(root, trim(json_content))

        if (json%failed()) then
            final_error_code = JSONRPC_PARSE_ERROR
            response_json = jsonrpc_build_error_response(JSONRPC_PARSE_ERROR, &
                'Parse error', 'null')
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            success = .true.
            return
        end if

        call json%get(root, 'jsonrpc', version_val, found)
        if (.not. found) then
            final_error_code = JSONRPC_INVALID_REQUEST
            response_json = jsonrpc_build_error_response(JSONRPC_INVALID_REQUEST, &
                'Invalid Request - missing jsonrpc version field', 'null')
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            success = .true.
            return
        end if

        call json%info(version_val, var_type=var_type)
        if (var_type /= json_string) then
            final_error_code = JSONRPC_INVALID_REQUEST
            response_json = jsonrpc_build_error_response(JSONRPC_INVALID_REQUEST, &
                'Invalid Request - jsonrpc version must be a string', 'null')
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            success = .true.
            return
        end if

        call json%get(version_val, version_str)
        if (trim(version_str) /= '2.0') then
            final_error_code = JSONRPC_INVALID_REQUEST
            response_json = jsonrpc_build_error_response(JSONRPC_INVALID_REQUEST, &
                'Invalid Request - unsupported jsonrpc version (expected "2.0")', 'null')
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            success = .true.
            return
        end if

        is_notification = .false.

        call json%get(root, 'id', id_val, found)
        if (.not. found) then
            is_notification = .true.
        else
            call json%info(id_val, var_type=var_type)
            ! json-fortran may classify a JSON integer literal as json_real depending
            ! on parse context; accept both and let json%get() coerce to integer
            if (var_type /= json_integer .and. var_type /= json_real) then
                final_error_code = JSONRPC_INVALID_REQUEST
                ! id is present but not an integer; per spec response id MUST be null
                response_json = jsonrpc_build_error_response(JSONRPC_INVALID_REQUEST, &
                    'Invalid Request - id must be an integer', 'null')
                call json%destroy(root)
                if (present(error_code_out)) error_code_out = final_error_code
                success = .true.
                return
            end if

            call json%get(id_val, request_id)
        end if
        
        call json%get(root, 'method', method_val, found)
        if (.not. found) then
            final_error_code = JSONRPC_INVALID_REQUEST
            call jsonrpc_create_error_response(json, JSONRPC_INVALID_REQUEST, &
                'Invalid Request - missing method field', request_id, response_json)
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            success = .true.
            return
        end if

        call json%info(method_val, var_type=var_type)
        if (var_type /= json_string) then
            final_error_code = JSONRPC_INVALID_REQUEST
            call jsonrpc_create_error_response(json, JSONRPC_INVALID_REQUEST, &
                'Invalid Request - method must be a string', request_id, response_json)
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            success = .true.
            return
        end if

        call json%get(method_val, method_name)

        if (len_trim(method_name) == 0) then
            final_error_code = JSONRPC_INVALID_REQUEST
            call jsonrpc_create_error_response(json, JSONRPC_INVALID_REQUEST, &
                'Invalid Request - method name cannot be empty', request_id, response_json)
            call json%destroy(root)
            if (present(error_code_out)) error_code_out = final_error_code
            success = .true.
            return
        end if

        ! Reserved method names (rpc.*) must not be used by applications per JSON-RPC 2.0 spec
        if (len_trim(method_name) >= 4) then
            if (method_name(1:4) == 'rpc.') then
                final_error_code = JSONRPC_METHOD_NOT_FOUND
                call jsonrpc_create_error_response(json, JSONRPC_METHOD_NOT_FOUND, &
                    'Reserved method name: ' // trim(method_name), request_id, response_json)
                call json%destroy(root)
                if (present(error_code_out)) error_code_out = final_error_code
                success = .true.
                return
            end if
        end if

        call json%get(root, 'params', params_array, found)
        if (.not. found) then
            params_array => null()
        else
            call json%info(params_array, var_type=var_type)
            if (var_type /= json_array .and. var_type /= json_object) then
                final_error_code = JSONRPC_INVALID_REQUEST
                call jsonrpc_create_error_response(json, JSONRPC_INVALID_REQUEST, &
                    'Invalid Request - params must be array or object', request_id, response_json)
                call json%destroy(root)
                if (present(error_code_out)) error_code_out = final_error_code
                success = .true.
                return
            end if
        end if
        
        method_found = .false.
        do i = 1, num_registered_methods
            if (method_registry(i)%is_registered .and. &
                trim(method_registry(i)%method_name) == trim(method_name)) then
                
                method_found = .true.
                
                call json%create_null(result_val, 'result')
                
                error_code = 0
                call method_registry(i)%handler(json, params_array, &
                                result_val, error_code, error_msg)

                if (error_code /= 0 .and. .not. allocated(error_msg)) then
                    error_msg = 'Internal error in method handler'
                    error_code = JSONRPC_INTERNAL_ERROR
                end if
                
                if (.not. is_notification) then
                    if (error_code /= 0) then
                        final_error_code = error_code
                        call jsonrpc_create_error_response(json, error_code, &
                            error_msg, request_id, response_json)
                    else
                        final_error_code = 0
                        call jsonrpc_create_response(json, result_val, &
                            request_id, response_json)
                    end if
                else
                    response_json = ''
                end if

                exit
            end if
        end do

        if (.not. method_found) then
            if (.not. is_notification) then
                final_error_code = JSONRPC_METHOD_NOT_FOUND
                call jsonrpc_create_error_response(json, JSONRPC_METHOD_NOT_FOUND, &
                    'Method not found: ' // trim(method_name), request_id, response_json)
            else
                response_json = ''
            end if
        end if
        
        call json%destroy(root)
        success = .true.
        
        if (present(error_code_out)) error_code_out = final_error_code
        
    end subroutine jsonrpc_parse_message
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_create_response
    !  PURPOSE:  Create a JSON-RPC success response
    !****************************************************************************
    subroutine jsonrpc_create_response(json, result_value, request_id, response_json)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: result_value
        integer, intent(in) :: request_id
        character(len=:), allocatable, intent(out) :: response_json

        type(json_value), pointer :: root
        character(len=20) :: id_str

        call json%create_object(root, '')
        call json%add(root, 'jsonrpc', '2.0')
        call json%add(root, result_value)
        call json%add(root, 'id', request_id)

        call json%serialize(root, response_json)

        ! Fallback: if json-fortran serialization fails (e.g. result_value contains
        ! an unresolved pointer), return a well-formed error response instead of
        ! leaving response_json unallocated, which would silently drop the reply
        if (json%failed()) then
            write(id_str, '(I0)') request_id
            response_json = '{"jsonrpc":"2.0","error":{"code":-32603,' // &
                           '"message":"Critical internal error: result serialization failed"},' // &
                           '"id":' // trim(id_str) // '}'
        end if

        call json%destroy(root)

    end subroutine jsonrpc_create_response

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_create_error_response
    !  PURPOSE:  Create a JSON-RPC error response
    !****************************************************************************
    subroutine jsonrpc_create_error_response(json, error_code, error_message, &
                                             request_id, response_json)
        type(json_core), intent(inout) :: json
        integer, intent(in) :: error_code
        character(len=*), intent(in) :: error_message
        integer, intent(in) :: request_id
        character(len=:), allocatable, intent(out) :: response_json

        type(json_value), pointer :: root, error_obj
        character(len=20) :: code_str, id_str

        call json%create_object(root, '')
        call json%add(root, 'jsonrpc', '2.0')

        call json%create_object(error_obj, 'error')
        call json%add(error_obj, 'code', error_code)
        call json%add(error_obj, 'message', trim(error_message))
        call json%add(root, error_obj)

        call json%add(root, 'id', request_id)

        call json%serialize(root, response_json)

        if (json%failed()) then
            write(code_str, '(I0)') error_code
            write(id_str, '(I0)') request_id
            response_json = '{"jsonrpc":"2.0","error":{"code":' // trim(code_str) // &
                           ',"message":"' // trim(error_message) // '"},' // &
                           '"id":' // trim(id_str) // '}'
        end if

        call json%destroy(root)

    end subroutine jsonrpc_create_error_response

end module jsonrpc_protocol


!****************************************************************************
!
!  MODULE: jsonrpc_helpers
!
!  PURPOSE:  Helper functions for common parameter extraction patterns
!
!****************************************************************************
module jsonrpc_helpers
    use json_module
    implicit none
    
contains

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_get_int_param
    !  PURPOSE:  Extract integer parameter from params array
    !****************************************************************************
    subroutine jsonrpc_get_int_param(json, params_array, index, value, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        integer, intent(in) :: index
        integer, intent(out) :: value
        logical, intent(out) :: success
        
        type(json_value), pointer :: param_val
        logical :: found
        
        success = .false.
        
        if (.not. associated(params_array)) return
        
        call json%get_child(params_array, index, param_val, found)
        if (found) then
            call json%get(param_val, value)
            success = .true.
        end if
        
    end subroutine jsonrpc_get_int_param
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_get_string_param
    !  PURPOSE:  Extract string parameter from params array
    !****************************************************************************
    subroutine jsonrpc_get_string_param(json, params_array, index, value, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        integer, intent(in) :: index
        character(len=:), allocatable, intent(out) :: value
        logical, intent(out) :: success
        
        type(json_value), pointer :: param_val
        logical :: found
        
        success = .false.
        
        if (.not. associated(params_array)) return
        
        call json%get_child(params_array, index, param_val, found)
        if (found) then
            call json%get(param_val, value)
            success = .true.
        end if
    
    end subroutine jsonrpc_get_string_param
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_set_int_result
    !  PURPOSE:  Set integer result value
    !****************************************************************************
    subroutine jsonrpc_set_int_result(json, result_value, int_result)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(in) :: int_result
        
        if (associated(result_value)) call json%destroy(result_value)
        call json%create_integer(result_value, int_result, 'result')
        
    end subroutine jsonrpc_set_int_result
    
    !****************************************************************************
    !  SUBROUTINE: jsonrpc_set_string_result
    !  PURPOSE:  Set string result value
    !****************************************************************************
    subroutine jsonrpc_set_string_result(json, result_value, string_result)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(inout) :: result_value
        character(len=*), intent(in) :: string_result
        
        if (associated(result_value)) call json%destroy(result_value)
        call json%create_string(result_value, trim(string_result), 'result')
        
    end subroutine jsonrpc_set_string_result

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_get_named_int_param
    !  PURPOSE:  Extract integer parameter by name from a params object
    !****************************************************************************
    subroutine jsonrpc_get_named_int_param(json, params_obj, name, value, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_obj
        character(len=*), intent(in) :: name
        integer, intent(out) :: value
        logical, intent(out) :: success

        type(json_value), pointer :: param_val
        logical :: found

        success = .false.
        value   = 0

        if (.not. associated(params_obj)) return

        call json%get(params_obj, name, param_val, found)
        if (.not. found) return

        call json%get(param_val, value)
        if (json%failed()) then
            call json%clear_exceptions()
            return
        end if

        success = .true.

    end subroutine jsonrpc_get_named_int_param

    !****************************************************************************
    !  SUBROUTINE: jsonrpc_get_named_string_param
    !  PURPOSE:  Extract string parameter by name from a params object
    !****************************************************************************
    subroutine jsonrpc_get_named_string_param(json, params_obj, name, value, success)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_obj
        character(len=*), intent(in) :: name
        character(len=:), allocatable, intent(out) :: value
        logical, intent(out) :: success

        type(json_value), pointer :: param_val
        logical :: found

        success = .false.

        if (.not. associated(params_obj)) return

        call json%get(params_obj, name, param_val, found)
        if (.not. found) return

        call json%get(param_val, value)
        if (json%failed()) then
            call json%clear_exceptions()
            return
        end if

        success = .true.

    end subroutine jsonrpc_get_named_string_param

end module jsonrpc_helpers
!****************************************************************************
!
!  MODULE: ServerComm
!
!  PURPOSE:  Server communication functions for named pipe operations.
!            Provides message framing (Content-Length header protocol),
!            dispatch to JSON-RPC protocol handlers, and progress/error
!            response helpers.
!
!  NOTE:     Uses Windows named pipe I/O (ReadFile, WriteFile, etc.) and
!            is intentionally Windows-specific.  The named pipe lifecycle
!            (CreateNamedPipe, ConnectNamedPipe, CloseHandle) is the
!            responsibility of the application, not this module.
!            Consumers targeting a different transport may replace the
!            pipe I/O calls with their own transport layer.
!
!****************************************************************************
module ServerComm
    use ifwin
    use ifwinty
    use jsonrpc_protocol
    use jsonrpc_utils,        only: GetTimeStamp
    use jsonrpc_server_state,  only: StorePipeHandle, StoreNotificationMode
    use jsonrpc_validation,    only: JSONRPC_PARSE_ERROR, JSONRPC_SERVER_ERROR, JSONRPC_INTERNAL_ERROR
    implicit none

    character(len=2), parameter :: CRLF = char(13)//char(10)

contains

    !****************************************************************************
    !  SUBROUTINE: SendErrorResponse
    !  PURPOSE:  Send JSON-RPC error response to client via named pipe.
    !            Uses manual string building to avoid json-fortran dependency
    !            on error paths.
    !  PARAMS:   request_id  — string literal 'null' or a decimal integer string
    !                          (passed as character so callers that fire before
    !                          the id field is parsed can supply 'null' directly)
    !            error_data  — optional additional data field in error object
    !****************************************************************************
    subroutine SendErrorResponse(hPipe, error_code, error_message, request_id, error_data)
        integer(HANDLE), intent(in) :: hPipe
        integer, intent(in) :: error_code
        character(len=*), intent(in) :: error_message
        character(len=*), intent(in) :: request_id
        character(len=*), intent(in), optional :: error_data

        character(len=:), allocatable :: response_json, error_obj, escaped_msg
        character(len=4096), target :: message_buffer
        character(len=100) :: lengthStr
        character(len=20) :: code_str
        integer(DWORD) :: bytesWritten
        integer(BOOL) :: fSuccess
        integer :: msg_len, json_len, header_len
        integer(INT_PTR) :: buffer_ptr

        write(code_str, '(I0)') error_code

        escaped_msg = escape_simple(trim(error_message))

        ! Build error object, appending optional "data" field when present
        error_obj = '{"code":' // trim(code_str) // ',"message":"' // escaped_msg // '"'
        if (present(error_data)) then
            error_obj = error_obj // ',"data":"' // trim(error_data) // '"'
        end if
        error_obj = error_obj // '}'

        if (trim(request_id) == 'null') then
            response_json = '{"jsonrpc":"2.0","error":' // error_obj // ',"id":null}'
        else
            response_json = '{"jsonrpc":"2.0","error":' // error_obj // ',"id":' // &
                           trim(request_id) // '}'
        end if

        print *, ''
        print *, GetTimeStamp(), ' JSON response: ', trim(response_json)
        print *, ''
        print *, GetTimeStamp(), ' Sending response...'
        print *, ''

        json_len = len_trim(response_json)

        write(lengthStr, '(A,I0)') 'Content-Length: ', json_len
        header_len = len_trim(lengthStr)

        message_buffer = ''
        message_buffer(1:header_len)              = trim(lengthStr)
        message_buffer(header_len+1:header_len+2) = CRLF
        message_buffer(header_len+3:header_len+4) = CRLF
        message_buffer(header_len+5:header_len+4+json_len) = trim(response_json)

        msg_len = header_len + 4 + json_len

        buffer_ptr = LOC(message_buffer)
        fSuccess = WriteFile(hPipe, buffer_ptr, msg_len, bytesWritten, NULL)

        if (fSuccess == 0) then
            print *, 'ERROR: WriteFile failed!'
            print *, 'Error code:', GetLastError()
            return
        end if

        fSuccess = FlushFileBuffers(hPipe)

        if (bytesWritten /= msg_len) then
            print *, 'WARNING: Incomplete write'
            print *, 'Expected:', msg_len, 'bytes'
            print *, 'Written:', bytesWritten, 'bytes'
        end if

        print *, GetTimeStamp(), ' Response sent (', bytesWritten, ' bytes)'
        print *, ''

    end subroutine SendErrorResponse

    !****************************************************************************
    !  SUBROUTINE: SendProgressNotification
    !  PURPOSE:  Send progress update as JSON-RPC 2.0 notification to client.
    !            params is an array: [percent] or [percent, "message"]
    !****************************************************************************
    subroutine SendProgressNotification(hPipe, percent, message)
        integer(HANDLE), intent(in) :: hPipe
        integer, intent(in) :: percent
        character(len=*), intent(in), optional :: message

        character(len=:), allocatable :: notification_json, escaped_msg
        character(len=2048), target :: message_buffer
        character(len=100) :: lengthStr, percentStr
        integer(DWORD) :: bytesWritten
        integer(BOOL) :: fSuccess
        integer :: msg_len, json_len, header_len
        integer(INT_PTR) :: buffer_ptr

        write(percentStr, '(I0)') percent

        if (present(message)) then
            escaped_msg = escape_simple(message)
            notification_json = '{"jsonrpc":"2.0","method":"progress","params":[' &
                              // trim(percentStr) // ',"' // escaped_msg // '"]}'
        else
            notification_json = '{"jsonrpc":"2.0","method":"progress","params":[' &
                              // trim(percentStr) // ']}'
        end if

        json_len = len_trim(notification_json)

        write(lengthStr, '(A,I0)') 'Content-Length: ', json_len
        header_len = len_trim(lengthStr)

        message_buffer = ''
        message_buffer(1:header_len)              = trim(lengthStr)
        message_buffer(header_len+1:header_len+2) = CRLF
        message_buffer(header_len+3:header_len+4) = CRLF
        message_buffer(header_len+5:header_len+4+json_len) = trim(notification_json)

        msg_len = header_len + 4 + json_len

        buffer_ptr = LOC(message_buffer)
        fSuccess = WriteFile(hPipe, buffer_ptr, msg_len, bytesWritten, NULL)

        if (fSuccess == 0) then
            print *, 'WARNING: Failed to send progress notification (Error: ', GetLastError(), ')'
        else
            fSuccess = FlushFileBuffers(hPipe)
            if (bytesWritten /= msg_len) then
                print *, 'WARNING: Incomplete progress notification write'
                print *, '  Expected:', msg_len, 'bytes'
                print *, '  Written:', bytesWritten, 'bytes'
            end if
        end if

    end subroutine SendProgressNotification

    !****************************************************************************
    !  FUNCTION: escape_simple
    !  PURPOSE:  JSON string escaping for error messages and notification text.
    !            Handles: \" \\ \t \n \r
    !****************************************************************************
    function escape_simple(input_str) result(output_str)
        character(len=*), intent(in) :: input_str
        character(len=:), allocatable :: output_str
        integer :: i, out_pos
        character(len=1) :: ch

        allocate(character(len=len(input_str)*2) :: output_str)

        out_pos = 1
        do i = 1, len(input_str)
            ch = input_str(i:i)
            select case (ch)
                case ('"')
                    output_str(out_pos:out_pos+1) = '\"'
                    out_pos = out_pos + 2
                case ('\')
                    output_str(out_pos:out_pos+1) = '\\'
                    out_pos = out_pos + 2
                case (char(10))
                    output_str(out_pos:out_pos+1) = '\n'
                    out_pos = out_pos + 2
                case (char(13))
                    output_str(out_pos:out_pos+1) = '\r'
                    out_pos = out_pos + 2
                case (char(9))
                    output_str(out_pos:out_pos+1) = '\t'
                    out_pos = out_pos + 2
                case default
                    output_str(out_pos:out_pos) = ch
                    out_pos = out_pos + 1
            end select
        end do

        output_str = output_str(1:out_pos-1)

    end function escape_simple

    !****************************************************************************
    !  SUBROUTINE: ProcessMessages
    !  PURPOSE:  Main message processing loop for JSON-RPC requests.
    !            Reads framed messages from the named pipe, extracts the JSON
    !            body, classifies each message (batch / request / notification),
    !            dispatches to the appropriate protocol handler, and writes the
    !            response back to the pipe.
    !****************************************************************************
    subroutine ProcessMessages(hPipe, max_request_size)
        integer(HANDLE), intent(in) :: hPipe
        integer, intent(in) :: max_request_size

        character(len=1048576), target :: buffer
        character(len=:), allocatable  :: jsonContent
        character(len=4096), target :: message_buffer
        integer(DWORD) :: bytesRead, bytesWritten
        integer(BOOL) :: fSuccess
        character(len=:), allocatable :: response_json
        character(len=100) :: lengthStr
        integer :: json_length, msg_len, json_len, header_len
        integer :: i
        logical :: extract_success, parse_success
        integer :: error_code_out
        character(len=1024) :: size_data
        integer(INT_PTR) :: buffer_ptr, msg_buffer_ptr

        if (max_request_size > 1048576) then
            print *, 'ERROR: max_request_size exceeds 1 MB limit'
            return
        end if

        allocate(character(len=max_request_size) :: jsonContent)

        call StorePipeHandle(hPipe)

        do
            bytesRead = 0
            buffer = ''

            buffer_ptr = LOC(buffer)
            fSuccess = ReadFile(hPipe, buffer_ptr, max_request_size, bytesRead, NULL)

            if (fSuccess == 0) then
                if (GetLastError() == ERROR_BROKEN_PIPE) then
                    print *, ''
                    print *, GetTimeStamp(), ' [INFO] Client disconnected (broken pipe)'
                    print *, ''
                    exit
                else
                    print *, ''
                    print *, 'ERROR: ReadFile failed!'
                    print *, 'Error code:', GetLastError()
                    print *, ''
                    exit
                end if
            end if

            if (bytesRead == 0) then
                print *, ''
                print *, GetTimeStamp(), ' [INFO] Client disconnected (0 bytes read)'
                print *, ''
                exit
            end if

            if (bytesRead > max_request_size) then
                write(size_data, '(A,I0,A,I0,A)') 'received ', int(bytesRead), &
                    ' bytes, maximum permitted is ', max_request_size, ' bytes'
                call SendErrorResponse(hPipe, JSONRPC_SERVER_ERROR, &
                    'Server error: request exceeds maximum permitted size', 'null', trim(size_data))
                cycle
            end if

            write(*, '(A,A,I0,A)') GetTimeStamp(), ' [RECEIVED] ', bytesRead, ' bytes'

            call jsonrpc_extract_json(buffer, int(bytesRead), jsonContent, json_length, extract_success)

            if (.not. extract_success) then
                write(*, '(A)') 'ERROR -32700: Parse error - Invalid JSON received'
                write(*, '(A)') '***WARNING! CLIENT WILL AUTOMATICALLY DISCONNECT!'
                print *, ''
                call SendErrorResponse(hPipe, JSONRPC_PARSE_ERROR, 'Parse error', 'null')
                cycle
            end if

            write(*, '(A,A,A)') GetTimeStamp(), ' JSON content: ', trim(jsonContent(1:json_length))

            ! Skip leading whitespace to find the opening character of the JSON value
            i = 1
            do while (i <= json_length .and. &
                      (jsonContent(i:i) == ' '     .or. jsonContent(i:i) == char(9) .or. &
                       jsonContent(i:i) == char(10) .or. jsonContent(i:i) == char(13)))
                i = i + 1
            end do

            if (i <= json_length .and. jsonContent(i:i) == '[') then
                ! Batch request: top-level JSON array of request objects
                print *, GetTimeStamp(), ' [TYPE] Batch request'
                call StoreNotificationMode(.false.)
                print *, ''
                call jsonrpc_parse_batch(jsonContent(1:json_length), response_json, &
                                         parse_success, error_code_out)
            else
                ! Single message: classify as notification or request
                if (.not. json_has_id_key(jsonContent(1:json_length), json_length)) then
                    print *, GetTimeStamp(), ' [TYPE] Notification (no response expected)'
                    call StoreNotificationMode(.true.)
                else
                    print *, GetTimeStamp(), ' [TYPE] Request (response expected)'
                    call StoreNotificationMode(.false.)
                end if
                print *, ''
                call jsonrpc_parse_message(jsonContent(1:json_length), response_json, &
                                           parse_success, error_code_out)
            end if

            if (.not. parse_success) then
                print *, 'ERROR: message dispatch failed (internal error)'
                call SendErrorResponse(hPipe, JSONRPC_INTERNAL_ERROR, &
                    'Internal error during message processing', 'null')
                cycle
            end if

            if (allocated(response_json) .and. len(response_json) > 0) then
                print *, ''
                print *, GetTimeStamp(), ' JSON response: ', trim(response_json)
                print *, ''
                print *, GetTimeStamp(), ' Sending response...'
                print *, ''

                json_len = len_trim(response_json)

                write(lengthStr, '(A,I0)') 'Content-Length: ', json_len
                header_len = len_trim(lengthStr)

                message_buffer = ''
                message_buffer(1:header_len)              = trim(lengthStr)
                message_buffer(header_len+1:header_len+2) = CRLF
                message_buffer(header_len+3:header_len+4) = CRLF
                message_buffer(header_len+5:header_len+4+json_len) = trim(response_json)

                msg_len = header_len + 4 + json_len

                msg_buffer_ptr = LOC(message_buffer)
                fSuccess = WriteFile(hPipe, msg_buffer_ptr, msg_len, bytesWritten, NULL)

                if (fSuccess == 0) then
                    print *, 'ERROR: WriteFile failed!'
                    print *, 'Error code:', GetLastError()
                else
                    fSuccess = FlushFileBuffers(hPipe)

                    if (fSuccess == 0) then
                        print *, 'WARNING: FlushFileBuffers failed'
                        print *, 'Error code:', GetLastError()
                    end if

                    if (bytesWritten /= msg_len) then
                        print *, 'WARNING: Incomplete write'
                        print *, 'Expected:', msg_len, 'bytes'
                        print *, 'Written:', bytesWritten, 'bytes'
                    end if

                    print *, GetTimeStamp(), ' Response sent (', bytesWritten, ' bytes)'
                    print *, ''
                end if
            else
                print *, ''
                print *, GetTimeStamp(), ' [NOTIFICATION] No response sent (notification processed)'
                print *, ''
            end if

        end do

        if (allocated(jsonContent)) deallocate(jsonContent)

    end subroutine ProcessMessages

    !****************************************************************************
    !  FUNCTION: json_has_id_key
    !  PURPOSE:  Return .true. if the JSON string contains "id" as a key
    !            (i.e., "id" followed by optional whitespace and ':').
    !            Requiring the colon distinguishes a key occurrence from the
    !            text "id" appearing inside a string value.
    !****************************************************************************
    function json_has_id_key(json_str, json_len) result(found)
        character(len=*), intent(in) :: json_str
        integer, intent(in) :: json_len
        logical :: found

        integer :: i, j

        found = .false.

        i = 1
        do while (i <= json_len - 4)
            if (json_str(i:i+3) == '"id"') then
                j = i + 4
                do while (j <= json_len .and. &
                          (json_str(j:j) == ' ' .or. json_str(j:j) == char(9)))
                    j = j + 1
                end do
                if (j <= json_len .and. json_str(j:j) == ':') then
                    found = .true.
                    return
                end if
            end if
            i = i + 1
        end do

    end function json_has_id_key

end module ServerComm

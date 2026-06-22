!****************************************************************************
!
!  MODULE: ServerComm
!
!  PURPOSE:  Server communication and message-processing functions for the
!            named pipe JSON-RPC server.  Includes the main receive/dispatch
!            loop (ProcessMessages), error response helpers, and the
!            progress notification sender used by the static library.
!
!  NOTE:     This file is NOT compiled directly into the server program.
!            It is compiled into JSONRPCStaticLibraryRevA.lib and its
!            module interface is provided via servercomm.mod in
!            AdditionalDirectories.  This source copy is kept here for
!            reference only.
!
!****************************************************************************
module ServerComm
    use ifwin
    use ifwinty
    use jsonrpc_protocol
    use JsonRpcErrorCodes
    use Functions, only: StorePipeHandle, StoreNotificationMode
    implicit none

    character(len=2), parameter :: CRLF = char(13)//char(10)

contains

    !****************************************************************************
    !  FUNCTION: GetTimeStamp
    !  PURPOSE:  Get current time as HH:MM:SS formatted string
    !****************************************************************************
    function GetTimeStamp() result(timestamp)
        character(len=8) :: timestamp
        integer :: values(8)
        
        call date_and_time(values=values)
        write(timestamp, '(I2.2,":",I2.2,":",I2.2)') values(5), values(6), values(7)
        
    end function GetTimeStamp

    !****************************************************************************
    !  SUBROUTINE: SendErrorResponse
    !  PURPOSE:  Send JSON-RPC error response to client via named pipe
    !            Uses manual string building (no json-fortran dependency)
    !****************************************************************************
    subroutine SendErrorResponse(hPipe, error_code, error_message, request_id, error_data)
        integer(HANDLE), intent(in) :: hPipe
        integer, intent(in) :: error_code
        character(len=*), intent(in) :: error_message
        character(len=*), intent(in) :: request_id
        character(len=*), intent(in), optional :: error_data

        character(len=:), allocatable :: response_json, error_obj
        character(len=4096), target :: message_buffer
        character(len=100) :: lengthStr
        character(len=20) :: code_str
        integer(DWORD) :: bytesWritten
        integer(BOOL) :: fSuccess
        integer :: msg_len, json_len, header_len
        integer(INT_PTR) :: buffer_ptr

        write(code_str, '(I0)') error_code

        ! Build error object, appending optional "data" field when present
        error_obj = '{"code":' // trim(code_str) // ',"message":"' // trim(error_message) // '"'
        if (present(error_data)) then
            error_obj = error_obj // ',"data":"' // trim(error_data) // '"'
        end if
        error_obj = error_obj // '}'

        if (trim(request_id) == 'null') then
            response_json = '{"jsonrpc":"2.0","error":' // error_obj // ',"id":null}'
        else
            response_json = '{"jsonrpc":"2.0","error":' // error_obj // ',"id":"' // &
                           trim(request_id) // '"}'
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
        message_buffer(1:header_len) = trim(lengthStr)
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
    !  PURPOSE:  Send a progress update as a JSON-RPC notification to client.
    !  WIRE FORMAT: {"jsonrpc":"2.0","method":"progress","params":{"percent":N}}
    !               or with optional message field:
    !               {"jsonrpc":"2.0","method":"progress","params":{"percent":N,"message":"..."}}
    !  NOTE:     Uses object params {"percent":N}, NOT the array params [N] used
    !            by SendProgressToClient in Functions.f90.  The VB.NET client
    !            must handle both formats depending on which path sends the update.
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
            notification_json = '{"jsonrpc":"2.0","method":"progress","params":{"percent":' &
                              // trim(percentStr) // ',"message":"' // escaped_msg // '"}}'
        else
            notification_json = '{"jsonrpc":"2.0","method":"progress","params":{"percent":' &
                              // trim(percentStr) // '}}'
        end if

        json_len = len_trim(notification_json)

        write(lengthStr, '(A,I0)') 'Content-Length: ', json_len
        header_len = len_trim(lengthStr)

        message_buffer = ''
        message_buffer(1:header_len) = trim(lengthStr)
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
    !  PURPOSE:  Simple JSON string escaping for progress messages
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
    !  PURPOSE:  Main message processing loop for JSON-RPC requests
    !****************************************************************************
    subroutine ProcessMessages(hPipe, max_request_size)
        integer(HANDLE), intent(in) :: hPipe
        integer, intent(in) :: max_request_size

        character(len=1048576), target :: buffer
        character(len=1048576) :: jsonContent
        character(len=4096), target :: message_buffer
        integer(DWORD) :: bytesRead, bytesWritten
        integer(BOOL) :: fSuccess
        character(len=:), allocatable :: response_json
        character(len=100) :: lengthStr
        integer :: json_length, msg_len, json_len, header_len
        logical :: extract_success, parse_success
        integer :: error_code_out
        character(len=1024) :: size_data
        integer(INT_PTR) :: buffer_ptr, msg_buffer_ptr

        if (max_request_size > 1048576) then
            print *, 'ERROR: max_request_size exceeds buffer size'
            return
        end if

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

            if (.not. parse_success) then
                print *, 'ERROR: jsonrpc_parse_message failed'
                call SendErrorResponse(hPipe, JSONRPC_INTERNAL_ERROR, &
                    'Internal error during message parsing', 'null')
                cycle
            end if

            if (len(response_json) > 0) then
                print *, ''
                print *, GetTimeStamp(), ' JSON response: ', trim(response_json)
                print *, ''
                print *, GetTimeStamp(), ' Sending response...'
                print *, ''
                
                json_len = len_trim(response_json)
                
                write(lengthStr, '(A,I0)') 'Content-Length: ', json_len
                header_len = len_trim(lengthStr)
                
                message_buffer = ''
                message_buffer(1:header_len) = trim(lengthStr)
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

    end subroutine ProcessMessages

    !****************************************************************************
    !  FUNCTION: json_has_id_key
    !  PURPOSE:  Return .true. if the JSON string contains "id" as a key
    !            (i.e., "id" followed by optional whitespace and ':').
    !            A bare substring search for "id" is unreliable because that
    !            text can appear inside string values; requiring the colon
    !            confirms this is a key occurrence, not a value.
    !****************************************************************************
    function json_has_id_key(json_str, json_len) result(found)
        character(len=*), intent(in) :: json_str
        integer, intent(in) :: json_len
        logical :: found

        integer :: i, j

        found = .false.

        ! Scan character by character for the 4-char sequence "id"
        i = 1
        do while (i <= json_len - 4)
            if (json_str(i:i+3) == '"id"') then
                ! Advance past the key string and skip any whitespace
                j = i + 4
                do while (j <= json_len .and. &
                          (json_str(j:j) == ' ' .or. json_str(j:j) == char(9)))
                    j = j + 1
                end do
                ! A colon here confirms "id" is a JSON key, not a value
                if (j <= json_len .and. json_str(j:j) == ':') then
                    found = .true.
                    return
                end if
            end if
            i = i + 1
        end do

    end function json_has_id_key

end module ServerComm
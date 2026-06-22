!****************************************************************************
!
!  PROGRAM: FortranServerJSONRPC
!
!  PURPOSE:  JSON-RPC 2.0 server that listens on a Windows Named Pipe,
!            receives JSON-RPC requests from a VB.NET client, dispatches
!            them to handler subroutines, and returns JSON-RPC responses.
!
!  PIPE:     \\.\pipe\MyTestPipe  (synchronous, duplex, single instance)
!  FRAMING:  LSP-style Content-Length header + CRLF CRLF + JSON body
!  LIBRARY:  JSONRPCStaticLibraryRevA.lib (jsonrpc_protocol, jsonrpc_types)
!
!****************************************************************************
program FortranServerJSONRPC

    use ifwin
    use ifwinty
    use jsonrpc_protocol
    ! jsonrpc_types is used by jsonrpc_protocol, but Intel Fortran (ifx) does
    ! not re-export derived types through a use-association chain.  Import
    ! method_entry explicitly so the methods() array declaration compiles.
    use jsonrpc_types, only: method_entry
    use JsonRpcErrorCodes
    use ConsoleUtils
    use Functions
    use ServerComm
    implicit none

    integer(HANDLE) :: hPipe
    character(len=256) :: pipeName
    logical :: connected
    integer(BOOL) :: fSuccess
    logical :: init_success, reg_success
    integer, parameter :: MAX_REQUEST_SIZE = 1048576
    integer, parameter :: NUM_METHODS = 28
    type(method_entry) :: methods(NUM_METHODS)
    integer :: i

    call SetServerConsoleTitle('Fortran JSON-RPC 2.0 Server')
    call AlignConsoleWindowRight()

    call jsonrpc_init(init_success)
    if (.not. init_success) then
        print *, 'ERROR: Failed to initialize JSON-RPC library'
        print *, '       (Internal Error -32603)'
        stop
    end if

    ! Build method table: each entry maps a JSON-RPC method name string to a
    ! handler procedure pointer.  The loop below registers all entries with
    ! the library in a single pass rather than with 28 individual call blocks.
    methods( 1)%method_name = 'addint';                 methods( 1)%handler => AddInt_handler
    methods( 2)%method_name = 'subtractint';            methods( 2)%handler => SubtractInt_handler
    methods( 3)%method_name = 'multiplyint';            methods( 3)%handler => MultiplyInt_handler
    methods( 4)%method_name = 'divideint';              methods( 4)%handler => DivideInt_handler
    methods( 5)%method_name = 'addreal';                methods( 5)%handler => AddReal_handler
    methods( 6)%method_name = 'subtractreal';           methods( 6)%handler => SubtractReal_handler
    methods( 7)%method_name = 'multiplyreal';           methods( 7)%handler => MultiplyReal_handler
    methods( 8)%method_name = 'dividereal';             methods( 8)%handler => DivideReal_handler
    methods( 9)%method_name = 'addcomplex';             methods( 9)%handler => AddComplex_handler
    methods(10)%method_name = 'subtractcomplex';        methods(10)%handler => SubtractComplex_handler
    methods(11)%method_name = 'multiplycomplex';        methods(11)%handler => MultiplyComplex_handler
    methods(12)%method_name = 'dividecomplex';          methods(12)%handler => DivideComplex_handler
    methods(13)%method_name = 'sendmessage';            methods(13)%handler => SendMessage_handler
    methods(14)%method_name = 'namedparameters';        methods(14)%handler => NamedParameters_handler
    methods(15)%method_name = 'mandelbrotbenchmark';    methods(15)%handler => MandelbrotBenchmark_handler
    methods(16)%method_name = 'matrixintegertranspose'; methods(16)%handler => MatrixIntegerTranspose_handler
    methods(17)%method_name = 'matrixintegercopy';      methods(17)%handler => MatrixIntegerCopy_handler
    methods(18)%method_name = 'matrixintegersquare';    methods(18)%handler => MatrixIntegerSquare_handler
    methods(19)%method_name = 'matrixrealcopy';         methods(19)%handler => MatrixRealCopy_handler
    methods(20)%method_name = 'matrixrealtranspose';    methods(20)%handler => MatrixRealTranspose_handler
    methods(21)%method_name = 'matrixrealsquare';       methods(21)%handler => MatrixRealSquare_handler
    methods(22)%method_name = 'matrixtextcopy';         methods(22)%handler => MatrixTextCopy_handler
    methods(23)%method_name = 'matrixtexttranspose';    methods(23)%handler => MatrixTextTranspose_handler
    methods(24)%method_name = 'matrixlogicalcopy';      methods(24)%handler => MatrixLogicalCopy_handler
    methods(25)%method_name = 'matrixlogicaltranspose'; methods(25)%handler => MatrixLogicalTranspose_handler
    methods(26)%method_name = 'matrixcomplexcopy';      methods(26)%handler => MatrixComplexCopy_handler
    methods(27)%method_name = 'matrixcomplextranspose'; methods(27)%handler => MatrixComplexTranspose_handler
    methods(28)%method_name = 'matrixcomplexsquare';    methods(28)%handler => MatrixComplexSquare_handler

    do i = 1, NUM_METHODS
        call jsonrpc_register_method(trim(methods(i)%method_name), methods(i)%handler, reg_success)
        if (.not. reg_success) then
            print *, 'WARNING: Failed to register method: ', trim(methods(i)%method_name)
        end if
    end do

    pipeName = "\\.\pipe\MyTestPipe"//char(0)

    print *, '=========================================='
    print *, 'Fortran JSON-RPC 2.0 Server'
    print *, '=========================================='
    print *, ''
    print *, '[1] Creating named pipe: MyTestPipe'

    ! Create the named pipe: byte-mode, synchronous (blocking), single
    ! instance (nMaxInstances=1), 4096-byte in/out buffers, no security.
    hPipe = CreateNamedPipe(pipeName, PIPE_ACCESS_DUPLEX, &
        IOR(IOR(PIPE_TYPE_BYTE, PIPE_READMODE_BYTE), PIPE_WAIT), &
        1, 4096, 4096, 0, NULL)

    if (hPipe == INVALID_HANDLE_VALUE) then
        print *, 'ERROR: Failed to create named pipe!'
        print *, 'Error code:', GetLastError()
        print *, '(Server Error -32000)'
        call jsonrpc_shutdown()
        stop
    end if

    print *, '[SUCCESS] Named pipe created'
    print *, ''
    print *, '[2] JSON-RPC 2.0 Server started'
    print *, '    Pipe name: MyTestPipe'
    print *, '    Mode: Synchronous'
    print *, '    Direction: Duplex (InOut)'
    print *, '    Registered methods: 28'
    print *, '    Method matching: Case-sensitive'
    print *, '    Error Codes: JSON-RPC 2.0 Standard'
    print *, '    Max request size:', MAX_REQUEST_SIZE, 'bytes'
    print *, ''
    print *, 'Available Methods:'
    print *, '  1. addint                  - Add two integers'
    print *, '  2. subtractint             - Subtract two integers'
    print *, '  3. multiplyint             - Multiply two integers'
    print *, '  4. divideint               - Divide two integers'
    print *, '  5. addreal                 - Add two real numbers'
    print *, '  6. subtractreal            - Subtract two real numbers'
    print *, '  7. multiplyreal            - Multiply two real numbers'
    print *, '  8. dividereal              - Divide two real numbers'
    print *, '  9. addcomplex              - Add two complex numbers'
    print *, ' 10. subtractcomplex         - Subtract two complex numbers'
    print *, ' 11. multiplycomplex         - Multiply two complex numbers'
    print *, ' 12. dividecomplex           - Divide two complex numbers'
    print *, ' 13. sendmessage             - Echo a string message'
    print *, ' 14. namedparameters         - Process named parameters'
    print *, ' 15. mandelbrotbenchmark     - CPU benchmark with progress'
    print *, ' 16. matrixintegertranspose  - Transpose 4x4 integer matrix'
    print *, ' 17. matrixintegercopy       - Copy 4x4 integer matrix'
    print *, ' 18. matrixintegersquare     - Square 4x4 integer matrix elements'
    print *, ' 19. matrixrealcopy          - Copy 4x4 real matrix (REAL(8))'
    print *, ' 20. matrixrealtranspose     - Transpose 4x4 real matrix'
    print *, ' 21. matrixrealsquare        - Square 4x4 real matrix elements'
    print *, ' 22. matrixtextcopy          - Copy 4x4 text matrix (strings)'
    print *, ' 23. matrixtexttranspose     - Transpose 4x4 text matrix'
    print *, ' 24. matrixlogicalcopy       - Copy 4x4 logical matrix (booleans)'
    print *, ' 25. matrixlogicaltranspose  - Transpose 4x4 logical matrix'
    print *, ' 26. matrixcomplexcopy       - Copy 4x4 complex matrix'
    print *, ' 27. matrixcomplextranspose  - Transpose 4x4 complex matrix'
    print *, ' 28. matrixcomplexsquare     - Square 4x4 complex matrix elements'
    print *, ''
    print *, '[3] Waiting for VB.NET client to connect...'
    print *, '    (Start the VB.NET client and click Connect)'
    print *, ''

    connected = ConnectNamedPipe(hPipe, NULL)

    if (.not. connected) then
        ! ERROR_PIPE_CONNECTED means the client connected before ConnectNamedPipe
        ! returned — this is a success condition, not an error.
        if (GetLastError() == ERROR_PIPE_CONNECTED) then
            connected = .true.
        else
            print *, 'ERROR: Failed to connect to client!'
            print *, 'Error code:', GetLastError()
            print *, '(Pipe Error -32002)'
            fSuccess = CloseHandle(hPipe)
            call jsonrpc_shutdown()
            stop
        end if
    end if

    print *, '=========================================='
    print *, '[SUCCESS] VB.NET Client Connected!'
    print *, '=========================================='
    print *, ''
    print *, 'Server is now ready to:'
    print *, '  - Receive JSON-RPC 2.0 requests'
    print *, '  - Process Batch requests'
    print *, '  - Send real-time progress notifications'
    print *, '  - Process AddInt calls'
    print *, '  - Process SubtractInt calls'
    print *, '  - Process MultiplyInt calls'
    print *, '  - Process AddReal calls'
    print *, '  - Process SubtractReal calls'
    print *, '  - Process MultiplyReal calls'
    print *, '  - Process DivideReal calls (with div-by-zero check)'
    print *, '  - Process DivideInt calls (with div-by-zero check)'
    print *, '  - Process AddComplex calls'
    print *, '  - Process SubtractComplex calls'
    print *, '  - Process MultiplyComplex calls'
    print *, '  - Process DivideComplex calls (with div-by-zero check)'
    print *, '  - Process NamedParameters calls'
    print *, '  - Process MandelbrotBenchmark calls'
    print *, '  - Process MatrixIntegerTranspose calls'
    print *, '  - Process MatrixIntegerCopy calls'
    print *, '  - Process MatrixIntegerSquare calls'
    print *, '  - Process MatrixRealCopy calls'
    print *, '  - Process MatrixRealTranspose calls'
    print *, '  - Process MatrixRealSquare calls'
    print *, '  - Process MatrixTextCopy calls'
    print *, '  - Process MatrixTextTranspose calls'
    print *, '  - Process MatrixLogicalCopy calls'
    print *, '  - Process MatrixLogicalTranspose calls'
    print *, '  - Process MatrixComplexCopy calls'
    print *, '  - Process MatrixComplexTranspose calls'
    print *, '  - Process MatrixComplexSquare calls'
    print *, '  - Handle Notifications (no response)'
    print *, '  - Return standard error codes (-32700 to -32000)'
    print *, ''
    print *, 'Press Ctrl+C to stop server'
    print *, '=========================================='
    print *, ''

    call ProcessMessages(hPipe, MAX_REQUEST_SIZE)

    print *, ''
    print *, 'Cleaning up...'
    
    fSuccess = DisconnectNamedPipe(hPipe)
    if (fSuccess == 0) then
        print *, 'WARNING: DisconnectNamedPipe failed during cleanup'
        print *, 'Error code:', GetLastError()
    end if

    fSuccess = CloseHandle(hPipe)
    if (fSuccess == 0) then
        print *, 'WARNING: CloseHandle failed during cleanup'
        print *, 'Error code:', GetLastError()
    end if

    call jsonrpc_shutdown()

    print *, ''
    print *, 'Server stopped.'
    print *, 'Press Enter to exit...'
    read(*,*)

end program FortranServerJSONRPC
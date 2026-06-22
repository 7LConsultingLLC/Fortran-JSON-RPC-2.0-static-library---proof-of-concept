!****************************************************************************
!
!  SUBMODULE: Functions_Benchmark
!
!  PURPOSE:  Computational benchmark handlers (Mandelbrot)
!
!****************************************************************************
submodule (Functions) Functions_Benchmark
    implicit none

    integer(kind=4), parameter :: STOP_CHECK_INTERVAL = 50000
    real(kind=8),    parameter :: CHECKSUM_ABORTED     = -1.0d0

    ! Signal constants returned by check_for_signals
    integer(kind=4), parameter :: SIGNAL_NONE   = 0
    integer(kind=4), parameter :: SIGNAL_STOP   = 1
    integer(kind=4), parameter :: SIGNAL_PAUSE  = 2
    integer(kind=4), parameter :: SIGNAL_RESUME = 3

contains

    !****************************************************************************
    !  SUBROUTINE: MandelbrotBenchmark_handler
    !  PURPOSE:  JSON-RPC handler for Mandelbrot benchmark computation
    !****************************************************************************
    module subroutine MandelbrotBenchmark_handler(json, params_array, result_value, error_code, error_msg)
        type(json_core), intent(inout) :: json
        type(json_value), pointer, intent(in) :: params_array
        type(json_value), pointer, intent(inout) :: result_value
        integer, intent(out) :: error_code
        character(len=:), allocatable, intent(out) :: error_msg

        integer :: seed
        real(8) :: checksum
        logical :: success

        error_code = 0

        if (.not. associated(params_array)) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: params must be an array'
            return
        end if

        call jsonrpc_get_int_param(json, params_array, 1, seed, success)
        if (.not. success) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: seed must be an integer'
            return
        end if

        if (seed < 1) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: seed must be at least 1'
            return
        end if

        if (seed > 10) then
            error_code = JSONRPC_INVALID_PARAMS
            error_msg = 'Invalid params: seed too large (maximum is 10)'
            return
        end if

        print *, '=========================================='
        print *, 'MandelbrotBenchmark: Starting computation'
        print *, '=========================================='
        print *, '  Seed value: ', seed
        print *, '  Grid resolution: ', BASE_RES * seed, ' x ', BASE_RES * seed
        print *, '  Max iterations: ', BASE_ITER * seed
        print *, '  Total points: ', int(BASE_RES * seed, 8) * int(BASE_RES * seed, 8)
        print *, '  Mode: REQUEST-RESPONSE (with progress notifications)'
        print *, '=========================================='
        print *, ''

        checksum = mandelbrot_benchmark(seed)

        ! Detect early exit via sentinel
        if (checksum == CHECKSUM_ABORTED) then
            print *, 'MandelbrotBenchmark: Aborted by client STOP notification'
            error_code = JSONRPC_SERVER_ERROR
            error_msg  = 'Benchmark aborted by client request'
            return
        end if

        print *, ''
        print *, '=========================================='
        print *, 'MandelbrotBenchmark: Computation complete'
        print *, '=========================================='
        print *, '  Checksum: ', checksum
        print *, '  Format: ', 'Real(8) / Double precision'
        print *, '=========================================='
        print *, ''

        if (associated(result_value)) call json%destroy(result_value)
        call json%create_real(result_value, checksum, 'result')

        print *, 'Response prepared: returning checksum as result'
        print *, ''

    end subroutine MandelbrotBenchmark_handler

    !****************************************************************************
    !  FUNCTION: mandelbrot_benchmark
    !  PURPOSE:  Compute Mandelbrot set checksum.
    !            Returns CHECKSUM_ABORTED (-1.0d0) if client sends STOP
    !            notification mid-computation.  PAUSE/RESUME are handled
    !            inline via the pause_loop subroutine.
    !****************************************************************************
    function mandelbrot_benchmark(seed) result(checksum)
        integer(kind=4), intent(in) :: seed
        real(kind=8) :: checksum

        integer(kind=4) :: ix, iy, iter, max_iter, nx, ny
        integer(kind=8) :: grid_point, total_points
        real(kind=8) :: x0, y0, x, y, x_new, x2, y2, dx, dy
        real(kind=8) :: x_min, x_max, y_min, y_max
        real(kind=8) :: smooth_val, log2_inv, weight_acc
        integer :: pct_done
        integer(kind=4) :: stop_counter, signal
        integer(kind=8) :: next_milestone
        integer(kind=8), allocatable :: histogram(:)

        nx = BASE_RES * seed
        ny = BASE_RES * seed
        max_iter = BASE_ITER * seed

        x_min = -2.5d0
        x_max = 1.0d0
        y_min = -1.25d0
        y_max = 1.25d0

        dx = (x_max - x_min) / dble(nx - 1)
        dy = (y_max - y_min) / dble(ny - 1)

        allocate(histogram(0:255))
        histogram = 0_8
        checksum = 0.0d0
        weight_acc = 0.0d0
        log2_inv = 1.0d0 / log(2.0d0)

        total_points = int(nx, kind=8) * int(ny, kind=8)
        pct_done = 0
        stop_counter = 0
        next_milestone = total_points / 10_8

        do grid_point = 0_8, total_points - 1_8

            ! --- Signal check counter ---
            stop_counter = stop_counter + 1
            if (stop_counter >= STOP_CHECK_INTERVAL) then
                stop_counter = 0
                signal = check_for_signals()

                select case (signal)

                    case (SIGNAL_STOP)
                        print *, '  Benchmark stopped by client request'
                        if (g_hPipe /= 0) then
                            call SendStatusToClient(g_hPipe, 'The function has been exited.')
                        end if
                        deallocate(histogram)
                        checksum = CHECKSUM_ABORTED
                        return

                    case (SIGNAL_PAUSE)
                        ! Enter pause loop; resumes here when client sends RESUME
                        call pause_loop()
                        ! Execution continues at the next iteration after resume

                end select
            end if

            ! --- Progress milestone ---
            if (grid_point >= next_milestone) then
                pct_done = pct_done + 10

                write(*,'(A,I0,A)', advance='YES') '  ', pct_done, '% complete'

                if (g_hPipe /= 0) then
                    call SendProgressToClient(g_hPipe, pct_done)
                end if

                next_milestone = (int(pct_done, kind=8) + 10_8) * total_points / 100_8
            end if

            iy = int(grid_point / int(nx, kind=8), kind=4)
            ix = int(mod(grid_point, int(nx, kind=8)), kind=4)

            x0 = x_min + ix * dx
            y0 = y_min + iy * dy

            x = 0.0d0
            y = 0.0d0
            x2 = 0.0d0
            y2 = 0.0d0
            iter = 0

            do while ((x2 + y2 <= 4.0d0) .and. (iter < max_iter))
                x_new = x2 - y2 + x0
                y = 2.0d0 * x * y + y0
                x = x_new
                x2 = x * x
                y2 = y * y
                iter = iter + 1
            end do

            ! Smooth (continuous) escape-time: removes integer banding artifacts.
            ! Formula: iter - log2(log(|z|)) gives a real-valued escape count.
            if (iter < max_iter) then
                smooth_val = dble(iter) - log(log(sqrt(x2 + y2))) * log2_inv
            else
                smooth_val = dble(max_iter)
            end if

            histogram(mod(iter, 256)) = histogram(mod(iter, 256)) + 1_8
            ! weight_acc accumulates the area-weighted smooth escape values.
            weight_acc = weight_acc + smooth_val * dx * dy

        end do

        ! Checksum = histogram-weighted bucket sum + continuous area integral.
        ! Deterministic for a given seed; used to verify computation correctness.
        do ix = 0, 255
            checksum = checksum + dble(histogram(ix)) * dble(ix + 1)
        end do
        checksum = checksum + weight_acc

        deallocate(histogram)

    end function mandelbrot_benchmark

    !****************************************************************************
    !  SUBROUTINE: pause_loop
    !  PURPOSE:  Entered when a PAUSE signal is detected.  Sends PAUSED
    !            acknowledgment to client, then polls the pipe every 100ms
    !            until a RESUME notification arrives, then sends RESUMED
    !            acknowledgment and returns.  STOP is not checked here —
    !            btnExitDemo is disabled on the client while paused.
    !****************************************************************************
    subroutine pause_loop()
        integer(kind=4) :: signal

        print *, '  Benchmark PAUSED by client request'

        if (g_hPipe /= 0) then
            call SendStatusToClient(g_hPipe, 'The function has been PAUSED.')
        end if

        ! Poll for RESUME; sleep 100ms between checks to avoid busy-wait
        do
            call SleepQQ(100_8)   ! Intel runtime: millisecond sleep
            signal = check_for_signals()
            if (signal == SIGNAL_RESUME) exit
            ! SIGNAL_NONE or spurious duplicate PAUSE: keep waiting
        end do

        print *, '  Benchmark RESUMED by client request'

        if (g_hPipe /= 0) then
            call SendStatusToClient(g_hPipe, 'The function has RESUMED.')
        end if

    end subroutine pause_loop

    !****************************************************************************
    !  FUNCTION: check_for_signals
    !  PURPOSE:  Non-blocking peek of g_hPipe.  Returns:
    !              SIGNAL_NONE  (0) — nothing present or unrecognised content
    !              SIGNAL_STOP  (1) — "method":"stop"  detected and consumed
    !              SIGNAL_PAUSE (2) — "method":"pause" detected and consumed
    !              SIGNAL_RESUME(3) — "method":"resume"detected and consumed
    !****************************************************************************
    function check_for_signals() result(signal)
        integer(kind=4) :: signal

        character(len=512), target :: peek_buf
        character(len=512), target :: drain_buf
        integer(DWORD)             :: bytes_available
        integer(DWORD)             :: bytes_read
        integer(BOOL)              :: peek_result
        integer(BOOL)              :: read_result

        signal = SIGNAL_NONE

        if (g_hPipe == 0) return

        bytes_available = 0_4

        peek_result = PeekNamedPipe( &
            g_hPipe,                          &
            LOC(peek_buf),                    &
            int(512, DWORD),                  &
            NULL,                             &
            LOC(bytes_available),             &
            NULL)

        if (peek_result == 0 .or. bytes_available == 0) return

        ! Identify signal by substring scan
        if (index(peek_buf, '"method":"stop"') > 0) then
            signal = SIGNAL_STOP
        else if (index(peek_buf, '"method":"pause"') > 0) then
            signal = SIGNAL_PAUSE
        else if (index(peek_buf, '"method":"resume"') > 0) then
            signal = SIGNAL_RESUME
        else
            return   ! Unrecognised content — leave on pipe, do not consume
        end if

        ! Consume the message so the main dispatch loop never sees it
        read_result = ReadFile( &
            g_hPipe,                          &
            LOC(drain_buf),                   &
            min(bytes_available, 512_4),      &
            LOC(bytes_read),                  &
            NULL)

        print *, 'check_for_signals: signal=', signal, ' consumed', bytes_read, 'bytes'

    end function check_for_signals

end submodule Functions_Benchmark

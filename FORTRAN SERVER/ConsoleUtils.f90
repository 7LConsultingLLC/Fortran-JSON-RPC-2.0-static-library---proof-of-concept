!****************************************************************************
!
!  MODULE: ConsoleUtils
!
!  PURPOSE:  Console window utilities (centering, sizing, etc.)
!
!****************************************************************************
module ConsoleUtils
    use ifwin
    use ifwinty
    implicit none

contains

    !****************************************************************************
    !  SUBROUTINE: AlignConsoleWindowRight
    !  PURPOSE:  Position the console window flush against the right edge of
    !            the primary display, vertically centered.  Allows the server
    !            console to sit alongside the VB.NET client window without
    !            overlapping it.
    !****************************************************************************
    subroutine AlignConsoleWindowRight()
        integer(HANDLE) :: hConsole
        integer(BOOL) :: success
        integer :: screenWidth, screenHeight
        integer :: windowWidth, windowHeight
        integer :: posX, posY
        type(T_RECT) :: windowRect

        hConsole = GetConsoleWindow()
        if (hConsole == 0) then
            print *, 'WARNING: Could not get console window handle'
            return
        end if

        screenWidth = GetSystemMetrics(SM_CXSCREEN)
        screenHeight = GetSystemMetrics(SM_CYSCREEN)

        success = GetWindowRect(hConsole, windowRect)
        if (success == 0) then
            print *, 'WARNING: Could not get window dimensions'
            return
        end if

        windowWidth = windowRect%right - windowRect%left
        windowHeight = windowRect%bottom - windowRect%top

        posX = screenWidth - windowWidth
        posY = (screenHeight - windowHeight) / 2

        if (posX < 0) posX = 0
        if (posY < 0) posY = 0

        success = SetWindowPos(hConsole, HWND_TOP, posX, posY, &
                              windowWidth, windowHeight, SWP_SHOWWINDOW)

        if (success == 0) then
            print *, 'WARNING: Could not set window position'
        end if

    end subroutine AlignConsoleWindowRight

    !****************************************************************************
    !  SUBROUTINE: SetServerConsoleTitle
    !  PURPOSE:  Set the console window title bar text.  Makes the server
    !            window easy to identify in the Windows taskbar.
    !****************************************************************************
    subroutine SetServerConsoleTitle(title)
        character(len=*), intent(in) :: title
        character(len=256) :: titleWithNull
        integer(BOOL) :: success

        titleWithNull = trim(title) // char(0)
        success = SetConsoleTitle(titleWithNull)

        if (success == 0) then
            print *, 'WARNING: Could not set console title'
        end if

    end subroutine SetServerConsoleTitle

end module ConsoleUtils

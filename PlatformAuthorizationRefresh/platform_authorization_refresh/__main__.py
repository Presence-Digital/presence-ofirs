from platform_authorization_refresh.auth_manager import main
import os

def enable_windows_terminal_colors() -> None:
    """
    Enables ANSI escape sequence (color) support in Windows terminal (CMD/PowerShell).
    On non-Windows platforms, this is a no-op.
    """
    if os.name != "nt":
        return  # Not Windows, nothing to do

    try:
        import ctypes
        kernel32 = ctypes.windll.kernel32

        # Get the current mode of the standard output handle
        handle = kernel32.GetStdHandle(-11)  # STD_OUTPUT_HANDLE = -11
        mode = ctypes.c_ulong()
        if kernel32.GetConsoleMode(handle, ctypes.byref(mode)):
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004
            new_mode = mode.value | ENABLE_VIRTUAL_TERMINAL_PROCESSING
            kernel32.SetConsoleMode(handle, new_mode)
    except Exception:
        # If anything fails, silently continue without color support
        pass

if __name__ == "__main__":
    enable_windows_terminal_colors()
    main()
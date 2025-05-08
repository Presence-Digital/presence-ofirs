import os
from datetime import datetime

class Colors:
    HEADER = '\033[95m'
    BLUE = '\033[94m'
    CYAN = '\033[96m'
    GREEN = '\033[92m'
    YELLOW = '\033[93m'
    RED = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'

def print_status(message: str, color: str = Colors.CYAN) -> None:
    """
    Print a formatted status message with a timestamp.

    :param message: The message to print.
    :param color: ANSI color code for the message.
    """
    timestamp = datetime.now().strftime("%H:%M:%S")
    print(f"{color}[{timestamp}] {message}{Colors.ENDC}")

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

class YouTubeHtmls:
    SUCCESS_PAGE_BASE: str = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>YouTube Authorization Complete</title>
        <style>
            body {{
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                background-color: #f9f9f9;
                color: #333;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                padding: 20px;
            }}
            .container {{
                background-color: white;
                border-radius: 12px;
                box-shadow: 0 4px 24px rgba(0,0,0,0.08);
                padding: 40px;
                max-width: 420px;
                text-align: center;
            }}
            h2 {{
                color: #4285f4;
                font-size: 28px;
                margin-top: 0;
                margin-bottom: 12px;
            }}
            p {{
                color: #5f6368;
                line-height: 1.6;
                margin-bottom: 12px;
            }}
            .success-icon {{
                color: #34a853;
                font-size: 48px;
                margin-bottom: 20px;
            }}
            .youtube-logo {{
                width: 100px;
                height: 70px;
                background-color: #ff0000;
                border-radius: 18px;
                display: flex;
                align-items: center;
                justify-content: center;
                margin: 0 auto 24px;
            }}
            .play-button {{
                width: 0;
                height: 0;
                border-style: solid;
                border-width: 15px 0 15px 26px;
                border-color: transparent transparent transparent #ffffff;
            }}
            .account-info {{
                margin-top: 8px;
                margin-bottom: 20px;
                font-size: 14px;
                color: #80868b;
                display: flex;
                align-items: center;
                justify-content: center;
            }}
            .account-info .dot {{
                display: inline-block;
                width: 6px;
                height: 6px;
                background-color: #ea4335;
                border-radius: 50%;
                margin: 0 6px;
            }}
        </style>
    </head>
    <body>
        <div class="container">
            <div class="youtube-logo">
                <div class="play-button"></div>
            </div>
            <div class="success-icon">✓</div>
            <h2>Authorization Complete</h2>
            {account_info}
            <p>Your YouTube authentication was successful. The tokens have been securely retrieved.</p>
            <p>You can close this window now and return to the application.</p>
        </div>
    </body>
    </html>
    """
    ERROR_PAGE: str = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Authorization Failed</title>
        <style>
            body {{
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                background-color: #f9f9f9;
                color: #333;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                padding: 20px;
            }}
            .container {{
                background-color: white;
                border-radius: 8px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                padding: 30px;
                max-width: 400px;
                text-align: center;
            }}
            h2 {{
                color: #ea4335;
                margin-top: 0;
            }}
            p {{
                color: #555;
                line-height: 1.5;
            }}
            .icon {{
                font-size: 48px;
                color: #ea4335;
                margin-bottom: 20px;
            }}
        </style>
    </head>
    <body>
        <div class="container">
            <div class="icon">✗</div>
            <h2>Authorization Failed</h2>
            <p>{error_message}</p>
            <p>You can close this window and check the terminal for more information.</p>
        </div>
    </body>
    </html>
    """

class TikTokHtmls:
    SUCCESS_PAGE_BASE: str = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>TikTok Authorization Complete</title>
        <style>
            body {{
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                background-color: #f8f9fa;
                color: #333;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                padding: 20px;
            }}
            .container {{
                background-color: white;
                border-radius: 12px;
                box-shadow: 0 4px 24px rgba(0,0,0,0.08);
                padding: 40px;
                max-width: 420px;
                text-align: center;
            }}
            h2 {{
                color: #fe2c55;
                font-size: 28px;
                margin-top: 0;
                margin-bottom: 12px;
            }}
            p {{
                color: #5f6368;
                line-height: 1.6;
                margin-bottom: 12px;
            }}
            .success-icon {{
                color: #25f4ee;
                font-size: 48px;
                margin-bottom: 20px;
            }}
            .tiktok-logo {{
                display: flex;
                align-items: center;
                justify-content: center;
                margin-bottom: 24px;
            }}
            .logo-part-1 {{
                font-size: 48px;
                color: #fe2c55;
                margin-right: 5px;
            }}
            .logo-part-2 {{
                font-size: 48px;
                color: #25f4ee;
            }}
            .account-info {{
                margin-top: 8px;
                margin-bottom: 20px;
                font-size: 14px;
                color: #80868b;
                display: flex;
                align-items: center;
                justify-content: center;
            }}
            .account-info .dot {{
                display: inline-block;
                width: 6px;
                height: 6px;
                background-color: #fe2c55;
                border-radius: 50%;
                margin: 0 6px;
            }}
        </style>
    </head>
    <body>
        <div class="container">
            <div class="tiktok-logo">
                <div class="logo-part-1">●</div>
                <div class="logo-part-2">●</div>
            </div>
            <div class="success-icon">✓</div>
            <h2>Authorization Complete</h2>
            {account_info}
            <p>Your TikTok authentication was successful. The tokens have been securely retrieved.</p>
            <p>You can close this window now and return to the application.</p>
        </div>
    </body>
    </html>
    """
    ERROR_PAGE: str = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Authorization Failed</title>
        <style>
            body {{
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                background-color: #f8f9fa;
                color: #333;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                padding: 20px;
            }}
            .container {{
                background-color: white;
                border-radius: 8px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                padding: 30px;
                max-width: 400px;
                text-align: center;
            }}
            h2 {{
                color: #fe2c55;
                margin-top: 0;
            }}
            p {{
                color: #555;
                line-height: 1.5;
            }}
            .icon {{
                font-size: 48px;
                color: #fe2c55;
                margin-bottom: 20px;
            }}
        </style>
    </head>
    <body>
        <div class="container">
            <div class="icon">✗</div>
            <h2>Authorization Failed</h2>
            <p>{error_message}</p>
            <p>You can close this window and check the terminal for more information.</p>
        </div>
    </body>
    </html>
    """

    SECURITY_ERROR_PAGE: str = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Security Error</title>
        <style>
            body {{
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                background-color: #f8f9fa;
                color: #333;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                padding: 20px;
            }}
            .container {{
                background-color: white;
                border-radius: 8px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                padding: 30px;
                max-width: 400px;
                text-align: center;
            }}
            h2 {{
                color: #fe2c55;
                margin-top: 0;
            }}
            p {{
                color: #555;
                line-height: 1.5;
            }}
            .icon {{
                font-size: 48px;
                color: #fe2c55;
                margin-bottom: 20px;
            }}
        </style>
    </head>
    <body>
        <div class="container">
            <div class="icon">⚠️</div>
            <h2>Security Error</h2>
            <p>State mismatch detected. This could indicate a security risk.</p>
            <p>You can close this window and check the terminal for more information.</p>
        </div>
    </body>
    </html>
    """

    TOKEN_ERROR_PAGE: str = """
    <!DOCTYPE html>
    <html>
    <head>
        <title>Token Retrieval Failed</title>
        <style>
            body {{
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                background-color: #f8f9fa;
                color: #333;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                padding: 20px;
            }}
            .container {{
                background-color: white;
                border-radius: 8px;
                box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                padding: 30px;
                max-width: 400px;
                text-align: center;
            }}
            h2 {{
                color: #fe2c55;
                margin-top: 0;
            }}
            p {{
                color: #555;
                line-height: 1.5;
            }}
            .icon {{
                font-size: 48px;
                color: #fe2c55;
                margin-bottom: 20px;
            }}
        </style>
    </head>
    <body>
        <div class="container">
            <div class="icon">✗</div>
            <h2>Token Retrieval Failed</h2>
            <p>There was a problem retrieving your TikTok access tokens.</p>
            <p>You can close this window and check the terminal for more information.</p>
        </div>
    </body>
    </html>
    """

import subprocess
import threading
import time
import webbrowser
import requests
import argparse
import sys
from flask import Flask, request, redirect
from typing import Tuple, Optional, Dict

# Import shared utilities for colors and logging
from platform_authorization_refresh.utils import Colors, print_status, YouTubeHtmls

# Create Flask app
app = Flask(__name__)

# OAuth configuration for YouTube
CLIENT_ID: str = "918479755586-191n89psglkjut294ibsfu3d95e7ng2j.apps.googleusercontent.com"
CLIENT_SECRET: str = "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"
SCOPE: str = "https://www.googleapis.com/auth/youtube.readonly"
REDIRECT_URI: str = "http://localhost:8080/callback"

# Global dictionary to store tokens once retrieved
tokens: Dict[str, str] = {}

# Global variable to hold the optional account login hint
ACCOUNT: Optional[str] = None

@app.route("/")
def index() -> "redirect":
    """
    Build the Google OAuth 2.0 consent URL using the localhost callback
    and redirect the user.
    """
    auth_url = (
        "https://accounts.google.com/o/oauth2/auth?"
        f"client_id={CLIENT_ID}&"
        f"redirect_uri={REDIRECT_URI}&"
        "response_type=code&"
        f"scope={SCOPE}&"
        "access_type=offline&"
        "prompt=consent"
    )
    if ACCOUNT:
        auth_url += f"&login_hint={ACCOUNT}"
    return redirect(auth_url)

def exchange_code_for_tokens(code: str) -> Optional[Dict[str, str]]:
    """
    Exchange the provided authorization code for access and refresh tokens.
    """
    token_url = "https://oauth2.googleapis.com/token"
    payload = {
        "code": code,
        "client_id": CLIENT_ID,
        "client_secret": CLIENT_SECRET,
        "redirect_uri": REDIRECT_URI,
        "grant_type": "authorization_code"
    }
    headers = {"Content-Type": "application/x-www-form-urlencoded"}

    try:
        response = requests.post(token_url, data=payload, headers=headers)
        response.raise_for_status()
        return response.json()
    except Exception as e:
        print_status(f"Token exchange failed: {str(e)}", Colors.RED)
        return None

@app.route("/callback")
def callback() -> Tuple[str, int]:
    """
    Callback endpoint that handles Google's response.
    Exchanges the authorization code for tokens and, if successful,
    returns a success page with the account information appended.
    """
    error = request.args.get("error")
    if error:
        print_status(f"Error during authorization: {error}", Colors.RED)
        return YouTubeHtmls.ERROR_PAGE.format(error_message=f"Error during authorization: {error}"), 400

    code = request.args.get("code")
    if not code:
        print_status("No code provided in callback.", Colors.RED)
        return YouTubeHtmls.ERROR_PAGE.format(error_message="No code provided in callback."), 400

    print_status("Exchanging authorization code for tokens...", Colors.BLUE)
    token_response = exchange_code_for_tokens(code)

    if token_response and "access_token" in token_response:
        tokens["access_token"] = token_response.get("access_token")
        tokens["refresh_token"] = token_response.get("refresh_token")
        print_status("Tokens successfully retrieved!", Colors.GREEN)

        account_info: str = ""
        if ACCOUNT:
            account_info = (
                f'<div class="account-info">'
                f'<span>{ACCOUNT}</span>'
                f'<span class="dot"></span>'
                f'<span>YouTube</span>'
                f'</div>'
            )
        return YouTubeHtmls.SUCCESS_PAGE_BASE.format(account_info=account_info)
    else:
        print_status("Failed to retrieve tokens.", Colors.RED)
        return YouTubeHtmls.ERROR_PAGE.format(error_message="Token retrieval failed."), 400

def run_flask_app() -> None:
    """Run the Flask app on port 8080 with minimal logging."""
    import logging
    log = logging.getLogger('werkzeug')
    log.setLevel(logging.ERROR)
    app.run(port=8080, debug=False, host="127.0.0.1")

def refresh_tokens(timeout: int = 60, account: Optional[str] = None,
                   chrome_path: Optional[str] = None, chrome_profile: Optional[str] = None) -> Tuple[str, str]:
    """
    Execute the YouTube OAuth flow and return (access_token, refresh_token).

    :param timeout: Maximum seconds to wait for authorization.
    :param account: Optional account email to use for login_hint.
    :param chrome_path: Optional path to a specific Chrome executable.
    :param chrome_profile: Optional Chrome profile directory to use (e.g. "Profile 1").
    :return: A tuple containing the access token and refresh token.
    :raises TimeoutError: If the authorization times out.
    :raises RuntimeError: If tokens cannot be retrieved.
    """
    global tokens, ACCOUNT
    tokens = {}
    ACCOUNT = account

    print("\n" + "=" * 70)
    print_status("Starting YouTube OAuth authorization process", Colors.HEADER)
    if ACCOUNT:
        print_status(f"Using account: {ACCOUNT}", Colors.BLUE)

    # Start the Flask app in a separate thread.
    print_status("Starting local server...", Colors.BLUE)
    flask_thread = threading.Thread(target=run_flask_app)
    flask_thread.daemon = True
    flask_thread.start()
    time.sleep(1)  # Give Flask a moment to start

    # Open the browser for authorization.
    if chrome_path:
        try:
            browser_args = [chrome_path]
            if chrome_profile:
                browser_args.append(f'--profile-directory={chrome_profile}')
            browser_args.append("http://localhost:8080/")

            print_status("Opening Chrome using subprocess...", Colors.BLUE)
            subprocess.Popen(browser_args)
        except Exception as e:
            print_status(f"Subprocess launch failed: {e}. Falling back to default browser.", Colors.YELLOW)
            webbrowser.open("http://localhost:8080/")
    else:
        print_status("Opening default web browser for authorization...", Colors.BLUE)
        webbrowser.open("http://localhost:8080/")

    print_status("Waiting for authorization...", Colors.BLUE)
    start_time = time.time()
    while "access_token" not in tokens:
        time.sleep(0.5)
        if timeout > 0 and (time.time() - start_time) > timeout:
            raise TimeoutError(f"Authorization timed out after {timeout} seconds")

    print_status("Authorization complete!", Colors.GREEN)
    print("\n" + "-" * 30 + " Token Information " + "-" * 30)
    print(f"{Colors.BOLD}Access Token:{Colors.ENDC} {tokens['access_token'][:20]}...{tokens['access_token'][-10:]}")
    if "refresh_token" in tokens:
        print(f"{Colors.BOLD}Refresh Token:{Colors.ENDC} {tokens['refresh_token'][:10]}...{tokens['refresh_token'][-10:]}")
    else:
        print(f"{Colors.YELLOW}No refresh token provided{Colors.ENDC}")
    if ACCOUNT:
        print(f"{Colors.BOLD}Account:{Colors.ENDC} {ACCOUNT}")
    print("-" * 70)
    print_status("You can now use these tokens for YouTube API requests", Colors.BLUE)
    print("=" * 70 + "\n")

    if "access_token" in tokens and "refresh_token" in tokens:
        return tokens["access_token"], tokens["refresh_token"]
    else:
        raise RuntimeError("Authorization failed; tokens not retrieved")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="YouTube OAuth authentication",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter
    )
    parser.add_argument("--account", help="Optional account email for login_hint", type=str, default=None)
    parser.add_argument("--timeout", help="Timeout in seconds for authorization", type=int, default=60)
    parser.add_argument("--chrome", help="Path to the specific Chrome executable (optional)", type=str, default=None)
    parser.add_argument("--chrome-profile", help="Name of the Chrome profile to use (e.g. 'Profile 1') (optional)", type=str, default=None)
    args = parser.parse_args()

    try:
        access_token, refresh_token = refresh_tokens(
            timeout=args.timeout,
            account=args.account,
            chrome_path=args.chrome,
            chrome_profile=args.chrome_profile
        )
        # For direct testing, print the tokens
        print_status(f"access_token: {access_token}", Colors.GREEN)
        print_status(f"refresh_token: {refresh_token}", Colors.GREEN)
    except Exception as e:
        print_status(f"Error: {str(e)}", Colors.RED)
        sys.exit(1)
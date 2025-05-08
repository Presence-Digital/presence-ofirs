import subprocess
import webbrowser
import hashlib
import urllib.parse
import random
import requests
import secrets
import string
import threading
import time
import argparse
import sys
from typing import Tuple, Optional, Dict
from flask import Flask, request

# Import shared utilities
from platform_authorization_refresh.utils import Colors, print_status, TikTokHtmls

# Create Flask app
app = Flask(__name__)

# TikTok app credentials
CLIENT_KEY: str = "sbaw3k64p0x5jmmsd1"
CLIENT_SECRET: str = "SuSeABmq5pfX6ayAZbgmNQYZaKS0m7y9"
REDIRECT_URI: str = "http://localhost:8080/callback"
SCOPES: str = "user.info.basic,user.info.profile,user.info.stats,video.list"

# Global variables for state management and token storage
tokens: Dict[str, str] = {}  # Unified tokens dictionary
state: str = ""
code_verifier: str = ""
ACCOUNT: Optional[str] = None

def generate_state_token(length: int = 30) -> str:
    """
    Generate a secure anti-forgery state token.
    """
    characters = string.ascii_letters + string.digits
    return ''.join(secrets.choice(characters) for _ in range(length))

def generate_code_verifier(length: int = 43) -> str:
    """
    Generate a random code verifier string compliant with OAuth 2.0 PKCE.
    """
    if not (43 <= length <= 128):
        raise ValueError("length must be between 43 and 128 characters")
    characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~'
    return ''.join(random.choice(characters) for _ in range(length))

def generate_code_challenge(code_verifier: str) -> str:
    """
    Generate a code challenge by hashing the code verifier using SHA256.
    """
    return hashlib.sha256(code_verifier.encode('utf-8')).hexdigest()

def exchange_code_for_tokens(encoded_code: str) -> Optional[Dict[str, str]]:
    """
    Exchange the provided authorization code for access and refresh tokens.
    """
    token_url = "https://open.tiktokapis.com/v2/oauth/token/"
    data = {
        "client_key": CLIENT_KEY,
        "client_secret": CLIENT_SECRET,
        "code": encoded_code,
        "grant_type": "authorization_code",
        "redirect_uri": REDIRECT_URI,
        "code_verifier": code_verifier
    }
    try:
        response = requests.post(token_url, data=data)
        if response.status_code == 200:
            token_data = response.json()
            if "access_token" in token_data:
                return token_data
            else:
                print_status("Error: No access_token in response", Colors.RED)
                return None
        else:
            print_status(f"Token exchange failed: {response.status_code}", Colors.RED)
            try:
                error_detail = response.json()
                print_status(f"Error details: {error_detail}", Colors.RED)
            except Exception:
                print_status(f"Error response: {response.text}", Colors.RED)
            return None
    except Exception as e:
        print_status(f"Exception during token exchange: {str(e)}", Colors.RED)
        return None

@app.route("/callback")
def callback() -> Tuple[str, int]:
    """
    Callback endpoint that handles TikTok's response.
    Exchanges the authorization code for tokens and, if successful,
    returns a success page with the account information appended.
    """
    global tokens, state
    code = request.args.get("code")
    received_state = request.args.get("state")
    error = request.args.get("error")

    if error:
        print_status(f"Error from TikTok: {error}", Colors.RED)
        return TikTokHtmls.ERROR_PAGE.format(error_message="There was an error during the TikTok authorization process."), 400

    if received_state != state:
        print_status("State mismatch! Security risk detected.", Colors.RED)
        return TikTokHtmls.SECURITY_ERROR_PAGE, 400

    # URL encode the received code as per TikTok's guidance
    encoded_code = urllib.parse.quote(code, safe='')
    print_status("Exchanging authorization code for tokens...", Colors.BLUE)
    token_response = exchange_code_for_tokens(encoded_code)

    if token_response and "access_token" in token_response:
        tokens.update(token_response)
        print_status("Tokens successfully retrieved!", Colors.GREEN)
        account_info: str = ""
        if ACCOUNT:
            account_info = (
                f'<div class="account-info">'
                f'<span>{ACCOUNT}</span>'
                f'<span class="dot"></span>'
                f'<span>TikTok</span>'
                f'</div>'
            )
        return TikTokHtmls.SUCCESS_PAGE_BASE.format(account_info=account_info), 200
    else:
        print_status("Failed to retrieve tokens.", Colors.RED)
        return TikTokHtmls.TOKEN_ERROR_PAGE.format(error_message="Token retrieval failed."), 400

def run_flask_app() -> None:
    """Run the Flask app on port 8080 with minimal logging."""
    import logging
    log = logging.getLogger('werkzeug')
    log.setLevel(logging.ERROR)
    app.run(port=8080, debug=False, host="127.0.0.1")

def refresh_tokens(timeout: int = 60, account: Optional[str] = None,
                   chrome_path: Optional[str] = None, chrome_profile: Optional[str] = None) -> Tuple[str, str]:
    """
    Execute the TikTok OAuth flow and return (access_token, refresh_token).

    :param timeout: Maximum seconds to wait for authorization.
    :param account: Optional TikTok username to prefill.
    :param chrome_path: Optional path to a specific Chrome executable.
    :param chrome_profile: Optional Chrome profile directory to use (e.g. "Profile 1").
    :return: Tuple containing the access token and refresh token.
    :raises TimeoutError: If the authorization times out.
    :raises RuntimeError: If tokens cannot be retrieved.
    """
    global tokens, state, code_verifier, ACCOUNT
    # Reset globals
    tokens = {}
    ACCOUNT = account

    print("\n" + "=" * 70)
    print_status("Starting TikTok OAuth authorization process", Colors.HEADER)

    # Generate PKCE values
    state = generate_state_token()
    code_verifier = generate_code_verifier()
    code_challenge = generate_code_challenge(code_verifier)

    # Build authorization URL
    auth_params = {
        "client_key": CLIENT_KEY,
        "scope": SCOPES,
        "response_type": "code",
        "redirect_uri": REDIRECT_URI,
        "state": state,
        "code_challenge": code_challenge,
        "code_challenge_method": "S256",
        "disable_auto_auth": 1  # Bypass session to force consent screen
    }
    if ACCOUNT:
        auth_params["prefill_username"] = ACCOUNT
        print_status(f"Using account: {ACCOUNT}", Colors.BLUE)

    auth_url = "https://www.tiktok.com/v2/auth/authorize/?" + urllib.parse.urlencode(auth_params, safe='')

    # Start the Flask app in a separate thread.
    print_status("Starting local server...", Colors.BLUE)
    flask_thread = threading.Thread(target=run_flask_app)
    flask_thread.daemon = True
    flask_thread.start()
    time.sleep(1)  # Allow Flask to start

    # Open browser for TikTok login.
    if chrome_path:
        try:
            browser_args = [chrome_path]
            if chrome_profile:
                browser_args.append(f'--profile-directory={chrome_profile}')
            browser_args.append(auth_url)

            print_status("Opening specified Chrome browser for TikTok login...", Colors.BLUE)
            subprocess.Popen(browser_args)
        except Exception as e:
            print_status(f"Subprocess launch failed: {e}. Falling back to default browser.", Colors.YELLOW)
            webbrowser.open(auth_url)
    else:
        print_status("Opening default web browser for TikTok login...", Colors.BLUE)
        webbrowser.open(auth_url)

    print_status("Waiting for authorization...", Colors.BLUE)
    start_time = time.time()
    while "access_token" not in tokens:
        time.sleep(0.5)
        if timeout > 0 and (time.time() - start_time) > timeout:
            raise TimeoutError(f"Authorization timed out after {timeout} seconds")

    print_status("Authorization complete!", Colors.GREEN)
    print("\n" + "-" * 30 + " Token Information " + "-" * 30)
    print(f"{Colors.BOLD}Access Token:{Colors.ENDC} {tokens['access_token'][:20]}...{tokens['access_token'][-10:]}")
    print(f"{Colors.BOLD}Refresh Token:{Colors.ENDC} {tokens['refresh_token'][:10]}...{tokens['refresh_token'][-10:]}")
    print(f"{Colors.BOLD}Expires In:{Colors.ENDC} {tokens.get('expires_in', 'Not provided')} seconds")
    if "open_id" in tokens:
        print(f"{Colors.BOLD}Open ID:{Colors.ENDC} {tokens['open_id']}")
    if ACCOUNT:
        print(f"{Colors.BOLD}Account:{Colors.ENDC} {ACCOUNT}")
    print("-" * 70)
    print_status("You can now use these tokens for TikTok API requests", Colors.BLUE)
    print("=" * 70 + "\n")

    if "access_token" in tokens and "refresh_token" in tokens:
        return tokens["access_token"], tokens["refresh_token"]
    else:
        raise RuntimeError("Authorization failed; tokens not retrieved")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="TikTok OAuth authentication",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter
    )
    parser.add_argument("--account", help="Optional TikTok username for authentication", type=str, default=None)
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
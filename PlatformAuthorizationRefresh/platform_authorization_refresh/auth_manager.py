import argparse
import json
import sys
from pathlib import Path
from typing import Dict, Optional

from platform_authorization_refresh.utils import Colors, print_status
from platform_authorization_refresh.tiktok_auth_refresh import refresh_tokens as refresh_tiktok_tokens
from platform_authorization_refresh.youtube_auth_refresh import refresh_tokens as refresh_youtube_tokens


def update_credentials_json(account: str, creds_file: str, token_data: Dict[str, str],
                            add_new_account: bool = False) -> None:
    """
    Update the credentials JSON file with the new token data for the given account.

    If the account is not found (case insensitive) in the JSON and add_new_account is True,
    a new account entry will be created with the token data.

    The JSON structure is expected to be in the following format:

    {
      "accountname@gmail.com": {
        "accessToken": "ya29....",
        "refreshToken": "1//03z_..."
      },
      ...
    }
    """
    creds_file_path = Path(creds_file)
    if not creds_file_path.exists():
        print_status(f"Error: Credentials file not found at {creds_file_path}", Colors.RED)
        sys.exit(1)
    else:
        print_status(f"Using credentials file: {creds_file_path}", Colors.BLUE)

    with creds_file_path.open("r", encoding="utf-8") as f:
        data = json.load(f)

    # Look for a matching key in a case-insensitive manner.
    matched_key = None
    for existing_account in data:
        if existing_account.lower() == account.lower():
            matched_key = existing_account
            break

    if matched_key is None:
        if add_new_account:
            print_status(f"Account '{account}' not found. Adding it as a new account.", Colors.BLUE)
            # Use the provided account string as the key for the new entry.
            matched_key = account
            data[matched_key] = {}
            new_account = True
        else:
            raise KeyError(f"Account '{account}' not found in credentials file.")
    else:
        new_account = False

    if not token_data.get("access_token") or not token_data.get("refresh_token"):
        raise ValueError("Token data is incomplete; not updating credentials.")

    data[matched_key]["accessToken"] = token_data["access_token"]
    data[matched_key]["refreshToken"] = token_data["refresh_token"]

    with creds_file_path.open("w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

    action = "added" if new_account else "updated"
    print_status(f"Successfully {action} tokens for '{matched_key}' in {creds_file_path}", Colors.GREEN)

def find_profile_by_gmail(email: str) -> str:
    """
    Read Chrome's Local State file to deduce the profile folder associated with the given Gmail account.
    Looks for a matching 'user_name' in the 'info_cache' section.
    """
    user_data_dir = Path.home() / "AppData/Local/Google/Chrome Beta/User Data"
    local_state_path = user_data_dir / "Local State"

    if not local_state_path.exists():
        raise FileNotFoundError("Chrome 'Local State' file not found.")

    with local_state_path.open("r", encoding="utf-8") as f:
        local_state = json.load(f)

    profiles = local_state.get("profile", {}).get("info_cache", {})
    for profile_dir, info in profiles.items():
        if info.get("user_name", "").lower() == email.lower():
            return profile_dir  # e.g., "Profile 1"

    raise ValueError(f"No Chrome profile found for Gmail: {email}")


def refresh_tokens(platform: str, account: str, timeout: int = 120,
                   chrome_path: Optional[str] = None, chrome_profile: Optional[str] = None) -> dict[str, str]:
    """
    Refresh tokens for a given platform and update the credentials file.
    Passes along chrome_path and chrome_profile to the platform-specific refresh functions.
    """
    if platform == "tiktok":
        access_token, refresh_token = refresh_tiktok_tokens(
            timeout=timeout, account=account, chrome_path=chrome_path, chrome_profile=chrome_profile
        )
    elif platform == "youtube":
        access_token, refresh_token = refresh_youtube_tokens(
            timeout=timeout, account=account, chrome_path=chrome_path, chrome_profile=chrome_profile
        )
    else:
        raise ValueError(f"Unsupported platform: {platform}")

    return {
        "access_token": access_token,
        "refresh_token": refresh_token
    }


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Refresh OAuth tokens for a specified platform and update the credentials JSON.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter
    )
    parser.add_argument("--platform", required=True, choices=["tiktok", "youtube"], type=str.lower,
                        help="Platform to refresh token for")
    parser.add_argument("--account", required=True,
                        help="Gmail account email (used as key in the credentials JSON and as login_hint)")
    parser.add_argument("--creds_file_path", required=True,
                        help="Credentials JSON file path to update the authorization data received from the platform.")
    parser.add_argument("--chrome", required=False, help="Path to Chrome executable (e.g., Chrome Beta)")
    parser.add_argument("--timeout", type=int, default=120, help="Timeout (in seconds) for the OAuth flow")
    parser.add_argument("--add_new_account", action="store_true", default=False,
                        help="If set, adds the account to the credentials JSON if it is not found (default: False)")
    args = parser.parse_args()

    try:
        chrome_profile = None
        if args.chrome:
            # Deduce the Chrome profile folder based on the Gmail account.
            chrome_profile = find_profile_by_gmail(args.account)
            print_status(f"Inferred Chrome profile: {chrome_profile}", Colors.BLUE)

        token_data = refresh_tokens(
            platform=args.platform,
            account=args.account,
            timeout=args.timeout,
            chrome_path=args.chrome,
            chrome_profile=chrome_profile
        )

        update_credentials_json(
            account=args.account,
            creds_file=args.creds_file_path,
            token_data=token_data,
            add_new_account=args.add_new_account
        )
    except Exception as e:
        print_status(f"Error: {str(e)}", Colors.RED)
        sys.exit(1)


if __name__ == "__main__":
    main()

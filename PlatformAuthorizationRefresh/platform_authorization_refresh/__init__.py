"""
PlatformAuthorizationRefresh

This package handles token refresh flows for supported platforms (YouTube, TikTok),
including updating credentials files and rendering OAuth success/error screens.
"""

from .auth_manager import refresh_tokens, update_credentials_json
from .tiktok_auth_refresh import refresh_tokens as refresh_tiktok_tokens
from .youtube_auth_refresh import refresh_tokens as refresh_youtube_tokens

__all__ = [
    "refresh_tokens",
    "update_credentials_json",
    "refresh_tiktok_tokens",
    "refresh_youtube_tokens",
]
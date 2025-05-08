using System;
using System.Net;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Added for JObject
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.PlatformClients;
using POC_PlatformEngagementPoller.Logging;

namespace POC_PlatformEngagementPoller.CredentialsManagers
{
    /// <summary>
    /// Manages TikTok credentials using caching.
    /// </summary>
    public class TikTokCredentialManager : BaseCredentialsManager<TikTokCredentials>
    {
        private const string c_clientKey = "sbaw3k64p0x5jmmsd1";
        private const string c_clientSecret = "SuSeABmq5pfX6ayAZbgmNQYZaKS0m7y9";

        public TikTokCredentialManager(Dictionary<string, TikTokCredentials> credentials, ILogger logger)
            : base(credentials, logger)
        {
        }

        /// <summary>
        /// Refreshes TikTok credentials for the given username.
        /// Uses the refresh token from the existing credentials to obtain a new access token.
        /// </summary>
        protected override async Task<TikTokCredentials> RefreshCredentialsAsync(string username)
        {
            try
            {
                _logger.Info($"Refreshing credentials for '{username}'.");
                string refreshToken = GetCredentialsData(username).RefreshToken;
                _logger.Info($"Retrieved refresh token for '{username}'.");

                // TikTok token endpoint for refresh
                var tokenEndpoint = "https://open.tiktokapis.com/v2/oauth/token/";
                var parameters = new Dictionary<string, string>
                {
                    { "client_key", c_clientKey },
                    { "client_secret", c_clientSecret },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", refreshToken }
                };

                using (var httpClient = new HttpClient())
                {
                    _logger.Info($"Sending token refresh request for '{username}'.");
                    var content = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errMsg = $"Received unsuccessful status code while requesting to refresh token for '{username}'. Status: {response.StatusCode}.";
                        _logger.Error(errMsg, null);
                        throw new PlatformException(errMsg, null, PlatformErrorType.Authentication);
                    }

                    string responseContent = await response.Content.ReadAsStringAsync();
                    if (IsUnauthorizedToRefreshToken(responseContent))
                    {
                        var errMsg = $"Refresh token invalid or expired for '{username}'. Status: {response.StatusCode}. Response: {responseContent}";
                        _logger.Error(errMsg);
                        throw new PlatformException(errMsg, null, PlatformErrorType.UnauthorizedToRefreshToken);
                    }

                    _logger.Info($"Received successful refresh response for '{username}'.");
                    var json = JObject.Parse(responseContent);

                    // Extract tokens and metadata
                    string newAccessToken = json.Value<string>("access_token");

                    if (string.IsNullOrWhiteSpace(newAccessToken))
                    {
                        _logger.Error($"No access token in TikTok response for '{username}'.", null);
                        throw new PlatformException($"TikTokCredentialManager: No access token returned during refresh.", null, PlatformErrorType.Authentication);
                    }

                    // Build new credentials object
                    var newCreds = new TikTokCredentials
                    {
                        AccessToken = newAccessToken,
                        RefreshToken = refreshToken
                    };

                    _logger.Info($"Credentials refreshed successfully for '{username}'.");
                    return newCreds;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception while refreshing credentials for '{username}'.", ex);
                throw;
            }
        }

        /// <summary>
        /// Determines if the response indicates that the refresh token is invalid or expired.
        /// </summary>
        protected override bool IsUnauthorizedToRefreshToken(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
                return false;

            try
            {
                var errorObj = JObject.Parse(responseContent);
                // TikTok v2 OAuth errors appear in "error" or v2 API errors in "code"
                string errorCode = errorObj.Value<string>("error") ?? errorObj.Value<string>("code");

                // According to OAuth 2.0, invalid_grant means expired/invalid token :contentReference[oaicite:13]{index=13}
                // Community examples confirm invalid_grant for expired refresh tokens :contentReference[oaicite:14]{index=14}
                // TikTok v2 API uses access_token_invalid for invalid tokens :contentReference[oaicite:15]{index=15}
                return errorCode == "invalid_grant"
                    || errorCode == "invalid_request"
                    || errorCode == "access_token_invalid";
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}

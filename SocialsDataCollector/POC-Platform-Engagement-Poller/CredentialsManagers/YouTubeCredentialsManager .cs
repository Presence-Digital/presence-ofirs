using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.PlatformClients;
using POC_PlatformEngagementPoller.Logging;

namespace POC_PlatformEngagementPoller.CredentialsManagers
{
    /// <summary>
    /// Manages YouTube credentials using caching.
    /// </summary>
    public class YouTubeCredentialManager : BaseCredentialsManager<YouTubeCredentials>
    {
        private const string c_clientId = "918479755586-191n89psglkjut294ibsfu3d95e7ng2j.apps.googleusercontent.com";
        private const string c_clientSecret = "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";

        public YouTubeCredentialManager(Dictionary<string, YouTubeCredentials> credentials, ILogger logger)
            : base(credentials, logger)
        {
        }

        /// <summary>
        /// Refreshes YouTube credentials for the given username.
        /// Uses the refresh token from the existing credentials to obtain a new access token.
        /// </summary>
        protected override async Task<YouTubeCredentials> RefreshCredentialsAsync(string username)
        {
            try
            {
                _logger.Info($"Refreshing credentials for '{username}'.");
                string refreshToken = GetCredentialsData(username).RefreshToken;
                _logger.Info($"Retrieved refresh token for '{username}'.");

                // Prepare the token refresh request.
                var tokenEndpoint = "https://oauth2.googleapis.com/token";
                var parameters = new Dictionary<string, string>
                {
                    { "client_id", c_clientId },
                    { "client_secret", c_clientSecret },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };

                using (var httpClient = new HttpClient())
                {
                    _logger.Info($"Sending HTTP request to get tokens for '{username}'.");
                    var content = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, content);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (IsUnauthorizedToRefreshToken(responseContent))
                        {
                            _logger.Error($"Refresh token invalid or expired for '{username}'. Status: {response.StatusCode}. Response: {responseContent}", null);
                            throw new PlatformException($"YouTubeCredentialManager is unauthorized to refresh token. Status: {response.StatusCode}. Response: {responseContent}", null, PlatformErrorType.UnauthorizedToRefreshToken);
                        }
                        _logger.Error($"Failed to refresh token for '{username}'. Status: {response.StatusCode}. Response: {responseContent}", null);
                        throw new PlatformException($"YouTubeCredentialManager failed to refresh token. Status: {response.StatusCode}. Response: {responseContent}", null, PlatformErrorType.Authentication);
                    }

                    _logger.Info($"HTTP response to get tokens returned successfully.");
                    // Deserialize the JSON response dynamically.
                    dynamic tokenResult = JsonConvert.DeserializeObject(responseContent);
                    if (tokenResult == null || tokenResult.access_token == null ||
                        string.IsNullOrWhiteSpace((string)tokenResult.access_token))
                    {
                        _logger.Error($"Failed to obtain a new access token from the refresh response for '{username}'.", null);
                        throw new Exception("Failed to obtain a new access token from the refresh response.");
                    }

                    // Extract the new access token.
                    string newAccessToken = tokenResult.access_token;
                    _logger.Info($"New access token obtained for '{username}'.");

                    // Create new credentials with the new access token and the existing refresh token.
                    var newCreds = new YouTubeCredentials
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

        protected override bool IsUnauthorizedToRefreshToken(string responseContent)
        {
            bool isUnauthorized = responseContent.Contains("revoked") || responseContent.Contains("expired");
            if (isUnauthorized)
            {
                _logger.Warning("Detected unauthorized refresh response (token revoked or expired).");
            }
            return isUnauthorized;
        }
    }
}

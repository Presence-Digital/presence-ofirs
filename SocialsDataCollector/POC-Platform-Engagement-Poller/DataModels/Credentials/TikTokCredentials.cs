namespace POC_PlatformEngagementPoller.DataModels.Credentials
{
    /// <summary>
    /// Represents the credentials required for TikTok API access.
    /// </summary>
    public class TikTokCredentials : BaseCredentials
    {
        /// <summary>
        /// Gets or sets the username associated with these credentials.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the TikTok access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the TikTok refresh token.
        /// </summary>
        public string RefreshToken { get; set; }
    }
}

namespace POC_PlatformEngagementPoller.DataModels.Credentials
{
    /// <summary>
    /// Represents the OAuth credentials required for YouTube API access.
    /// </summary>
    public class YouTubeCredentials : BaseCredentials
    {
        /// <summary>
        /// Gets or sets the username associated with these credentials.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the OAuth access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the OAuth refresh token.
        /// </summary>
        public string RefreshToken { get; set; }
    }
}

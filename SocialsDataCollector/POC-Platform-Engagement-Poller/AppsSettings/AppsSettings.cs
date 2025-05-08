using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace POC_PlatformEngagementPoller.AppsSettings
{
    public class AppSettings
    {
        // Configuration properties (used for binding)
        [Required]
        public string OutputFolderBasePath { get; set; }

        [Required]
        public string CredentialsFolderBasePath { get; set; }

        public string OutputFolderYouTube { get; set; } = "YoutubeDashboardData";
        public string OutputFolderTikTok { get; set; } = "TikTokDashboardData";
        public string CredentialsYoutubeFileName { get; set; } = "YouTubeCredentials.json";
        public string CredentialsTikTokFileName { get; set; } = "TikTokCredentials.json";
        public string AuthorizationRefreshExePath { get; set; }
        public string AuthorizationRefreshNotificationsFilePath { get; set; }
        public string AuthorizationRefreshChromePath { get; set; } = string.Empty;
        public string OutputLogFolderPath { get; set; }

        // Encapsulated dictionary of settings
        private static Dictionary<string, string> _settings;

        // Public key constants (exposed throughout the project).
        public const string AUTHORIZATION_REFRESH_CHROME_PATH_KEY = nameof(AuthorizationRefreshChromePath);
        public const string AUTHORIZATION_REFRESH_EXE_PATH_KEY = nameof(AuthorizationRefreshExePath);
        public const string AUTHORIZATION_REFRESH_NOTIFICATIONS_FILE_PATH_KEY = nameof(AuthorizationRefreshNotificationsFilePath);
        public const string OUTPUT_LOG_FOLDER_PATH_KEY = nameof(OutputLogFolderPath);

        // Private key constants (used for public methods in AppsSettings class)
        private const string CREDENTIALS_FOLDER_BASE_PATH_KEY = nameof(CredentialsFolderBasePath);
        private const string CREDENTIALS_TIKTOK_FILE_NAME_KEY = nameof(CredentialsTikTokFileName);
        private const string CREDENTIALS_YOUTUBE_FILE_NAME_KEY = nameof(CredentialsYoutubeFileName);
        private const string OUTPUT_FOLDER_BASE_PATH_KEY = nameof(OutputFolderBasePath);
        private const string OUTPUT_FOLDER_TIKTOK_KEY = nameof(OutputFolderTikTok);
        private const string OUTPUT_FOLDER_YOUTUBE_KEY = nameof(OutputFolderYouTube);

        /// <summary>
        /// Initializes the configuration settings by reading from the JSON file,
        /// binding them to an AppSettings instance, and storing them in a private dictionary.
        /// </summary>
        public static void InitializeSettings()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("AppsSettings\\appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var settings = new AppSettings();
            configuration.Bind(settings);

            _settings = new Dictionary<string, string>
            {
                // Public keys
                { AUTHORIZATION_REFRESH_CHROME_PATH_KEY, settings.AuthorizationRefreshChromePath },
                { AUTHORIZATION_REFRESH_EXE_PATH_KEY, settings.AuthorizationRefreshExePath },
                { AUTHORIZATION_REFRESH_NOTIFICATIONS_FILE_PATH_KEY, settings.AuthorizationRefreshNotificationsFilePath },
                { OUTPUT_LOG_FOLDER_PATH_KEY, settings.OutputLogFolderPath },

                // Private keys
                { CREDENTIALS_FOLDER_BASE_PATH_KEY, settings.CredentialsFolderBasePath },
                { CREDENTIALS_TIKTOK_FILE_NAME_KEY, settings.CredentialsTikTokFileName },
                { CREDENTIALS_YOUTUBE_FILE_NAME_KEY, settings.CredentialsYoutubeFileName },
                { OUTPUT_FOLDER_BASE_PATH_KEY, settings.OutputFolderBasePath },
                { OUTPUT_FOLDER_TIKTOK_KEY, settings.OutputFolderTikTok },
                { OUTPUT_FOLDER_YOUTUBE_KEY, settings.OutputFolderYouTube },
            };
        }

        /// <summary>
        /// Retrieves the setting value for the specified key.
        /// </summary>
        public static string Get(string key)
        {
            if (_settings == null)
            {
                throw new Exception("Settings have not been initialized. Call InitializeSettings() first.");
            }

            if (_settings.TryGetValue(key, out var value))
            {
                return value;
            }

            throw new Exception($"Configuration key '{key}' not found.");
        }

        public static string GetFullCredentialsYoutubeFilePath()
        {
            var basePath = Get(CREDENTIALS_FOLDER_BASE_PATH_KEY);
            var fileName = Get(CREDENTIALS_YOUTUBE_FILE_NAME_KEY);
            return Path.Combine(basePath, fileName);
        }

        public static string GetFullCredentialsTikTokFilePath()
        {
            var basePath = Get(CREDENTIALS_FOLDER_BASE_PATH_KEY);
            var fileName = Get(CREDENTIALS_TIKTOK_FILE_NAME_KEY);
            return Path.Combine(basePath, fileName);
        }

        public static string GetFullOutputFolderYouTube()
        {
            var basePath = Get(OUTPUT_FOLDER_BASE_PATH_KEY);
            var folderName = Get(OUTPUT_FOLDER_YOUTUBE_KEY);
            return Path.Combine(basePath, folderName);
        }

        public static string GetFullOutputFolderTikTok()
        {
            var basePath = Get(OUTPUT_FOLDER_BASE_PATH_KEY);
            var folderName = Get(OUTPUT_FOLDER_TIKTOK_KEY);
            return Path.Combine(basePath, folderName);
        }
    }
}

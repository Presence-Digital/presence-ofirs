using Newtonsoft.Json;
using POC_PlatformEngagementPoller.AppsSettings;
using POC_PlatformEngagementPoller.CredentialsManagers;
using POC_PlatformEngagementPoller.DataModels;
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.PlatformClients;
using POC_PlatformEngagementPoller.AuthorizationExpiredManagement;
using POC_PlatformEngagementPoller.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using POC_PlatformEngagementPoller.DataModels.Entities;

namespace POC_PlatformEngagementPoller
{
    class Program
    {
        private const string c_program_name = "Presence Engagement Poller Application";
        private const string c_youtube = "YouTube";
        private const string c_tiktok = "TikTok";

        // Global logger instance for the application.
        private static ILogger _logger;

        static async Task Main(string[] args)
        {
            // Initialize settings.
            AppSettings.InitializeSettings();

            // Initialize the LocalFileLogger using the log folder setting.
            string logFolder = AppSettings.Get(AppSettings.OUTPUT_LOG_FOLDER_PATH_KEY);
            _logger = new LocalFileLogger(logFolder);

            _logger.Info($"'{c_program_name}' started.");

            try
            {
                bool success = await RunAsync();
                _logger.Info($"'{c_program_name}' finished {(success ? "successfully" : "unsuccessfully")}.");
            }
            catch (Exception ex)
            {
                // Log any unexpected exception thrown in Main.
                _logger.Critical("Unhandled exception in Main.", ex);
            }
        }

        private static async Task<bool> RunAsync()
        {
            // Local flags to enable flows for each platform.
            bool enableYouTube = false;
            bool enableTikTok = true;

            AuthorizationExpiredRecordManager.SetLogger(_logger);
            AuthorizationExpiredRecordManager.ClearAllAuthorizationExpiredRecords();

            if (enableYouTube)
            {
                // Build credentials file path.
                var youtubeCredsPath = AppSettings.GetFullCredentialsYoutubeFilePath();

                var youtubeCredentialsDict = await LoadYouTubeCredentialsAsync(youtubeCredsPath);
                YouTubeClient youtubeClient = new YouTubeClient(new YouTubeCredentialManager(youtubeCredentialsDict, _logger), _logger);

                var youtubeAccounts = youtubeCredentialsDict.Keys;
                await ProcessAllYouTubeAccountsAsync(youtubeClient, youtubeAccounts);
            }

            if (enableTikTok)
            {
                var tiktokCredsPath = AppSettings.GetFullCredentialsTikTokFilePath();

                var tiktokCredentialsDict = await LoadTikTokCredentialsAsync(tiktokCredsPath);
                TikTokClient tiktokClient = new TikTokClient(new TikTokCredentialManager(tiktokCredentialsDict, _logger), _logger);

                var tiktokAccounts = tiktokCredentialsDict.Keys;
                await ProcessAllTikTokAccountsAsync(tiktokClient, tiktokAccounts);
            }

            return true;
        }

        private static async Task<Dictionary<string, YouTubeCredentials>> LoadYouTubeCredentialsAsync(string credsFilePath)
        {
            try
            {
                if (!File.Exists(credsFilePath))
                    throw new Exception($"File not found: {credsFilePath}");

                string credentialsDataJson = await File.ReadAllTextAsync(credsFilePath);
                var credentials = JsonConvert.DeserializeObject<Dictionary<string, YouTubeCredentials>>(credentialsDataJson);
                if (credentials == null)
                    throw new Exception("Failed to deserialize YouTube credentials from JSON.");

                return credentials;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception in LoadYouTubeCredentialsAsync.", ex);
                throw;
            }
        }

        private static async Task<Dictionary<string, TikTokCredentials>> LoadTikTokCredentialsAsync(string credsFilePath)
        {
            try
            {
                if (!File.Exists(credsFilePath))
                    throw new Exception($"File not found: {credsFilePath}");

                string credentialsDataJson = await File.ReadAllTextAsync(credsFilePath);
                var credentials = JsonConvert.DeserializeObject<Dictionary<string, TikTokCredentials>>(credentialsDataJson);
                if (credentials == null)
                    throw new Exception("Failed to deserialize TikTok credentials from JSON.");

                return credentials;
            }
            catch (Exception ex)
            {
                _logger.Error("Exception in LoadTikTokCredentialsAsync.", ex);
                throw;
            }
        }

        private static async Task ProcessAllYouTubeAccountsAsync(YouTubeClient client, IEnumerable<string> youtubeAccounts)
        {
            foreach (var account in youtubeAccounts)
            {
                var engagementData = await GetYouTubeEngagementDataAsync(client, account);
                if (engagementData != null)
                {
                    await StoreDataAsync(account, engagementData, c_youtube);
                }
                else
                {
                    _logger.Warning($"Failed to get data for account: '{account}'");
                }
            }
        }

        private static async Task ProcessAllTikTokAccountsAsync(TikTokClient client, IEnumerable<string> tiktokAccounts)
        {
            foreach (var account in tiktokAccounts)
            {
                var engagementData = await GetTikTokEngagementDataAsync(client, account);
                await StoreDataAsync(account, engagementData, c_tiktok);
            }
        }

        private static async Task<object> GetYouTubeEngagementDataAsync(YouTubeClient ytClient, string username)
        {
            try
            {
                SocialMediaAccount accountStats = await ytClient.GetAccountStatisticsAsync(username);
                List<SocialMediaPost> allPostsStats = await ytClient.GetPostsStatisticsAsync(username);
                return new { Account = accountStats, Posts = allPostsStats };
            }
            catch (Exception ex)
            {
                if (ex is PlatformException PlatEx && PlatEx.ErrorType == PlatformErrorType.UnauthorizedToRefreshToken)
                {
                    _logger.Warning($"PlatformException.UnauthorizedToRefreshToken for account '{username}'. Platform: 'Youtube'.");
                    AuthorizationExpiredRecordManager.LogAuthorizationExpiredNotification(c_youtube, username);
                    _logger.Info($"LogAuthorizationExpiredNotification succeeded for account '{username}' on Platform: 'Youtube'.");
                }
                else
                {
                    _logger.Critical($"Exception in GetYouTubeEngagementDataAsync for account '{username}'.", ex);
                    throw;
                }
            }

            return null;
        }

        private static async Task<object> GetTikTokEngagementDataAsync(TikTokClient ttClient, string username)
        {
            try
            {
                SocialMediaAccount accountStats = await ttClient.GetAccountStatisticsAsync(username);
                List<SocialMediaPost> allPostsStats = await ttClient.GetPostsStatisticsAsync(username);
                return new { Account = accountStats, Posts = allPostsStats };
            }
            catch (Exception ex)
            {
                _logger.Critical($"Exception in GetTikTokEngagementDataAsync for account '{username}'.", ex);
                throw;
            }
        }

        private static async Task StoreDataAsync(string username, object engagementData, string platformName)
        {
            try
            {
                string prettyJson = JsonConvert.SerializeObject(engagementData, Formatting.Indented);
                string outputFilePath = GetOutputFilePath(username, platformName);
                await File.WriteAllTextAsync(outputFilePath, prettyJson);
                _logger.Info($"Data for user '{username}' was written to: {outputFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in StoreDataAsync for account '{username}'.", ex);
                throw;
            }
        }

        private static string GetOutputFilePath(string username, string platformName)
        {
            try
            {
                // Sanitize username for file paths.
                string sanitizedUser = username;
                foreach (char c in Path.GetInvalidFileNameChars())
                    sanitizedUser = sanitizedUser.Replace(c, '_');

                // Get platform-specific folder name.
                string folderName = platformName == c_youtube
                    ? AppSettings.GetFullOutputFolderYouTube()
                    : AppSettings.GetFullOutputFolderTikTok();

                string userFolder = Path.Combine(folderName, sanitizedUser);
                Directory.CreateDirectory(userFolder);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd__HH-mm");
                return Path.Combine(userFolder, $"{timestamp}.json");
            }
            catch (Exception ex)
            {
                _logger.Error("Exception in GetOutputFilePath.", ex);
                throw;
            }
        }
    }
}

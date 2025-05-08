using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using POC_PlatformEngagementPoller.AppsSettings;
using POC_PlatformEngagementPoller.DataModels;
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.Logging;

namespace POC_PlatformEngagementPoller.AuthorizationExpiredManagement
{
    public static class AuthorizationExpiredRecordManager
    {
        // Get the notifications file path from AppSettings.
        private static readonly string AuthorizationRefreshNotificationsFilePath =
            Environment.ExpandEnvironmentVariables(AppSettings.Get(AppSettings.AUTHORIZATION_REFRESH_NOTIFICATIONS_FILE_PATH_KEY));

        // Path to the unified Authorization Refresh executable.
        private static readonly string PlatformAuthorizationRefreshExecutablePath = 
            AppSettings.Get(AppSettings.AUTHORIZATION_REFRESH_EXE_PATH_KEY);

        // Static ILogger instance.
        private static ILogger _logger;

        /// <summary>
        /// Sets the logger to be used by the AuthorizationExpiredRecordManager.
        /// </summary>
        /// <param name="logger">An ILogger implementation.</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Clears all authorization refresh notifications by clearing the notifications file.
        /// </summary>
        public static void ClearAllAuthorizationExpiredRecords()
        {
            EnsureLogger();
            _logger.Debug($"PlatformAuthorizationRefresh.EXE file is known to be at: '{PlatformAuthorizationRefreshExecutablePath}'.");
            try
            {
                if (File.Exists(AuthorizationRefreshNotificationsFilePath))
                {
                    File.WriteAllText(AuthorizationRefreshNotificationsFilePath, "[]");
                    _logger.Info($"{AuthorizationRefreshNotificationsFilePath} file has been cleared successfully.");
                }
                else
                {
                    _logger.Warning($"{AuthorizationRefreshNotificationsFilePath} file not found. No action taken.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error clearing AuthorizationRefreshNotifications.json file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Logs an expired authorization notification by generating a notification record
        /// and saving it to the notifications file.
        /// </summary>
        /// <param name="platform">The platform name (e.g., "YouTube", "TikTok").</param>
        /// <param name="account">The account identifier.</param>
        public static void LogAuthorizationExpiredNotification(string platform, string account)
        {
            EnsureLogger();
            try
            {
                string executableCommand = GenerateAuthorizationRefreshCommand(platform, account);

                var record = new AuthorizationExpiredRecord
                {
                    UpdatedOn = DateTime.UtcNow,
                    Platform = platform,
                    Account = account,
                    ExecutableCommand = executableCommand
                };

                SaveRecordToNotificationFile(record);
                _logger.Info($"AuthorizationExpiredRecord logged for platform '{platform}', account '{account}' at {record.UpdatedOn}.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error logging AuthorizationExpiredRecord for platform '{platform}', account '{account}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generates the executable command string to refresh tokens using the unified executable.
        /// </summary>
        /// <param name="platform">The platform name ("youtube" or "tiktok").</param>
        /// <param name="account">The Gmail account used in credentials.</param>
        /// <returns>The full command line to execute the token refresh.</returns>
        private static string GenerateAuthorizationRefreshCommand(string platform, string account)
        {
            // Get and quote the .exe path
            string exePath = AppSettings.Get(AppSettings.AUTHORIZATION_REFRESH_EXE_PATH_KEY)?.Trim();
            if (string.IsNullOrWhiteSpace(exePath))
                throw new ArgumentException("AuthorizationRefreshExePath is not set.");
            string quotedExePath = $"\"{exePath}\"";

            // Validate and normalize platform
            string platformArg = platform.ToLowerInvariant();
            if (platformArg != "youtube" && platformArg != "tiktok")
                throw new ArgumentException($"Unsupported platform: {platform}");

            // Get credentials path
            string credsPath = platformArg switch
            {
                "youtube" => AppSettings.GetFullCredentialsYoutubeFilePath(),
                "tiktok" => AppSettings.GetFullCredentialsTikTokFilePath(),
                _ => throw new ArgumentException($"Unsupported platform: {platform}")
            };

            // Quote credsPath only if it contains spaces
            string maybeQuotedCredsPath = credsPath.Contains(' ')
                ? $"\"{credsPath}\""
                : credsPath;

            // Start command
            string command = $"{quotedExePath} --platform {platformArg} --account {account} --creds_file_path {maybeQuotedCredsPath}";

            // Get and quote the Chrome path
            string chromePath = AppSettings.Get(AppSettings.AUTHORIZATION_REFRESH_CHROME_PATH_KEY)?.Trim();
            if (!string.IsNullOrEmpty(chromePath))
            {
                string quotedChromePath = $"\"{chromePath}\"";
                command += $" --chrome {quotedChromePath}";
            }

            return command;
        }



        /// <summary>
        /// Loads all authorization refresh notification records from the notifications file.
        /// </summary>
        /// <returns>A list of AuthorizationExpiredRecord objects.</returns>
        private static List<AuthorizationExpiredRecord> LoadAuthorizationExpiredRecords()
        {
            try
            {
                if (!File.Exists(AuthorizationRefreshNotificationsFilePath))
                {
                    _logger.Warning("AuthorizationRefreshNotifications.json file does not exist. Returning an empty list.");
                    return new List<AuthorizationExpiredRecord>();
                }
                string json = File.ReadAllText(AuthorizationRefreshNotificationsFilePath);
                var records = JsonConvert.DeserializeObject<List<AuthorizationExpiredRecord>>(json);
                _logger.Info("AuthorizationRefreshNotifications.json loaded successfully.");
                return records ?? new List<AuthorizationExpiredRecord>();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading AuthorizationRefreshNotifications.json: {ex.Message}", ex);
                return new List<AuthorizationExpiredRecord>();
            }
        }

        /// <summary>
        /// Saves the provided list of records to the notifications file.
        /// </summary>
        /// <param name="records">The list of AuthorizationExpiredRecord objects.</param>
        private static void SaveRecords(List<AuthorizationExpiredRecord> records)
        {
            try
            {
                string directory = Path.GetDirectoryName(AuthorizationRefreshNotificationsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                string json = JsonConvert.SerializeObject(records, Formatting.Indented);
                File.WriteAllText(AuthorizationRefreshNotificationsFilePath, json);
                _logger.Info("AuthorizationExpiredRecords saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving AuthorizationExpiredRecords: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves a single record by loading existing records, appending the new record if it does not already exist,
        /// and persisting the updated list.
        /// </summary>
        /// <param name="record">The AuthorizationExpiredRecord to save.</param>
        private static void SaveRecordToNotificationFile(AuthorizationExpiredRecord record)
        {
            try
            {
                var records = LoadAuthorizationExpiredRecords();

                if (!records.Any(r => r.Platform.Equals(record.Platform, StringComparison.OrdinalIgnoreCase)
                                       && r.Account.Equals(record.Account, StringComparison.OrdinalIgnoreCase)))
                {
                    records.Add(record);
                    SaveRecords(records);
                    _logger.Info($"New AuthorizationExpiredRecord saved for platform '{record.Platform}' and account '{record.Account}'.");
                }
                else
                {
                    _logger.Debug($"Notification record for platform '{record.Platform}' and account '{record.Account}' already exists; not saving duplicate.");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error saving AuthorizationExpiredRecord for platform '{record.Platform}' and account '{record.Account}': {ex.Message}", ex);
            }
        }

        private static void EnsureLogger()
        {
            if (_logger == null)
            {
                throw new InvalidOperationException("Logger not set. Please call SetLogger() before using AuthorizationExpiredRecordManager methods.");
            }
        }
    }
}

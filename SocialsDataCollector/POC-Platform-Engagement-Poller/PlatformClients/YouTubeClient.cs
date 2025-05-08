using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Google;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.DataMappers;
using POC_PlatformEngagementPoller.Logging;
using POC_PlatformEngagementPoller.CredentialsManagers;
using POC_PlatformEngagementPoller.DataModels.Entities;

namespace POC_PlatformEngagementPoller.PlatformClients
{
    /// <summary>
    /// Client for interacting with the YouTube API to retrieve engagement data.
    /// Inherits common functionality from BasePlatformClient.
    /// </summary>
    public class YouTubeClient : BasePlatformClient<YouTubeCredentials>
    {
        private const int YouTubePlaylistPageSize = 50;  // max per YouTube API

        private readonly YouTubePlatformDataMapper _dataMapper;

        /// <summary>
        /// Initializes a new instance of the YouTube client.
        /// </summary>
        /// <param name="credentialManager">The credential manager for YouTube.</param>
        /// <param name="logger">The logger instance to log errors and informational messages.</param>
        public YouTubeClient(ICredentialsManager<YouTubeCredentials> credentialManager, ILogger logger)
            : base(credentialManager, logger)
        {
            _dataMapper = new YouTubePlatformDataMapper();
        }

        /// <summary>
        /// Retrieves detailed account-level data for the authenticated YouTube channel.
        /// </summary>
        /// <param name="accountId">The credentials cache key (accountId) of the account.</param>
        /// <returns>An Account instance with structured channel data.</returns>
        public override async Task<SocialMediaAccount> GetAccountStatisticsAsync(string accountId)
        {
            try
            {
                _logger.Info($"Retrieving account statistics for account '{accountId}'.");
                var channelResponse = await ExecuteApiCallAsync(accountId, async (service) =>
                {
                    var youtubeService = service as YouTubeService;
                    if (youtubeService == null)
                        throw new InvalidOperationException("Invalid YouTubeService instance.");

                    var request = youtubeService.Channels.List("snippet,statistics,brandingSettings,contentDetails,status,topicDetails");
                    request.Mine = true;
                    return await request.ExecuteAsync();
                });

                if (channelResponse.Items.Count == 0)
                {
                    _logger.Warning($"No channel found for account '{accountId}'.");
                    throw new PlatformException("Channel not found.", null, PlatformErrorType.NotFound);
                }

                _logger.Info($"Account statistics retrieved successfully for account '{accountId}'.");
                var channel = channelResponse.Items[0];

                // Map the raw Channel object into our structured Account model.
                return _dataMapper.MapChannelToSocialMediaAccount(channel);
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in GetAccountStatisticsAsync for account '{accountId}'.", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves detailed post-level (video) data for the authenticated YouTube channel.
        /// Uses a Shorts-specific retrieval mechanism.
        /// </summary>
        /// <param name="accountId">The credentials cache key (accountId) of the account.</param>
        /// <param name="topRecentPosts">Optional parameter specifying the number of recent posts to retrieve.</param>
        /// <returns>A list of Post objects with detailed video data.</returns>
        public override async Task<List<SocialMediaPost>> GetPostsStatisticsAsync(string accountId, int? topRecentPosts = null)
        {
            try
            {
                _logger.Info($"Retrieving posts statistics (Shorts) for account '{accountId}'.");
                return await GetPostsStatsUsingShortsPlaylistAsync(accountId, topRecentPosts);
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in GetPostsStatisticsAsync for account '{accountId}'.", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves Shorts-specific post statistics using a Shorts-specific playlist.
        /// </summary>
        private async Task<List<SocialMediaPost>> GetPostsStatsUsingShortsPlaylistAsync(string accountId, int? topRecentPosts)
        {
            try
            {
                _logger.Info($"Retrieving channel details for account '{accountId}' to build Shorts playlist.");
                var channelResponse = await ExecuteApiCallAsync(accountId, async (service) =>
                {
                    var youtubeService = service as YouTubeService;
                    if (youtubeService == null)
                        throw new InvalidOperationException("Invalid YouTubeService instance.");

                    var channelRequest = youtubeService.Channels.List("id");
                    channelRequest.Mine = true;
                    return await channelRequest.ExecuteAsync();
                });

                if (channelResponse.Items.Count == 0)
                {
                    var errMsg = $"Found 0 posts for account '{accountId}'.";
                    _logger.Warning(errMsg);
                    throw new PlatformException(errMsg, null, PlatformErrorType.NotFound);
                }

                string channelId = channelResponse.Items[0].Id;
                _logger.Info($"Retrieved channel ID '{channelId}' for account '{accountId}'.");

                if (string.IsNullOrEmpty(channelId) || !channelId.StartsWith("UC"))
                    throw new PlatformException("Invalid channel ID format.", null, PlatformErrorType.InvalidRequest);

                string shortsPlaylistId = "UUSH" + channelId.Substring(2);
                _logger.Info($"Derived Shorts playlist ID '{shortsPlaylistId}' for account '{accountId}'.");

                // Page through the Shorts playlist.
                var playlistItems = new List<PlaylistItem>();
                string nextPageToken = null;

                do
                {
                    // Determine how many items to ask for on this page
                    int desired = topRecentPosts.HasValue
                        ? topRecentPosts.Value - playlistItems.Count
                        : YouTubePlaylistPageSize;

                    // Never ask for more than the API allows
                    int pageSize = Math.Min(desired, YouTubePlaylistPageSize);

                    if (pageSize <= 0)
                        break;  // we already have enough

                    var playlistResponse = await ExecuteApiCallAsync(accountId, async (service) =>
                    {
                        var youtubeService = service as YouTubeService;
                        if (youtubeService == null)
                            throw new InvalidOperationException("Invalid YouTubeService instance.");

                        var playlistRequest = youtubeService.PlaylistItems.List("snippet");
                        playlistRequest.PlaylistId = shortsPlaylistId;
                        playlistRequest.MaxResults = pageSize;
                        playlistRequest.PageToken = nextPageToken;
                        return await playlistRequest.ExecuteAsync();
                    });

                    if (playlistResponse.Items != null)
                    {
                        _logger.Info($"Retrieved {playlistResponse.Items.Count} items from a playlist page for account '{accountId}'.");
                        playlistItems.AddRange(playlistResponse.Items);
                    }

                    nextPageToken = playlistResponse.NextPageToken;
                } while (!string.IsNullOrEmpty(nextPageToken));

                if (playlistItems.Count == 0)
                    _logger.Warning($"No items found in Shorts playlist for account '{accountId}'.");

                // Order items by published date (descending) and limit if specified.
                playlistItems = playlistItems.OrderByDescending(pi => pi.Snippet.PublishedAtDateTimeOffset).ToList();
                if (topRecentPosts.HasValue)
                    playlistItems = playlistItems.Take(topRecentPosts.Value).ToList();

                var videoIds = playlistItems.Select(pi => pi.Snippet.ResourceId.VideoId).ToList();
                _logger.Info($"Retrieving details for {videoIds.Count} videos for account '{accountId}'.");
                return await GetVideosDetailsAsync(accountId, videoIds);
            }
            catch (Exception ex)
            {
                _logger.Error($"Exception in GetPostsStatsUsingShortsPlaylistAsync for account '{accountId}'.", ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves detailed video information for a batch of video IDs.
        /// </summary>
        private async Task<List<SocialMediaPost>> GetVideosDetailsAsync(string accountId, List<string> videoIds)
        {
            const int batchSize = 50;
            var posts = new List<SocialMediaPost>();

            // Process video IDs in batches (using C# 8+ Chunk extension).
            var videoBatches = videoIds.Chunk(batchSize);

            foreach (var batchIds in videoBatches)
            {
                _logger.Info($"Processing batch of {batchIds.Length} video IDs for account '{accountId}'.");
                var response = await ExecuteApiCallAsync(accountId, async (service) =>
                {
                    if (!(service is YouTubeService youtubeService))
                        throw new InvalidOperationException("Invalid YouTubeService instance.");

                    var request = youtubeService.Videos.List("snippet,statistics,contentDetails,player,status,topicDetails");
                    request.Id = string.Join(",", batchIds);
                    return await request.ExecuteAsync();
                });

                if (response?.Items == null || !response.Items.Any())
                {
                    _logger.Warning($"No video details returned for a batch in account '{accountId}'.");
                    continue;
                }

                posts.AddRange(_dataMapper.MapVideosToSocialMediaPosts(response.Items));
            }

            _logger.Info($"Retrieved details for {posts.Count} posts for account '{accountId}'.");
            return posts;
        }


        /// <summary>
        /// Creates a YouTube API client using the provided credentials.
        /// </summary>
        /// <param name="credentials">The YouTube credentials.</param>
        /// <returns>A YouTubeService instance with the appropriate authentication.</returns>
        protected override object CreateApiClient(YouTubeCredentials credentials)
        {
            _logger.Info($"Creating YouTubeService API client.");
            return new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = new AccessTokenCredential(credentials.AccessToken),
                ApplicationName = "Platform Engagement Poller"
            });
        }

        /// <summary>
        /// Overrides the unauthorized exception check to handle GoogleApiException.
        /// </summary>
        protected override bool IsUnauthorizedException(Exception ex)
        {
            if (ex is GoogleApiException googleEx)
            {
                return googleEx.HttpStatusCode == HttpStatusCode.Unauthorized;
            }
            return base.IsUnauthorizedException(ex);
        }

        /// <summary>
        /// Internal helper class to initialize the HTTP client with the OAuth access token.
        /// </summary>
        private class AccessTokenCredential : Google.Apis.Http.IConfigurableHttpClientInitializer
        {
            private readonly string _accessToken;

            public AccessTokenCredential(string accessToken)
            {
                _accessToken = accessToken;
            }

            public void Initialize(Google.Apis.Http.ConfigurableHttpClient httpClient)
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using POC_PlatformEngagementPoller.CredentialsManagers;
using POC_PlatformEngagementPoller.DataModels;
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.DataModels.DataTranferObjects;
using POC_PlatformEngagementPoller.DataModels.Entities;
using POC_PlatformEngagementPoller.DataModels.PlatformDataMappers;
using POC_PlatformEngagementPoller.Logging;

namespace POC_PlatformEngagementPoller.PlatformClients
{
    public class TikTokClient : BasePlatformClient<TikTokCredentials>
    {
        private const string BaseApiUrl = "https://open.tiktokapis.com/v2";
        private const string UserInfoFields = "union_id,avatar_large_url,display_name,bio_description,profile_deep_link,is_verified,username,follower_count,following_count,likes_count,video_count";
        private const string VideoListFields = "id,title,video_description,create_time,duration,cover_image_url,embed_link";
        private const string VideoQueryFields = "id,like_count,comment_count,share_count,view_count";

        private readonly TikTokPlatformDataMapper _dataMapper;


        public TikTokClient(ICredentialsManager<TikTokCredentials> credentialManager, ILogger logger)
            : base(credentialManager, logger)
        {
            _dataMapper = new TikTokPlatformDataMapper();

        }


        public override async Task<SocialMediaAccount> GetAccountStatisticsAsync(string accountId)
        {
            var tikTokUserInfoResponse = await ExecuteApiCallAsync<TikTokUserInfoResponse>(accountId, async obj =>
            {
                var http = (HttpClient)obj;
                var url = $"{BaseApiUrl}/user/info/?fields={UserInfoFields}";
                _logger.Debug($"GET {url}");

                var resp = await http.GetAsync(url);
                _logger.Info($"HTTP {(int)resp.StatusCode} for account {accountId}");
                resp.EnsureSuccessStatusCode();

                using var sr = new StreamReader(await resp.Content.ReadAsStreamAsync());
                string json = await sr.ReadToEndAsync().ConfigureAwait(false);
                try
                {
                    return JsonConvert.DeserializeObject<TikTokUserInfoResponse>(json);
                }
                catch (JsonException ex)
                {
                    _logger.Error($"Failed to deserialize TikTokUserInfoResponse JSON for '{accountId}': {ex.Message}", ex);
                    throw new PlatformException($"Invalid JSON returned from TikTokUserInfoResponse for account '{accountId}'.", ex, PlatformErrorType.InternalServerError);
                }
            });

            return _dataMapper.MapToSocialMediaAccount(tikTokUserInfoResponse);
        }

        public override async Task<List<SocialMediaPost>> GetPostsStatisticsAsync(string accountId, int? topRecentPosts = null)
        {
            _logger.Info($"Starting GetPostsStatisticsAsync for '{accountId}' (pageSize={topRecentPosts})");

            // Fetch all list-side items (paginated)
            var listItems = await FetchAllVideoListItemsAsync(accountId, topRecentPosts.GetValueOrDefault());
            _logger.Info($"Fetched {listItems.Count} videos from list endpoint for '{accountId}'");

            // Fetch metrics in batches
            var metrics = await FetchAllVideoMetricsAsync(accountId, listItems.Select(v => v.Id));
            _logger.Info($"Fetched metrics for {metrics.Count} videos for '{accountId}'");

            // Merge into a unified model
            var aggregatedVideos = MergeVideoListAndMetrics(listItems, metrics);
            _logger.Info($"Merged into {aggregatedVideos.Count} aggregated video records for '{accountId}'");

            _logger.Info($"Mapping aggregated videos to SocialMediaPost objects for '{accountId}'");
            return aggregatedVideos.Select(_dataMapper.MapToSocialMediaPost).ToList();
        }

        private async Task<List<TikTokVideoListItem>> FetchAllVideoListItemsAsync(string accountId, int pageSize)
        {
            _logger.Debug($"Starting pagination (pageSize={pageSize})");
            var results = new List<TikTokVideoListItem>();
            long cursor = 0;
            TikTokVideoListResponse response;

            do
            {
                _logger.Debug($"Requesting page with cursor={cursor}");
                response = await ExecuteApiCallAsync<TikTokVideoListResponse>(accountId, async obj =>
                {
                    var http = (HttpClient)obj;
                    var url = $"{BaseApiUrl}/video/list/?fields={VideoListFields}";
                    _logger.Debug($"POST {url} (cursor={cursor}, max_count={pageSize})");

                    var payload = new Dictionary<string, object> { ["max_count"] = pageSize };
                    if (cursor != 0) payload["cursor"] = cursor;

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                    var resp = await http.PostAsync(url, content);
                    _logger.Info($"HTTP {(int)resp.StatusCode} for video list page (cursor={cursor})");
                    resp.EnsureSuccessStatusCode();

                    using var sr = new StreamReader(await resp.Content.ReadAsStreamAsync());
                    var json = await sr.ReadToEndAsync().ConfigureAwait(false);

                    try
                    {
                        return JsonConvert.DeserializeObject<TikTokVideoListResponse>(json);
                    }
                    catch (JsonException ex)
                    {
                        _logger.Error($"Failed to deserialize TikTokVideoListResponse JSON (cursor={cursor}): {ex.Message}", ex);
                        throw new PlatformException($"Invalid JSON for video list of account '{accountId}' at cursor {cursor}.", ex, PlatformErrorType.InternalServerError);
                    }
                });

                var pageCount = response.Data.Videos?.Count ?? 0;
                _logger.Info($"FetchAllVideoListItemsAsync: Retrieved {pageCount} videos, hasMore={response.Data.HasMore}, nextCursor={response.Data.Cursor}");
                results.AddRange(response.Data.Videos);
                cursor = response.Data.Cursor;

            } while (response.Data.HasMore && cursor != 0);

            _logger.Debug($"FetchAllVideoListItemsAsync: Completed pagination with total {results.Count} videos");
            return results;
        }

        private async Task<List<TikTokVideoMetrics>> FetchAllVideoMetricsAsync(string accountId, IEnumerable<string> allIds)
        {
            var idList = allIds.ToList();
            _logger.Debug($"FetchAllVideoMetricsAsync: Starting metrics fetch for {idList.Count} IDs");

            const int BatchSize = 50;
            var metrics = new List<TikTokVideoMetrics>();

            for (int i = 0; i < idList.Count; i += BatchSize)
            {
                var batchIndex = (i / BatchSize) + 1;
                var batch = idList.Skip(i).Take(BatchSize).ToList();
                _logger.Debug($"FetchAllVideoMetricsAsync: Fetching batch {batchIndex} with {batch.Count} IDs");

                var response = await ExecuteApiCallAsync<TikTokVideoQueryResponse>(accountId, async obj =>
                {
                    var http = (HttpClient)obj;
                    var url = $"{BaseApiUrl}/video/query/?fields={VideoQueryFields}";
                    _logger.Debug($"POST {url} (ids batch {batchIndex})");

                    var payload = new Dictionary<string, object>
                    {
                        ["filters"] = new Dictionary<string, object> { ["video_ids"] = batch }
                    };

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                    var resp = await http.PostAsync(url, content);
                    _logger.Info($"HTTP {(int)resp.StatusCode} for video query batch {batchIndex}");
                    resp.EnsureSuccessStatusCode();

                    using var sr = new StreamReader(await resp.Content.ReadAsStreamAsync());
                    var json = await sr.ReadToEndAsync().ConfigureAwait(false);

                    try
                    {
                        return JsonConvert.DeserializeObject<TikTokVideoQueryResponse>(json);
                    }
                    catch (JsonException ex)
                    {
                        _logger.Error($"Failed to deserialize TikTokVideoQueryResponse JSON (batch {batchIndex}): {ex.Message}", ex);
                        throw new PlatformException(
                            $"Invalid JSON for video query of account '{accountId}' in batch {batchIndex}.",
                            ex,
                            PlatformErrorType.InternalServerError
                        );
                    }
                });

                var metricCount = response.Data.Videos?.Count ?? 0;
                _logger.Info($"FetchAllVideoMetricsAsync: Retrieved {metricCount} metrics for batch {batchIndex}");
                metrics.AddRange(response.Data.Videos);
            }

            _logger.Debug($"FetchAllVideoMetricsAsync: Completed metrics fetch with total {metrics.Count} metrics");
            return metrics;
        }

        private List<TikTokVideoAggregate> MergeVideoListAndMetrics(
            IEnumerable<TikTokVideoListItem> listItems,
            IEnumerable<TikTokVideoMetrics> metrics)
        {
            var listCount = listItems.Count();
            var metricsCount = metrics.Count();
            _logger.Debug($"MergeListAndMetrics: Merging {listCount} list items with {metricsCount} metrics");

            var metricsById = metrics.ToDictionary(m => m.Id, m => m);
            var aggregatedVideo = listItems.Select(item =>
            {
                metricsById.TryGetValue(item.Id, out var met);
                return new TikTokVideoAggregate
                {
                    Id = item.Id,
                    Title = item.Title,
                    VideoDescription = item.VideoDescription,
                    CreateTime = item.CreateTime,
                    Duration = item.Duration,
                    CoverImageUrl = item.CoverImageUrl,
                    EmbedLink = item.EmbedLink,
                    LikeCount = met?.LikeCount ?? 0,
                    CommentCount = met?.CommentCount ?? 0,
                    ShareCount = met?.ShareCount ?? 0,
                    ViewCount = met?.ViewCount ?? 0
                };
            }).ToList();

            _logger.Info($"MergeVideoListAndMetrics: Produced {aggregatedVideo.Count} aggregated records");
            return aggregatedVideo;
        }

        protected override object CreateApiClient(TikTokCredentials credentials)
        {
            _logger.Info($"TikTokClient: Creating HttpClient with Bearer token.");
            var handler = new AuthenticatedHttpClientHandler(credentials.AccessToken);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseApiUrl)
            };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }

        protected override bool IsUnauthorizedException(Exception ex)
        {
            if (ex is HttpRequestException httpEx && httpEx.StatusCode == HttpStatusCode.Unauthorized)
                return true;
            return base.IsUnauthorizedException(ex);
        }
    

        private class AuthenticatedHttpClientHandler : DelegatingHandler
        {
            private readonly string _accessToken;
            public AuthenticatedHttpClientHandler(string accessToken)
            {
                _accessToken = accessToken;
                InnerHandler = new HttpClientHandler();
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

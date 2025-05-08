using Newtonsoft.Json;
using System.Collections.Generic;

namespace POC_PlatformEngagementPoller.DataModels.DataTranferObjects
{
    /// <summary>
    /// Response wrapper for the TikTok /v2/video/query/ endpoint.
    /// </summary>
    public class TikTokVideoQueryResponse
    {
        [JsonProperty("data")]
        public TikTokVideoQueryData Data { get; set; }

        [JsonProperty("error")]
        public TikTokErrorInfo Error { get; set; }
    }

    public class TikTokVideoQueryData
    {
        /// <summary>
        /// Collection of video metrics in this batch.
        /// </summary>
        [JsonProperty("videos")]
        public List<TikTokVideoMetrics> Videos { get; set; }

        /// <summary>
        /// Cursor for pagination; 0 if no further pages.
        /// </summary>
        [JsonProperty("cursor")]
        public long Cursor { get; set; }

        /// <summary>
        /// Indicates if more pages are available.
        /// </summary>
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
    }

    public class TikTokVideoMetrics
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("like_count")]
        public ulong LikeCount { get; set; }

        [JsonProperty("comment_count")]
        public ulong CommentCount { get; set; }

        [JsonProperty("share_count")]
        public ulong ShareCount { get; set; }

        [JsonProperty("view_count")]
        public ulong ViewCount { get; set; }
    }
}

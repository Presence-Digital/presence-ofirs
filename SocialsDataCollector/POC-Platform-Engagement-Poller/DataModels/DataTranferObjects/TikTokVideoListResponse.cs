using Newtonsoft.Json;
using System.Collections.Generic;

namespace POC_PlatformEngagementPoller.DataModels.DataTranferObjects
{
    /// <summary>
    /// Response wrapper for the TikTok /v2/video/list/ endpoint.
    /// </summary>
    public class TikTokVideoListResponse
    {
        [JsonProperty("data")]
        public TikTokVideoListData Data { get; set; }

        [JsonProperty("error")]
        public TikTokErrorInfo Error { get; set; }
    }

    public class TikTokVideoListData
    {
        /// <summary>
        /// List of videos in this page.
        /// </summary>
        [JsonProperty("videos")]
        public List<TikTokVideoListItem> Videos { get; set; }

        /// <summary>
        /// Cursor for the next page; null or zero if no further pages.
        /// </summary>
        [JsonProperty("cursor")]
        public long Cursor { get; set; }

        /// <summary>
        /// Indicates if there are more pages of results.
        /// </summary>
        [JsonProperty("has_more")]
        public bool HasMore { get; set; }
    }

    public class TikTokVideoListItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("video_description")]
        public string VideoDescription { get; set; }

        [JsonProperty("create_time")]
        public string CreateTime { get; set; }

        /// <summary>
        /// Duration in seconds.
        /// </summary>
        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("cover_image_url")]
        public string CoverImageUrl { get; set; }

        [JsonProperty("embed_link")]
        public string EmbedLink { get; set; }
    }
}

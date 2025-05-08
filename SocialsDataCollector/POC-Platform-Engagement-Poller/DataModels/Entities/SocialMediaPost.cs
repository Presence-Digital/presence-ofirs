using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using POC_PlatformEngagementPoller.PlatformClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POC_PlatformEngagementPoller.DataModels.Entities
{
    /// <summary>
    /// Represents a unified video/post entity across platforms such as YouTube and TikTok.
    /// Designed to hold shared fields across platforms and extensible platform-specific data.
    /// </summary>
    public class SocialMediaPost
    {
        /// <summary>
        /// The unique ID of the video/post on the platform.
        /// </summary>
        public string PostId { get; set; }

        /// <summary>
        /// The platform this post belongs to.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public PlatformType Platform { get; set; }

        /// <summary>
        /// The type of post (YouTube Shorts, Youtube Video, TikTok Video, etc.).
        /// </summary>
        //[JsonConverter(typeof(StringEnumConverter))]
        //public PostType? PostType { get; set; }

        /// <summary>
        /// Metadata about the post including title, description, publish date, and thumbnail.
        /// </summary>
        public PostProfile Profile { get; set; }

        /// <summary>
        /// Engagement statistics such as views, likes, comments, and shares.
        /// </summary>
        public PostStatistics Statistics { get; set; }

        /// <summary>
        /// Additional platform-specific data that does not map to common fields.
        /// Example: YouTube license, TikTok music ID, or raw API responses.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();

        /// <summary>
        /// The post ETag (entity tag) for cache validation.
        /// </summary>
        public string ETag { get; set; }
    }

    /// <summary>
    /// Contains platform-agnostic metadata about a post.
    /// </summary>
    public class PostProfile
    {
        /// <summary>
        /// The title of the post.
        /// YouTube: Video.Snippet.Title
        /// TikTok: Video.title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The full description or caption of the post.
        /// YouTube: Snippet.Description
        /// TikTok: video_description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// URL to the thumbnail/cover image of the video.
        /// YouTube: Thumbnails.High.Url
        /// TikTok: cover_image_url
        /// </summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// The date and time the post was published (only available on YouTube).
        /// Example: 2025-04-09T01:00:40Z
        /// </summary>
        public DateTimeOffset? PublishedAt { get; set; }
    }

    /// <summary>
    /// Contains statistics for a post such as views, likes, and engagement.
    /// </summary>
    public class PostStatistics
    {
        /// <summary>
        /// Number of views the post has received.
        /// </summary>
        public ulong? ViewCount { get; set; }

        /// <summary>
        /// Number of likes the post has received.
        /// </summary>
        public ulong? LikeCount { get; set; }

        /// <summary>
        /// Number of comments on the post.
        /// </summary>
        public ulong? CommentCount { get; set; }

        /// <summary>
        /// Number of times the post has been shared.
        /// TikTok only.
        /// </summary>
        public ulong? ShareCount { get; set; }
    }
}

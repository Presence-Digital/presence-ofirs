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
    /// Represents a unified social media account across platforms like YouTube, TikTok etc.
    /// </summary>
    public class SocialMediaAccount
    {
        /// <summary>
        /// The unique platform-specific account ID (e.g., channel ID, open_id).
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// The platform the account belongs to (YouTube, TikTok, etc.).
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public PlatformType Platform { get; set; }

        /// <summary>
        /// Basic profile data (name, description, avatar, etc.).
        /// </summary>
        public AccountProfile Profile { get; set; } = new();

        /// <summary>
        /// Key public statistics for the account (followers, video count, etc.).
        /// </summary>
        public AccountStatistics Statistics { get; set; } = new();

        /// <summary>
        /// Additional platform-specific fields that do not fit into shared structure.
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();

        /// <summary>
        /// The account ETag (entity tag) for cache validation.
        /// </summary>
        public string ETag { get; set; }
    }

    /// <summary>
    /// Contains platform-agnostic account profile details for a social media account.
    /// Maps common fields such as title, avatar, and profile links.
    /// </summary>
    public class AccountProfile
    {
        /// <summary>
        /// Display name or account title.
        /// YouTube: Snippet.Title
        /// TikTok: display_name
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Description or bio.
        /// YouTube: Snippet.Description
        /// TikTok: bio_description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Vanity URL or public-facing link.
        /// YouTube: Snippet.CustomUrl
        /// TikTok: profile_deep_link
        /// </summary>
        public string CustomUrl { get; set; }

        /// <summary>
        /// URL to the account’s avatar or profile image.
        /// YouTube: Thumbnails.High.Url  
        /// TikTok: avatar_large_url
        /// </summary>
        public string ThumbnailUrl { get; set; }
    }

    /// <summary>
    /// Contains platform-agnostic account aggregated statistics for a social media account.
    /// Handles differences in metric availability between platforms.
    /// </summary>
    public class AccountStatistics
    {

        /// <summary>
        /// Number of subscribers or followers.
        /// YouTube: Statistics.SubscriberCount  
        /// TikTok: follower_count
        /// </summary>
        public ulong? SubscriberCount { get; set; }

        /// <summary>
        /// Number of videos/posts uploaded by the account.
        /// YouTube: Statistics.VideoCount  
        /// TikTok: video_count
        /// </summary>
        public ulong? VideoCount { get; set; }
    }
}

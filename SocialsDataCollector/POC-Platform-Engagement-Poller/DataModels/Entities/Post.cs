//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
//using System;
//using System.Collections.Generic;

//namespace POC_PlatformEngagementPoller.DataModels
//{
//    /// <summary>
//    /// Represents a unified post containing video details gathered from YouTube.
//    /// </summary>
//    public class Post
//    {
//        /// <summary>
//        /// Gets or sets the unique identifier for the video.
//        /// </summary>
//        public string PlatformId { get; set; }

//        /// <summary>
//        /// Contains basic snippet data from the video.
//        /// </summary>
//        public PostProfile PostProfile { get; set; }

//        /// <summary>
//        /// Contains statistical data for the video (views, likes, dislikes, favorites, comments).
//        /// </summary>
//        public PostStatistics PostStatistics { get; set; }

//        /// <summary>
//        /// Contains content-related details (duration, dimensions, definition, etc.).
//        /// </summary>
//        public ContentDetails ContentDetails { get; set; }

//        /// <summary>
//        /// Contains video status information such as privacy status, license, embeddability, and upload status.
//        /// </summary>
//        public PostStatusInfo PostStatusInfo { get; set; }

//        /// <summary>
//        /// Contains player information for embedding the video.
//        /// </summary>
//        public PlayerInfo Player { get; set; }

//        /// <summary>
//        /// Contains topic details used for categorizing the video.
//        /// Example: A video on "Space Exploration" might include topic IDs related to science and astronomy.
//        /// </summary>
//        public TopicDetails TopicDetails { get; set; }
//    }

//    /// <summary>
//    /// Contains snippet data from the video.
//    /// </summary>
//    public class PostProfile1
//    {
//        /// <summary>
//        /// Gets or sets the publication date and time.
//        /// Real-life example: "2021-07-14T10:30:00Z" indicates when the video was published.
//        /// </summary>
//        public DateTimeOffset PublishedAt { get; set; }

//        /// <summary>
//        /// Gets or sets the video title.
//        /// Real-life example: "How to Bake a Perfect Cake".
//        /// </summary>
//        public string Title { get; set; }

//        /// <summary>
//        /// Gets or sets the video description.
//        /// Real-life example: "This video provides a step-by-step guide to baking a cake."
//        /// </summary>
//        public string Description { get; set; }

//        /// <summary>
//        /// Gets or sets the URL for the video's thumbnail.
//        /// The logic to choose the best thumbnail is: prefer "standard", then "high", then "medium".
//        /// Real-life example: "https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg"
//        /// </summary>
//        public string ThumbnailUrl { get; set; }

//        /// <summary>
//        /// Gets or sets the list of tags associated with the video.
//        /// Real-life example: ["cooking", "baking", "tutorial"]
//        /// </summary>
//        public IEnumerable<string> Tags { get; set; }

//        /// <summary>
//        /// Gets or sets the category ID of the video.
//        /// Real-life example: "22" (commonly used for "People & Blogs")
//        /// </summary>
//        public string CategoryId { get; set; }

//        /// <summary>
//        /// Localized information for the account.
//        /// </summary>
//        public PostLocalizedInfo LocalizedInfo { get; set; }

//        /// <summary>
//        /// Gets or sets the ETag for caching and concurrency control.
//        /// Example: "XI7nbFXulYBIpL0ayR_gDh3eu1k"
//        /// </summary>
//        public string ETag { get; set; }
//    }

//    /// <summary>
//    /// Represents the localized title and description of a video, if available.
//    /// This helps provide region-specific translations for different audiences.
//    /// </summary>
//    public class PostLocalizedInfo
//    {
//        /// <summary>
//        /// Gets or sets the localized title if available.
//        /// Real-life example: "Cómo Hornear un Pastel Perfecto" for Spanish.
//        /// </summary>
//        public string LocalizedTitle { get; set; }

//        /// <summary>
//        /// Gets or sets the localized description if available.
//        /// Real-life example: "Este video explica cómo hornear un pastel de manera detallada."
//        /// </summary>
//        public string LocalizedDescription { get; set; }
//    }
//}

///// <summary>
///// Contains statistical information about the video.
///// </summary>
//public class PostStatistics1
//{
//    /// <summary>
//    /// Gets or sets the view count.
//    /// Real-life example: A popular video may have 1,000,000 views.
//    /// </summary>
//    public ulong? ViewCount { get; set; }

//    /// <summary>
//    /// Gets or sets the like count.
//    /// Real-life example: A trending video might have 50,000 likes.
//    /// </summary>
//    public ulong? LikeCount { get; set; }

//    /// <summary>
//    /// Gets or sets the dislike count.
//    /// Real-life example: Controversial videos might have a notable number of dislikes.
//    /// </summary>
//    public ulong? DislikeCount { get; set; }

//    /// <summary>
//    /// Gets or sets the comment count.
//    /// Real-life example: A viral video might have thousands of comments.
//    /// </summary>
//    public ulong? CommentCount { get; set; }
//}

///// <summary>
///// Contains details about the video's content.
///// </summary>
//public class ContentDetails
//{
//    /// <summary>
//    /// Gets or sets the duration in ISO 8601 format.
//    /// Real-life example: "PT5M33S" means the video is 5 minutes and 33 seconds long.
//    /// </summary>
//    public TimeSpan Duration { get; set; }

//    /// <summary>
//    /// Gets or sets the dimension of the video.
//    /// Possible values: "2d" or "3d".
//    /// </summary>
//    public string Dimension { get; set; }

//    /// <summary>
//    /// Gets or sets the definition of the video.
//    /// Possible values: "hd" (high definition) or "sd" (standard definition).
//    /// </summary>
//    public string Definition { get; set; }

//    /// <summary>
//    /// Gets or sets the caption status.
//    /// Typically "true" if captions are available; "false" otherwise.
//    /// </summary>
//    public string Caption { get; set; }

//    /// <summary>
//    /// Gets or sets a value indicating whether the video is licensed content.
//    /// Real-life example: true indicates that the video is under a licensing agreement.
//    /// </summary>
//    public bool IsLicensedContent { get; set; }

//    /// <summary>
//    /// Gets or sets the region restriction details.
//    /// Example: Allowed countries ["US", "CA"] and blocked countries ["CN", "RU"].
//    /// </summary>
//    public RegionRestriction RegionRestriction { get; set; }
//}

///// <summary>
///// Represents region restriction information.
///// </summary>
//public class RegionRestriction
//{
//    /// <summary>
//    /// Gets or sets the list of country codes where the video is allowed.
//    /// Real-life example: ["US", "CA"]
//    /// </summary>
//    public IEnumerable<string> Allowed { get; set; }

//    /// <summary>
//    /// Gets or sets the list of country codes where the video is blocked.
//    /// Real-life example: ["CN", "RU"]
//    /// </summary>
//    public IEnumerable<string> Blocked { get; set; }
//}

///// <summary>
///// Contains status information about the video.
///// </summary>
//public class PostStatusInfo
//{
//    /// <summary>
//    /// Gets or sets the privacy status.
//    /// Possible values: "public" (viewable by anyone), "private" (only accessible to the owner and designated users), "unlisted" (accessible via direct link).
//    /// Real-life example: "public" means the video is available to all users.
//    /// </summary>
//    public string PrivacyStatus { get; set; }

//    /// <summary>
//    /// Gets or sets the license type.
//    /// Possible values: "youtube" (default) or "creativeCommon".
//    /// Real-life example: "youtube" indicates standard YouTube licensing.
//    /// </summary>
//    public string License { get; set; }

//    /// <summary>
//    /// Gets or sets a value indicating whether the video can be embedded on other sites.
//    /// Real-life example: If true, websites (like blogs or news articles) can display the video using its embed code.
//    /// </summary>
//    public bool IsEmbeddable { get; set; }

//    /// <summary>
//    /// Gets or sets the upload status.
//    /// Possible values: "uploaded" (file sent but not yet processed), "processed" (ready for viewing), "failed", "rejected", "deleted".
//    /// Real-life example: "processed" means the video is fully processed and available for playback.
//    /// </summary>
//    public string UploadStatus { get; set; }
//}

///// <summary>
///// Contains player information for embedding the video.
///// </summary>
//public class PlayerInfo
//{
//    /// <summary>
//    /// Gets or sets the HTML snippet used to embed the video.
//    /// Real-life example:
//    /// &lt;iframe width="560" height="315" src="https://www.youtube.com/embed/dQw4w9WgXcQ" frameborder="0" allowfullscreen&gt;&lt;/iframe&gt;
//    /// </summary>
//    public string EmbedHtml { get; set; }

//    // Additional player properties can be added here if needed.
//}

///// <summary>
///// Contains topic details for classifying the video's subject matter.
///// Real-life example: A video on "Space Exploration" may include topic IDs related to astronomy and science,
///// and topic categories as URLs (e.g., "https://en.wikipedia.org/wiki/Science").
///// </summary>
//public class TopicDetails
//{
//    /// <summary>
//    /// Gets or sets the list of topic IDs associated with the video.
//    /// </summary>
//    public IEnumerable<string> TopicIds { get; set; }

//    /// <summary>
//    /// Gets or sets the list of relevant topic IDs.
//    /// </summary>
//    public IEnumerable<string> RelevantTopicIds { get; set; }

//    /// <summary>
//    /// Gets or sets the list of topic categories, usually represented as URLs.
//    /// Real-life example: "https://en.wikipedia.org/wiki/Science"
//    /// </summary>
//    public IEnumerable<string> TopicCategories { get; set; }
//}

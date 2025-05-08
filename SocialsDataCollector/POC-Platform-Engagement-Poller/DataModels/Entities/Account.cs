//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
//using POC_PlatformEngagementPoller.PlatformClients;
//using System;
//using System.Collections.Generic;

//namespace POC_PlatformEngagementPoller.DataModels
//{
//    /// <summary>
//    /// Represents comprehensive data for an account across various social media platforms.
//    /// This class serves as the main container for all account-related data.
//    /// </summary>
//    public class Account
//    {
//        /// <summary>
//        /// Gets or sets the unique identifier of the account as set by the platform.
//        /// </summary>
//        public string PlatformId { get; set; }

//        /// <summary>
//        /// Gets or sets the platform type of this account (YouTube, TikTok, etc.)
//        /// </summary>
//        [JsonConverter(typeof(StringEnumConverter))]
//        public PlatformType Platform { get; set; }

//        /// <summary>
//        /// Gets or sets the account profile information containing basic details.
//        /// </summary>
//        public AccountProfile AccountProfile { get; set; }

//        /// <summary>
//        /// Gets or sets the account statistics containing engagement metrics.
//        /// </summary>
//        public AccountStatistics AccountStatistics { get; set; }

//        /// <summary>
//        /// Gets or sets the branding settings information for the account.
//        /// </summary>
//        public BrandingSettingsInfo BrandingSettings { get; set; }

//        /// <summary>
//        /// Gets or sets the account status information for the account.
//        /// </summary>
//        public AccountStatus AccountStatus { get; set; }

//        /// <summary>
//        /// Gets or sets a list of Wikipedia URLs that describe the channel's content topics.
//        /// These URLs represent standardized topics associated with the channel.
//        /// Example: A technology channel might have URLs linking to "Technology" and "Software Development" Wikipedia pages.
//        /// </summary>
//        public List<string> TopicCategories { get; set; }
//    }

//    /// <summary>
//    /// Contains essential profile details of an account.
//    /// </summary>
//    public class AccountProfile1
//    {
//        /// <summary>
//        /// Gets or sets the account's display name or title.
//        /// </summary>
//        public string Title { get; set; }

//        /// <summary>
//        /// Gets or sets the description or bio of the account.
//        /// </summary>
//        public string Description { get; set; }

//        /// <summary>
//        /// Gets or sets the creation date and time of the account.
//        /// </summary>
//        public DateTimeOffset PublishedAt { get; set; }

//        /// <summary>
//        /// Gets or sets the custom URL or vanity URL of the account.
//        /// </summary>
//        public string CustomUrl { get; set; }

//        /// <summary>
//        /// Gets or sets the URL to the account's thumbnail or profile image.
//        /// </summary>
//        public string ThumbnailUrl { get; set; }

//        /// <summary>
//        /// Gets or sets the URL to the account's banner or cover image.
//        /// </summary>
//        public string BannerUrl { get; set; }

//        /// <summary>
//        /// Localized information for the account.
//        /// </summary>
//        public AccountLocalizedInfo LocalizedInfo { get; set; }

//        /// <summary>
//        /// Gets or sets the country associated with the account.
//        /// </summary>
//        public string Country { get; set; }

//        /// <summary>
//        /// Gets or sets the language of the account's content.
//        /// </summary>
//        public string Language { get; set; }

//        /// <summary>
//        /// Gets or sets the accounts ETag (A unique identifier representing the current state of the resource. When the resource's data changes, its ETag value also changes.).
//        /// </summary>
//        public string ETag { get; set; }
//    }

//    /// <summary>
//    /// Contains channel localized title and description information.
//    /// </summary>
//    public class AccountLocalizedInfo
//    {
//        /// <summary>
//        /// Gets or sets the localized title.
//        /// </summary>
//        public string LocalizedTitle { get; set; }

//        /// <summary>
//        /// Gets or sets the localized description.
//        /// </summary>
//        public string LocalizedDescription { get; set; }
//    }

//    /// <summary>
//    /// Contains statistical information about an account.
//    /// </summary>
//    public class AccountStatistics1
//    {
//        /// <summary>
//        /// Gets or sets the total view count for the account.
//        /// </summary>
//        public ulong? ViewCount { get; set; }

//        /// <summary>
//        /// Gets or sets the total subscriber/follower count.
//        /// </summary>
//        public ulong? SubscriberCount { get; set; }

//        /// <summary>
//        /// Gets or sets whether the subscriber count is hidden.
//        /// </summary>
//        public bool? IsSubscriberCountHidden { get; set; }

//        /// <summary>
//        /// Gets or sets the number of posts/videos created by the account.
//        /// </summary>
//        public ulong? VideoCount { get; set; }

//    }

//    /// <summary>
//    /// Contains branding settings information for the YouTube channel.
//    /// </summary>
//    public class BrandingSettingsInfo
//    {
//        /// <summary>
//        /// Gets or sets the keywords or tags associated with the channel.
//        /// Used for search optimization.
//        /// </summary>
//        public string ChannelKeywords { get; set; }

//        /// <summary>
//        /// Gets or sets whether comments require approval before being public.
//        /// If true, comments are moderated by the channel owner before appearing.
//        /// </summary>
//        public bool IsModerateCommentsEnabled { get; set; }

//        /// <summary>
//        /// Gets or sets the featured channels or related accounts.
//        /// A list of channel URLs that are promoted on this channel.
//        /// Example: A beauty vlogger featuring other beauty influencers.
//        /// </summary>
//        public List<string> FeaturedChannels { get; set; }

//        /// <summary>
//        /// Gets or sets the title of the featured channels section.
//        /// Example: "Recommended Channels" or "My Favorite Creators".
//        /// </summary>
//        public string FeaturedChannelsTitle { get; set; }

//        /// <summary>
//        /// Gets or sets the URL to the banner image.
//        /// This is the image displayed at the top of the channel page.
//        /// </summary>
//        public string BannerImageUrl { get; set; }

//        /// <summary>
//        /// Gets or sets the unsubscribed trailer video ID.
//        /// This is the video that plays for users who haven't subscribed yet.
//        /// Example: A welcome video for a new audience.
//        /// </summary>
//        public string UnsubscribedTrailer { get; set; }

//        /// <summary>
//        /// Gets or sets the default tab users see when they visit the channel.
//        /// Example values: "Home", "Videos", "Playlists", "Community".
//        /// Example: A news channel might set this to "Home" to highlight curated playlists.
//        /// </summary>
//        public string DefaultTab { get; set; }

//        /// <summary>
//        /// Gets or sets whether the "Browse" view is enabled.
//        /// If true, the channel can customize the homepage layout.
//        /// Example: Enabling this allows a gaming channel to have sections like "Top Streams" or "Best Clips".
//        /// </summary>
//        public bool ShowBrowseView { get; set; }

//        /// <summary>
//        /// Gets or sets whether related channels should be displayed on the channel page.
//        /// If false, the channel will not show recommended channels on the sidebar.
//        /// Example: A brand may disable this to prevent competitors from appearing on their channel.
//        /// </summary>
//        public bool ShowRelatedChannels { get; set; }

//        /// <summary>
//        /// Gets or sets the custom branding color of the channel.
//        /// Example: A tech brand might set it to "#4285F4" (Google Blue).
//        /// </summary>
//        public string ProfileColor { get; set; }
//    }

//    /// <summary>
//    /// Represents the account status information of a YouTube channel.
//    /// </summary>
//    public class AccountStatus
//    {
//        /// <summary>
//        /// Gets or sets the privacy status of the channel.
//        /// Possible values: "public", "unlisted", "private".
//        /// </summary>
//        public string PrivacyStatus { get; set; }

//        /// <summary>
//        /// Gets or sets a value indicating whether the channel is linked to a YouTube account.
//        /// </summary>
//        public bool IsLinked { get; set; }

//        /// <summary>
//        /// Gets or sets the channel's ability to upload videos longer than 15 minutes.
//        /// Possible values: "eligible", "ineligible".
//        /// </summary>
//        public string LongUploadsStatus { get; set; }

//        /// <summary>
//        /// Gets or sets a value indicating whether the channel is marked as "Made for Kids" under COPPA regulations.
//        /// </summary>
//        public bool IsMadeForKids { get; set; }

//        /// <summary>
//        /// Gets or sets a value indicating whether the channel owner has self-declared the channel as "Made for Kids".
//        /// </summary>
//        public bool IsSelfDeclaredMadeForKids { get; set; }
//    }

//    /// <summary>
//    /// Contains content-related details for the account.
//    /// </summary>
//    public class AccountContentDetails
//    {
//        /// <summary>
//        /// Gets or sets the uploads playlist ID (YouTube-specific).
//        /// </summary>
//        public string UploadsPlaylistId { get; set; }

//        /// <summary>
//        /// Gets or sets the favorites playlist ID (YouTube-specific).
//        /// </summary>
//        public string FavoritesPlaylistId { get; set; }

//        /// <summary>
//        /// Gets or sets the likes playlist ID (YouTube-specific).
//        /// </summary>
//        public string LikesPlaylistId { get; set; }

//        /// <summary>
//        /// Gets or sets the playlists owned by the account.
//        /// </summary>
//        public List<string> Playlists { get; set; }

//        /// <summary>
//        /// Gets or sets related app links.
//        /// </summary>
//        public Dictionary<string, string> RelatedApps { get; set; }
//    }

//    /// <summary>
//    /// Contains monetization details for the account.
//    /// </summary>
//    public class MonetizationInfo
//    {
//        /// <summary>
//        /// Gets or sets whether the account is monetized.
//        /// </summary>
//        public bool IsMonetized { get; set; }

//        /// <summary>
//        /// Gets or sets the monetization types enabled for the account.
//        /// </summary>
//        public List<string> MonetizationTypes { get; set; }

//        /// <summary>
//        /// Gets or sets the estimated revenue for the account.
//        /// </summary>
//        public double EstimatedRevenue { get; set; }

//        /// <summary>
//        /// Gets or sets the revenue by source.
//        /// </summary>
//        public Dictionary<string, double> RevenueBySource { get; set; }

//        /// <summary>
//        /// Gets or sets the monetization eligibility status.
//        /// </summary>
//        public string EligibilityStatus { get; set; }
//    }

//    /// <summary>
//    /// Contains privacy and authentication settings.
//    /// </summary>
//    public class PrivacySettings
//    {
//        /// <summary>
//        /// Gets or sets whether the account is private.
//        /// </summary>
//        public bool IsPrivate { get; set; }

//        /// <summary>
//        /// Gets or sets whether comments are enabled.
//        /// </summary>
//        public bool CommentsEnabled { get; set; }

//        /// <summary>
//        /// Gets or sets the default video privacy setting.
//        /// </summary>
//        public string DefaultVideoPrivacy { get; set; }

//        /// <summary>
//        /// Gets or sets whether the account shows related content.
//        /// </summary>
//        public bool ShowRelatedContent { get; set; }
//    }

//    /// <summary>
//    /// Contains information about related accounts.
//    /// </summary>
//    public class RelatedAccount
//    {
//        /// <summary>
//        /// Gets or sets the ID of the related account.
//        /// </summary>
//        public string Id { get; set; }

//        /// <summary>
//        /// Gets or sets the name of the related account.
//        /// </summary>
//        public string Name { get; set; }

//        /// <summary>
//        /// Gets or sets the relationship type.
//        /// </summary>
//        public string RelationshipType { get; set; }

//        /// <summary>
//        /// Gets or sets the URL to the related account.
//        /// </summary>
//        public string Url { get; set; }
//    }
//}
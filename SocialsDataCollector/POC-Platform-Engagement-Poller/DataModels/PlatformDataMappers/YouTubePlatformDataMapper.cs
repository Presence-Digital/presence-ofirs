using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using POC_PlatformEngagementPoller.DataModels.Entities;
using POC_PlatformEngagementPoller.PlatformClients;
using Channel = Google.Apis.YouTube.v3.Data.Channel;

namespace POC_PlatformEngagementPoller.DataMappers
{
    /// <summary>
    /// Maps YouTube-specific data structures to platform-agnostic unified data models.
    /// Provides methods to convert YouTube API objects (Channel, Video, etc.) to our 
    /// standardized Account and Post models.
    /// </summary>
    public class YouTubePlatformDataMapper
    {
        public SocialMediaAccount MapChannelToSocialMediaAccount(Channel channel)
        {
            if (channel is null)
                throw new ArgumentNullException(nameof(channel));

            var profile = new AccountProfile
            {
                Title = channel.Snippet?.Title ?? string.Empty,
                Description = channel.Snippet?.Description ?? string.Empty,
                CustomUrl = channel.Snippet?.CustomUrl ?? string.Empty,
                ThumbnailUrl = channel.Snippet?.Thumbnails?.High?.Url
                            ?? channel.Snippet?.Thumbnails?.Default__?.Url
                            ?? string.Empty
            };

            var stats = new AccountStatistics
            {
                SubscriberCount = channel.Statistics?.SubscriberCount,
                VideoCount = channel.Statistics?.VideoCount,
            };

            var addtionalProps = new Dictionary<string, object>
            {
                ["AdditionalStatistics"] = new Dictionary<string, object>
                {
                    ["ViewCount"] = channel.Statistics?.ViewCount,
                    ["HiddenSubscriberCount"] = channel.Statistics?.HiddenSubscriberCount
                },
                ["Country"] = channel.Snippet?.Country,
                ["Language"] = channel.Snippet?.DefaultLanguage,
                ["PublishedAt"] = DateTimeOffset.TryParse(channel.Snippet?.PublishedAtRaw, out var pubAt) ? pubAt : null,
                ["LocalizedInfo"] = channel.Snippet?.Localized == null ? null : new Dictionary<string, string>
                {
                    ["LocalizedTitle"] = channel.Snippet.Localized.Title,
                    ["LocalizedDescription"] = channel.Snippet.Localized.Description
                },
                ["AccountStatus"] = new Dictionary<string, object>
                {
                    ["IsChannelMonetizationEnabled"] = channel.Status?.IsChannelMonetizationEnabled,
                    ["IsLinked"] = channel.Status?.IsLinked,
                    ["LongUploadsStatus"] = channel.Status?.LongUploadsStatus,
                    ["MadeForKids"] = channel.Status?.MadeForKids,
                    ["PrivacyStatus"] = channel.Status?.PrivacyStatus,
                    ["SelfDeclaredMadeForKids"] = channel.Status?.SelfDeclaredMadeForKids
                },
                ["BrandingSettings"] = new Dictionary<string, object>
                {
                    ["Keywords"] = channel.BrandingSettings?.Channel?.Keywords,
                    ["UnsubscribedTrailer"] = channel.BrandingSettings?.Channel?.UnsubscribedTrailer,
                    ["BannerExternalUrl"] = channel.BrandingSettings?.Image?.BannerExternalUrl
                },
                ["TopicCategories"] = channel.TopicDetails?.TopicCategories ?? new List<string>(),
            };

            return new SocialMediaAccount
            {
                AccountId = channel.Id,
                Platform = PlatformType.YouTube,   
                Profile = profile,
                Statistics = stats,
                AdditionalProperties = new Dictionary<string, object>
                {
                    ["AdditionalProperties"] = addtionalProps
                },
                ETag = channel.ETag
            };
        }

        public List<SocialMediaPost> MapVideosToSocialMediaPosts(IEnumerable<Video> videos)
        {
            if (videos is null)
                throw new ArgumentNullException(nameof(videos));

            return videos.Where(v => v != null).Select(MapVideoToSocialMediaPost).ToList();
        }

        private SocialMediaPost MapVideoToSocialMediaPost(Video video)
        {
            var snippet = video.Snippet;
            var statistics = video.Statistics;
            var details = video.ContentDetails;
            var player = video.Player;

            var profile = new PostProfile
            {
                Title = snippet?.Title ?? string.Empty,
                Description = snippet?.Description ?? string.Empty,
                ThumbnailUrl = snippet?.Thumbnails?.Standard?.Url
                            ?? snippet?.Thumbnails?.High?.Url
                            ?? snippet?.Thumbnails?.Medium?.Url
                            ?? string.Empty,
                PublishedAt = DateTimeOffset.TryParse(snippet?.PublishedAtRaw, out var p) ? p : null
            };

            var postStats = new PostStatistics
            {
                ViewCount = statistics?.ViewCount,
                LikeCount = statistics?.LikeCount,
                CommentCount = statistics?.CommentCount,
                ShareCount = null         // YouTube doesn’t expose shares
            };

            var platformExtras = new Dictionary<string, object>
            {
                ["AdditionalStatistics"] = new Dictionary<string, object>
                {
                    ["DislikeCount"] = statistics?.DislikeCount
                },
                ["MediaDetails"] = new Dictionary<string, object>
                {
                    ["Duration"] = ParseIsoDurationToTimespan(details?.Duration),
                    ["EmbedUrl"] = player?.EmbedHtml
                },
                ["Tags"] = snippet?.Tags,
                ["CategoryId"] = snippet?.CategoryId,
                ["LocalizedInfo"] = snippet?.Localized == null ? null : new Dictionary<string, string>
                {
                    ["LocalizedTitle"] = snippet.Localized.Title,
                    ["LocalizedDescription"] = snippet.Localized.Description
                },
                ["Definition"] = details?.Definition,
                ["Dimension"] = details?.Dimension,
                ["Caption"] = details?.Caption,
                ["IsLicensedContent"] = details?.LicensedContent,
                ["RegionRestriction"] = details?.RegionRestriction,
                ["PrivacyStatus"] = video.Status?.PrivacyStatus,
                ["License"] = video.Status?.License,
                ["IsEmbeddable"] = video.Status?.Embeddable,
                ["UploadStatus"] = video.Status?.UploadStatus,
                ["TopicDetails"] = video.TopicDetails,
            };

            return new SocialMediaPost
            {
                PostId = video.Id,
                Platform = PlatformType.YouTube,
                // PostType = GetYouTubePostType(video), // TODO: find a way to conclude this
                Profile = profile,
                Statistics = postStats,
                AdditionalProperties = new Dictionary<string, object>
                {
                    ["AdditionalProperties"] = platformExtras
                },
                ETag = video.ETag
            };
        }


        /// <summary>
        /// Parses an ISO 8601 duration string to seconds.
        /// </summary>
        /// <param name="isoDuration">The ISO 8601 duration string (e.g., "PT1H2M3S").</param>
        /// <returns>The total duration in seconds.</returns>
        private int ParseIsoDuration(string isoDuration)
        {
            if (string.IsNullOrEmpty(isoDuration))
                return 0;

            // Try to use XmlConvert first, as it's the most reliable
            try
            {
                var timeSpan = XmlConvert.ToTimeSpan(isoDuration);
                return (int)timeSpan.TotalSeconds;
            }
            catch
            {
                // Fall back to regex parsing if XmlConvert fails
                var match = Regex.Match(isoDuration, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
                if (match.Success)
                {
                    int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                    int minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                    int seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

                    return hours * 3600 + minutes * 60 + seconds;
                }
                return 0;
            }
        }

        /// <summary>
        /// Parses an ISO 8601 duration string into a TimeSpan.
        /// </summary>
        /// <param name="isoDuration">The ISO 8601 duration string (e.g., "PT1H2M3S").</param>
        /// <returns>The parsed TimeSpan value.</returns>
        private TimeSpan ParseIsoDurationToTimespan(string isoDuration)
        {
            if (string.IsNullOrEmpty(isoDuration))
                return TimeSpan.Zero;

            // Try to use XmlConvert first, as it's the most reliable
            try
            {
                return XmlConvert.ToTimeSpan(isoDuration);
            }
            catch
            {
                // Fall back to regex parsing if XmlConvert fails
                var match = Regex.Match(isoDuration, @"PT(?:(\d+)H)?(?:(\d+)M)?(?:(\d+)S)?");
                if (match.Success)
                {
                    int hours = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
                    int minutes = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
                    int seconds = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

                    return new TimeSpan(hours, minutes, seconds);
                }

                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Returns <see cref="PostType.YouTubeShort"/> when a clip is within the Shorts
        /// length limit *and* the creator marks it with #Shorts; returns
        /// <see cref="PostType.YouTubeVideo"/> when it exceeds the limit; otherwise null.
        /// 
        /// • Shorts length cap: ≤ 60 s for clips published before 15 Oct 2024,
        ///                      ≤ 180 s on/after that date :contentReference[oaicite:0]{index=0}  
        /// • #Shorts hashtag is the official way to tell YouTube a clip is a Short :contentReference[oaicite:1]{index=1}
        /// </summary>
        private PostType? GetYouTubePostType(Video video)
        {
            if (video?.ContentDetails?.Duration == null)
                return null;

            /* ---------- 1. Duration check ---------- */
            int seconds;
            try
            {
                seconds = ParseIsoDuration(video.ContentDetails.Duration); // ISO‑8601 → seconds  :contentReference[oaicite:8]{index=8}
            }
            catch
            {
                return null; // unparsable duration → cannot decide confidently
            }

            DateTime? publishedUtc = DateTimeOffset.TryParse(video.Snippet?.PublishedAtRaw, out var p)
                                     ? p.UtcDateTime
                                     : (DateTime?)null;

            // Shorts length policy switch (60 s → 180 s) effective 15 Oct 2024  :contentReference[oaicite:9]{index=9}&#8203;:contentReference[oaicite:10]{index=10}
            const int LEGACY_CAP = 60;
            const int NEW_CAP = 180;
            DateTime POLICY_SWITCH_UTC = new DateTime(2024, 10, 15, 0, 0, 0, DateTimeKind.Utc);

            int cap = (publishedUtc.HasValue && publishedUtc.Value >= POLICY_SWITCH_UTC) ? NEW_CAP : LEGACY_CAP;
            if (seconds == 0) return null;              // corrupt data
            bool withinCap = seconds <= cap + 2;        // +2 s buffer for rounding on upload
            if (!withinCap) return PostType.YouTubeVideo; // exceeds cap → certainly not a Short

            /* ---------- 2. Orientation check ---------- */
            bool isPortrait = false;
            var thumbs = new[]                       // widths & heights per API  :contentReference[oaicite:11]{index=11}
            {
                video.Snippet?.Thumbnails?.Maxres,
                video.Snippet?.Thumbnails?.Standard,
                video.Snippet?.Thumbnails?.High,
                video.Snippet?.Thumbnails?.Medium,
                video.Snippet?.Thumbnails?.Default__
            };
            var t = thumbs.FirstOrDefault(th => th?.Width != null && th?.Height != null);
            if (t != null)
                isPortrait = t.Height > t.Width * 1.1; // allow a 10 % tolerance for cropped frames

            /* ---------- 3. Explicit #Shorts cue ---------- */
            bool hasShortsTag = video.Snippet?.Tags?.Any(tag =>
                                 tag.Equals("shorts", StringComparison.OrdinalIgnoreCase) ||
                                 tag.Equals("#shorts", StringComparison.OrdinalIgnoreCase)) == true; //  :contentReference[oaicite:12]{index=12}

            bool hasShortsInTitle = !string.IsNullOrEmpty(video.Snippet?.Title) &&
                                    video.Snippet.Title.IndexOf("#shorts", StringComparison.OrdinalIgnoreCase) >= 0; //  :contentReference[oaicite:13]{index=13}

            bool explicitCue = hasShortsTag || hasShortsInTitle;

            /* ---------- final decision matrix ---------- */
            if (withinCap && (isPortrait || explicitCue))
                return PostType.YouTubeShort;  // confident match

            if (!withinCap && !isPortrait && !explicitCue)
                return PostType.YouTubeVideo;  // confident long‑form

            return null; // conflicting or missing evidence → report “unsure”
        }
    }
}
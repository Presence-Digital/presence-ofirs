using POC_PlatformEngagementPoller.DataModels.DataTranferObjects;
using POC_PlatformEngagementPoller.DataModels.Entities;
using POC_PlatformEngagementPoller.PlatformClients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POC_PlatformEngagementPoller.DataModels.PlatformDataMappers
{
    /// <summary>
    /// Maps TikTok user‐info responses to our platform‑agnostic SocialMediaAccount.
    /// </summary>
    public class TikTokPlatformDataMapper
    {
        /// <summary>
        /// Converts the TikTok UserInfo response into a SocialMediaAccount.
        /// </summary>
        /// <param name="response">
        ///   The root of the TikTok user‑info payload, 
        ///   e.g. a typed DTO with response.Data.User populated.
        /// </param>
        public SocialMediaAccount MapToSocialMediaAccount(TikTokUserInfoResponse response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            var user = response.Data?.User
                ?? throw new ArgumentException("TikTok response missing user data", nameof(response));

            var profile = new AccountProfile
            {
                Title = user.DisplayName ?? string.Empty,
                Description = user.BioDescription ?? string.Empty,
                CustomUrl = user.ProfileDeepLink ?? string.Empty,
                ThumbnailUrl = user.AvatarLargeUrl ?? string.Empty
            };

            var stats = new AccountStatistics
            {
                SubscriberCount = user.FollowerCount,
                VideoCount = user.VideoCount
            };

            var additional = new Dictionary<string, object>
            {
                ["IsVerified"] = user.IsVerified,
                ["LikesCount"] = user.LikesCount,
                ["Username"] = user.Username,
                ["FollowingCount"] = user.FollowingCount,
            };

            return new SocialMediaAccount
            {
                AccountId = user.UnionId, // // TikTok uses the union_id as the unique key
                Platform = PlatformType.TikTok,
                Profile = profile,
                Statistics = stats,
                AdditionalProperties = additional,
            };
        }

        /// <summary>
        /// Maps aggregated TikTok video data to a single SocialMediaPost.
        /// </summary>
        public SocialMediaPost MapToSocialMediaPost(TikTokVideoAggregate tiktokVideo)
        {
            if (tiktokVideo == null)
                throw new ArgumentNullException(nameof(tiktokVideo));

            // Build the profile
            var profile = new PostProfile
            {
                Title = tiktokVideo.Title ?? string.Empty,
                Description = tiktokVideo.VideoDescription ?? string.Empty,
                ThumbnailUrl = tiktokVideo.CoverImageUrl ?? string.Empty,
                PublishedAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(tiktokVideo.CreateTime))
            };

            // Build the statistics
            var stats = new PostStatistics
            {
                ViewCount = tiktokVideo.ViewCount,
                LikeCount = tiktokVideo.LikeCount,
                CommentCount = tiktokVideo.CommentCount,
                ShareCount = tiktokVideo.ShareCount
            };

            // Additional platform-specific fields
            var additional = new Dictionary<string, object>
            {
                ["Duration"] = tiktokVideo.Duration,
                ["EmbedLink"] = tiktokVideo.EmbedLink
            };

            return new SocialMediaPost
            {
                PostId = tiktokVideo.Id,
                Platform = PlatformType.TikTok,
                Profile = profile,
                Statistics = stats,
                AdditionalProperties = additional,
                ETag = null
            };
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using POC_PlatformEngagementPoller.DataModels;
using POC_PlatformEngagementPoller.DataModels.Entities;

namespace POC_PlatformEngagementPoller.PlatformClients
{
    /// <summary>
    /// Defines a common contract for platform clients (YouTube, TikTok, etc.) to retrieve engagement data.
    /// This interface provides methods to obtain account-level statistics and post-level engagement metrics.
    /// </summary>
    public interface IPlatformClient
    {
        /// <summary>
        /// Asynchronously retrieves comprehensive account statistics.
        /// </summary>
        /// <param name="accountId">
        /// The unique identifier for the account. For YouTube, this might be the channel ID;
        /// for TikTok, it could be the username.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains an <see cref="Account"/> object
        /// with comprehensive statistics such as follower count, demographic data, content analytics, and monetization info.
        /// </returns>
        Task<SocialMediaAccount> GetAccountStatisticsAsync(string accountId);

        /// <summary>
        /// Asynchronously retrieves comprehensive engagement statistics for multiple posts from an account.
        /// </summary>
        /// <param name="accountId">
        /// The unique identifier for the account from which to retrieve post statistics.
        /// </param>
        /// <param name="topRecentPosts">
        /// An optional parameter specifying the maximum number of recent posts to retrieve.
        /// If not provided, the method will return all the account's posts.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of <see cref="Post"/> objects,
        /// each representing detailed engagement metrics for a post including views, likes, comments, shares,
        /// audience demographics, retention data, and platform-specific analytics.
        /// </returns>
        Task<List<SocialMediaPost>> GetPostsStatisticsAsync(string accountId, int? topRecentPosts = null);
    }

    /// <summary>
    /// Enum representing the supported social media platforms.
    /// </summary>
    public enum PlatformType
    {
        YouTube,
        TikTok,
        Instagram,
        Facebook,
        Twitter
    }

    /// <summary>
    /// Enum representing the type of posts.
    /// </summary>
    public enum PostType
    {
        YouTubeShort,
        YouTubeVideo,
        TikTokVideo
    }

}
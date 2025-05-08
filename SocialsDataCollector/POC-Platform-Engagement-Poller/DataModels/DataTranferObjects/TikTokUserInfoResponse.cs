using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POC_PlatformEngagementPoller.DataModels.DataTranferObjects
{
    public class TikTokUserInfoResponse
    {
        [JsonProperty("data")]
        public TikTokUserInfoData Data { get; set; }

        [JsonProperty("error")]
        public TikTokErrorInfo Error { get; set; }
    }

    public class TikTokUserInfoData
    {
        [JsonProperty("user")]
        public TikTokUserInfo User { get; set; }
    }

    public class TikTokUserInfo
    {
        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("is_verified")]
        public bool IsVerified { get; set; }

        [JsonProperty("likes_count")]
        public ulong LikesCount { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("video_count")]
        public ulong VideoCount { get; set; }

        [JsonProperty("avatar_large_url")]
        public string AvatarLargeUrl { get; set; }

        [JsonProperty("follower_count")]
        public ulong FollowerCount { get; set; }

        [JsonProperty("following_count")]
        public ulong FollowingCount { get; set; }

        [JsonProperty("profile_deep_link")]
        public string ProfileDeepLink { get; set; }

        [JsonProperty("union_id")]
        public string UnionId { get; set; }

        [JsonProperty("bio_description")]
        public string BioDescription { get; set; }
    }

    public class TikTokErrorInfo
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("log_id")]
        public string LogId { get; set; }
    }
}

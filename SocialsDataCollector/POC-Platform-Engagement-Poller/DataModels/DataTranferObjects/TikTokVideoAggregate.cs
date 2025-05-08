using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POC_PlatformEngagementPoller.DataModels.DataTranferObjects
{
    /// <summary>
    /// Holds merged data from list and query endpoints.
    /// </summary>
    public class TikTokVideoAggregate
    {
        // List endpoint fields
        public string Id { get; set; }
        public string Title { get; set; }
        public string VideoDescription { get; set; }
        public string CreateTime { get; set; }
        public int Duration { get; set; }
        public string CoverImageUrl { get; set; }
        public string EmbedLink { get; set; }

        // Query endpoint fields
        public ulong LikeCount { get; set; }
        public ulong CommentCount { get; set; }
        public ulong ShareCount { get; set; }
        public ulong ViewCount { get; set; }
    }
}

using System;
using System.Threading.Tasks;
using POC_PlatformEngagementPoller.PlatformClients;
using Xunit;

namespace POC_Platform_Engagement_Poller.Tests
{
    public class YouTubeClientTests
    {
        /// <summary>
        /// Tests that GetAccountStatisticsAsync throws an exception for an invalid channel ID.
        /// </summary>
        [Fact]
        public async Task GetAccountStatisticsAsync_ChannelNotFound_ThrowsException()
        {
            // Arrange
            string invalidChannelId = "INVALID_CHANNEL_ID";
            // Replace with your valid API key for testing if available.
            var apiKey = "YOUR_YOUTUBE_API_KEY";
            var client = new YouTubeClient(apiKey);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await client.GetAccountStatisticsAsync(invalidChannelId);
            });
        }

        // Additional tests can be added here to simulate valid responses,
        // test GetPostsStatisticsAsync and GetPostStatisticsAsync, etc.
    }
}

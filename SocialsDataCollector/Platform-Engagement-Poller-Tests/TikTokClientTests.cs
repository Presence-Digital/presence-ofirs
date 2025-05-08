using System;
using System.Threading.Tasks;
using POC_PlatformEngagementPoller.PlatformClients;
using Xunit;

namespace POC_Platform_Engagement_Poller.Tests
{
    public class TikTokClientTests
    {
        /// <summary>
        /// Tests that GetAccountStatisticsAsync throws an exception for an invalid username.
        /// </summary>
        [Fact]
        public async Task GetAccountStatisticsAsync_InvalidUsername_ThrowsException()
        {
            // Arrange
            string invalidUsername = "INVALID_USERNAME";
            // Replace with your valid TikTok access token for testing if available.
            var accessToken = "YOUR_TIKTOK_ACCESS_TOKEN";
            var client = new TikTokClient(accessToken);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () =>
            {
                await client.GetAccountStatisticsAsync(invalidUsername);
            });
        }

        // Additional tests can be added here to simulate valid responses,
        // test GetPostsStatisticsAsync and GetPostStatisticsAsync, etc.
    }
}

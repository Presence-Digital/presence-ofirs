using System.Threading.Tasks;
using POC_PlatformEngagementPoller.DataModels.Credentials;

namespace POC_PlatformEngagementPoller.CredentialsManagers
{
    /// <summary>
    /// Defines a contract for managing platform-specific credentials.
    /// </summary>
    /// <typeparam name="TCredentials">The type of credentials, which must inherit from BaseCredentials.</typeparam>
    public interface ICredentialsManager<TCredentials> where TCredentials : BaseCredentials
    {
        /// <summary>
        /// Asynchronously retrieves the platform-specific credentials.
        /// This method handles both the cached retrieval and fresh fetching when necessary.
        /// </summary>
        /// <returns>The credentials of type TCredentials.</returns>
        Task<TCredentials> GetCredentialsAsync(string cacheKey);

        /// <summary>
        /// Invalidates the cached credentials.
        /// When called, the next call to GetCredentialsAsync() will fetch fresh credentials.
        /// </summary>
        void Invalidate(string cacheKey);
    }
}

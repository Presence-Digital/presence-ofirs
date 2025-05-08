using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_Platform_Engagement_Poller.Caching;
using POC_PlatformEngagementPoller.Logging;

namespace POC_PlatformEngagementPoller.CredentialsManagers
{
    /// <summary>
    /// Abstract base class for credentials managers that provides caching functionality.
    /// </summary>
    /// <typeparam name="TCredentials">The credentials type derived from BaseCredentials.</typeparam>
    public abstract class BaseCredentialsManager<TCredentials> : ICredentialsManager<TCredentials>
        where TCredentials : BaseCredentials
    {
        private readonly BaseCachingCredentialsHelper<TCredentials> _cachingHelper;
        protected readonly ILogger _logger;

        // Private property to hold the name of the credentials manager.
        private string CredentialsManagerName => this.GetType().Name;

        /// <summary>
        /// Initializes a new instance of the BaseCredentialsManager.
        /// </summary>
        /// <param name="credentials">A dictionary of credentials to populate the cache.</param>
        /// <param name="logger">The logger instance.</param>
        protected BaseCredentialsManager(Dictionary<string, TCredentials> credentials, ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cachingHelper = new BaseCachingCredentialsHelper<TCredentials>(_logger);

            _logger.Info($"{CredentialsManagerName}: Instance created.");

            if (credentials == null || credentials.Count == 0)
            {
                _logger.Warning($"{CredentialsManagerName}: Created without any initial credentials.");
            }
            else
            {
                _logger.Info($"{CredentialsManagerName}: Populating cache with {credentials.Count} credentials.");
                PopulateCredentials(credentials);
            }
        }

        /// <summary>
        /// Must be implemented to refresh credentials.
        /// </summary>
        /// <param name="username">The cache key (e.g., username).</param>
        /// <returns>A task returning fresh credentials.</returns>
        protected abstract Task<TCredentials> RefreshCredentialsAsync(string username);

        /// <inheritdoc />
        public Task<TCredentials> GetCredentialsAsync(string username)
        {
            _logger.Debug($"{CredentialsManagerName}: GetCredentialsAsync called for '{username}'.");
            return _cachingHelper.GetOrAddAsync(username, (username) => RefreshCredentialsAsync(username));
        }

        /// <summary>
        /// Retrieves the raw cached credentials (without triggering refresh).
        /// </summary>
        protected TCredentials GetCredentialsData(string username)
        {
            _logger.Debug($"{CredentialsManagerName}: GetCredentialsData called for '{username}'.");
            return _cachingHelper.GetEntry(username);
        }

        /// <inheritdoc />
        public void Invalidate(string username)
        {
            _logger.Info($"{CredentialsManagerName}: Invalidating credentials for '{username}'.");
            _cachingHelper.Invalidate(username);
        }

        /// <summary>
        /// Populates the cache with the specified credentials for the given key.
        /// </summary>
        /// <param name="credentials">The credentials to populate.</param>
        private void PopulateCredentials(Dictionary<string, TCredentials> credentials)
        {
            foreach (var kvp in credentials)
            {
                _logger.Debug($"{CredentialsManagerName}: Adding credentials for '{kvp.Key}' to cache.");
                _cachingHelper.SetValue(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Derived classes must implement to determine if a response indicates that credentials cannot be refreshed.
        /// </summary>
        protected abstract bool IsUnauthorizedToRefreshToken(string responseContent);
    }
}

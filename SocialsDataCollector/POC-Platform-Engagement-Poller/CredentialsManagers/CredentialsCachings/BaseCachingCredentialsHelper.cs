using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.Logging;

namespace POC_Platform_Engagement_Poller.Caching
{
    /// <summary>
    /// Encapsulates caching and concurrency logic for credentials keyed by a provided identifier (e.g., username).
    /// Each entry is stored as a composite object that holds the credentials and a validity flag.
    /// </summary>
    /// <typeparam name="TCredentials">The credentials type derived from BaseCredentials.</typeparam>
    public class BaseCachingCredentialsHelper<TCredentials> where TCredentials : BaseCredentials
    {
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the BaseCachingCredentialsHelper.
        /// </summary>
        /// <param name="logger">The logger instance used for logging cache events.</param>
        public BaseCachingCredentialsHelper(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = new MemoryCache(new MemoryCacheOptions());
            _logger.Info("BaseCachingCredentialsHelper: Instance created.");
        }

        /// <summary>
        /// Internal composite cache entry that holds the credentials and a validity flag.
        /// </summary>
        private class CachedCredential
        {
            public TCredentials Credentials { get; set; }
            public bool IsValid { get; set; }

            public CachedCredential(TCredentials credentials)
            {
                Credentials = credentials;
                IsValid = true;
            }
        }

        /// <summary>
        /// Retrieves the cached credentials for the specified key.
        /// If the entry exists but is marked invalid, the provided refreshFunc is invoked to refresh the credentials.
        /// If no entry is found, an exception is thrown.
        /// </summary>
        /// <param name="key">The cache key (e.g., username).</param>
        /// <param name="refreshFunc">A function that fetches fresh credentials.</param>
        /// <returns>The credentials of type TCredentials.</returns>
        public async Task<TCredentials> GetOrAddAsync(string key, Func<string, Task<TCredentials>> refreshFunc)
        {
            if (!_cache.TryGetValue(key, out CachedCredential composite))
            {
                _logger.Error($"BaseCachingCredentialsHelper: No cached credentials found for key: {key}");
                throw new Exception($"No cached credentials found for key: {key}");
            }

            // If the entry is marked as invalid, refresh the credentials.
            if (!composite.IsValid)
            {
                await _semaphore.WaitAsync();
                try
                {
                    // Double-check after acquiring the lock.
                    if (!composite.IsValid)
                    {
                        _logger.Info($"BaseCachingCredentialsHelper: Refreshing credentials for key: {key}");
                        TCredentials newCreds = await refreshFunc(key);
                        composite.Credentials = newCreds;
                        composite.IsValid = true;
                        _logger.Debug($"BaseCachingCredentialsHelper: Credentials refreshed for key: {key}");
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return composite.Credentials;
        }

        /// <summary>
        /// Retrieves the cached credentials without triggering a refresh.
        /// </summary>
        public TCredentials GetEntry(string key)
        {
            if (_cache.TryGetValue(key, out CachedCredential composite))
            {
                _logger.Debug($"BaseCachingCredentialsHelper: Retrieved cache entry for key: {key}");
                return composite.Credentials;
            }
            _logger.Error($"BaseCachingCredentialsHelper: No cache entry found for key: {key} during GetEntry.");
            throw new Exception($"No cache entry found for key: {key} during GetEntry.");
        }

        /// <summary>
        /// Sets (or updates) the cached credentials for the specified key.
        /// This method is intended for internal use by credentials managers.
        /// </summary>
        /// <param name="key">The cache key (e.g., username).</param>
        /// <param name="credentials">The credentials to cache.</param>
        public void SetValue(string key, TCredentials credentials)
        {
            var composite = new CachedCredential(credentials);
            _cache.Set(key, composite);
            _logger.Info($"BaseCachingCredentialsHelper: Cached credentials set for key: {key}");
        }

        /// <summary>
        /// Marks the cached credentials for the specified key as invalid.
        /// If the entry isn't found, an exception is thrown.
        /// </summary>
        /// <param name="key">The cache key to invalidate (e.g., username).</param>
        public void Invalidate(string key)
        {
            if (_cache.TryGetValue(key, out CachedCredential composite))
            {
                composite.IsValid = false;
                _logger.Info($"BaseCachingCredentialsHelper: Cache entry invalidated for key: {key}");
            }
            else
            {
                _logger.Error($"BaseCachingCredentialsHelper: No cache entry found for key: {key} during Invalidate.");
                throw new Exception($"No cache entry found for key: {key} during Invalidate.");
            }
        }
    }
}

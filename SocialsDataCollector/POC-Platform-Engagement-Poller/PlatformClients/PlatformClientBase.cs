using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using POC_PlatformEngagementPoller.DataModels;
using POC_PlatformEngagementPoller.DataModels.Credentials;
using POC_PlatformEngagementPoller.Logging;
using POC_PlatformEngagementPoller.CredentialsManagers;
using POC_PlatformEngagementPoller.DataModels.Entities;

namespace POC_PlatformEngagementPoller.PlatformClients
{
    /// <summary>
    /// Abstract base class for platform-specific clients that provides common functionality for 
    /// authentication, retry logic, and error handling.
    /// </summary>
    /// <typeparam name="TCredentials">The credentials type for the specific platform, derived from BaseCredentials.</typeparam>
    public abstract class BasePlatformClient<TCredentials> : IPlatformClient
        where TCredentials : BaseCredentials
    {
        protected readonly ICredentialsManager<TCredentials> _credentialManager;
        protected readonly ILogger _logger;
        private readonly int _maxRetries;

        /// <summary>
        /// Gets the name of the concrete platform client.
        /// This value is used for logging context.
        /// </summary>
        private string PlatformName => this.GetType().Name;

        /// <summary>
        /// Initializes a new instance of the platform client base.
        /// </summary>
        /// <param name="credentialManager">The credential manager for the specific platform.</param>
        /// <param name="logger">The logger instance to log errors and other messages.</param>
        /// <param name="maxRetries">The maximum number of retry attempts for API calls. Default is 3.</param>
        protected BasePlatformClient(ICredentialsManager<TCredentials> credentialManager, ILogger logger, int maxRetries = 3)
        {
            _credentialManager = credentialManager ?? throw new ArgumentNullException(nameof(credentialManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRetries = maxRetries;
            _logger.Info($"{PlatformName}: Instance created.");
        }

        /// <summary>
        /// Retrieves account statistics from the platform.
        /// </summary>
        /// <param name="accountId">The account identifier used to retrieve credentials and identify the account.</param>
        /// <returns>Account information including profile and statistics.</returns>
        public abstract Task<SocialMediaAccount> GetAccountStatisticsAsync(string accountId);

        /// <summary>
        /// Retrieves statistics for posts from the platform.
        /// </summary>
        /// <param name="accountId">The account identifier used to retrieve credentials and identify the account.</param>
        /// <param name="topRecentPosts">Optional parameter specifying the number of recent posts to retrieve.</param>
        /// <returns>List of posts with engagement statistics.</returns>
        public abstract Task<List<SocialMediaPost>> GetPostsStatisticsAsync(string accountId, int? topRecentPosts = null);

        /// <summary>
        /// Creates a platform-specific API client using the provided credentials.
        /// </summary>
        /// <param name="credentials">The credentials to authenticate the API client.</param>
        /// <returns>The platform-specific API client.</returns>
        protected abstract object CreateApiClient(TCredentials credentials);


        // TODO: protected async Task<TResult> ExecuteApiCallAsync<TClient, TResult>(string accountId, Func<TClient, Task<TResult>> apiCall)

        /// <summary>
        /// Executes a platform-specific API call with retry logic.
        /// </summary>
        /// <typeparam name="TResult">The expected result type of the API call.</typeparam>
        /// <param name="accountId">The account identifier used to retrieve credentials.</param>
        /// <param name="apiCall">The API call function to execute.</param>
        /// <returns>The result of the API call.</returns>
        protected async Task<TResult> ExecuteApiCallAsync<TResult>(string accountId, Func<object, Task<TResult>> apiCall)
        {
            for (int attempt = 1; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    var credentials = await _credentialManager.GetCredentialsAsync(accountId);
                    var apiClient = CreateApiClient(credentials);
                    return await apiCall(apiClient);
                }
                catch (Exception ex) when (IsUnauthorizedException(ex))
                {
                    _logger.Error($"{PlatformName}: Unauthorized exception on attempt {attempt} for account '{accountId}'.", ex);
                    _credentialManager.Invalidate(accountId);

                    if (attempt == _maxRetries)
                    {
                        throw CreatePlatformException($"{PlatformName}: Unauthorized after maximum retry attempts for account '{accountId}'.", ex, PlatformErrorType.Authentication);
                    }
                }
                catch (Exception ex) when (IsUnauthorizedToRefreshTokenException(ex))
                {
                    _logger.Error($"{PlatformName}: UnauthorizedToRefreshToken exception for account '{accountId}'.", ex);
                    throw;
                }
                catch (Exception ex) when (IsRateLimitException(ex))
                {
                    _logger.Error($"{PlatformName}: Rate limit exception on attempt {attempt} for account '{accountId}'.", ex);
                    if (attempt == _maxRetries)
                    {
                        throw CreatePlatformException($"{PlatformName}: Rate limit exceeded after maximum retry attempts for account '{accountId}'.", ex, PlatformErrorType.RateLimit);
                    }
                    await Task.Delay(GetBackoffDelay(attempt));
                }
                catch (Exception ex) when (IsServiceUnavailableException(ex))
                {
                    _logger.Error($"{PlatformName}: Service unavailable exception on attempt {attempt} for account '{accountId}'.", ex);
                    if (attempt == _maxRetries)
                    {
                        throw CreatePlatformException($"{PlatformName}: Service unavailable after maximum retry attempts for account '{accountId}'.", ex, PlatformErrorType.ServiceUnavailable);
                    }
                    await Task.Delay(1000 * attempt);
                }
                catch (Exception ex)
                {
                    _logger.Error($"{PlatformName}: Unexpected exception on attempt {attempt} for account '{accountId}'.", ex);
                    if (attempt == _maxRetries)
                    {
                        throw CreatePlatformException($"{PlatformName}: API call failed after {_maxRetries} attempts for account '{accountId}'.", ex, PlatformErrorType.Unknown);
                    }
                }
            }

            // This should never happen due to exception handling above.
            throw CreatePlatformException($"{PlatformName}: Failed API call after retries for account '{accountId}'.", null, PlatformErrorType.Unknown);
        }

        /// <summary>
        /// Determines whether the exception represents an unauthorized response.
        /// </summary>
        protected virtual bool IsUnauthorizedException(Exception ex)
        {
            return (ex is PlatformException PlatEx && PlatEx.ErrorType == PlatformErrorType.Authentication) ||
                   (ex is WebException webEx &&
                    webEx.Response is HttpWebResponse response &&
                    response.StatusCode == HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Determines whether the exception represents an UnauthorizedToRefreshToken response.
        /// </summary>
        protected virtual bool IsUnauthorizedToRefreshTokenException(Exception ex)
        {
            return ex is PlatformException PlatEx && PlatEx.ErrorType == PlatformErrorType.UnauthorizedToRefreshToken;
        }

        /// <summary>
        /// Determines whether the exception represents a rate limit response.
        /// </summary>
        protected virtual bool IsRateLimitException(Exception ex)
        {
            return (ex is PlatformException PlatEx && PlatEx.ErrorType == PlatformErrorType.RateLimit) ||
                   (ex is WebException webEx &&
                    webEx.Response is HttpWebResponse response &&
                   (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode == 429));
        }

        /// <summary>
        /// Determines whether the exception represents a service unavailable response.
        /// </summary>
        protected virtual bool IsServiceUnavailableException(Exception ex)
        {
            return (ex is PlatformException PlatEx && PlatEx.ErrorType == PlatformErrorType.ServiceUnavailable) ||
                   (ex is WebException webEx &&
                    webEx.Response is HttpWebResponse response &&
                   (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.InternalServerError));
        }

        /// <summary>
        /// Calculates the backoff delay for retry attempts.
        /// </summary>
        protected virtual int GetBackoffDelay(int attempt)
        {
            // Exponential backoff with jitter.
            var baseDelay = Math.Pow(2, attempt) * 1000;
            var jitter = new Random().Next(0, 1000);
            return (int)Math.Min(baseDelay + jitter, 30000); // Cap at 30 seconds.
        }

        /// <summary>
        /// Creates a standardized platform exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="errorType">The type of platform error.</param>
        /// <returns>A new exception object.</returns>
        public virtual Exception CreatePlatformException(string message, Exception innerException, PlatformErrorType errorType)
        {
            // Optionally include the platform context in the message.
            return new PlatformException($"{PlatformName}: {message}", innerException, errorType);
        }
    }

    /// <summary>
    /// Enumerates types of platform errors.
    /// </summary>
    public enum PlatformErrorType
    {
        Unknown,
        InternalServerError,
        Authentication,
        UnauthorizedToRefreshToken,
        RateLimit,
        ServiceUnavailable,
        InvalidRequest,
        NotFound,
        Network,
    }

    /// <summary>
    /// Represents an exception specific to platform API operations.
    /// </summary>
    public class PlatformException : Exception
    {
        public PlatformErrorType ErrorType { get; }

        public PlatformException(string message, Exception innerException, PlatformErrorType errorType)
            : base(message, innerException)
        {
            ErrorType = errorType;
        }
    }
}

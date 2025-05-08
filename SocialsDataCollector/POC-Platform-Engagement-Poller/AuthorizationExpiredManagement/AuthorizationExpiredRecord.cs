using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POC_PlatformEngagementPoller.AuthorizationExpiredManagement
{
    /// <summary>
    /// Represents a notification record for an expired authorization.
    /// </summary>
    public class AuthorizationExpiredRecord
    {
        /// <summary>
        /// The timestamp when the record was updated (in UTC).
        /// </summary>
        public DateTime UpdatedOn { get; set; }

        /// <summary>
        /// The platform name (e.g., YouTube, TikTok).
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// The account identifier.
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// The complete executable command (including all arguments) to refresh tokens.
        /// </summary>
        public string ExecutableCommand { get; set; }
    }
}

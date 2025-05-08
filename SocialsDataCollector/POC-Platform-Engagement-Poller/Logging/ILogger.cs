using System;
using System.Runtime.CompilerServices;

namespace POC_PlatformEngagementPoller.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public interface ILogger
    {
        /// <summary>
        /// Logs a message with the specified log level.
        /// </summary>
        /// <param name="level">The level of the log entry.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="ex">Optional exception to include in the log.</param>
        /// <param name="callerFilePath">Automatically provided file path of the caller.</param>
        /// <param name="callerMemberName">Automatically provided member name of the caller.</param>
        void Log(LogLevel level, string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "");

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        void Debug(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "");

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        void Info(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "");

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void Warning(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "");

        /// <summary>
        /// Logs an error message.
        /// </summary>
        void Error(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "");

        /// <summary>
        /// Logs a critical error message.
        /// </summary>
        void Critical(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "");
    }
}

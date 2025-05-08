using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace POC_PlatformEngagementPoller.Logging
{
    /// <summary>
    /// Abstract base logger that encapsulates console logging (with color)
    /// and enriches log entries with caller (class and method) information.
    /// </summary>
    public abstract class BaseLogger : ILogger
    {
        public void Log(LogLevel level, string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "")
        {
            LogWithCaller(level, message, ex, callerFilePath, callerMemberName);
        }

        public void Debug(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "")
        {
            LogWithCaller(LogLevel.Debug, message, ex, callerFilePath, callerMemberName);
        }

        public void Info(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "")
        {
            LogWithCaller(LogLevel.Info, message, ex, callerFilePath, callerMemberName);
        }

        public void Warning(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "")
        {
            LogWithCaller(LogLevel.Warning, message, ex, callerFilePath, callerMemberName);
        }

        public void Error(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "")
        {
            LogWithCaller(LogLevel.Error, message, ex, callerFilePath, callerMemberName);
        }

        public void Critical(string message, Exception ex = null,
            [CallerFilePath] string callerFilePath = "",
            [CallerMemberName] string callerMemberName = "")
        {
            LogWithCaller(LogLevel.Critical, message, ex, callerFilePath, callerMemberName);
        }

        // Private helper that builds a LogEntry and routes it for output.
        private void LogWithCaller(
            LogLevel level,
            string message,
            Exception ex,
            string callerFilePath,
            string callerMemberName)
        {
            // Extract the caller's class name from the file path.
            string className = Path.GetFileNameWithoutExtension(callerFilePath);

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                CallerClass = className,
                CallerMember = callerMemberName,
                Message = message,
                Exception = ex
            };

            WriteLogEntry(entry);
            WriteToConsole(entry);
        }

        // Derived classes must implement this method to write the log entry to their target.
        protected abstract void WriteLogEntry(LogEntry entry);

        // Default implementation for writing to the console.
        protected virtual void WriteToConsole(LogEntry entry)
        {
            Console.ForegroundColor = GetConsoleColor(entry.Level);
            Console.WriteLine(entry.ToString());
            Console.ResetColor();
        }

        // Maps LogLevel to ConsoleColor.
        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Magenta,
                _ => ConsoleColor.White,
            };
        }
    }

    /// <summary>
    /// A simple data structure representing a log entry.
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string CallerClass { get; set; }
        public string CallerMember { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public override string ToString()
        {
            string baseMessage = $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] ({CallerClass}.{CallerMember}) {Message}";
            if (Exception != null)
            {
                baseMessage += Environment.NewLine + Exception.ToString();
            }
            return baseMessage;
        }
    }
}

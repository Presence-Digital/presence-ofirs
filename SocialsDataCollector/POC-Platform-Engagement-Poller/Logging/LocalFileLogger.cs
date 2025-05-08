using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;

namespace POC_PlatformEngagementPoller.Logging
{
    public class LocalFileLogger : BaseLogger
    {
        private readonly string _logFilePath;
        private readonly object _lock = new object();

        public LocalFileLogger(string logFolderPath)
        {
            // Ensure the log folder exists; if not, create it.
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            // Create a log file with the current date and time in its name.
            _logFilePath = Path.Combine(logFolderPath, $"Log_{DateTime.Now:yyyy-MM-dd__HH-mm}.txt");
        }

        /// <summary>
        /// Writes the log entry to the local file in a thread-safe manner.
        /// The LogEntry.ToString() method formats the log with timestamp, log level, caller info, and message.
        /// </summary>
        protected override void WriteLogEntry(LogEntry entry)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, entry.ToString() + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // If file logging fails, output an error message to the console.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("LocalFileLogger failure: " + ex.Message);
                Console.ResetColor();
            }
        }
    }
}

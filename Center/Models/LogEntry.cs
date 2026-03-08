using System;

namespace Center.Models
{
    public class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public enum LogLevel { Debug, Info, Warning, Error, System }
}

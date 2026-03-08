using System;

namespace Center.Models
{
    public class AlertEntry
    {
        public Guid Id { get; set; }
        public string StationCode { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsAcknowledged { get; set; }
    }

    public enum AlertSeverity { Info, Warning, Critical }
}

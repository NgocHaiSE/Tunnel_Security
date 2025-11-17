using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Station.Models
{
    public enum AlertState
    {
        Unprocessed = 0,
        InProgress = 1,
        Resolved = 2
    }

    public enum AlertSeverity
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public class Alert
    {
        public Guid Id { get; set; }
        public string? AlertId { get; set; }
        public DateTimeOffset Ts { get; set; }
        public string? RawPayload { get; set; }  
        public bool Ack { get; set; }
        public DateTimeOffset CreateAt { get; set; }
        public AlertState State { get; set; }
        public Guid DeviceId { get; set; }
        public AlertSeverity Severity { get; set; }

        // Navigation property
        public Device? Device { get; set; }
    }
}

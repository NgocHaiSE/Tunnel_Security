using System;

namespace Center.Models
{
    public class StationInfo
    {
        public string StationCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public StationStatus Status { get; set; }
        public int LatencyMs { get; set; }
        public double TransferRateMbps { get; set; }
        public DateTimeOffset LastHeartbeat { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public enum StationStatus { Online, Warning, Offline }
}

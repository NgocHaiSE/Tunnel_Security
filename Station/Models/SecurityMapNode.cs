using System;

namespace Station.Models
{
    /// <summary>
    /// Represents status of a security node
    /// </summary>
    public enum NodeStatus
    {
        Secure,      // Green - Normal operation
        Warning,     // Yellow/Orange - Warning detected
        Critical,    // Red - Critical alert
        Offline      // Gray - Offline/disconnected
    }

    /// <summary>
    /// Represents a security monitoring node on the map
    /// </summary>
    public class SecurityMapNode
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;        // Area A, B, C, D
        public string Region { get; set; } = string.Empty;      // R1, R2, R3, R4
        public string NodeNumber { get; set; } = string.Empty;  // N1, N2, N3...
        public double X { get; set; }  // Canvas X position (0-700)
        public double Y { get; set; }  // Canvas Y position (0-500)
        public NodeStatus Status { get; set; }

        // Sensor readings
        public double? RadarValue { get; set; }         // mm/m
        public double? VibrationValue { get; set; }             // mm/s
        public double? SmokeFireValue { get; set; }           // L or %
        public double? TemperatureValue { get; set; }       // °C
        public double? HumidityValue { get; set; }       // %

        // Device info
        public string? CameraId { get; set; }
        public bool IsHub { get; set; }  // Is this a hub node?

        public DateTimeOffset LastUpdate { get; set; }

        /// <summary>
        /// Full location string (Area / Region / Node)
        /// </summary>
        public string FullLocation => $"{Area} / {Region} / {NodeNumber}";

        /// <summary>
        /// Get color based on status
        /// </summary>
        public string GetColorCode()
        {
            return Status switch
            {
                NodeStatus.Secure => "#3FCF8E",      // Green
                NodeStatus.Warning => "#FFD166",     // Yellow
                NodeStatus.Critical => "#F0625D",    // Red
                NodeStatus.Offline => "#7B7E85",     // Gray
                _ => "#7B7E85"
            };
        }

        /// <summary>
        /// Determine status based on sensor values
        /// </summary>
        public void UpdateStatus()
        {
            // Check if offline
            if ((DateTimeOffset.Now - LastUpdate).TotalMinutes > 5)
            {
                Status = NodeStatus.Offline;
                return;
            }

            // Check for critical conditions
            if (RadarValue > 3.0 || VibrationValue > 4.0 || SmokeFireValue > 50 || TemperatureValue > 45)
            {
                Status = NodeStatus.Critical;
                return;
            }

            // Check for warning conditions
            if (RadarValue > 2.0 || VibrationValue > 3.0 || SmokeFireValue > 30 || TemperatureValue > 35)
            {
                Status = NodeStatus.Warning;
                return;
            }

            // Normal operation
            Status = NodeStatus.Secure;
        }
    }
}

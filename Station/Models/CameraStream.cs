using System;

namespace Station.Models
{
    /// <summary>
    /// Camera stream configuration
    /// </summary>
    public class CameraStream
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string CameraName { get; set; } = string.Empty;
        public string StreamUrl { get; set; } = string.Empty;

        // Stream properties
        public string Resolution { get; set; } = "1280×720";
        public int Fps { get; set; } = 30;
        public string Codec { get; set; } = "H.264";

        // Camera settings
        public bool IrEnabled { get; set; }
        public string IrMode { get; set; } = "AUTO"; // AUTO, ON, OFF
        public bool HdrEnabled { get; set; }
        public string HdrMode { get; set; } = "AUTO";

        // Status
        public bool IsOnline { get; set; }
        public bool IsRecording { get; set; }
        public DateTimeOffset? LastFrameTime { get; set; }

        // Stats
        public double Bitrate { get; set; } // Mbps
        public int FrameCount { get; set; }
        public int DroppedFrames { get; set; }

        // Navigation
        public Device? Device { get; set; }
    }

    /// <summary>
    /// Grid layout options for camera display
    /// </summary>
    public enum CameraGridLayout
    {
        Single = 1,     // 1x1
        TwoByTwo = 4, // 2x2
        ThreeByThree = 9, // 3x3
        FourByFour = 16  // 4x4
    }

    /// <summary>
    /// Camera snapshot for AI analysis
    /// </summary>
    public class CameraSnapshot
    {
        public Guid Id { get; set; }
        public Guid CameraStreamId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public byte[]? ImageData { get; set; }

        // AI Analysis results
        public bool HasDetection { get; set; }
        public string? DetectionType { get; set; }
        public double Confidence { get; set; }
        public string? ObjectClass { get; set; }

        public CameraStream? CameraStream { get; set; }
    }
}

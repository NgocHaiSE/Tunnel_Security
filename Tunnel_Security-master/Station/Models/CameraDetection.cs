using System;

namespace Station.Models
{
    /// <summary>
    /// Dữ liệu phát hiện từ camera
    /// </summary>
    public class CameraDetection
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        // Thông tin phát hiện
        public DetectionType Type { get; set; }
        public double Confidence { get; set; } // Độ tin cậy 0-1
        public string? ObjectClass { get; set; } // Loại đối tượng: person, vehicle, animal...

        // Vị trí trong frame
        public int? BoundingBoxX { get; set; }
        public int? BoundingBoxY { get; set; }
        public int? BoundingBoxWidth { get; set; }
        public int? BoundingBoxHeight { get; set; }

        // Metadata
        public string? ImagePath { get; set; } // Đường dẫn ảnh chứng cứ
        public string? VideoPath { get; set; } // Đường dẫn video
        public bool IsIntrusion { get; set; } // Có phải xâm nhập không
        public bool GeneratedAlert { get; set; } // Đã tạo cảnh báo chưa

        // Navigation
        public Device? Device { get; set; }
        public Alert? Alert { get; set; }
    }

    /// <summary>
    /// Loại phát hiện
    /// </summary>
    public enum DetectionType
    {
        Motion = 0,         // Chuyển động
        Object = 1,         // Phát hiện vật thể
        Face = 2,     // Nhận diện khuôn mặt
        LicensePlate = 3,   // Biển số xe
        Intrusion = 4,      // Xâm nhập khu vực cấm
        Loitering = 5,      // Lảng vảng
        Crossing = 6,    // Vượt qua vùng
        Abandoned = 7       // Vật bỏ quên
    }

    /// <summary>
    /// Thống kê camera theo thời gian
    /// </summary>
    public class CameraStatistics
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;

        public int TotalDetections { get; set; }
        public int IntrusionCount { get; set; }
        public int FalseAlarmCount { get; set; }
        public double AvgConfidence { get; set; }

        public DateTimeOffset PeriodStart { get; set; }
        public DateTimeOffset PeriodEnd { get; set; }

        // Phân tích theo giờ (24 giờ)
        public int[] DetectionsByHour { get; set; } = new int[24];
    }
}

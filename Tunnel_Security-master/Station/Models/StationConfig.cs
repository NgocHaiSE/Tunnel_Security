using System;

namespace Station.Models
{
    /// <summary>
    /// Cấu hình thông tin trạm - lưu trong DB hoặc file config
    /// </summary>
    public class StationConfig
    {
        public Guid Id { get; set; }
        
        // Thông tin nhận dạng trạm
        public string StationCode { get; set; } = string.Empty;  // VD: "TRM-001", "TRM-HN-01"
        public string StationName { get; set; } = string.Empty;
        
        // Thông tin địa lý
        public string? Area { get; set; }          // Khu vực: "Miền Bắc", "Khu A", "Vùng 1"
        public string? Route { get; set; }         // Tuyến: "Tuyến 1A", "Cao tốc HN-HP"
        public string? Zone { get; set; }          // Phân khu nhỏ hơn (nếu cần)
        
        // Vị trí chi tiết
        public string? Province { get; set; }      // Tỉnh/Thành phố
        public string? District { get; set; }      // Quận/Huyện
        public string? Address { get; set; }       // Địa chỉ cụ thể
        
        // Tọa độ GPS (nếu cần)
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        // Thông tin kết nối Center
        public string? CenterUrl { get; set; }     // URL của Center API
        public string? CenterApiKey { get; set; }  // API Key để xác thực
        
        // Thông tin liên hệ
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        
        // Metadata
        public DateTimeOffset ConfiguredAt { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Cài đặt hoạt động
        public int HeartbeatIntervalSeconds { get; set; } = 30;    // Gửi heartbeat mỗi 30s
        public int AlertSyncIntervalSeconds { get; set; } = 5;     // Đồng bộ alert mỗi 5s
        public bool AutoReconnect { get; set; } = true;
        public int MaxRetryAttempts { get; set; } = 5;
    }
}
using System;

namespace Station.DTOs
{
    /// <summary>
    /// DTO gửi thông tin đăng ký trạm lên Center
    /// </summary>
    public class StationRegistrationDto
    {
        public string StationCode { get; set; } = string.Empty;
        public string StationName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string? Zone { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Address { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTimeOffset RegisteredAt { get; set; }
    }

    /// <summary>
    /// DTO nhận phản hồi từ Center sau khi đăng ký
    /// </summary>
    public class StationRegistrationResponseDto
    {
        public Guid StationId { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace Station.Models
{
    public enum DeviceStatus
    {
        Offline = 0,
        Online = 1,
        Fault = 2,
        Disabled = 3
    }

    public class Device
    {
        public Guid Id { get; set; }
        public string? DeviceId { get; set; }
        public string? Name { get; set; }
        public string? Ip { get; set; }
        public string? Location { get; set; }      // Vị trí trong trạm
        public DateTimeOffset CreatedAt { get; set; }
        public string? Type { get; set; }          // Loại thiết bị: Camera, Sensor, Radar...
        public string? Manufacturer { get; set; }   // Sửa typo "Manufactuer"
        public DeviceStatus Status { get; set; }
        public DateTimeOffset? LastOnline { get; set; }  // Lần cuối online

        // Thông tin bổ sung
        public string? SerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Description { get; set; }

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}

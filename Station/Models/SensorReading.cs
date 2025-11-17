using System;

namespace Station.Models
{
    /// <summary>
    /// Loại sensor
    /// </summary>
    public enum SensorType
    {
        Motion = 0,         // Cảm biến chuyển động
        Temperature = 1,    // Cảm biến nhiệt độ
        Vibration = 2,      // Cảm biến rung
        Acoustic = 3,    // Cảm biến âm thanh
        Pressure = 4,       // Cảm biến áp suất
        Humidity = 5,// Cảm biến độ ẩm
        Light = 6  // Cảm biến ánh sáng
    }

    /// <summary>
    /// Đơn vị đo
    /// </summary>
    public enum MeasurementUnit
    {
        None = 0,
        Celsius = 1,        // °C
        Fahrenheit = 2,     // °F
        Percent = 3,        // %
        Decibel = 4,        // dB
        Pascal = 5,         // Pa
        Meter = 6,    // m
        MilliMeter = 7,     // mm
        Lux = 8,     // lx
        Count = 9   // số lần
    }

    /// <summary>
    /// Dữ liệu đo từ sensor
    /// </summary>
    public class SensorReading
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        // Loại sensor và dữ liệu
        public SensorType SensorType { get; set; }
        public double Value { get; set; }
        public MeasurementUnit Unit { get; set; }

        // Metadata
        public string? Location { get; set; }
        public bool IsAnomaly { get; set; }  // Có phải giá trị bất thường không
        public double? Threshold { get; set; } // Ngưỡng cảnh báo

        // Navigation
        public Device? Device { get; set; }
    }

    /// <summary>
    /// Thống kê sensor theo khoảng thời gian
    /// </summary>
    public class SensorStatistics
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public SensorType SensorType { get; set; }

        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double AvgValue { get; set; }
        public int ReadingCount { get; set; }
        public int AnomalyCount { get; set; }

        public DateTimeOffset PeriodStart { get; set; }
        public DateTimeOffset PeriodEnd { get; set; }
    }
}

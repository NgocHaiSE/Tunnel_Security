using System;
using System.ComponentModel;

namespace Station.ViewModels
{
    public class SensorReadingViewModel : INotifyPropertyChanged
    {
        public string DeviceName { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsAnomaly { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}

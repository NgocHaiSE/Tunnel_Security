using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Station.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.Generic;

namespace Station.ViewModels
{
    public partial class DataViewModel : ObservableObject
    {
        private readonly DispatcherQueue? _dispatcherQueue;
        private Timer? _realtimeUpdateTimer;
        private readonly Random _random = new();

        // Real-time Statistics
        [ObservableProperty]
        private int _totalReadingsToday = 0;

        [ObservableProperty]
        private int _activeSensors = 0;

        [ObservableProperty]
        private int _cameraDetections = 0;

        [ObservableProperty]
        private int _anomalyCount = 0;

        // Collections
        public ObservableCollection<SensorReadingViewModel> RealtimeSensorReadings { get; } = new();
        public ObservableCollection<CameraDetectionViewModel> RecentCameraDetections { get; } = new();
        public ObservableCollection<SensorStatisticsViewModel> SensorStatistics { get; } = new();

        // LiveCharts
        public ObservableCollection<ISeries> SensorTrendSeries { get; set; } = new();
        public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> SensorTrendXAxes { get; set; }
        public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> SensorTrendYAxes { get; set; }
        public ObservableCollection<ISeries> SensorDistributionSeries { get; set; } = new();

        // Chart data storage
        private ObservableCollection<double> _temperatureData = new();
        private ObservableCollection<double> _motionData = new();
        private ObservableCollection<double> _vibrationData = new();
        private ObservableCollection<double> _acousticData = new();

        public DataViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            InitializeMockData();
            InitializeCharts();
            StartRealtimeUpdates();
        }

        private void InitializeMockData()
        {
            // Initialize 24 hours of data
            for (int i = 0; i < 24; i++)
            {
                _temperatureData.Add(20 + _random.Next(-3, 3));
                _motionData.Add(_random.Next(0, 100));
                _vibrationData.Add(_random.Next(0, 50));
                _acousticData.Add(40 + _random.Next(-10, 10));
            }

            // Set statistics
            TotalReadingsToday = 3847;
            ActiveSensors = 24;
            CameraDetections = 156;
            AnomalyCount = 8;

            // Load data
            LoadSensorStatistics();
            LoadRealtimeSensorReadings();
            LoadRecentCameraDetections();
        }

        private void LoadSensorStatistics()
        {
            SensorStatistics.Clear();

            SensorStatistics.Add(new SensorStatisticsViewModel
            {
                DeviceName = "SNS-01 (Motion)",
                SensorType = "Motion Sensor",
                CurrentValue = 45.2,
                Unit = "%",
                MinValue = 0,
                MaxValue = 100,
                AvgValue = 42.8,
                Status = "Bình thường",
                StatusColor = new SolidColorBrush(Colors.Green),
                TrendIcon = "↑",
                TrendColor = new SolidColorBrush(Colors.Green)
            });

            SensorStatistics.Add(new SensorStatisticsViewModel
            {
                DeviceName = "SNS-02 (Temperature)",
                SensorType = "Temperature Sensor",
                CurrentValue = 22.5,
                Unit = "°C",
                MinValue = 18.2,
                MaxValue = 26.8,
                AvgValue = 22.1,
                Status = "Bình thường",
                StatusColor = new SolidColorBrush(Colors.Green),
                TrendIcon = "→",
                TrendColor = new SolidColorBrush(Colors.Gray)
            });

            SensorStatistics.Add(new SensorStatisticsViewModel
            {
                DeviceName = "SNS-03 (Vibration)",
                SensorType = "Vibration Sensor",
                CurrentValue = 15.8,
                Unit = "mm/s",
                MinValue = 2.1,
                MaxValue = 28.4,
                AvgValue = 14.2,
                Status = "Cảnh báo",
                StatusColor = new SolidColorBrush(Colors.Orange),
                TrendIcon = "↑",
                TrendColor = new SolidColorBrush(Colors.Orange)
            });

            SensorStatistics.Add(new SensorStatisticsViewModel
            {
                DeviceName = "SNS-04 (Acoustic)",
                SensorType = "Acoustic Sensor",
                CurrentValue = 42.3,
                Unit = "dB",
                MinValue = 30.5,
                MaxValue = 65.2,
                AvgValue = 41.8,
                Status = "Bình thường",
                StatusColor = new SolidColorBrush(Colors.Green),
                TrendIcon = "↓",
                TrendColor = new SolidColorBrush(Colors.Red)
            });
        }

        private void LoadRealtimeSensorReadings()
        {
            RealtimeSensorReadings.Clear();
            var now = DateTimeOffset.Now;

            RealtimeSensorReadings.Add(new SensorReadingViewModel
            {
                DeviceName = "SNS-01",
                SensorType = "Motion",
                Value = 45.2,
                Unit = "%",
                Timestamp = now.AddSeconds(-2),
                IsAnomaly = false
            });

            RealtimeSensorReadings.Add(new SensorReadingViewModel
            {
                DeviceName = "SNS-02",
                SensorType = "Temperature",
                Value = 22.5,
                Unit = "°C",
                Timestamp = now.AddSeconds(-3),
                IsAnomaly = false
            });

            RealtimeSensorReadings.Add(new SensorReadingViewModel
            {
                DeviceName = "SNS-03",
                SensorType = "Vibration",
                Value = 15.8,
                Unit = "mm/s",
                Timestamp = now.AddSeconds(-5),
                IsAnomaly = true
            });
        }

        private void LoadRecentCameraDetections()
        {
            RecentCameraDetections.Clear();
            var now = DateTimeOffset.Now;

            RecentCameraDetections.Add(new CameraDetectionViewModel
            {
                DeviceName = "CAM-01",
                DetectionType = "Motion",
                Confidence = 0.95,
                Timestamp = now.AddSeconds(-10),
                IsIntrusion = false,
                ObjectClass = "Person"
            });

            RecentCameraDetections.Add(new CameraDetectionViewModel
            {
                DeviceName = "CAM-02",
                DetectionType = "Intrusion",
                Confidence = 0.87,
                Timestamp = now.AddSeconds(-25),
                IsIntrusion = true,
                ObjectClass = "Person"
            });
        }

        private void InitializeCharts()
        {
            // Sensor Trend Chart
            SensorTrendSeries.Clear();

            SensorTrendSeries.Add(new LineSeries<double>
            {
                Name = "Temperature (°C)",
                Values = _temperatureData,
                Fill = null,
                Stroke = new SolidColorPaint(new SKColor(239, 68, 68)) { StrokeThickness = 2 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            });

            SensorTrendSeries.Add(new LineSeries<double>
            {
                Name = "Motion (%)",
                Values = _motionData,
                Fill = null,
                Stroke = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 2 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            });

            // X Axis
            SensorTrendXAxes = new Axis[]
           {
    new Axis
             {
      Labels = Enumerable.Range(0, 24).Select(h => $"{h:D2}h").ToArray(),
        LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
          TextSize = 11
    }
                };

            // Y Axis
            SensorTrendYAxes = new Axis[]
                   {
          new Axis
            {
 LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
             SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
         TextSize = 11,
MinLimit = 0
     }
                   };

            // Sensor Distribution Pie Chart
            SensorDistributionSeries.Clear();

            SensorDistributionSeries.Add(new PieSeries<int>
            {
                Name = "Motion",
                Values = new int[] { 8 },
                Fill = new SolidColorPaint(new SKColor(59, 130, 246)),
                DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                DataLabelsSize = 14,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}"
            });

            SensorDistributionSeries.Add(new PieSeries<int>
            {
                Name = "Temperature",
                Values = new int[] { 6 },
                Fill = new SolidColorPaint(new SKColor(239, 68, 68)),
                DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                DataLabelsSize = 14,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}"
            });
        }

        private void StartRealtimeUpdates()
        {
            _realtimeUpdateTimer = new Timer(_ =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                   {
                       UpdateRealtimeData();
                   });
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        private void UpdateRealtimeData()
        {
            // Update statistics
            TotalReadingsToday += _random.Next(1, 5);
            CameraDetections += _random.Next(0, 2);

            if (_random.Next(100) < 5)
            {
                AnomalyCount++;
            }

            // Update chart data
            if (_temperatureData.Count >= 24)
            {
                _temperatureData.RemoveAt(0);
                _motionData.RemoveAt(0);
            }

            _temperatureData.Add(20 + _random.Next(-3, 3));
            _motionData.Add(_random.Next(0, 100));
        }

        public void RefreshData()
        {
            InitializeMockData();
            InitializeCharts();
        }

        public void ExportToCsv()
        {
            System.Diagnostics.Debug.WriteLine("Export to CSV requested");
        }

        public void ExportToPdf()
        {
            System.Diagnostics.Debug.WriteLine("Export to PDF requested");
        }

        ~DataViewModel()
        {
            _realtimeUpdateTimer?.Dispose();
        }
    }

    // === VIEW MODELS FOR UI BINDING ===

    public class SensorReadingViewModel : ObservableObject
    {
        private string _deviceName = string.Empty;
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        private string _sensorType = string.Empty;
        public string SensorType
        {
            get => _sensorType;
            set => SetProperty(ref _sensorType, value);
        }

        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    OnPropertyChanged(nameof(ValueText));
                }
            }
        }

        private string _unit = string.Empty;
        public string Unit
        {
            get => _unit;
            set
            {
                if (SetProperty(ref _unit, value))
                {
                    OnPropertyChanged(nameof(ValueText));
                }
            }
        }

        private DateTimeOffset _timestamp;
        public DateTimeOffset Timestamp
        {
            get => _timestamp;
            set
            {
                if (SetProperty(ref _timestamp, value))
                {
                    OnPropertyChanged(nameof(TimestampText));
                }
            }
        }

        private bool _isAnomaly;
        public bool IsAnomaly
        {
            get => _isAnomaly;
            set
            {
                if (SetProperty(ref _isAnomaly, value))
                {
                    OnPropertyChanged(nameof(BackgroundColor));
                }
            }
        }

        public string TimestampText
        {
            get
            {
                var diff = DateTimeOffset.Now - _timestamp;
                return diff.TotalSeconds < 60
        ? $"{(int)diff.TotalSeconds}s trước"
      : $"{(int)diff.TotalMinutes}m trước";
            }
        }

        public string ValueText => $"{_value:F1} {_unit}";

        public SolidColorBrush BackgroundColor =>
            _isAnomaly
    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 254, 226, 226))
        : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 248, 250, 252));
    }

    public class CameraDetectionViewModel : ObservableObject
    {
        private string _deviceName = string.Empty;
        public string DeviceName
        {
            get => _deviceName;
            set => SetProperty(ref _deviceName, value);
        }

        private string _detectionType = string.Empty;
        public string DetectionType
        {
            get => _detectionType;
            set => SetProperty(ref _detectionType, value);
        }

        private double _confidence;
        public double Confidence
        {
            get => _confidence;
            set
            {
                if (SetProperty(ref _confidence, value))
                {
                    OnPropertyChanged(nameof(ConfidenceText));
                }
            }
        }

        private DateTimeOffset _timestamp;
        public DateTimeOffset Timestamp
        {
            get => _timestamp;
            set
            {
                if (SetProperty(ref _timestamp, value))
                {
                    OnPropertyChanged(nameof(TimestampText));
                }
            }
        }

        private bool _isIntrusion;
        public bool IsIntrusion
        {
            get => _isIntrusion;
            set
            {
                if (SetProperty(ref _isIntrusion, value))
                {
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private string _objectClass = string.Empty;
        public string ObjectClass
        {
            get => _objectClass;
            set => SetProperty(ref _objectClass, value);
        }

        public string TimestampText
        {
            get
            {
                var diff = DateTimeOffset.Now - _timestamp;
                return diff.TotalSeconds < 60
                            ? $"{(int)diff.TotalSeconds}s trước"
                 : $"{(int)diff.TotalMinutes}m trước";
            }
        }

        public string ConfidenceText => $"{_confidence * 100:F0}%";

        public SolidColorBrush StatusColor =>
         _isIntrusion
      ? new SolidColorBrush(Colors.Red)
  : new SolidColorBrush(Colors.Green);

        public string StatusText => _isIntrusion ? "Xâm nhập" : "Bình thường";
    }

    public class SensorStatisticsViewModel
    {
        public string DeviceName { get; set; } = string.Empty;
        public string SensorType { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public double AvgValue { get; set; }
        public string Status { get; set; } = string.Empty;
        public SolidColorBrush StatusColor { get; set; } = new(Colors.Gray);
        public string TrendIcon { get; set; } = "→";
        public SolidColorBrush TrendColor { get; set; } = new(Colors.Gray);

        public string CurrentValueText => $"{CurrentValue:F1} {Unit}";
        public string RangeText => $"{MinValue:F1} - {MaxValue:F1}";
        public string AvgValueText => $"TB: {AvgValue:F1}";
    }
}

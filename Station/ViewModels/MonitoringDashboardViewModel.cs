using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Linq;
using Station.Models;

namespace Station.ViewModels
{
    public partial class MonitoringDashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private string currentTime = DateTime.Now.ToString("HH:mm:ss");

        [ObservableProperty]
        private string currentDate = DateTime.Now.ToString("dd/MM/yyyy");

        [ObservableProperty]
        private int totalNodes = 42;

        [ObservableProperty]
        private int activeNodes = 38;

        [ObservableProperty]
        private int offlineNodes = 4;

        [ObservableProperty]
        private int todayAlerts = 127;

        [ObservableProperty]
        private int criticalAlerts = 8;

        [ObservableProperty]
        private int warningAlerts = 23;

        [ObservableProperty]
        private double averageTemperature = 28.5;

        [ObservableProperty]
        private double maxTemperature = 42.3;

        [ObservableProperty]
        private string systemStatus = "Hoạt động bình thường";

        [ObservableProperty]
        private string networkLatency = "12ms";

        [ObservableProperty]
        private double cpuUsage = 45.2;

        [ObservableProperty]
        private double memoryUsage = 68.7;

        public ISeries[] AlertDistributionSeries { get; set; }

        // Mini chart series for sensor data
        public ISeries[] TemperatureSeries { get; set; }
        public ISeries[] HumiditySeries { get; set; }
        public ISeries[] VibrationSeries { get; set; }
        public ISeries[] WaterLevelSeries { get; set; }
        public ISeries[] LightSeries { get; set; }

        // Hidden axes for mini charts
        public System.Collections.Generic.IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> HiddenXAxis { get; set; }
        public System.Collections.Generic.IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> HiddenYAxis { get; set; }

        // Alert type counts
        [ObservableProperty]
        private int intruAlerts = 0;

        [ObservableProperty]
        private int motionAlerts = 0;

        [ObservableProperty]
        private int fireAlerts = 0;

        [ObservableProperty]
        private int smokeAlerts = 0;

        [ObservableProperty]
        private int waterAlerts = 0;

        private readonly AlertsViewModel _alertsViewModel;

        public ObservableCollection<NodeStatus> Nodes { get; } = new();
        public ObservableCollection<RealtimeAlert> RealtimeAlerts { get; } = new();
        public ObservableCollection<TemperatureReading> TemperatureReadings { get; } = new();
        public ObservableCollection<CameraStatus> Cameras { get; } = new();

        public MonitoringDashboardViewModel()
        {
            _alertsViewModel = new AlertsViewModel();
            CalculateAlertDistribution();
            InitializeCharts();
            InitializeMiniCharts();
            InitializeSampleData();
            StartRealtimeUpdates();
        }

        private void CalculateAlertDistribution()
        {
            // Calculate alert distribution based on AlertsViewModel data
            var allAlerts = _alertsViewModel.AllAlerts;

            // Map severity levels to chart values
            IntruAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.Low);
            MotionAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.Medium);
            FireAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.High);
            SmokeAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.Critical);
            WaterAlerts = 0; // Not used in current dataset

            // Update total alerts
            TodayAlerts = allAlerts.Count;
            CriticalAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.Critical);
            WarningAlerts = allAlerts.Count(a => a.Severity == AlertSeverity.High || a.Severity == AlertSeverity.Medium);
        }

        private void InitializeCharts()
        {
            // Alert Distribution Pie Chart with data from AlertsViewModel
            // Only show severity levels that have alerts
            var seriesList = new System.Collections.Generic.List<ISeries>();

            if (IntruAlerts > 0)
            {
                seriesList.Add(new PieSeries<int>
                {
                    Name = "Thấp",
                    Values = new int[] { IntruAlerts },
                    Fill = new SolidColorPaint(new SKColor(34, 197, 94)), // #22C55E - Green
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 16,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    HoverPushout = 15,
                    MaxRadialColumnWidth = double.MaxValue
                });
            }

            if (MotionAlerts > 0)
            {
                seriesList.Add(new PieSeries<int>
                {
                    Name = "Trung bình",
                    Values = new int[] { MotionAlerts },
                    Fill = new SolidColorPaint(new SKColor(234, 179, 8)), // #EAB308 - Yellow
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 16,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    HoverPushout = 15,
                    MaxRadialColumnWidth = double.MaxValue
                });
            }

            if (FireAlerts > 0)
            {
                seriesList.Add(new PieSeries<int>
                {
                    Name = "Cao",
                    Values = new int[] { FireAlerts },
                    Fill = new SolidColorPaint(new SKColor(249, 115, 22)), // #F97316 - Orange
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 16,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    HoverPushout = 15,
                    MaxRadialColumnWidth = double.MaxValue
                });
            }

            if (SmokeAlerts > 0)
            {
                seriesList.Add(new PieSeries<int>
                {
                    Name = "Nghiêm trọng",
                    Values = new int[] { SmokeAlerts },
                    Fill = new SolidColorPaint(new SKColor(239, 68, 68)), // #EF4444 - Red
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 16,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                    HoverPushout = 15,
                    MaxRadialColumnWidth = double.MaxValue
                });
            }

            AlertDistributionSeries = seriesList.ToArray();
        }

        private void InitializeMiniCharts()
        {
            var random = new Random();

            // Hidden axes configuration
            HiddenXAxis = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[]
            {
                new Axis
                {
                    IsVisible = false,
                    MinLimit = 0,
                    MaxLimit = 24
                }
            };

            HiddenYAxis = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[]
            {
                new Axis
                {
                    IsVisible = false
                }
            };

            // Temperature mini chart (last 24 points)
            var tempValues = new double[24];
            for (int i = 0; i < 24; i++)
            {
                tempValues[i] = 26 + random.NextDouble() * 6; // 26-32°C
            }
            TemperatureSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = tempValues,
                    Fill = new SolidColorPaint(new SKColor(239, 68, 68, 30)), // #EF4444 with transparency
                    Stroke = new SolidColorPaint(new SKColor(239, 68, 68)) { StrokeThickness = 2 },
                    GeometrySize = 0,
                    LineSmoothness = 0.65
                }
            };

            // Humidity mini chart
            var humidityValues = new double[24];
            for (int i = 0; i < 24; i++)
            {
                humidityValues[i] = 60 + random.NextDouble() * 15; // 60-75%
            }
            HumiditySeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = humidityValues,
                    Fill = new SolidColorPaint(new SKColor(59, 130, 246, 30)), // #3B82F6 with transparency
                    Stroke = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 2 },
                    GeometrySize = 0,
                    LineSmoothness = 0.65
                }
            };

            // Vibration mini chart
            var vibrationValues = new double[24];
            for (int i = 0; i < 24; i++)
            {
                vibrationValues[i] = 0.1 + random.NextDouble() * 0.5; // 0.1-0.6 m/s²
            }
            VibrationSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = vibrationValues,
                    Fill = new SolidColorPaint(new SKColor(245, 158, 11, 30)), // #F59E0B with transparency
                    Stroke = new SolidColorPaint(new SKColor(245, 158, 11)) { StrokeThickness = 2 },
                    GeometrySize = 0,
                    LineSmoothness = 0.65
                }
            };

            // Water Level mini chart
            var waterValues = new double[24];
            for (int i = 0; i < 24; i++)
            {
                waterValues[i] = 5 + random.NextDouble() * 15; // 5-20 cm
            }
            WaterLevelSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = waterValues,
                    Fill = new SolidColorPaint(new SKColor(239, 68, 68, 30)), // #EF4444 with transparency
                    Stroke = new SolidColorPaint(new SKColor(239, 68, 68)) { StrokeThickness = 2 },
                    GeometrySize = 0,
                    LineSmoothness = 0.65
                }
            };

            // Light Level mini chart
            var lightValues = new double[24];
            for (int i = 0; i < 24; i++)
            {
                lightValues[i] = 150 + random.NextDouble() * 50; // 150-200 lux
            }
            LightSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = lightValues,
                    Fill = new SolidColorPaint(new SKColor(139, 92, 246, 30)), // #8B5CF6 with transparency
                    Stroke = new SolidColorPaint(new SKColor(139, 92, 246)) { StrokeThickness = 2 },
                    GeometrySize = 0,
                    LineSmoothness = 0.65
                }
            };
        }

        private void InitializeSampleData()
        {
            // Sample Nodes
            var random = new Random();
            for (int i = 1; i <= 12; i++)
            {
                Nodes.Add(new NodeStatus
                {
                    NodeId = $"NODE-{i:D3}",
                    Location = $"Khu vực {(char)('A' + (i - 1) / 4)}-{((i - 1) % 4) + 1}",
                    IsOnline = random.Next(100) > 10,
                    Temperature = 20 + random.Next(25),
                    LastUpdate = DateTime.Now.AddMinutes(-random.Next(30))
                });
            }

            // Sample Cameras
            for (int i = 1; i <= 8; i++)
            {
                Cameras.Add(new CameraStatus
                {
                    CameraId = $"CAM-{i:D2}",
                    Location = $"Cổng {i}",
                    IsRecording = random.Next(100) > 5,
                    Fps = 25 + random.Next(5),
                    Resolution = "1920x1080"
                });
            }

            // Sample Temperature Readings
            for (int i = 0; i < 24; i++)
            {
                TemperatureReadings.Add(new TemperatureReading
                {
                    Hour = i,
                    Temperature = 20 + random.Next(15) + random.NextDouble()
                });
            }

            // Sample Realtime Alerts
            AddRealtimeAlert("NODE-005", "Phát hiện chuyển động bất thường", "high");
            AddRealtimeAlert("CAM-03", "Mất tín hiệu camera", "critical");
            AddRealtimeAlert("NODE-012", "Nhiệt độ vượt ngưỡng", "warning");
            AddRealtimeAlert("NODE-008", "Cảnh báo radar", "high");
        }

        private void AddRealtimeAlert(string source, string message, string severity)
        {
            var alert = new RealtimeAlert
            {
                Source = source,
                Message = message,
                Timestamp = DateTime.Now,
                Severity = severity
            };

            RealtimeAlerts.Insert(0, alert);
            if (RealtimeAlerts.Count > 10)
            {
                RealtimeAlerts.RemoveAt(RealtimeAlerts.Count - 1);
            }
        }

        private async void StartRealtimeUpdates()
        {
            var random = new Random();
            
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(2000); // Update every 2 seconds

                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
                CurrentDate = DateTime.Now.ToString("dd/MM/yyyy");

                // Update metrics
                CpuUsage = 40 + random.Next(20) + random.NextDouble();
                MemoryUsage = 65 + random.Next(10) + random.NextDouble();
                AverageTemperature = 26 + random.Next(6) + random.NextDouble();
                MaxTemperature = 38 + random.Next(8) + random.NextDouble();

                // Update alert counts with random variations
                if (random.Next(100) < 20) // 20% chance
                {
                    IntruAlerts = Math.Max(1, IntruAlerts + random.Next(-1, 2));
                    MotionAlerts = Math.Max(1, MotionAlerts + random.Next(-1, 2));
                    FireAlerts = Math.Max(1, FireAlerts + random.Next(-1, 2));
                    SmokeAlerts = Math.Max(1, SmokeAlerts + random.Next(-1, 2));
                    
                    // Refresh pie chart
                    InitializeCharts();
                    OnPropertyChanged(nameof(AlertDistributionSeries));
                }

                // Update mini charts - shift data and add new point
                UpdateMiniChart(TemperatureSeries, 26 + random.NextDouble() * 6);
                UpdateMiniChart(HumiditySeries, 60 + random.NextDouble() * 15);
                UpdateMiniChart(VibrationSeries, 0.1 + random.NextDouble() * 0.5);
                UpdateMiniChart(WaterLevelSeries, 5 + random.NextDouble() * 15);
                UpdateMiniChart(LightSeries, 150 + random.NextDouble() * 50);

                // Update node statuses
                foreach (var node in Nodes)
                {
                    if (random.Next(100) < 10) // 10% chance to update
                    {
                        node.Temperature = 20 + random.Next(25) + random.NextDouble();
                        node.LastUpdate = DateTime.Now;
                    }
                }

                // Random alerts
                if (random.Next(100) < 8) // 8% chance per update
                {
                    var messages = new[]
                    {
                        "Phát hiện chuyển động",
                        "Cảnh báo nhiệt độ",
                        "Tín hiệu yếu",
                        "Kết nối không ổn định",
                        "Độ ẩm cao bất thường",
                        "Rung động vượt ngưỡng",
                        "Mất kết nối cảm biến",
                        "Camera mờ hình"
                    };
                    var severities = new[] { "warning", "high", "critical", "low" };

                    AddRealtimeAlert(
                        $"NODE-{random.Next(1, 13):D3}",
                        messages[random.Next(messages.Length)],
                        severities[random.Next(severities.Length)]
                    );

                    TodayAlerts++;
                }
            }
        }

        private void UpdateMiniChart(ISeries[] series, double newValue)
        {
            if (series == null || series.Length == 0) return;

            var lineSeries = series[0] as LineSeries<double>;
            if (lineSeries?.Values is double[] values)
            {
                // Shift all values left and add new value at the end
                var newValues = new double[values.Length];
                Array.Copy(values, 1, newValues, 0, values.Length - 1);
                newValues[values.Length - 1] = newValue;
                
                lineSeries.Values = newValues;
            }
        }
    }

    public class NodeStatus
    {
        public string NodeId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public double Temperature { get; set; }
        public DateTime LastUpdate { get; set; }

        public SolidColorBrush StatusBrush => IsOnline
       ? new SolidColorBrush(Color.FromArgb(255, 63, 207, 142)) // #3FCF8E
          : new SolidColorBrush(Color.FromArgb(255, 123, 126, 133)); // #7B7E85

        public SolidColorBrush TemperatureBrush => Temperature > 35
               ? new SolidColorBrush(Color.FromArgb(255, 240, 98, 93)) // #F0625D
               : new SolidColorBrush(Color.FromArgb(255, 154, 166, 178)); // #9AA6B2

        public string StatusText => IsOnline ? "Online" : "Offline";
        public string TemperatureText => $"{Temperature:F1}°C";
        public string LastUpdateText => $"{(DateTime.Now - LastUpdate).TotalMinutes:F0}m trước";
    }

    public class RealtimeAlert
    {
        public string Source { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Severity { get; set; } = "low";

        public string TimeText => Timestamp.ToString("HH:mm:ss");

        public SolidColorBrush SeverityBrush => Severity switch
        {
            "critical" => new SolidColorBrush(Color.FromArgb(255, 240, 98, 93)), // #F0625D
            "high" => new SolidColorBrush(Color.FromArgb(255, 255, 209, 102)), // #FFD166
            "warning" => new SolidColorBrush(Color.FromArgb(255, 255, 209, 102)), // #FFD166
            _ => new SolidColorBrush(Color.FromArgb(255, 154, 166, 178)) // #9AA6B2
        };

        public string SeverityIcon => Severity switch
        {
            "critical" => "🔴",
            "high" => "⚠️",
            "warning" => "⚡",
            _ => "ℹ️"
        };
    }

    public class TemperatureReading
    {
        public int Hour { get; set; }
        public double Temperature { get; set; }
        public string HourLabel => $"{Hour:D2}:00";
    }

    public class CameraStatus
    {
        public string CameraId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsRecording { get; set; }
        public int Fps { get; set; }
        public string Resolution { get; set; } = string.Empty;

        public SolidColorBrush StatusBrush => IsRecording
     ? new SolidColorBrush(Color.FromArgb(255, 63, 207, 142)) // #3FCF8E
    : new SolidColorBrush(Color.FromArgb(255, 123, 126, 133)); // #7B7E85

        public string StatusText => IsRecording ? $"Recording • {Fps} FPS" : "Offline";
    }
}

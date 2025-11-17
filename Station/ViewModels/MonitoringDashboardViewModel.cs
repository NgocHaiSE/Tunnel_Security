using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Windows.UI;
using Microsoft.UI.Xaml.Media;

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

        public ObservableCollection<NodeStatus> Nodes { get; } = new();
        public ObservableCollection<RealtimeAlert> RealtimeAlerts { get; } = new();
        public ObservableCollection<TemperatureReading> TemperatureReadings { get; } = new();
        public ObservableCollection<CameraStatus> Cameras { get; } = new();

        public MonitoringDashboardViewModel()
        {
            InitializeSampleData();
            StartRealtimeUpdates();
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
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(1000);

                currentTime = DateTime.Now.ToString("HH:mm:ss");

                // Simulate random updates
                var random = new Random();
                if (random.Next(100) < 5) // 5% chance per second
                {
                    var messages = new[]
                    {
   "Phát hiện chuyển động",
     "Cảnh báo nhiệt độ",
       "Tín hiệu yếu",
       "Kết nối không ổn định"
      };
                    var severities = new[] { "warning", "high", "critical" };

                    AddRealtimeAlert(
                $"NODE-{random.Next(1, 13):D3}",
                  messages[random.Next(messages.Length)],
                 severities[random.Next(severities.Length)]
                   );
                }

                // Update metrics
                cpuUsage = 40 + random.Next(20) + random.NextDouble();
                memoryUsage = 65 + random.Next(10) + random.NextDouble();
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

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Station.Models;
using Station.Services;

namespace Station.ViewModels
{
    public partial class MonitoringDashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private string currentTime = DateTime.Now.ToString("HH:mm:ss");

        [ObservableProperty]
        private string currentDate = DateTime.Now.ToString("dd/MM/yyyy");

        [ObservableProperty]
        private int totalNodes;

        [ObservableProperty]
        private int activeNodes;

        [ObservableProperty]
        private int offlineNodes;

        [ObservableProperty]
        private int todayAlerts;

        [ObservableProperty]
        private int criticalAlerts;

        [ObservableProperty]
        private int warningAlerts;

        [ObservableProperty]
        private double averageTemperature;

        [ObservableProperty]
        private double maxTemperature;

        [ObservableProperty]
        private string systemStatus = "Hoạt động bình thường";

        [ObservableProperty]
        private string networkLatency = "12ms";

        [ObservableProperty]
        private double cpuUsage = 45.2;

        [ObservableProperty]
        private double memoryUsage = 68.7;

        public ISeries[] AlertDistributionSeries { get; set; } = Array.Empty<ISeries>();

        // Mini chart series for sensor data
        public ISeries[] TemperatureSeries { get; set; } = Array.Empty<ISeries>();
        public ISeries[] HumiditySeries { get; set; } = Array.Empty<ISeries>();
        public ISeries[] VibrationSeries { get; set; } = Array.Empty<ISeries>();
        public ISeries[] WaterLevelSeries { get; set; } = Array.Empty<ISeries>();
        public ISeries[] LightSeries { get; set; } = Array.Empty<ISeries>();

        // Hidden axes for mini charts
        public System.Collections.Generic.IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> HiddenXAxis { get; set; }
            = Array.Empty<LiveChartsCore.Kernel.Sketches.ICartesianAxis>();
        public System.Collections.Generic.IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> HiddenYAxis { get; set; }
            = Array.Empty<LiveChartsCore.Kernel.Sketches.ICartesianAxis>();

        // Alert type counts
        [ObservableProperty]
        private int intruAlerts;

        [ObservableProperty]
        private int motionAlerts;

        [ObservableProperty]
        private int fireAlerts;

        [ObservableProperty]
        private int smokeAlerts;

        [ObservableProperty]
        private int waterAlerts;

        // Device counts
        [ObservableProperty]
        private int totalCameras;

        [ObservableProperty]
        private int onlineCameras;

        [ObservableProperty]
        private int totalSensors;

        [ObservableProperty]
        private int onlineSensors;

        [ObservableProperty]
        private double systemHealth;

        [ObservableProperty]
        private string systemHealthText = "0%";

        [ObservableProperty]
        private string cameraCountText = "0 / 0";

        [ObservableProperty]
        private string sensorCountText = "0 / 0";

        [ObservableProperty]
        private int anomalyCount;

        [ObservableProperty]
        private int activeAlertCount;

        // Sensor category averages
        [ObservableProperty]
        private double averageHumidity;

        [ObservableProperty]
        private double averageVibration;

        [ObservableProperty]
        private double averageGasLevel;

        [ObservableProperty]
        private double averageWaterLevel;

        [ObservableProperty]
        private string averageHumidityText = "0%";

        [ObservableProperty]
        private string averageVibrationText = "0 mm/s";

        [ObservableProperty]
        private string averageGasLevelText = "0 ppm";

        public ObservableCollection<NodeStatus> Nodes { get; } = new();
        public ObservableCollection<RealtimeAlert> RealtimeAlerts { get; } = new();
        public ObservableCollection<TemperatureReading> TemperatureReadings { get; } = new();
        public ObservableCollection<CameraStatus> Cameras { get; } = new();

        private readonly MockDataService _mock = MockDataService.Instance;
        private readonly DispatcherQueue? _dispatcherQueue;
        private readonly Random _rng = new();

        public MonitoringDashboardViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            InitializeMiniCharts();
            LoadFromMockData();
            RefreshAlertCounts();
            InitializeCharts();

            _mock.SensorTick += OnSensorTick;
            _mock.AlertGenerated += OnAlertGenerated;

            StartClockUpdates();
        }

        // ── Load initial state from MockDataService ──────────────────

        private void LoadFromMockData()
        {
            var sensors = _mock.Sensors;
            var cameras = _mock.Cameras;

            var allNodes = _mock.Lines.SelectMany(l => l.Nodes).ToList();
            TotalNodes   = allNodes.Count;
            ActiveNodes  = allNodes.Count(n => sensors.Any(s => s.NodeId == n.NodeId && s.IsOnline));
            OfflineNodes = allNodes.Count(n => sensors.All(s => s.NodeId == n.NodeId && !s.IsOnline));

            TotalCameras = cameras.Count;
            OnlineCameras = cameras.Count(c => c.IsOnline);
            TotalSensors = sensors.Count;
            OnlineSensors = sensors.Count(s => s.IsOnline);
            int totalDevices = TotalCameras + TotalSensors;
            SystemHealth = totalDevices > 0
                ? Math.Round((OnlineCameras + OnlineSensors) * 100.0 / totalDevices, 0)
                : 0;
            SystemHealthText = $"{SystemHealth:F0}%";
            CameraCountText = $"{OnlineCameras} / {TotalCameras}";
            SensorCountText = $"{OnlineSensors} / {TotalSensors}";
            AnomalyCount = sensors.Count(s => s.CurrentLevel >= SensorAlertLevel.Warning);
            ActiveAlertCount = _mock.ActiveAlerts.Count;

            var tempSensors = sensors.Where(s => s.Category == AlertCategory.Temperature).ToList();
            AverageTemperature = tempSensors.Count > 0 ? Math.Round(tempSensors.Average(s => s.CurrentValue), 1) : 0;
            MaxTemperature = tempSensors.Count > 0 ? Math.Round(tempSensors.Max(s => s.CurrentValue), 1) : 0;

            var humSensors = sensors.Where(s => s.Category == AlertCategory.Humidity).ToList();
            AverageHumidity = humSensors.Count > 0 ? Math.Round(humSensors.Average(s => s.CurrentValue), 1) : 0;
            AverageHumidityText = $"{AverageHumidity:F0}%";

            var accSensors = sensors.Where(s => s.Category == AlertCategory.Accelerometer).ToList();
            AverageVibration     = accSensors.Count > 0 ? Math.Round(accSensors.Average(s => s.CurrentValue), 2) : 0;
            AverageVibrationText = $"{AverageVibration:F2} m/s²";

            var radSensors = sensors.Where(s => s.Category == AlertCategory.Radar).ToList();
            AverageGasLevel     = radSensors.Count > 0 ? Math.Round(radSensors.Average(s => s.CurrentValue), 1) : 0;
            AverageGasLevelText = $"{AverageGasLevel:F0}%";

            // Nodes
            Nodes.Clear();
            foreach (var node in allNodes)
            {
                var nodeSensors = sensors.Where(s => s.NodeId == node.NodeId).ToList();
                var tempSensor = nodeSensors.FirstOrDefault(s => s.Category == AlertCategory.Temperature);
                Nodes.Add(new NodeStatus
                {
                    NodeId = node.NodeId,
                    Location = node.NodeName,
                    IsOnline = nodeSensors.Any(s => s.IsOnline),
                    Temperature = tempSensor != null ? Math.Round(tempSensor.CurrentValue, 1) : 0,
                    LastUpdate = DateTime.Now
                });
            }

            // Cameras
            Cameras.Clear();
            foreach (var cam in cameras)
            {
                Cameras.Add(new CameraStatus
                {
                    CameraId = cam.CameraId,
                    Location = cam.Location,
                    IsRecording = cam.IsOnline,
                    Fps = 25,
                    Resolution = "1920x1080"
                });
            }

            // Temperature readings (hourly baseline using nominal values)
            TemperatureReadings.Clear();
            double baseTemp = tempSensors.Count > 0 ? tempSensors.Average(s => s.NominalValue) : 24.0;
            for (int i = 0; i < 24; i++)
            {
                TemperatureReadings.Add(new TemperatureReading
                {
                    Hour = i,
                    Temperature = baseTemp
                });
            }

            // Recent alerts from history
            RealtimeAlerts.Clear();
            foreach (var alert in _mock.AlertHistory.Take(10))
            {
                RealtimeAlerts.Add(new RealtimeAlert
                {
                    Source = alert.NodeId,
                    Message = alert.Title,
                    Timestamp = alert.CreatedAt.DateTime,
                    Severity = SeverityToString(alert.Severity)
                });
            }
        }

        private void RefreshAlertCounts()
        {
            var history = _mock.AlertHistory;
            TodayAlerts = history.Count;
            CriticalAlerts = history.Count(a => a.Severity == AlertSeverity.Critical);
            WarningAlerts = history.Count(a => a.Severity == AlertSeverity.High || a.Severity == AlertSeverity.Medium);

            IntruAlerts = history.Count(a => a.Severity == AlertSeverity.Low);
            MotionAlerts = history.Count(a => a.Severity == AlertSeverity.Medium);
            FireAlerts = history.Count(a => a.Severity == AlertSeverity.High);
            SmokeAlerts = history.Count(a => a.Severity == AlertSeverity.Critical);
            WaterAlerts = 0;
        }

        // ── Live event handlers ───────────────────────────────────────

        private void OnSensorTick(object? sender, SensorTickEventArgs e)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                switch (e.Sensor.Category)
                {
                    case AlertCategory.Temperature:
                        UpdateMiniChart(TemperatureSeries, e.NewValue);
                        var tempSensors = _mock.Sensors.Where(s => s.Category == AlertCategory.Temperature).ToList();
                        if (tempSensors.Count > 0)
                        {
                            AverageTemperature = Math.Round(tempSensors.Average(s => s.CurrentValue), 1);
                            MaxTemperature = Math.Round(tempSensors.Max(s => s.CurrentValue), 1);
                        }
                        var node = Nodes.FirstOrDefault(n => n.NodeId == e.Sensor.NodeId);
                        if (node != null)
                        {
                            node.Temperature = Math.Round(e.NewValue, 1);
                            node.LastUpdate = DateTime.Now;
                        }
                        // Update hourly temperature reading for current hour
                        int hour = DateTime.Now.Hour;
                        if (hour < TemperatureReadings.Count)
                            TemperatureReadings[hour].Temperature = Math.Round(e.NewValue, 1);
                        break;

                    case AlertCategory.Humidity:
                        UpdateMiniChart(HumiditySeries, e.NewValue);
                        var humSensors = _mock.Sensors.Where(s => s.Category == AlertCategory.Humidity).ToList();
                        if (humSensors.Count > 0)
                        {
                            AverageHumidity = Math.Round(humSensors.Average(s => s.CurrentValue), 1);
                            AverageHumidityText = $"{AverageHumidity:F0}%";
                        }
                        break;

                    case AlertCategory.Accelerometer:
                        UpdateMiniChart(VibrationSeries, e.NewValue);
                        var accSensors2 = _mock.Sensors.Where(s => s.Category == AlertCategory.Accelerometer).ToList();
                        if (accSensors2.Count > 0)
                        {
                            AverageVibration     = Math.Round(accSensors2.Average(s => s.CurrentValue), 2);
                            AverageVibrationText = $"{AverageVibration:F2} m/s²";
                        }
                        break;

                    case AlertCategory.Light:
                        UpdateMiniChart(LightSeries, e.NewValue);
                        break;

                    case AlertCategory.Radar:
                    case AlertCategory.Infrared:
                        UpdateMiniChart(WaterLevelSeries, e.NewValue);
                        var radSensors2 = _mock.Sensors.Where(s => s.Category == AlertCategory.Radar).ToList();
                        if (radSensors2.Count > 0)
                        {
                            AverageGasLevel     = Math.Round(radSensors2.Average(s => s.CurrentValue), 1);
                            AverageGasLevelText = $"{AverageGasLevel:F0}%";
                        }
                        break;

                    default:
                        UpdateMiniChart(LightSeries, e.NewValue);
                        break;
                }
            });
        }

        private void OnAlertGenerated(object? sender, AlertGeneratedEventArgs e)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                RefreshAlertCounts();
                AnomalyCount = _mock.Sensors.Count(s => s.CurrentLevel >= SensorAlertLevel.Warning);
                ActiveAlertCount = _mock.ActiveAlerts.Count;
                InitializeCharts();
                OnPropertyChanged(nameof(AlertDistributionSeries));

                RealtimeAlerts.Insert(0, new RealtimeAlert
                {
                    Source = e.Alert.NodeId,
                    Message = e.Alert.Title,
                    Timestamp = e.Alert.CreatedAt.DateTime,
                    Severity = SeverityToString(e.Alert.Severity)
                });
                if (RealtimeAlerts.Count > 10)
                    RealtimeAlerts.RemoveAt(RealtimeAlerts.Count - 1);

                SystemStatus = CriticalAlerts > 0 ? "Có cảnh báo khẩn cấp" : "Hoạt động bình thường";
            });
        }

        // ── Clock + system metrics (still simulated) ─────────────────

        private async void StartClockUpdates()
        {
            while (true)
            {
                await System.Threading.Tasks.Task.Delay(2000);
                _dispatcherQueue?.TryEnqueue(() =>
                {
                    CurrentTime = DateTime.Now.ToString("HH:mm:ss");
                    CurrentDate = DateTime.Now.ToString("dd/MM/yyyy");

                    // CPU/RAM are OS-level — keep as simulated approximation
                    CpuUsage = Math.Round(40 + _rng.NextDouble() * 20, 1);
                    MemoryUsage = Math.Round(65 + _rng.NextDouble() * 10, 1);
                    NetworkLatency = $"{10 + _rng.Next(10)}ms";
                });
            }
        }

        // ── Chart builders ────────────────────────────────────────────

        private void InitializeCharts()
        {
            var seriesList = new System.Collections.Generic.List<ISeries>();

            if (IntruAlerts > 0)
                seriesList.Add(MakePieSeries("Thấp", IntruAlerts, new SKColor(34, 197, 94)));

            if (MotionAlerts > 0)
                seriesList.Add(MakePieSeries("Trung bình", MotionAlerts, new SKColor(234, 179, 8)));

            if (FireAlerts > 0)
                seriesList.Add(MakePieSeries("Cao", FireAlerts, new SKColor(249, 115, 22)));

            if (SmokeAlerts > 0)
                seriesList.Add(MakePieSeries("Nghiêm trọng", SmokeAlerts, new SKColor(239, 68, 68)));

            // Show a placeholder if there are no alerts yet
            if (seriesList.Count == 0)
                seriesList.Add(MakePieSeries("Không có cảnh báo", 1, new SKColor(75, 85, 99)));

            AlertDistributionSeries = seriesList.ToArray();
        }

        private static PieSeries<int> MakePieSeries(string name, int value, SKColor color) =>
            new()
            {
                Name = name,
                Values = new int[] { value },
                Fill = new SolidColorPaint(color),
                DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                DataLabelsSize = 16,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
                HoverPushout = 15,
                MaxRadialColumnWidth = double.MaxValue
            };

        private void InitializeMiniCharts()
        {
            HiddenXAxis = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[]
            {
                new Axis { IsVisible = false, MinLimit = 0, MaxLimit = 24 }
            };
            HiddenYAxis = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[]
            {
                new Axis { IsVisible = false }
            };

            TemperatureSeries = MakeMiniChart(FillArray(24, 24.0), new SKColor(239, 68, 68));
            HumiditySeries    = MakeMiniChart(FillArray(24, 55.0), new SKColor(59, 130, 246));
            VibrationSeries   = MakeMiniChart(FillArray(24, 5.0),  new SKColor(245, 158, 11));
            WaterLevelSeries  = MakeMiniChart(FillArray(24, 8.0),  new SKColor(239, 68, 68));
            LightSeries       = MakeMiniChart(FillArray(24, 150.0),new SKColor(139, 92, 246));
        }

        private static ISeries[] MakeMiniChart(double[] values, SKColor color) =>
            new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(new SKColor(color.Red, color.Green, color.Blue, 30)),
                    Stroke = new SolidColorPaint(color) { StrokeThickness = 2 },
                    GeometrySize = 0,
                    LineSmoothness = 0.65
                }
            };

        private static double[] FillArray(int length, double value)
        {
            var arr = new double[length];
            Array.Fill(arr, value);
            return arr;
        }

        private void UpdateMiniChart(ISeries[] series, double newValue)
        {
            if (series == null || series.Length == 0) return;
            if (series[0] is LineSeries<double> line && line.Values is double[] values)
            {
                var newValues = new double[values.Length];
                Array.Copy(values, 1, newValues, 0, values.Length - 1);
                newValues[values.Length - 1] = newValue;
                line.Values = newValues;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private static string SeverityToString(AlertSeverity severity) => severity switch
        {
            AlertSeverity.Critical => "critical",
            AlertSeverity.High     => "high",
            AlertSeverity.Medium   => "warning",
            _                      => "low"
        };
    }

    public class NodeStatus
    {
        public string NodeId { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public double Temperature { get; set; }
        public DateTime LastUpdate { get; set; }

        public SolidColorBrush StatusBrush => IsOnline
            ? new SolidColorBrush(Color.FromArgb(255, 63, 207, 142))
            : new SolidColorBrush(Color.FromArgb(255, 123, 126, 133));

        public SolidColorBrush TemperatureBrush => Temperature > 35
            ? new SolidColorBrush(Color.FromArgb(255, 240, 98, 93))
            : new SolidColorBrush(Color.FromArgb(255, 154, 166, 178));

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
            "critical" => new SolidColorBrush(Color.FromArgb(255, 240, 98, 93)),
            "high"     => new SolidColorBrush(Color.FromArgb(255, 255, 209, 102)),
            "warning"  => new SolidColorBrush(Color.FromArgb(255, 255, 209, 102)),
            _          => new SolidColorBrush(Color.FromArgb(255, 154, 166, 178))
        };

        public string SeverityIcon => Severity switch
        {
            "critical" => "🔴",
            "high"     => "⚠️",
            "warning"  => "⚡",
            _          => "ℹ️"
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
            ? new SolidColorBrush(Color.FromArgb(255, 63, 207, 142))
            : new SolidColorBrush(Color.FromArgb(255, 123, 126, 133));

        public string StatusText => IsRecording ? $"Recording • {Fps} FPS" : "Offline";
    }
}

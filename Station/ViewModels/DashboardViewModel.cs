using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Station.Models;
using Station.Services;

namespace Station.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private Timer? _clockTimer;
        private readonly DispatcherQueue? _dispatcherQueue;
        private readonly MockDataService _mock = MockDataService.Instance;

        // Trend chart: 24 slots (one per hour), updated when alerts arrive
        private readonly ObservableCollection<double> _trendHourlyData;

        // Pie chart live collections
        private readonly ObservableCollection<int> _pieLow     = new() { 0 };
        private readonly ObservableCollection<int> _pieMedium  = new() { 0 };
        private readonly ObservableCollection<int> _pieHigh    = new() { 0 };
        private readonly ObservableCollection<int> _pieCritical = new() { 0 };

        // Station Info
        private string _stationName = "Trạm Giám Sát Nghĩa Đô";
        public string StationName
        {
            get => _stationName;
            set => SetProperty(ref _stationName, value);
        }

        private string _lineName = "Tuyến hầm chính";
        public string LineName
        {
            get => _lineName;
            set => SetProperty(ref _lineName, value);
        }

        private string _currentTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        // Statistics
        private int _totalDevices;
        public int TotalDevices
        {
            get => _totalDevices;
            set => SetProperty(ref _totalDevices, value);
        }

        private int _onlineDevices;
        public int OnlineDevices
        {
            get => _onlineDevices;
            set => SetProperty(ref _onlineDevices, value);
        }

        private int _offlineDevices;
        public int OfflineDevices
        {
            get => _offlineDevices;
            set => SetProperty(ref _offlineDevices, value);
        }

        private int _todayAlerts;
        public int TodayAlerts
        {
            get => _todayAlerts;
            set => SetProperty(ref _todayAlerts, value);
        }

        private string _todayAlertsChange = "Hôm nay";
        public string TodayAlertsChange
        {
            get => _todayAlertsChange;
            set => SetProperty(ref _todayAlertsChange, value);
        }

        private int _unprocessedAlerts;
        public int UnprocessedAlerts
        {
            get => _unprocessedAlerts;
            set => SetProperty(ref _unprocessedAlerts, value);
        }

        // Connection Status
        private string _connectionStatus = "Đang kết nối";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        private SolidColorBrush _connectionStatusColor = new SolidColorBrush(Colors.Green);
        public SolidColorBrush ConnectionStatusColor
        {
            get => _connectionStatusColor;
            set => SetProperty(ref _connectionStatusColor, value);
        }

        private string _lastHeartbeat = "vừa xong";
        public string LastHeartbeat
        {
            get => _lastHeartbeat;
            set => SetProperty(ref _lastHeartbeat, value);
        }

        // Alert Severity Counts
        private int _lowAlerts;
        public int LowAlerts
        {
            get => _lowAlerts;
            set => SetProperty(ref _lowAlerts, value);
        }

        private int _mediumAlerts;
        public int MediumAlerts
        {
            get => _mediumAlerts;
            set => SetProperty(ref _mediumAlerts, value);
        }

        private int _highAlerts;
        public int HighAlerts
        {
            get => _highAlerts;
            set => SetProperty(ref _highAlerts, value);
        }

        private int _criticalAlerts;
        public int CriticalAlerts
        {
            get => _criticalAlerts;
            set => SetProperty(ref _criticalAlerts, value);
        }

        private int _recentAlertsCount;
        public int RecentAlertsCount
        {
            get => _recentAlertsCount;
            set => SetProperty(ref _recentAlertsCount, value);
        }

        // LiveCharts
        public ISeries[] AlertsTrendSeries { get; set; }
        public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> AlertsTrendXAxes { get; set; }
        public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> AlertsTrendYAxes { get; set; }
        public ISeries[] SeverityDistributionSeries { get; set; }

        // Collections
        public ObservableCollection<DeviceStatusItem> Devices { get; } = new();
        public ObservableCollection<RecentAlertItem> RecentAlerts { get; } = new();

        public DashboardViewModel()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // Init 24-slot trend with zeros
            _trendHourlyData = new ObservableCollection<double>(new double[24]);

            InitializeLiveCharts();
            LoadFromMockData();
            StartClock();

            _mock.AlertGenerated += OnAlertGenerated;
            _mock.SensorTick     += OnSensorTick;
        }

        // ── Load initial state from MockDataService ───────────────────────

        private void LoadFromMockData()
        {
            // Device counts: 16 sensors + 16 cameras
            int sensorOnline  = _mock.Sensors.Count(s => s.IsOnline);
            int sensorOffline = _mock.Sensors.Count(s => !s.IsOnline);
            int camOnline     = _mock.Cameras.Count(c => c.IsOnline);
            int camOffline    = _mock.Cameras.Count(c => !c.IsOnline);

            TotalDevices   = _mock.Sensors.Count + _mock.Cameras.Count;
            OnlineDevices  = sensorOnline + camOnline;
            OfflineDevices = sensorOffline + camOffline;

            // Populate Devices list (sensors first, then cameras — cap at 8)
            Devices.Clear();
            foreach (var s in _mock.Sensors.Take(4))
                Devices.Add(MakeSensorDevice(s));
            foreach (var c in _mock.Cameras.Take(4))
                Devices.Add(MakeCameraDevice(c));

            // Alert stats
            RefreshAlertCounts();

            // Recent alerts (up to 5 latest from history)
            RecentAlerts.Clear();
            foreach (var a in _mock.AlertHistory.Take(5))
                RecentAlerts.Add(MakeRecentAlertItem(a));
        }

        private void RefreshAlertCounts()
        {
            var history = _mock.AlertHistory;
            TodayAlerts       = history.Count;
            UnprocessedAlerts = _mock.ActiveAlerts.Count;
            RecentAlertsCount = _mock.ActiveAlerts.Count;

            int low = 0, med = 0, high = 0, crit = 0;
            foreach (var a in history)
            {
                switch (a.Severity)
                {
                    case AlertSeverity.Low:      low++;  break;
                    case AlertSeverity.Medium:   med++;  break;
                    case AlertSeverity.High:     high++; break;
                    case AlertSeverity.Critical: crit++; break;
                }
            }

            LowAlerts      = low;
            MediumAlerts   = med;
            HighAlerts     = high;
            CriticalAlerts = crit;

            // Update live pie collections
            _pieLow[0]      = low;
            _pieMedium[0]   = med;
            _pieHigh[0]     = high;
            _pieCritical[0] = crit;
        }

        // ── Event handlers ────────────────────────────────────────────────

        private void OnAlertGenerated(object? sender, AlertGeneratedEventArgs e)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                var a = e.Alert;

                // Stats
                TodayAlerts++;
                UnprocessedAlerts = _mock.ActiveAlerts.Count;
                RecentAlertsCount = _mock.ActiveAlerts.Count;

                // Severity buckets
                switch (a.Severity)
                {
                    case AlertSeverity.Low:
                        LowAlerts++;
                        _pieLow[0] = LowAlerts;
                        break;
                    case AlertSeverity.Medium:
                        MediumAlerts++;
                        _pieMedium[0] = MediumAlerts;
                        break;
                    case AlertSeverity.High:
                        HighAlerts++;
                        _pieHigh[0] = HighAlerts;
                        break;
                    case AlertSeverity.Critical:
                        CriticalAlerts++;
                        _pieCritical[0] = CriticalAlerts;
                        break;
                }

                // Trend: increment current hour slot
                int h = DateTime.Now.Hour;
                _trendHourlyData[h] = _trendHourlyData[h] + 1;

                // Recent alerts list (keep top 5)
                RecentAlerts.Insert(0, MakeRecentAlertItem(a));
                while (RecentAlerts.Count > 5)
                    RecentAlerts.RemoveAt(RecentAlerts.Count - 1);

                // Heartbeat
                LastHeartbeat = "vừa xong";
            });
        }

        private void OnSensorTick(object? sender, SensorTickEventArgs e)
        {
            // Throttle: only update device status every 10th tick per sensor
            if (e.Timestamp.Second % 10 != 0) return;

            _dispatcherQueue?.TryEnqueue(() =>
            {
                OnlineDevices  = _mock.Sensors.Count(s => s.IsOnline) + _mock.Cameras.Count(c => c.IsOnline);
                OfflineDevices = TotalDevices - OnlineDevices;

                // Refresh sensor device cards
                for (int i = 0; i < Math.Min(4, _mock.Sensors.Count) && i < Devices.Count; i++)
                {
                    var s = _mock.Sensors[i];
                    Devices[i] = MakeSensorDevice(s);
                }
            });
        }

        // ── Builders ──────────────────────────────────────────────────────

        private static DeviceStatusItem MakeSensorDevice(SimulatedSensor s) => new()
        {
            Name       = s.SensorId,
            Type       = $"Cảm biến {CategoryName(s.Category)}",
            StatusText = s.IsOnline ? s.StatusText : "Offline",
            StatusColor = s.IsOnline
                ? s.CurrentLevel switch
                {
                    SensorAlertLevel.Critical => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                    SensorAlertLevel.Warning  => new SolidColorBrush(Color.FromArgb(255, 249, 115, 22)),
                    _                         => new SolidColorBrush(Colors.Green)
                }
                : new SolidColorBrush(Colors.Gray)
        };

        private static DeviceStatusItem MakeCameraDevice(SimulatedCamera c) => new()
        {
            Name        = c.CameraId,
            Type        = "Camera quan sát",
            StatusText  = c.IsOnline ? "Hoạt động" : "Offline",
            StatusColor = c.IsOnline
                ? new SolidColorBrush(Colors.Green)
                : new SolidColorBrush(Colors.Gray)
        };

        private static RecentAlertItem MakeRecentAlertItem(Alert a)
        {
            var (brush, bg) = a.Severity switch
            {
                AlertSeverity.Critical => (
                    Color.FromArgb(255, 239, 68, 68),
                    Color.FromArgb(255, 254, 226, 226)),
                AlertSeverity.High => (
                    Color.FromArgb(255, 249, 115, 22),
                    Color.FromArgb(255, 255, 237, 213)),
                AlertSeverity.Medium => (
                    Color.FromArgb(255, 234, 179, 8),
                    Color.FromArgb(255, 254, 249, 195)),
                _ => (
                    Color.FromArgb(255, 34, 197, 94),
                    Color.FromArgb(255, 220, 252, 231))
            };

            var diff = DateTimeOffset.Now - a.CreatedAt;
            string timeAgo = diff.TotalMinutes < 1 ? "vừa xong"
                           : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes} phút trước"
                           : $"{(int)diff.TotalHours} giờ trước";

            return new RecentAlertItem
            {
                DeviceName         = a.SensorName ?? a.CameraId ?? a.NodeName,
                Message            = a.Title,
                TimeAgo            = timeAgo,
                SeverityBrush      = new SolidColorBrush(brush),
                SeverityBackground = new SolidColorBrush(bg)
            };
        }

        private static string CategoryName(AlertCategory cat) => cat switch
        {
            AlertCategory.Temperature => "nhiệt độ",
            AlertCategory.Humidity    => "độ ẩm",
            AlertCategory.Gas         => "khí gas",
            AlertCategory.WaterLevel  => "mực nước",
            AlertCategory.Motion      => "chuyển động",
            _                         => cat.ToString().ToLower()
        };

        // ── Charts ────────────────────────────────────────────────────────

        private void InitializeLiveCharts()
        {
            AlertsTrendSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name          = "Cảnh báo",
                    Values        = _trendHourlyData,
                    Fill          = new LinearGradientPaint(
                        new SKColor(59, 130, 246, 80),
                        new SKColor(59, 130, 246, 10),
                        new SKPoint(0, 0), new SKPoint(0, 1)),
                    Stroke        = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 3 },
                    GeometrySize  = 10,
                    GeometryStroke = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 3 },
                    GeometryFill  = new SolidColorPaint(new SKColor(255, 255, 255)),
                    LineSmoothness = 0.7,
                    DataPadding   = new LiveChartsCore.Drawing.LvcPoint(0, 0)
                }
            };

            AlertsTrendXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = new[]
                    {
                        "00h","01h","02h","03h","04h","05h","06h","07h",
                        "08h","09h","10h","11h","12h","13h","14h","15h",
                        "16h","17h","18h","19h","20h","21h","22h","23h"
                    },
                    LabelsPaint      = new SolidColorPaint(new SKColor(148, 163, 184)),
                    SeparatorsPaint  = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
                    TextSize         = 11
                }
            };

            AlertsTrendYAxes = new Axis[]
            {
                new Axis
                {
                    LabelsPaint     = new SolidColorPaint(new SKColor(148, 163, 184)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
                    TextSize        = 11,
                    MinLimit        = 0
                }
            };

            SeverityDistributionSeries = new ISeries[]
            {
                new PieSeries<int>
                {
                    Name = "Thấp", Values = _pieLow,
                    Fill = new SolidColorPaint(new SKColor(34, 197, 94)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue}",
                    HoverPushout = 20, MaxRadialColumnWidth = double.MaxValue
                },
                new PieSeries<int>
                {
                    Name = "Trung bình", Values = _pieMedium,
                    Fill = new SolidColorPaint(new SKColor(234, 179, 8)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue}",
                    HoverPushout = 20, MaxRadialColumnWidth = double.MaxValue
                },
                new PieSeries<int>
                {
                    Name = "Cao", Values = _pieHigh,
                    Fill = new SolidColorPaint(new SKColor(249, 115, 22)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue}",
                    HoverPushout = 20, MaxRadialColumnWidth = double.MaxValue
                },
                new PieSeries<int>
                {
                    Name = "Nghiêm trọng", Values = _pieCritical,
                    Fill = new SolidColorPaint(new SKColor(239, 68, 68)),
                    DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                    DataLabelsSize = 14,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue}",
                    HoverPushout = 20, MaxRadialColumnWidth = double.MaxValue
                }
            };
        }

        // ── Clock ─────────────────────────────────────────────────────────

        private void StartClock()
        {
            _clockTimer = new Timer(_ =>
            {
                _dispatcherQueue?.TryEnqueue(() =>
                    CurrentTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"));
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        ~DashboardViewModel()
        {
            _mock.AlertGenerated -= OnAlertGenerated;
            _mock.SensorTick     -= OnSensorTick;
            _clockTimer?.Dispose();
        }
    }

    // Helper Classes
    public class DeviceStatusItem
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public SolidColorBrush StatusColor { get; set; } = new(Colors.Gray);
    }

    public class RecentAlertItem
    {
        public string DeviceName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string TimeAgo { get; set; } = string.Empty;
        public SolidColorBrush SeverityBrush { get; set; } = new(Colors.Gray);
        public SolidColorBrush SeverityBackground { get; set; } = new(Colors.LightGray);
    }
}

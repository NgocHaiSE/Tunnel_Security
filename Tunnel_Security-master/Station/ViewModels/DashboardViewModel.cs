using System;
using System.Collections.ObjectModel;
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

namespace Station.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private Timer? _clockTimer;
        private readonly DispatcherQueue? _dispatcherQueue;

        // Station Info
        private string _stationName = "Trạm Giám Sát Nghĩa Đô";
        public string StationName
        {
            get => _stationName;
            set => SetProperty(ref _stationName, value);
        }

        private string _currentTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        // Statistics
        private int _totalDevices = 12;
        public int TotalDevices
        {
            get => _totalDevices;
            set => SetProperty(ref _totalDevices, value);
        }

        private int _onlineDevices = 10;
        public int OnlineDevices
        {
            get => _onlineDevices;
            set => SetProperty(ref _onlineDevices, value);
        }

        private int _offlineDevices = 2;
        public int OfflineDevices
        {
            get => _offlineDevices;
            set => SetProperty(ref _offlineDevices, value);
        }

        private int _todayAlerts = 47;
        public int TodayAlerts
        {
            get => _todayAlerts;
            set => SetProperty(ref _todayAlerts, value);
        }

        private string _todayAlertsChange = "+15% so với\nhôm qua";
        public string TodayAlertsChange
        {
            get => _todayAlertsChange;
            set => SetProperty(ref _todayAlertsChange, value);
        }

        private int _unprocessedAlerts = 8;
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

        private string _lastHeartbeat = "2 giây trước";
        public string LastHeartbeat
        {
            get => _lastHeartbeat;
            set => SetProperty(ref _lastHeartbeat, value);
        }

        // Alert Severity Counts
        private int _lowAlerts = 12;
        public int LowAlerts
        {
            get => _lowAlerts;
            set => SetProperty(ref _lowAlerts, value);
        }

        private int _mediumAlerts = 18;
        public int MediumAlerts
        {
            get => _mediumAlerts;
            set => SetProperty(ref _mediumAlerts, value);
        }

        private int _highAlerts = 15;
        public int HighAlerts
        {
            get => _highAlerts;
            set => SetProperty(ref _highAlerts, value);
        }

        private int _criticalAlerts = 2;
        public int CriticalAlerts
        {
            get => _criticalAlerts;
            set => SetProperty(ref _criticalAlerts, value);
        }

        private int _recentAlertsCount = 24;
        public int RecentAlertsCount
        {
            get => _recentAlertsCount;
            set => SetProperty(ref _recentAlertsCount, value);
        }

        // LiveCharts - Line Chart (Xu hướng cảnh báo 24h)
        public ISeries[] AlertsTrendSeries { get; set; }
        public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> AlertsTrendXAxes { get; set; }
        public IEnumerable<LiveChartsCore.Kernel.Sketches.ICartesianAxis> AlertsTrendYAxes { get; set; }

        // LiveCharts - Pie Chart (Phân bố mức độ)
        public ISeries[] SeverityDistributionSeries { get; set; }

        // Collections
        public ObservableCollection<DeviceStatusItem> Devices { get; } = new();
        public ObservableCollection<RecentAlertItem> RecentAlerts { get; } = new();

        public DashboardViewModel()
        {
            // Lấy DispatcherQueue từ UI thread
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            LoadSampleData();
            InitializeLiveCharts();
            StartClock();
        }

        private void InitializeLiveCharts()
        {
            // Mock data cho biểu đồ đường - xu hướng cảnh báo 24h
            var hourlyAlerts = new List<double>
     {
       5, 8, 6, 10, 15, 12, 18, 20, 16, 14, 22, 25,
                30, 28, 24, 20, 18, 15, 12, 10, 8, 6, 4, 2
   };

            // ===== LINE CHART - Xu hướng cảnh báo 24h =====
            AlertsTrendSeries = new ISeries[]
    {
          new LineSeries<double>
        {
         Name = "Cảnh báo",
        Values = hourlyAlerts,
               Fill = new LinearGradientPaint(
    new SKColor(59, 130, 246, 80),  // #3B82F6 với opacity
      new SKColor(59, 130, 246, 10),
        new SKPoint(0, 0),
    new SKPoint(0, 1)),
     Stroke = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 3 },
 GeometrySize = 10,
         GeometryStroke = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 3 },
      GeometryFill = new SolidColorPaint(new SKColor(255, 255, 255)),
     LineSmoothness = 0.7,
            DataPadding = new LiveChartsCore.Drawing.LvcPoint(0, 0)
      }
  };

            // X Axis - Hours
            AlertsTrendXAxes = new Axis[]
            {
new Axis
  {
          Labels = new string[]
        {
            "00h", "01h", "02h", "03h", "04h", "05h", "06h", "07h",
     "08h", "09h", "10h", "11h", "12h", "13h", "14h", "15h",
          "16h", "17h", "18h", "19h", "20h", "21h", "22h", "23h"
         },
 LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)), // #94A3B8
 SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
  TextSize = 11,
       LabelsRotation = 0
        }
                };

            // Y Axis
            AlertsTrendYAxes = new Axis[]
       {
 new Axis
    {
      LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
       SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
         TextSize = 11,
         MinLimit = 0
      }
    };

            // ===== PIE CHART - Phân bố mức độ =====
            SeverityDistributionSeries = new ISeries[]
 {
        new PieSeries<int>
             {
     Name = "Thấp",
 Values = new int[] { LowAlerts },
    Fill = new SolidColorPaint(new SKColor(34, 197, 94)), // #22C55E
     DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
    DataLabelsSize = 14,
          DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
         DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
         HoverPushout = 20,  // Đẩy ra 20px khi hover
      MaxRadialColumnWidth = double.MaxValue
},
          new PieSeries<int>
{
              Name = "Trung bình",
        Values = new int[] { MediumAlerts },
           Fill = new SolidColorPaint(new SKColor(234, 179, 8)), // #EAB308
  DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
                  DataLabelsSize = 14,
     DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
        DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
       HoverPushout = 20,
             MaxRadialColumnWidth = double.MaxValue
       },
 new PieSeries<int>
       {
        Name = "Cao",
      Values = new int[] { HighAlerts },
            Fill = new SolidColorPaint(new SKColor(249, 115, 22)), // #F97316
       DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
             DataLabelsSize = 14,
               DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
           DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
     HoverPushout = 20,
       MaxRadialColumnWidth = double.MaxValue
       },
  new PieSeries<int>
       {
         Name = "Nghiêm trọng",
       Values = new int[] { CriticalAlerts },
       Fill = new SolidColorPaint(new SKColor(239, 68, 68)), // #EF4444
  DataLabelsPaint = new SolidColorPaint(new SKColor(255, 255, 255)),
     DataLabelsSize = 14,
      DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue}",
       HoverPushout = 20,
    MaxRadialColumnWidth = double.MaxValue
          }
            };
        }

        private void LoadSampleData()
        {
            // Sample Devices
            Devices.Add(new DeviceStatusItem
            {
                Name = "Camera 01",
                Type = "Camera quan sát",
                StatusText = "Hoạt động",
                StatusColor = new SolidColorBrush(Colors.Green)
            });
            Devices.Add(new DeviceStatusItem
            {
                Name = "Sensor 01",
                Type = "Cảm biến chuyển động",
                StatusText = "Hoạt động",
                StatusColor = new SolidColorBrush(Colors.Green)
            });
            Devices.Add(new DeviceStatusItem
            {
                Name = "Radar 01",
                Type = "Radar phát hiện",
                StatusText = "Offline",
                StatusColor = new SolidColorBrush(Colors.Gray)
            });
            Devices.Add(new DeviceStatusItem
            {
                Name = "Camera 02",
                Type = "Camera quan sát",
                StatusText = "Hoạt động",
                StatusColor = new SolidColorBrush(Colors.Green)
            });
            Devices.Add(new DeviceStatusItem
            {
                Name = "Sensor 02",
                Type = "Cảm biến nhiệt độ",
                StatusText = "Hoạt động",
                StatusColor = new SolidColorBrush(Colors.Green)
            });
            Devices.Add(new DeviceStatusItem
            {
                Name = "Camera 03",
                Type = "Camera quan sát",
                StatusText = "Hoạt động",
                StatusColor = new SolidColorBrush(Colors.Green)
            });

            // Sample Recent Alerts
            RecentAlerts.Add(new RecentAlertItem
            {
                DeviceName = "Camera 01",
                Message = "Phát hiện chuyển động bất thường",
                TimeAgo = "2 phút trước",
                SeverityBrush = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                SeverityBackground = new SolidColorBrush(Color.FromArgb(255, 254, 226, 226))
            });
            RecentAlerts.Add(new RecentAlertItem
            {
                DeviceName = "Sensor 03",
                Message = "Nhiệt độ vượt ngưỡng cho phép",
                TimeAgo = "5 phút trước",
                SeverityBrush = new SolidColorBrush(Color.FromArgb(255, 249, 115, 22)),
                SeverityBackground = new SolidColorBrush(Color.FromArgb(255, 255, 237, 213))
            });
            RecentAlerts.Add(new RecentAlertItem
            {
                DeviceName = "Radar 02",
                Message = "Mất kết nối tạm thời",
                TimeAgo = "10 phút trước",
                SeverityBrush = new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)),
                SeverityBackground = new SolidColorBrush(Color.FromArgb(255, 254, 249, 195))
            });
            RecentAlerts.Add(new RecentAlertItem
            {
                DeviceName = "Camera 04",
                Message = "Phát hiện xâm nhập khu vực hạn chế",
                TimeAgo = "15 phút trước",
                SeverityBrush = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                SeverityBackground = new SolidColorBrush(Color.FromArgb(255, 254, 226, 226))
            });
            RecentAlerts.Add(new RecentAlertItem
            {
                DeviceName = "Sensor 01",
                Message = "Độ ẩm tăng bất thường",
                TimeAgo = "20 phút trước",
                SeverityBrush = new SolidColorBrush(Color.FromArgb(255, 234, 179, 8)),
                SeverityBackground = new SolidColorBrush(Color.FromArgb(255, 254, 249, 195))
            });
        }

        private void StartClock()
        {
            _clockTimer = new Timer(_ =>
                  {
                      var newTime = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");

                      // Dispatch về UI thread để update property
                      _dispatcherQueue?.TryEnqueue(() =>
                  {
                      CurrentTime = newTime;
                  });
                  }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        ~DashboardViewModel()
        {
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

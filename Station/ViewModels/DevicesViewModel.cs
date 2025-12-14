using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Station.Models;
using Windows.UI;

namespace Station.ViewModels
{
    public enum DeviceSidebarMode
    {
        Summary,
        Details,
        Edit
    }

    public partial class DevicesViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSidebarSummaryVisible))]
        [NotifyPropertyChangedFor(nameof(IsSidebarDetailVisible))]
        [NotifyPropertyChangedFor(nameof(IsSidebarEditVisible))]
        private DeviceSidebarMode _sidebarMode = DeviceSidebarMode.Summary;

        public bool IsSidebarSummaryVisible => SidebarMode == DeviceSidebarMode.Summary;
        public bool IsSidebarDetailVisible => SidebarMode == DeviceSidebarMode.Details;
        public bool IsSidebarEditVisible => SidebarMode == DeviceSidebarMode.Edit;

        // Filter properties
        private string? _selectedStatus = "Tất cả trạng thái";
        public string? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                SetProperty(ref _selectedStatus, value);
                ApplyFilters();
            }
        }

        [ObservableProperty]
        private SensorItemViewModel? _selectedSensor;

        partial void OnSelectedSensorChanged(SensorItemViewModel? value)
        {
            if (value != null)
            {
                SidebarMode = DeviceSidebarMode.Summary;
            }
        }

        [ObservableProperty]
        private NodeItemViewModel? _selectedNode;

        private string? _selectedLine = "Tất cả tuyến";
        public string? SelectedLine
        {
            get => _selectedLine;
            set
            {
                SetProperty(ref _selectedLine, value);
                ApplyFilters();
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                ApplyFilters();
            }
        }

        // Device collections
        public ObservableCollection<DeviceItemViewModel> AllDevices { get; } = new();
        public ObservableCollection<DeviceItemViewModel> FilteredDevices { get; } = new();
        public ObservableCollection<NodeItemViewModel> FilteredNodes { get; } = new();
        public ObservableCollection<string> StatusFilters { get; } = new();
        public ObservableCollection<string> LineFilters { get; } = new();

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

        private int _faultDevices;
        public int FaultDevices
        {
            get => _faultDevices;
            set => SetProperty(ref _faultDevices, value);
        }

        public DevicesViewModel()
        {
            LoadMockData();
            ApplyFilters();
            UpdateStatistics();
        }

        private void LoadMockData()
        {
            // Mock status filters
            StatusFilters.Add("Tất cả trạng thái");
            StatusFilters.Add("Hoạt động");
            StatusFilters.Add("Ngoại tuyến");
            StatusFilters.Add("Lỗi");
            StatusFilters.Add("Tắt");

            // Mock line filters
            LineFilters.Add("Tất cả tuyến");
            LineFilters.Add("Tuyến A1");
            LineFilters.Add("Tuyến A2");
            LineFilters.Add("Tuyến A3");

            // Mock devices
            var mockDevices = new[]
     {
      new DeviceItemViewModel
      {
   Name = "Camera 01",
 DeviceId = "CAM-001",
       Type = "Camera",
  TypeDisplay = "Camera hồng ngoại",
      Location = "A1 / R1 / N1",
        IpAddress = "192.168.1.101",
             Status = DeviceStatus.Online,
            LastOnline = DateTimeOffset.Now.AddMinutes(-2),
             Manufacturer = "Hikvision",
FirmwareVersion = "V5.7.3",
AlertCount = 3
     },
            new DeviceItemViewModel
     {
     Name = "Camera 02",
          DeviceId = "CAM-002",
       Type = "Camera",
           TypeDisplay = "Camera AI",
         Location = "A1 / R2 / N3",
   IpAddress = "192.168.1.102",
           Status = DeviceStatus.Online,
       LastOnline = DateTimeOffset.Now.AddMinutes(-5),
  Manufacturer = "Dahua",
          FirmwareVersion = "V4.2.1",
             AlertCount = 1
    },
           new DeviceItemViewModel
     {
    Name = "Sensor 01",
         DeviceId = "SEN-001",
         Type = "Sensor",
         TypeDisplay = "Cảm biến chuyển động",
            Location = "A2 / R3 / N5",
           IpAddress = "192.168.1.201",
      Status = DeviceStatus.Online,
         LastOnline = DateTimeOffset.Now.AddMinutes(-1),
    Manufacturer = "Bosch",
              FirmwareVersion = "V3.1.0",
        AlertCount = 5
},
            new DeviceItemViewModel
        {
    Name = "Radar 01",
       DeviceId = "RAD-001",
        TypeDisplay = "Radar phát hiện",
         Location = "A2 / R4 / N7",
        IpAddress = "192.168.1.301",
   Status = DeviceStatus.Offline,
    LastOnline = DateTimeOffset.Now.AddHours(-2),
              Manufacturer = "Siemens",
  FirmwareVersion = "V2.5.4",
               AlertCount = 0
        },
     new DeviceItemViewModel
     {
  Name = "Sensor 02",
        DeviceId = "SEN-002",
              Type = "Sensor",
     TypeDisplay = "Cảm biến nhiệt độ",
  Location = "A1 / R1 / N2",
           IpAddress = "192.168.1.202",
       Status = DeviceStatus.Fault,
          LastOnline = DateTimeOffset.Now.AddMinutes(-30),
       Manufacturer = "Honeywell",
         FirmwareVersion = "V1.8.2",
        AlertCount = 2
      },
      new DeviceItemViewModel
 {
    Name = "Camera 03",
       DeviceId = "CAM-003",
   Type = "Camera",
    TypeDisplay = "Camera 360°",
          Location = "A3 / R5 / N9",
       IpAddress = "192.168.1.103",
     Status = DeviceStatus.Online,
    LastOnline = DateTimeOffset.Now.AddMinutes(-3),
          Manufacturer = "Axis",
 FirmwareVersion = "V6.1.2",
            AlertCount = 0
          },
        new DeviceItemViewModel
       {
            Name = "Camera 04",
           DeviceId = "CAM-004",
          Type = "Camera",
  TypeDisplay = "Camera PTZ",
   Location = "A2 / R3 / N6",
         IpAddress = "192.168.1.104",
          Status = DeviceStatus.Disabled,
             LastOnline = DateTimeOffset.Now.AddDays(-1),
    Manufacturer = "Samsung",
          FirmwareVersion = "V4.0.3",
        AlertCount = 0
         },
  new DeviceItemViewModel
       {
            Name = "Sensor 03",
        DeviceId = "SEN-003",
Type = "Sensor",
             TypeDisplay = "Cảm biến rung động",
              Location = "A1 / R2 / N4",
     IpAddress = "192.168.1.203",
       Status = DeviceStatus.Online,
        LastOnline = DateTimeOffset.Now.AddMinutes(-7),
        Manufacturer = "Bosch",
     FirmwareVersion = "V2.9.1",
    AlertCount = 1
                }
      };

            foreach (var device in mockDevices)
            {
                AllDevices.Add(device);
            }

            TotalDevices = AllDevices.Count;
        }

        private void ApplyFilters()
        {
            FilteredDevices.Clear();
            FilteredNodes.Clear();

            var filtered = AllDevices.AsEnumerable();

            // Filter by status
            if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Tất cả trạng thái")
            {
                filtered = filtered.Where(d =>
                {
                    return SelectedStatus switch
                    {
                        "Hoạt động" => d.Status == DeviceStatus.Online,
                        "Ngoại tuyến" => d.Status == DeviceStatus.Offline,
                        "Lỗi" => d.Status == DeviceStatus.Fault,
                        "Tắt" => d.Status == DeviceStatus.Disabled,
                        _ => true
                    };
                });
            }

            // Filter by line
            if (!string.IsNullOrEmpty(SelectedLine) && SelectedLine != "Tất cả tuyến")
            {
                filtered = filtered.Where(d => d.Location.StartsWith(SelectedLine.Replace("Tuyến ", "")));
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(d =>
              d.Name.ToLower().Contains(searchLower) ||
               d.DeviceId.ToLower().Contains(searchLower) ||
                       d.Location.ToLower().Contains(searchLower) ||
                  d.IpAddress.ToLower().Contains(searchLower));
            }

            foreach (var device in filtered)
            {
                FilteredDevices.Add(device);
            }

            // Group by nodes (location-based grouping)
            var nodeGroups = filtered.GroupBy(d =>
            {
                var parts = d.Location.Split('/');
                return parts.Length >= 3 ? $"{parts[0].Trim()}/{parts[1].Trim()}/{parts[2].Trim()}" : d.Location;
            });

            foreach (var group in nodeGroups)
            {
                var nodeDevices = group.ToList();
                var firstDevice = nodeDevices.First();
                var parts = firstDevice.Location.Split('/');

                var node = new NodeItemViewModel
                {
                    NodeName = parts.Length >= 3 ? parts[2].Trim() : "Unknown Node",
                    LineName = parts.Length >= 1 ? parts[0].Trim() : "Unknown Line",
                    Location = string.Join(" / ", parts.Take(2).Select(p => p.Trim())),
                    Status = nodeDevices.Any(d => d.Status == DeviceStatus.Fault) ? DeviceStatus.Fault :
                             nodeDevices.Any(d => d.Status == DeviceStatus.Offline) ? DeviceStatus.Offline :
                             DeviceStatus.Online
                };

                // Add all 6 sensor types to each node
                var random = new Random();
                var nodeLocation = string.Join("/", parts.Select(p => p.Trim()));

                // 1. Radar phát hiện người
                node.Sensors.Add(new SensorItemViewModel
                {
                    SensorId = $"RAD-{parts[2].Trim()}-001",
                    SensorName = "Radar phát hiện người",
                    SensorType = "Radar Detection",
                    CurrentValue = random.Next(0, 2) == 0 ? "Không phát hiện" : "Phát hiện",
                    Unit = "",
                    LastUpdateText = "Vừa xong",
                    SensorStatus = DeviceStatus.Online,
                    TypeIcon = "\uE701", // Radar
                    LineName = node.LineName,
                    NodeName = node.NodeName,
                    Location = node.Location
                });

                // 2. Camera hồng ngoại
                node.Sensors.Add(new SensorItemViewModel
                {
                    SensorId = $"CAM-{parts[2].Trim()}-001",
                    SensorName = "Camera hồng ngoại",
                    SensorType = "Infrared Camera",
                    CurrentValue = "Online",
                    Unit = "",
                    LastUpdateText = "Vừa xong",
                    SensorStatus = DeviceStatus.Online,
                    TypeIcon = "\uE714", // Camera
                    LineName = node.LineName,
                    NodeName = node.NodeName,
                    Location = node.Location
                });

                // 3. Cảm biến hồng ngoại phát hiện người
                node.Sensors.Add(new SensorItemViewModel
                {
                    SensorId = $"PIR-{parts[2].Trim()}-001",
                    SensorName = "Cảm biến hồng ngoại",
                    SensorType = "PIR Motion Sensor",
                    CurrentValue = random.Next(0, 2) == 0 ? "Không chuyển động" : "Có chuyển động",
                    Unit = "",
                    LastUpdateText = "Vừa xong",
                    SensorStatus = DeviceStatus.Online,
                    TypeIcon = "\uE7C1", // Motion
                    LineName = node.LineName,
                    NodeName = node.NodeName,
                    Location = node.Location
                });

                // 4. Cảm biến nhiệt độ, độ ẩm
                node.Sensors.Add(new SensorItemViewModel
                {
                    SensorId = $"THM-{parts[2].Trim()}-001",
                    SensorName = "Cảm biến nhiệt độ & độ ẩm",
                    SensorType = "Temperature & Humidity Sensor",
                    CurrentValue = $"{20 + random.Next(15)}.{random.Next(10)}°C / {40 + random.Next(40)}%",
                    Unit = "",
                    LastUpdateText = "Vừa xong",
                    SensorStatus = DeviceStatus.Online,
                    TypeIcon = "\uE9CA", // Temperature
                    LineName = node.LineName,
                    NodeName = node.NodeName,
                    Location = node.Location
                });

                // 5. Cảm biến ánh sáng
                node.Sensors.Add(new SensorItemViewModel
                {
                    SensorId = $"LUX-{parts[2].Trim()}-001",
                    SensorName = "Cảm biến ánh sáng",
                    SensorType = "Light Sensor",
                    CurrentValue = $"{100 + random.Next(400)}",
                    Unit = "lux",
                    LastUpdateText = "Vừa xong",
                    SensorStatus = DeviceStatus.Online,
                    TypeIcon = "\uE706", // Light
                    LineName = node.LineName,
                    NodeName = node.NodeName,
                    Location = node.Location
                });

                // 6. Cảm biến đo mực nước
                node.Sensors.Add(new SensorItemViewModel
                {
                    SensorId = $"WTR-{parts[2].Trim()}-001",
                    SensorName = "Cảm biến mực nước",
                    SensorType = "Water Level Sensor",
                    CurrentValue = $"{random.Next(50, 150)}.{random.Next(10)}",
                    Unit = "cm",
                    LastUpdateText = "Vừa xong",
                    SensorStatus = DeviceStatus.Online,
                    TypeIcon = "\uE9F2", // Water
                    LineName = node.LineName,
                    NodeName = node.NodeName,
                    Location = node.Location
                });

                // 7. Cảm biến gia tốc (rung động)
                node.Sensors.Add(new SensorItemViewModel
                {
                    SensorId = $"ACC-{parts[2].Trim()}-001",
                    SensorName = "Cảm biến gia tốc",
                    SensorType = "Accelerometer Sensor",
                    CurrentValue = $"{random.Next(1, 10)}.{random.Next(10)}",
                    Unit = "m/s²",
                    LastUpdateText = "Vừa xong",
                    SensorStatus = DeviceStatus.Online,
                    TypeIcon = "\uEDA4", // Vibration
                    LineName = node.LineName,
                    NodeName = node.NodeName,
                    Location = node.Location
                });

                FilteredNodes.Add(node);
            }

            UpdateStatistics();
        }

        private string GetMockSensorValue(string type)
        {
            var random = new Random();
            return type switch
            {
                "Camera" => "Online",
                "Sensor" => $"{20 + random.Next(15)}.{random.Next(10)}",
                "Radar" => $"{random.Next(100)}",
                _ => "N/A"
            };
        }

        private string GetSensorUnit(string type)
        {
            return type switch
            {
                "Camera" => "",
                "Sensor" => "°C",
                "Radar" => "%",
                _ => ""
            };
        }

        private void UpdateStatistics()
        {
            OnlineDevices = AllDevices.Count(d => d.Status == DeviceStatus.Online);
            OfflineDevices = AllDevices.Count(d => d.Status == DeviceStatus.Offline);
            FaultDevices = AllDevices.Count(d => d.Status == DeviceStatus.Fault);
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void ShowDetails()
        {
            SidebarMode = DeviceSidebarMode.Details;
        }

        [RelayCommand]
        private void ShowEdit()
        {
            SidebarMode = DeviceSidebarMode.Edit;
        }

        [RelayCommand]
        private void ShowSummary()
        {
            SidebarMode = DeviceSidebarMode.Summary;
        }

        [RelayCommand]
        private void CloseSidebar()
        {
            SelectedSensor = null;
        }

        [RelayCommand]
        private void AddDevice()
        {
            // Placeholder for Add Device logic
            System.Diagnostics.Debug.WriteLine("Add Device command executed");
        }
    }

    public partial class DeviceItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _deviceId = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _typeDisplay = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private string _ipAddress = string.Empty;

        [ObservableProperty]
        private DeviceStatus _status;

        [ObservableProperty]
        private DateTimeOffset _lastOnline;

        [ObservableProperty]
        private string _manufacturer = string.Empty;

        [ObservableProperty]
        private string _firmwareVersion = string.Empty;

        [ObservableProperty]
        private int _alertCount;

        public string LastOnlineText
        {
            get
            {
                var diff = DateTimeOffset.Now - LastOnline;
                if (diff.TotalMinutes < 1) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
                return LastOnline.ToString("dd/MM/yyyy HH:mm");
            }
        }

        // Status badge properties
        public string StatusText
        {
            get => Status switch
            {
                DeviceStatus.Online => "Hoạt động",
                DeviceStatus.Offline => "Ngoại tuyến",
                DeviceStatus.Fault => "Lỗi",
                DeviceStatus.Disabled => "Tắt",
                _ => "Không xác định"
            };
        }

        public SolidColorBrush StatusColor
        {
            get => Status switch
            {
                DeviceStatus.Online => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)), // #22C55E - Green
                DeviceStatus.Offline => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)), // #94A3B8 - Gray
                DeviceStatus.Fault => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)), // #EF4444 - Red
                DeviceStatus.Disabled => new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)), // #64748B - Slate
                _ => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
            };
        }

        public SolidColorBrush StatusBackgroundColor
        {
            get => Status switch
            {
                DeviceStatus.Online => new SolidColorBrush(Color.FromArgb(255, 220, 252, 231)), // #DCFCE7 - Light Green
                DeviceStatus.Offline => new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)), // #F1F5F9 - Light Gray
                DeviceStatus.Fault => new SolidColorBrush(Color.FromArgb(255, 254, 226, 226)), // #FEE2E2 - Light Red
                DeviceStatus.Disabled => new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)), // #E2E8F0 - Light Slate
                _ => new SolidColorBrush(Color.FromArgb(255, 241, 245, 249))
            };
        }

        public string StatusIcon
        {
            get => Status switch
            {
                DeviceStatus.Online => "\uE73E", // Checkmark
                DeviceStatus.Offline => "\uE894", // Disconnect
                DeviceStatus.Fault => "\uE783", // Error
                DeviceStatus.Disabled => "\uE8D8", // Blocked
                _ => "\uE946" // Info
            };
        }

        // Type icon
        public string TypeIcon
        {
            get => Type switch
            {
                "Camera" => "\uE714", // Video camera
                "Sensor" => "\uE957", // Sensor
                "Radar" => "\uE701", // Radar
                _ => "\uE8EA" // Device
            };
        }

        // Alert count display
        public bool HasAlerts => AlertCount > 0;
        public string AlertCountText => AlertCount > 99 ? "99+" : AlertCount.ToString();

        // Device Menu Commands
        [RelayCommand]
        private async void EditDevice()
        {
            try
            {
                // Create and show the Edit Device Dialog
                var dialog = new Station.Dialogs.EditDeviceDialog(this);

                // Set XamlRoot from App's main window
                if (Microsoft.UI.Xaml.Application.Current is App app && app.m_window is MainWindow mainWindow)
                {
                    dialog.XamlRoot = mainWindow.Content.XamlRoot;
                }

                var result = await dialog.ShowAsync();

                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    System.Diagnostics.Debug.WriteLine($"Device successfully updated: {Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening edit dialog: {ex.Message}");
            }
        }

        [RelayCommand]
        private async void ControlDevice()
        {
            try
            {
                // Create and show the Device Control Dialog
                var dialog = new Station.Dialogs.DeviceControlDialog(this);

                // Set XamlRoot from App's main window
                if (Microsoft.UI.Xaml.Application.Current is App app && app.m_window is MainWindow mainWindow)
                {
                    dialog.XamlRoot = mainWindow.Content.XamlRoot;
                }

                var result = await dialog.ShowAsync();

                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    System.Diagnostics.Debug.WriteLine($"Device control completed: {Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening control dialog: {ex.Message}");
            }
        }

        [RelayCommand]
        private async void ViewData()
        {
            try
            {
                // Create and show the Edit Device Dialog
                var dialog = new Station.Dialogs.DeviceDataDialog(this);

                // Set XamlRoot from App's main window
                if (Microsoft.UI.Xaml.Application.Current is App app && app.m_window is MainWindow mainWindow)
                {
                    dialog.XamlRoot = mainWindow.Content.XamlRoot;
                }

                var result = await dialog.ShowAsync();

                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    System.Diagnostics.Debug.WriteLine($"Device successfully updated: {Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening edit dialog: {ex.Message}");
            }
        }

        [RelayCommand]
        private async void PlaybackDevice()
        {
            try
            {
                // Create and show the Playback Dialog
                var dialog = new Station.Dialogs.PlaybackDialog(this);

                // Set XamlRoot from App's main window
                if (Microsoft.UI.Xaml.Application.Current is App app && app.m_window is MainWindow mainWindow)
                {
                    dialog.XamlRoot = mainWindow.Content.XamlRoot;
                }

                var result = await dialog.ShowAsync();

                if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    System.Diagnostics.Debug.WriteLine($"Playback completed: {Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening playback dialog: {ex.Message}");
            }
        }
    }

    public partial class NodeItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _nodeName = string.Empty;

        [ObservableProperty]
        private string _lineName = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        [ObservableProperty]
        private DeviceStatus _status;

        [ObservableProperty]
        private bool _isExpanded = false;

        public ObservableCollection<SensorItemViewModel> Sensors { get; } = new();

        public string StatusText
        {
            get => Status switch
            {
                DeviceStatus.Online => "Hoạt động",
                DeviceStatus.Offline => "Ngoại tuyến",
                DeviceStatus.Fault => "Lỗi",
                DeviceStatus.Disabled => "Tắt",
                _ => "Không xác định"
            };
        }

        public SolidColorBrush StatusColor
        {
            get => Status switch
            {
                DeviceStatus.Online => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),
                DeviceStatus.Offline => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
                DeviceStatus.Fault => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                DeviceStatus.Disabled => new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                _ => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
            };
        }

        public SolidColorBrush StatusBackgroundColor
        {
            get => Status switch
            {
                DeviceStatus.Online => new SolidColorBrush(Color.FromArgb(40, 34, 197, 94)),
                DeviceStatus.Offline => new SolidColorBrush(Color.FromArgb(40, 148, 163, 184)),
                DeviceStatus.Fault => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),
                DeviceStatus.Disabled => new SolidColorBrush(Color.FromArgb(40, 100, 116, 139)),
                _ => new SolidColorBrush(Color.FromArgb(40, 148, 163, 184))
            };
        }

        public string SensorCountText => $"{Sensors.Count} cảm biến";
    }

    public partial class SensorItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _sensorId = string.Empty;

        [ObservableProperty]
        private string _sensorName = string.Empty;

        [ObservableProperty]
        private string _sensorType = string.Empty;

        [ObservableProperty]
        private string _currentValue = string.Empty;

        [ObservableProperty]
        private string _unit = string.Empty;

        [ObservableProperty]
        private string _lastUpdateText = string.Empty;

        [ObservableProperty]
        private DeviceStatus _sensorStatus;

        [ObservableProperty]
        private string _typeIcon = string.Empty;

        [ObservableProperty]
        private string _lineName = string.Empty;

        [ObservableProperty]
        private string _nodeName = string.Empty;

        [ObservableProperty]
        private string _location = string.Empty;

        public SolidColorBrush SensorStatusColor
        {
            get => SensorStatus switch
            {
                DeviceStatus.Online => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),
                DeviceStatus.Offline => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184)),
                DeviceStatus.Fault => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                DeviceStatus.Disabled => new SolidColorBrush(Color.FromArgb(255, 100, 116, 139)),
                _ => new SolidColorBrush(Color.FromArgb(255, 148, 163, 184))
            };
        }

        [RelayCommand]
        private async void OpenSensorDetail()
        {
            try
            {
                // Create and show the Sensor Detail Dialog
                var dialog = new Station.Dialogs.SensorDetailDialog(this);

                // Set XamlRoot from App's main window
                if (Microsoft.UI.Xaml.Application.Current is App app && app.m_window is MainWindow mainWindow)
                {
                    dialog.XamlRoot = mainWindow.Content.XamlRoot;
                }

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening sensor detail dialog: {ex.Message}");
            }
        }
    }
}

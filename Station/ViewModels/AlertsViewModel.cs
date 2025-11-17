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
    public partial class AlertsViewModel : ObservableObject
    {
        // Filter properties
        private string? _selectedDevice = "Tất cả thiết bị";
        public string? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                SetProperty(ref _selectedDevice, value);
                ApplyFilters();
            }
        }

        private string? _selectedSeverityFilter = "Mọi mức cảnh báo";
        public string? SelectedSeverityFilter
        {
            get => _selectedSeverityFilter;
            set
            {
                SetProperty(ref _selectedSeverityFilter, value);
                ApplyFilters();
            }
        }

        private string? _selectedStatusFilter = "Tất cả trạng thái";
        public string? SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                SetProperty(ref _selectedStatusFilter, value);
                ApplyFilters();
            }
        }

        // Alerts collections
        public ObservableCollection<AlertItemViewModel> AllAlerts { get; } = new();
        public ObservableCollection<AlertItemViewModel> FilteredAlerts { get; } = new();
        public ObservableCollection<string> Devices { get; } = new();
        public ObservableCollection<string> SeverityFilters { get; } = new();
        public ObservableCollection<string> StatusFilters { get; } = new();

        // Statistics
        private int _totalAlerts;
        public int TotalAlerts
        {
            get => _totalAlerts;
            set => SetProperty(ref _totalAlerts, value);
        }

        private int _filteredCount;
        public int FilteredCount
        {
            get => _filteredCount;
            set => SetProperty(ref _filteredCount, value);
        }

        public AlertsViewModel()
        {
            LoadMockData();
            ApplyFilters();
        }

        private void LoadMockData()
        {
            // Mock devices
            Devices.Add("Tất cả thiết bị");
            Devices.Add("Camera 01");
            Devices.Add("Camera 02");
            Devices.Add("Sensor 01");
            Devices.Add("Sensor 02");
            Devices.Add("Radar 01");

            // Mock severity filters
            SeverityFilters.Add("Mọi mức cảnh báo");
            SeverityFilters.Add("Thấp");
            SeverityFilters.Add("Trung bình");
            SeverityFilters.Add("Cao");
            SeverityFilters.Add("Nghiêm trọng");

            // Mock status filters
            StatusFilters.Add("Tất cả trạng thái");
            StatusFilters.Add("Chưa xử lý");
            StatusFilters.Add("Đang xử lý");
            StatusFilters.Add("Đã xử lý");

            // Mock alerts
            var mockAlerts = new[]
                  {
    new AlertItemViewModel
    {
    Title = "Sự kiện tại Nút 3A",
           DeviceName = "Camera 01",
     Location = "A2 / R3 / N4 - Camera hồng ngoại",
        Timestamp = DateTimeOffset.Now.AddMinutes(-4),
               Severity = AlertSeverity.Low,
     State = AlertState.Unprocessed
     },
       new AlertItemViewModel
         {
        Title = "Sự kiện tại Nút 1A",
     DeviceName = "Camera 02",
  Location = "A1 / R1 / N1 - Camera hồng ngoại",
        Timestamp = DateTimeOffset.Now.AddMinutes(-10),
    Severity = AlertSeverity.High,
    State = AlertState.Unprocessed
            },
           new AlertItemViewModel
       {
      Title = "Sự kiện tại Nút 3B",
           DeviceName = "Sensor 01",
 Location = "A2 / R3 / N5 - Cảm biến chuyển động",
    Timestamp = DateTimeOffset.Now.AddMinutes(-12),
         Severity = AlertSeverity.Critical,
       State = AlertState.Unprocessed
            },
     new AlertItemViewModel
            {
      Title = "Sự kiện tại Nút 3A",
                    DeviceName = "Camera 01",
           Location = "A2 / R3 / N4 - Camera hồng ngoại",
              Timestamp = DateTimeOffset.Now.AddMinutes(-19),
          Severity = AlertSeverity.Medium,
               State = AlertState.InProgress
      },
            new AlertItemViewModel
                {
    Title = "Sự kiện tại Nút 1A",
            DeviceName = "Sensor 02",
            Location = "A1 / R1 / N1 - Cảm biến nhiệt độ",
          Timestamp = DateTimeOffset.Now.AddMinutes(-24),
   Severity = AlertSeverity.Medium,
     State = AlertState.Resolved
                },
       new AlertItemViewModel
                {
        Title = "Sự kiện tại Nút 3B",
      DeviceName = "Radar 01",
  Location = "A2 / R3 / N5 - Radar phát hiện",
          Timestamp = DateTimeOffset.Now.AddMinutes(-30),
    Severity = AlertSeverity.Critical,
           State = AlertState.Unprocessed
      },
    new AlertItemViewModel
    {
            Title = "Sự kiện tại Nút 4A",
                DeviceName = "Camera 02",
  Location = "A3 / R4 / N6 - Camera AI",
         Timestamp = DateTimeOffset.Now.AddMinutes(-41),
     Severity = AlertSeverity.Low,
        State = AlertState.Resolved
     },
     new AlertItemViewModel
  {
            Title = "Sự kiện tại Nút 1A",
                  DeviceName = "Camera 01",
   Location = "A1 / R1 / N1 - Camera hồng ngoại",
            Timestamp = DateTimeOffset.Now.AddMinutes(-49),
           Severity = AlertSeverity.Medium,
     State = AlertState.InProgress
      }
       };

            foreach (var alert in mockAlerts)
            {
                // Initialize the SelectedStatus after State is set
                alert.InitializeSelectedStatus();
                AllAlerts.Add(alert);
            }

            TotalAlerts = AllAlerts.Count;
        }

        private void ApplyFilters()
        {
            FilteredAlerts.Clear();

            var filtered = AllAlerts.AsEnumerable();

            // Filter by device
            if (!string.IsNullOrEmpty(SelectedDevice) && SelectedDevice != "Tất cả thiết bị")
            {
                filtered = filtered.Where(a => a.DeviceName == SelectedDevice);
            }

            // Filter by severity
            if (!string.IsNullOrEmpty(SelectedSeverityFilter) && SelectedSeverityFilter != "Mọi mức cảnh báo")
            {
                filtered = filtered.Where(a =>
                  {
                      return SelectedSeverityFilter switch
                      {
                          "Thấp" => a.Severity == AlertSeverity.Low,
                          "Trung bình" => a.Severity == AlertSeverity.Medium,
                          "Cao" => a.Severity == AlertSeverity.High,
                          "Nghiêm trọng" => a.Severity == AlertSeverity.Critical,
                          _ => true
                      };
                  });
            }

            // Filter by status
            if (!string.IsNullOrEmpty(SelectedStatusFilter) && SelectedStatusFilter != "Tất cả trạng thái")
            {
                filtered = filtered.Where(a =>
             {
                 return SelectedStatusFilter switch
                 {
                     "Chưa xử lý" => a.State == AlertState.Unprocessed,
                     "Đang xử lý" => a.State == AlertState.InProgress,
                     "Đã xử lý" => a.State == AlertState.Resolved,
                     _ => true
                 };
             });
            }

            foreach (var alert in filtered)
            {
                FilteredAlerts.Add(alert);
            }

            FilteredCount = FilteredAlerts.Count;
        }
    }

    public partial class AlertItemViewModel : ObservableObject
    {
        public string Title { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public AlertSeverity Severity { get; set; }

        private AlertState _state;
        public AlertState State
        {
            get => _state;
            set
            {
                if (SetProperty(ref _state, value))
                {
                    OnPropertyChanged(nameof(StateText));
                    OnPropertyChanged(nameof(StateIcon));
                    OnPropertyChanged(nameof(StateColor));
                    // Update SelectedStatus to match the new State
                    UpdateSelectedStatusFromState();
                }
            }
        }

        private AlertStatusOption? _selectedStatus;
        public AlertStatusOption? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (value != null && _selectedStatus?.State != value.State)
                {
                    if (SetProperty(ref _selectedStatus, value))
                    {
                        // Update State when SelectedStatus changes (avoid circular update)
                        _state = value.State;
                        OnPropertyChanged(nameof(State));
                        OnPropertyChanged(nameof(StateText));
                        OnPropertyChanged(nameof(StateIcon));
                        OnPropertyChanged(nameof(StateColor));
                    }
                }
            }
        }

        public string TimeAgo
        {
            get
            {
                var diff = DateTimeOffset.Now - Timestamp;
                if (diff.TotalMinutes < 1) return "Vừa xong";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ";
                return Timestamp.ToString("dd/MM HH:mm");
            }
        }

        // Danh sách các trạng thái có thể chọn
        public ObservableCollection<AlertStatusOption> AvailableStatuses { get; } = new();

        public AlertItemViewModel()
        {
            // Initialize AvailableStatuses first
            InitializeAvailableStatuses();
        }

        private void InitializeAvailableStatuses()
        {
            AvailableStatuses.Add(new AlertStatusOption
            {
                State = AlertState.Unprocessed,
                Text = "Chưa xử lý",
                Icon = "\uE711" // Clock icon
            });
            AvailableStatuses.Add(new AlertStatusOption
            {
                State = AlertState.InProgress,
                Text = "Đang xử lý",
                Icon = "\uE768" // Sync icon
            });
            AvailableStatuses.Add(new AlertStatusOption
            {
                State = AlertState.Resolved,
                Text = "Đã xử lý",
                Icon = "\uE73E" // Checkmark icon
            });
        }

        // Call this method after State is set (from LoadMockData)
        public void InitializeSelectedStatus()
        {
            _selectedStatus = AvailableStatuses.FirstOrDefault(s => s.State == _state);
            OnPropertyChanged(nameof(SelectedStatus));
        }

        private void UpdateSelectedStatusFromState()
        {
            if (AvailableStatuses.Any())
            {
                _selectedStatus = AvailableStatuses.FirstOrDefault(s => s.State == _state);
                OnPropertyChanged(nameof(SelectedStatus));
            }
        }

        [RelayCommand]
        private void UpdateStatus(AlertStatusOption statusOption)
        {
            if (statusOption != null)
            {
                State = statusOption.State;
                // TODO: Gọi API để cập nhật status trên server
                // await _alertService.UpdateAlertStatusAsync(alertId, State);
            }
        }

        // Icon color - Màu đậm cho icon
        public SolidColorBrush SeverityColor
        {
            get => Severity switch
            {
                AlertSeverity.Low => new SolidColorBrush(Color.FromArgb(255, 5, 150, 105)), // #059669 - Emerald-700
                AlertSeverity.Medium => new SolidColorBrush(Color.FromArgb(255, 217, 119, 6)), // #D97706 - Amber-600
                AlertSeverity.High => new SolidColorBrush(Color.FromArgb(255, 234, 88, 12)), // #EA580C - Orange-600
                AlertSeverity.Critical => new SolidColorBrush(Color.FromArgb(255, 185, 28, 28)), // #B91C1C - Red-700
                _ => new SolidColorBrush(Color.FromArgb(255, 71, 85, 105)) // Slate-600
            };
        }

        // Badge background - Màu nhạt (pastel)
        public SolidColorBrush SeverityBackgroundColor
        {
            get => Severity switch
            {
                AlertSeverity.Low => new SolidColorBrush(Color.FromArgb(255, 209, 250, 229)), // #D1FAE5 - Emerald-100
                AlertSeverity.Medium => new SolidColorBrush(Color.FromArgb(255, 254, 243, 199)), // #FEF3C7 - Amber-100
                AlertSeverity.High => new SolidColorBrush(Color.FromArgb(255, 255, 237, 213)), // #FFEDD5 - Orange-100
                AlertSeverity.Critical => new SolidColorBrush(Color.FromArgb(255, 254, 226, 226)), // #FEE2E2 - Red-100
                _ => new SolidColorBrush(Color.FromArgb(255, 241, 245, 249)) // Slate-100
            };
        }

        // Badge text color - Màu đậm tương ứng với background
        public SolidColorBrush SeverityTextColor
        {
            get => Severity switch
            {
                AlertSeverity.Low => new SolidColorBrush(Color.FromArgb(255, 4, 120, 87)), // #047857 - Emerald-700
                AlertSeverity.Medium => new SolidColorBrush(Color.FromArgb(255, 180, 83, 9)), // #B45309 - Amber-700
                AlertSeverity.High => new SolidColorBrush(Color.FromArgb(255, 194, 65, 12)), // #C2410C - Orange-700
                AlertSeverity.Critical => new SolidColorBrush(Color.FromArgb(255, 153, 27, 27)), // #991B1B - Red-800
                _ => new SolidColorBrush(Color.FromArgb(255, 51, 65, 85)) // Slate-700
            };
        }

        public string SeverityIcon
        {
            get => Severity switch
            {
                AlertSeverity.Low => "\uE946", // Info icon
                AlertSeverity.Medium => "\uE7BA", // Warning icon
                AlertSeverity.High => "\uE783", // Error icon
                AlertSeverity.Critical => "\uEB90", // Critical/Alert icon
                _ => "\uE7BA"
            };
        }

        public string SeverityText
        {
            get => Severity switch
            {
                AlertSeverity.Low => "Thấp",
                AlertSeverity.Medium => "Trung bình",
                AlertSeverity.High => "Cao",
                AlertSeverity.Critical => "Nghiêm trọng",
                _ => "Không xác định"
            };
        }

      // Status badges - dynamic color based on State
        public SolidColorBrush StateColor
        {
            get => new SolidColorBrush(Color.FromArgb(255, 30, 58, 138)); // #1E3A8A - PrimaryDark
        }

        public string StateText
        {
  get => State switch
{
                AlertState.Unprocessed => "Chưa xử lý",
           AlertState.InProgress => "Đang xử lý",
    AlertState.Resolved => "Đã xử lý",
         _ => "Không xác định"
            };
        }

 public string StateIcon
  {
            get => State switch
     {
    AlertState.Unprocessed => "\uE711", // Clock icon
           AlertState.InProgress => "\uE768", // Sync icon
      AlertState.Resolved => "\uE73E", // Checkmark icon
  _ => "\uE946"
      };
        }
    }

    public class AlertStatusOption
    {
        public AlertState State { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}

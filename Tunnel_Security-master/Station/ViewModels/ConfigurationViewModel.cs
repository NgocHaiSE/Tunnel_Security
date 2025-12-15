using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Station.Models;
using Station.Services;

namespace Station.ViewModels
{
    public partial class ConfigurationViewModel : ObservableObject
    {
        private readonly StationConfigService _configService;

        // Station Information
        private string _stationCode = string.Empty;
        public string StationCode
        {
            get => _stationCode;
            set => SetProperty(ref _stationCode, value);
        }

        private string _stationName = string.Empty;
        public string StationName
        {
            get => _stationName;
            set => SetProperty(ref _stationName, value);
        }

        private string _area = string.Empty;
        public string Area
        {
            get => _area;
            set => SetProperty(ref _area, value);
        }

        private string _route = string.Empty;
        public string Route
        {
            get => _route;
            set => SetProperty(ref _route, value);
        }

        // Connection Configuration
        private string _centerUrl = string.Empty;
        public string CenterUrl
        {
            get => _centerUrl;
            set => SetProperty(ref _centerUrl, value);
        }

        // Contact Information
        private string _contactPerson = string.Empty;
        public string ContactPerson
        {
            get => _contactPerson;
            set => SetProperty(ref _contactPerson, value);
        }

        private string _phone = string.Empty;
        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        // UI State
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        private string _successMessage = string.Empty;
        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                SetProperty(ref _successMessage, value);
                OnPropertyChanged(nameof(HasSuccessMessage));
            }
        }

        public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ConfigurationViewModel(StationConfigService configService)
        {
            _configService = configService;
            _ = LoadCurrentConfigurationAsync();
        }

        /// <summary>
        /// Load cấu hình hiện tại khi mở page
        /// </summary>
        private async Task LoadCurrentConfigurationAsync()
        {
            try
            {
                var config = await _configService.GetConfigAsync();
                if (config != null)
                {
                    StationCode = config.StationCode ?? string.Empty;
                    StationName = config.StationName ?? string.Empty;
                    Area = config.Area ?? string.Empty;
                    Route = config.Route ?? string.Empty;
                    CenterUrl = config.CenterUrl ?? string.Empty;
                    ContactPerson = config.ContactPerson ?? string.Empty;
                    Phone = config.Phone ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể tải cấu hình: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SaveConfigurationAsync()
        {
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;
            IsLoading = true;

            try
            {
                // Load existing config to preserve other fields
                var existingConfig = await _configService.GetConfigAsync();

                var config = new StationConfig
                {
                    Id = existingConfig?.Id ?? Guid.NewGuid(),
                    StationCode = StationCode,
                    StationName = StationName,
                    Area = Area,
                    Route = Route,
                    Zone = existingConfig?.Zone,
                    Province = existingConfig?.Province,
                    District = existingConfig?.District,
                    Address = existingConfig?.Address,
                    CenterUrl = CenterUrl,
                    ContactPerson = ContactPerson,
                    Phone = Phone,
                    ConfiguredAt = existingConfig?.ConfiguredAt ?? DateTimeOffset.UtcNow,
                    IsActive = true
                };

                // Validate
                var (isValid, errors) = _configService.ValidateConfig(config);
                if (!isValid)
                {
                    ErrorMessage = string.Join("\n", errors);
                    return;
                }

                // Save
                var success = await _configService.SaveConfigAsync(config);
                if (!success)
                {
                    ErrorMessage = "Không thể lưu cấu hình. Vui lòng thử lại.";
                    return;
                }

                // Show success message
                SuccessMessage = "Đã lưu cấu hình thành công!";

                // Reload main window to reflect changes
                if (Microsoft.UI.Xaml.Application.Current is App app &&
                    app.m_window is MainWindow mainWindow)
                {
                    await mainWindow.ReloadStationConfigAsync();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void LoadSampleData()
        {
            StationCode = "TRM-HN-001";
            StationName = "Trạm giám sát Hà Nội 01";
            Area = "Miền Bắc";
            Route = "Tuyến Hà Nội - Hải Phòng";
            CenterUrl = "http://localhost:5000";
            ContactPerson = "Nguyễn Văn A";
            Phone = "0123456789";

            SuccessMessage = "Đã tải dữ liệu mẫu thành công!";
        }
    }
}

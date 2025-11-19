using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Station.ViewModels;
using System;

namespace Station.Dialogs
{
    public sealed partial class DeviceControlDialog : ContentDialog
    {
        private DeviceItemViewModel _device;
        private string _nodeId;
        private string _deviceType;

        public DeviceControlDialog()
        {
            this.InitializeComponent();
        }

        public DeviceControlDialog(DeviceItemViewModel device) : this()
        {
            _device = device;
            _deviceType = device.Type ?? "Sensor";
            LoadDeviceInfo();
            ConfigureUIForDeviceType();
        }

        public DeviceControlDialog(string nodeId, string deviceType) : this()
        {
            _nodeId = nodeId;
            _deviceType = deviceType;
            LoadNodeInfo(nodeId, deviceType);
            ConfigureUIForDeviceType();
        }

        private void LoadNodeInfo(string nodeId, string deviceType)
        {
            // Set device info based on node ID
            DeviceNameText.Text = $"{deviceType} - {nodeId}";
            DeviceIdText.Text = nodeId;

            // Set initial power state (default to online)
            PowerToggle.IsChecked = true;
            UpdatePowerButtonUI(true);
        }

        private void ConfigureUIForDeviceType()
        {
            // Show "View Camera" button only for cameras
            if (_deviceType.Equals("Camera", StringComparison.OrdinalIgnoreCase))
            {
                ViewCameraCard.Visibility = Visibility.Visible;
                Title = "Điều khiển Camera";
            }
            else
            {
                ViewCameraCard.Visibility = Visibility.Collapsed;
                Title = $"Điều khiển {_deviceType}";
            }
        }

        private void LoadDeviceInfo()
        {
            if (_device != null)
            {
                DeviceNameText.Text = _device.Name;
                DeviceIdText.Text = _device.DeviceId;

                // Set initial power state
                PowerToggle.IsChecked = _device.Status == Station.Models.DeviceStatus.Online;
                UpdatePowerButtonUI(PowerToggle.IsChecked == true);
            }
        }

        private async void ViewCameraButton_Click(object sender, RoutedEventArgs e)
        {
            // Close current dialog
            this.Hide();
            
            // Get camera ID from device or node
            string cameraId = _device?.DeviceId ?? _nodeId ?? "CAM-001";
            
            // Open PlaybackDialog with camera ID
            var playbackDialog = new PlaybackDialog(cameraId);
            playbackDialog.XamlRoot = this.XamlRoot;
            await playbackDialog.ShowAsync();
        }

        private void PowerToggle_Click(object sender, RoutedEventArgs e)
        {
            bool isOn = PowerToggle.IsChecked == true;
            UpdatePowerButtonUI(isOn);

            // Send power command to device
            ShowNotification(
                isOn ? "Đã gửi lệnh bật thiết bị" : "Đã gửi lệnh tắt thiết bị",
                InfoBarSeverity.Success);

            // Update device status
            if (_device != null)
            {
                _device.Status = isOn ? Station.Models.DeviceStatus.Online : Station.Models.DeviceStatus.Offline;
            }
        }

        private void UpdatePowerButtonUI(bool isOn)
        {
            if (isOn)
            {
                PowerText.Text = "Đang bật";
                PowerIcon.Glyph = "\uE7E8"; // PowerButton
                // Use MonitoringNodeNormal color (#3FCF8E)
                PowerToggle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                          Microsoft.UI.ColorHelper.FromArgb(255, 63, 207, 142));
                PowerToggle.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
           Microsoft.UI.ColorHelper.FromArgb(255, 230, 238, 243)); // MonitoringTextPrimary
            }
            else
            {
                PowerText.Text = "Đang tắt";
                PowerIcon.Glyph = "\uE7E8"; // PowerButton
                // Use MonitoringTempHigh color (#F0625D)
                PowerToggle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
       Microsoft.UI.ColorHelper.FromArgb(255, 240, 98, 93));
                PowerToggle.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.ColorHelper.FromArgb(255, 230, 238, 243)); // MonitoringTextPrimary
            }
        }

        private async void RebootButton_Click(object sender, RoutedEventArgs e)
        {
            // Disable button immediately
            RebootButton.IsEnabled = false;
            var originalContent = RebootButton.Content;
            RebootButton.Content = "Đang khởi động...";

            try
            {
                // TODO: Send reboot command to device
                ShowNotification("Đã gửi lệnh khởi động lại thiết bị", InfoBarSeverity.Informational);

                // Simulate reboot time
                await System.Threading.Tasks.Task.Delay(3000);

                // Update success
                ShowNotification($"Thiết bị {_device?.Name} đã khởi động lại thành công", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowNotification($"Lỗi khởi động lại: {ex.Message}", InfoBarSeverity.Error);
            }
            finally
            {
                // Re-enable button
                RebootButton.IsEnabled = true;
                RebootButton.Content = originalContent;
            }
        }

        private async void ShowNotification(string message, InfoBarSeverity severity = InfoBarSeverity.Success)
        {
            NotificationBar.Message = message;
            NotificationBar.Severity = severity;
            NotificationBar.IsOpen = true;

            // Auto-hide after 3 seconds
            await System.Threading.Tasks.Task.Delay(3000);
            NotificationBar.IsOpen = false;
        }
    }
}

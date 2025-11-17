using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Station.ViewModels;
using System;

namespace Station.Dialogs
{
    public sealed partial class DeviceControlDialog : ContentDialog
    {
        private DeviceItemViewModel _device;

        public DeviceControlDialog()
        {
            this.InitializeComponent();
        }

        public DeviceControlDialog(DeviceItemViewModel device) : this()
        {
            _device = device;
            LoadDeviceInfo();
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
                PowerToggle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                          Microsoft.UI.ColorHelper.FromArgb(255, 16, 185, 129)); // Green
                PowerToggle.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
           Microsoft.UI.Colors.White);
            }
            else
            {
                PowerText.Text = "Đang tắt";
                PowerIcon.Glyph = "\uE7E8"; // PowerButton
                PowerToggle.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
       Microsoft.UI.ColorHelper.FromArgb(255, 239, 68, 68)); // Red
                PowerToggle.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.White);
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

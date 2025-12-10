using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Station.ViewModels;
using System;

namespace Station.Views
{
    /// <summary>
    /// Devices management page with grid layout, filtering, and device actions menu
    /// </summary>
    public sealed partial class DevicesPage : Page
    {
        public DevicesViewModel ViewModel { get; }

        public DevicesPage()
        {
            this.InitializeComponent();
            ViewModel = new DevicesViewModel();
        }

        /// <summary>
        /// Opens Add Node Dialog when "Thêm thiết bị" button is clicked
        /// </summary>
        private async void AddDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show the Add Node Dialog
                var dialog = new Station.Dialogs.AddNodeDialog(ViewModel);
                dialog.XamlRoot = this.XamlRoot;

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    System.Diagnostics.Debug.WriteLine("Node added successfully!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening add node dialog: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Show error dialog to user
                var errorDialog = new ContentDialog
                {
                    Title = "Lỗi",
                    Content = $"Không thể mở dialog thêm thiết bị: {ex.Message}",
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        /// <summary>
        /// Shows device action flyout
        /// </summary>
        private void DeviceMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                FlyoutBase.ShowAttachedFlyout(button);
            }
        }

        /// <summary>
        /// Highlights device card on mouse enter
        /// </summary>
        private void DeviceCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["PrimaryMediumBrush"];
                border.BorderThickness = new Thickness(2);
            }
        }

        /// <summary>
        /// Removes highlight on mouse leave
        /// </summary>
        private void DeviceCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderLightBrush"];
                border.BorderThickness = new Thickness(1);
            }
        }

        /// <summary>
        /// Highlights node card on mouse enter
        /// </summary>
        private void NodeCard_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["PrimaryMediumBrush"];
                border.BorderThickness = new Thickness(2);
            }
        }

        /// <summary>
        /// Restores node card on mouse leave
        /// </summary>
        private void NodeCard_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.BorderBrush = (SolidColorBrush)Application.Current.Resources["BorderLightBrush"];
                border.BorderThickness = new Thickness(1);
            }
        }

        /// <summary>
        /// Selects node and opens sidebar when node card is tapped
        /// </summary>
        private void NodeCard_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is NodeItemViewModel node)
            {
                ViewModel.SelectedNode = node;
                ViewModel.SelectedSensor = null; // Clear sensor selection when new node is selected
            }
        }

        /// <summary>
        /// Selects sensor when sensor icon in sidebar is tapped
        /// </summary>
        private void SensorIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is SensorItemViewModel sensor)
            {
                ViewModel.SelectedSensor = sensor;
            }
        }

        /// <summary>
        /// Shows Node info when node icon in sidebar is tapped
        /// </summary>
        private void NodeIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Clear sensor selection to show node info
            ViewModel.SelectedSensor = null;
        }
    }
}
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Station.ViewModels;
using System;

namespace Station.Views
{
    public sealed partial class AlertsPage : Page
    {
        public AlertsViewModel ViewModel { get; }

        public AlertsPage()
        {
            this.InitializeComponent();
            ViewModel = new AlertsViewModel();
        }

        private void AlertItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is AlertItemViewModel alert)
            {
                ViewModel.SelectAlertCommand.Execute(alert);
            }
        }

        private void ViewDetailButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AlertItemViewModel alert)
            {
                ViewModel.SelectAlertCommand.Execute(alert);
            }
        }

        private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAlert != null)
            {
                ViewModel.AcknowledgeAlertCommand.Execute(ViewModel.SelectedAlert);
            }
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAlert != null)
            {
                ViewModel.StartProcessingCommand.Execute(ViewModel.SelectedAlert);
            }
        }

        private void ResolveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAlert != null)
            {
                ViewModel.ResolveAlertCommand.Execute(ViewModel.SelectedAlert);
            }
        }

        private void CloseAlertButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAlert != null)
            {
                ViewModel.CloseAlertCommand.Execute(ViewModel.SelectedAlert);
            }
        }

        private void AddNoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAlert != null && !string.IsNullOrWhiteSpace(NoteTextBox.Text))
            {
                ViewModel.AddNoteCommand.Execute(NoteTextBox.Text);
                NoteTextBox.Text = string.Empty;
            }
        }

        private async void ViewCameraButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAlert?.CameraId != null)
            {
                // TODO: Open camera dialog with the alert's camera
                var dialog = new ContentDialog
                {
                    Title = $"Camera - {ViewModel.SelectedAlert.NodeName}",
                    Content = $"Đang mở camera {ViewModel.SelectedAlert.CameraId}...",
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void ViewOnMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedAlert != null)
            {
                // TODO: Navigate to map and focus on alert location
                var dialog = new ContentDialog
                {
                    Title = "Xem trên bản đồ",
                    Content = $"Định vị: {ViewModel.SelectedAlert.NodeName}\nTọa độ: {ViewModel.SelectedAlert.Lat}, {ViewModel.SelectedAlert.Lng}",
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }
}

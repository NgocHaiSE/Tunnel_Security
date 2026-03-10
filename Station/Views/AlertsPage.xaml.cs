using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Station.Dialogs;
using Station.Models;
using Station.Services;
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

            // Subscribe to live alerts from simulation
            MockDataService.Instance.AlertGenerated += OnLiveAlertGenerated;
        }

        // ── MockDataService integration ─────────────────────────────────

        private void OnLiveAlertGenerated(object? sender, AlertGeneratedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() => ViewModel.AddLiveAlert(e.Alert));
        }

        // ── Header buttons ──────────────────────────────────────────────

        private void CurrentViewButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentViewButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 20, 75, 184));
            CurrentViewButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 255, 255, 255));
            HistoryViewButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0, 0, 0, 0));
            HistoryViewButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 148, 163, 184));
            ViewModel.SelectedPeriod = "Hôm nay";
        }

        private void HistoryViewButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryViewButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 20, 75, 184));
            HistoryViewButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 255, 255, 255));
            CurrentViewButton.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(0, 0, 0, 0));
            CurrentViewButton.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(255, 148, 163, 184));
            ViewModel.SelectedPeriod = "Tất cả";
        }

        private async void ExportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Xuất CSV",
                Content = $"Xuất {ViewModel.FilteredCount} cảnh báo ra file CSV...\n(Tính năng đang phát triển)",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                RequestedTheme = ElementTheme.Dark
            };
            await dialog.ShowAsync();
        }

        // ── Filter buttons ──────────────────────────────────────────────

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RefreshAlertsCommand.Execute(null);
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearFiltersCommand.Execute(null);
        }

        // ── Table row action buttons ────────────────────────────────────

        private void AcknowledgeRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AlertItemViewModel alert)
                ViewModel.AcknowledgeAlertCommand.Execute(alert);
        }

        private async void DetailRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AlertItemViewModel alertVm)
            {
                if (!string.IsNullOrEmpty(alertVm.CameraId))
                {
                    // Has camera — open AlertVideoDialog
                    var alert = new Alert
                    {
                        Title = alertVm.Title,
                        Description = alertVm.Description,
                        Severity = alertVm.Severity,
                        CameraId = alertVm.CameraId,
                        NodeId = alertVm.NodeId,
                        NodeName = alertVm.NodeName,
                        LineName = alertVm.LineName,
                        CreatedAt = alertVm.CreatedAt,
                        SensorValue = alertVm.SensorValue,
                        SensorUnit = alertVm.SensorUnit,
                        Threshold = alertVm.Threshold
                    };
                    var videoDialog = new AlertVideoDialog(alert) { XamlRoot = this.XamlRoot };
                    await videoDialog.ShowAsync();
                    if (videoDialog.WasAcknowledged)
                        ViewModel.AcknowledgeAlertCommand.Execute(alertVm);
                }
                else
                {
                    // Text-only detail
                    var dialog = new ContentDialog
                    {
                        Title = alertVm.Title,
                        Content = BuildDetailContent(alertVm),
                        CloseButtonText = "Đóng",
                        PrimaryButtonText = alertVm.CanAcknowledge ? "Xác nhận" : null,
                        XamlRoot = this.XamlRoot,
                        RequestedTheme = ElementTheme.Dark
                    };
                    var result = await dialog.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                        ViewModel.AcknowledgeAlertCommand.Execute(alertVm);
                }
            }
        }

        private static string BuildDetailContent(AlertItemViewModel a)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Mức độ:     {a.SeverityText}");
            sb.AppendLine($"Loại:       {a.CategoryText}");
            sb.AppendLine($"Tuyến:      {a.LineName}");
            sb.AppendLine($"Nút:        {a.NodeName}");
            sb.AppendLine($"Thời gian:  {a.CreatedAtFormatted}");
            sb.AppendLine($"Trạng thái: {a.StateText}");
            if (!string.IsNullOrEmpty(a.SensorId))
                sb.AppendLine($"Cảm biến:   {a.SensorName} = {a.SensorValue:F1} {a.SensorUnit} (ngưỡng: {a.Threshold:F1})");
            sb.AppendLine();
            sb.Append(a.Description);
            return sb.ToString();
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Station.Models;
using Station.Services;
using System;
using System.Linq;
using Windows.UI;

namespace Station.Dialogs
{
    public sealed partial class AlertVideoDialog : ContentDialog
    {
        private readonly Alert _alert;
        private bool _acknowledged = false;

        public bool WasAcknowledged => _acknowledged;

        public AlertVideoDialog(Alert alert)
        {
            _alert = alert;
            this.InitializeComponent();
            PopulateAlertInfo();
        }

        private void PopulateAlertInfo()
        {
            var mock = MockDataService.Instance;
            var camera = _alert.CameraId != null
                ? mock.Cameras.FirstOrDefault(c => c.CameraId == _alert.CameraId)
                : null;

            // Header
            AlertTitleText.Text = _alert.Title;
            CameraIdText.Text = _alert.CameraId ?? _alert.NodeId ?? "—";
            AlertTimeText.Text = _alert.CreatedAt.ToString("HH:mm:ss  dd/MM/yyyy");
            VideoCameraName.Text = camera != null
                ? $"{camera.CameraId} — {camera.Location}"
                : (_alert.CameraId ?? "Camera không xác định");
            CameraDetailText.Text = camera != null
                ? $"{camera.CameraId} · {camera.Location}"
                : (_alert.CameraId ?? "—");

            // Severity styling
            ApplySeverityStyle(_alert.Severity);

            // Detail panel
            AlertTitleDetail.Text = _alert.Title;
            AlertDescriptionText.Text = string.IsNullOrEmpty(_alert.Description) ? "Không có mô tả." : _alert.Description;
            AlertLocationText.Text = string.IsNullOrEmpty(_alert.NodeName)
                ? (_alert.NodeId ?? "—")
                : $"{_alert.NodeName} ({_alert.NodeId})";
            AlertTimeDetailText.Text = _alert.CreatedAt.ToString("HH:mm:ss - dd/MM/yyyy");

            // Duration
            var elapsed = DateTimeOffset.Now - _alert.CreatedAt;
            int minutes = (int)elapsed.TotalMinutes;
            AlertDurationText.Text = minutes <= 0
                ? "Vừa xảy ra"
                : $"Đang diễn ra · {minutes} phút trước";

            AlertCategoryText.Text = "Phát hiện bởi AI · Cảnh báo tự động";
        }

        private void ApplySeverityStyle(AlertSeverity severity)
        {
            switch (severity)
            {
                case AlertSeverity.Critical:
                    SeverityText.Text = "KHẨN CẤP";
                    SeverityDetailText.Text = "KHẨN CẤP";
                    SeverityBadge.Background = new SolidColorBrush(Color.FromArgb(255, 127, 29, 29));
                    SeverityBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
                    SeverityText.Foreground = new SolidColorBrush(Color.FromArgb(255, 252, 165, 165));
                    SeverityDetailText.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
                    SeverityDetailBlock.Background = new SolidColorBrush(Color.FromArgb(255, 31, 14, 14));
                    SeverityDetailBlock.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 127, 29, 29));
                    AlertDurationText.Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
                    break;

                case AlertSeverity.High:
                    SeverityText.Text = "NGUY HIỂM";
                    SeverityDetailText.Text = "NGUY HIỂM";
                    SeverityBadge.Background = new SolidColorBrush(Color.FromArgb(255, 120, 53, 15));
                    SeverityBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 249, 115, 22));
                    SeverityText.Foreground = new SolidColorBrush(Color.FromArgb(255, 253, 186, 116));
                    SeverityDetailText.Foreground = new SolidColorBrush(Color.FromArgb(255, 249, 115, 22));
                    SeverityDetailBlock.Background = new SolidColorBrush(Color.FromArgb(255, 28, 20, 10));
                    SeverityDetailBlock.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 120, 53, 15));
                    AlertDurationText.Foreground = new SolidColorBrush(Color.FromArgb(255, 249, 115, 22));
                    break;

                case AlertSeverity.Medium:
                    SeverityText.Text = "TRUNG BÌNH";
                    SeverityDetailText.Text = "TRUNG BÌNH";
                    SeverityBadge.Background = new SolidColorBrush(Color.FromArgb(255, 92, 76, 0));
                    SeverityBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 234, 179, 8));
                    SeverityText.Foreground = new SolidColorBrush(Color.FromArgb(255, 253, 230, 138));
                    SeverityDetailText.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 179, 8));
                    SeverityDetailBlock.Background = new SolidColorBrush(Color.FromArgb(255, 28, 24, 5));
                    SeverityDetailBlock.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 92, 76, 0));
                    AlertDurationText.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 179, 8));
                    break;

                default:
                    SeverityText.Text = "THẤP";
                    SeverityDetailText.Text = "THẤP";
                    SeverityBadge.Background = new SolidColorBrush(Color.FromArgb(255, 6, 78, 59));
                    SeverityBadge.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
                    SeverityText.Foreground = new SolidColorBrush(Color.FromArgb(255, 110, 231, 183));
                    SeverityDetailText.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
                    SeverityDetailBlock.Background = new SolidColorBrush(Color.FromArgb(255, 5, 30, 22));
                    SeverityDetailBlock.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 6, 78, 59));
                    AlertDurationText.Foreground = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
                    break;
            }
        }

        private void AcknowledgeButton_Click(object sender, RoutedEventArgs e)
        {
            _acknowledged = true;
            this.Hide();
        }

        private void DismissButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void SnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[AlertDialog] Snapshot taken for {_alert.CameraId ?? _alert.NodeId}");
        }
    }
}

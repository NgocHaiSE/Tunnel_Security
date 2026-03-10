using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Station.Dialogs;
using Station.Models;
using Station.ViewModels;
using System;

namespace Station.Views
{
    public sealed partial class LiveVideoPage : Page
    {
        public LiveVideoViewModel ViewModel { get; }

        public LiveVideoPage()
        {
            this.InitializeComponent();
            ViewModel = new LiveVideoViewModel();
            this.DataContext = ViewModel;

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            ViewModel.AlertDialogRequested += OnAlertDialogRequested;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LiveVideoViewModel.GridColumns))
                UpdateCameraItemSize();
        }

        private async void OnAlertDialogRequested(CameraStreamViewModel camera)
        {
            var alert = new Alert
            {
                Title = camera.AlertTitle,
                Description = camera.AlertDescription,
                Severity = camera.AlertSeverityLevel,
                CameraId = camera.CameraId,
                NodeId = camera.CameraId,
                NodeName = camera.CameraName,
                CreatedAt = camera.AlertTime
            };

            var dialog = new AlertVideoDialog(alert)
            {
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var camera in ViewModel.CameraStreams)
                camera.IsSelected = true;
            ViewModel.UpdateActiveCameras();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var camera in ViewModel.CameraStreams)
                camera.IsSelected = false;
            ViewModel.UpdateActiveCameras();
        }

        private void VideoGridContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateCameraItemSize();
        }

        private void UpdateCameraItemSize()
        {
            if (VideoGridContainer == null || ViewModel == null) return;

            double containerWidth = VideoGridContainer.ActualWidth;
            if (containerWidth <= 0) return;

            double availableWidth = containerWidth - 24;

            int columns = ViewModel.GridColumns;
            if (columns < 1) columns = 1;

            double newWidth = Math.Floor(availableWidth / columns);
            double newHeight = Math.Floor(newWidth * 0.75);

            if (newWidth < 200) newWidth = 200;
            if (newHeight < 150) newHeight = 150;

            ViewModel.CameraItemWidth = newWidth;
            ViewModel.CameraItemHeight = newHeight;
        }
    }
}

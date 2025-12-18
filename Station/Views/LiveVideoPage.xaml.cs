using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Station.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

            // Listen for GridColumns changes to recalculate size
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LiveVideoViewModel.GridColumns))
            {
                UpdateCameraItemSize();
            }
        }

        // Camera selection handlers
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var camera in ViewModel.CameraStreams)
            {
                camera.IsSelected = true;
            }
            ViewModel.UpdateActiveCameras();
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var camera in ViewModel.CameraStreams)
            {
                camera.IsSelected = false;
            }
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

            // Subtract margins/padding if necessary (ScrollViewer Padding + Grid Margin)
            // Assuming simplified calc for now, adjusting for scrollbar
            double availableWidth = containerWidth - 24; // Minimal padding buffer

            int columns = ViewModel.GridColumns;
            if (columns < 1) columns = 1;

            double newWidth = Math.Floor(availableWidth / columns);
            // Maintain 16:9 aspect ratio or 4:3? Let's use roughly 4:3 or fit content
            double newHeight = Math.Floor(newWidth * 0.75); // 4:3 aspect ratio

            // Minimum size constraint
            if (newWidth < 200) newWidth = 200;
            if (newHeight < 150) newHeight = 150;

            ViewModel.CameraItemWidth = newWidth;
            ViewModel.CameraItemHeight = newHeight;
        }
    }
}
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Station.Models;

namespace Station.ViewModels
{
    public partial class LiveVideoViewModel : ObservableObject
    {
        // Current layout
        [ObservableProperty]
        private CameraGridLayout _currentLayout = CameraGridLayout.TwoByTwo;

        [ObservableProperty]
        private int _gridColumns = 2;

        [ObservableProperty]
        private int _gridRows = 2;

        // Camera streams
        public ObservableCollection<CameraStreamViewModel> CameraStreams { get; } = new();

        // Statistics
        [ObservableProperty]
        private int _activeCameras = 0;

        private double _cameraItemWidth = 420;
        public double CameraItemWidth
        {
            get => _cameraItemWidth;
            set => SetProperty(ref _cameraItemWidth, value);
        }

        private double _cameraItemHeight = 340;
        public double CameraItemHeight
        {
            get => _cameraItemHeight;
            set => SetProperty(ref _cameraItemHeight, value);
        }

        [ObservableProperty]
        private int _totalCameras = 0;

        [ObservableProperty]
        private string _selectedLayoutText = "2�2 (4 cameras)";

        public LiveVideoViewModel()
        {
            LoadCameraStreams();
            ChangeLayout(CameraGridLayout.TwoByTwo);
        }

        private void LoadCameraStreams()
        {
            CameraStreams.Clear();

            // Load camera streams (mock data for demo)
            for (int i = 1; i <= 16; i++)
            {
                CameraStreams.Add(new CameraStreamViewModel
                {
                    CameraId = $"CAM-{i:D2}",
                    CameraName = $"Camera #{i} (demo)",
                    StreamUrl = $"rtsp://demo/camera{i}",
                    Resolution = "1280�720",
                    IrStatus = "ON",
                    HdrStatus = "AUTO",
                    IsOnline = i <= 12, // First 12 cameras online
                    IsRecording = i <= 10,
                    Fps = 30,
                    Bitrate = 2.5
                });
            }

            TotalCameras = CameraStreams.Count;
            UpdateActiveCameras();
        }

        private void UpdateActiveCameras()
        {
            int count = 0;
            foreach (var camera in CameraStreams)
            {
                if (camera.IsOnline) count++;
            }
            ActiveCameras = count;
        }

        [RelayCommand]
        private void ChangeLayout(object? parameter)
        {
            if (parameter is int count || (parameter is string s && int.TryParse(s, out count)))
            {
                // Layout changed to count cameras
                switch (count)
                {
                    case 1: CameraItemWidth = 800; CameraItemHeight = 600; break;
                    case 4: CameraItemWidth = 420; CameraItemHeight = 340; break;
                    case 9: CameraItemWidth = 300; CameraItemHeight = 250; break;
                    case 16: CameraItemWidth = 240; CameraItemHeight = 200; break;
                    default: CameraItemWidth = 420; CameraItemHeight = 340; break;
                }
            }
        }

        private void ChangeLayoutOLD(object? parameter, object? parameter2)
        {
            if (parameter == null) return;

            // Convert parameter to CameraGridLayout
            CameraGridLayout layout;
            if (parameter is int intValue)
            {
                layout = (CameraGridLayout)intValue;
            }
            else if (parameter is string strValue && int.TryParse(strValue, out int parsedValue))
            {
                layout = (CameraGridLayout)parsedValue;
            }
            else
            {
                return;
            }

            CurrentLayout = layout;

            switch (layout)
            {
                case CameraGridLayout.Single:
                    GridColumns = 1;
                    GridRows = 1;
                    SelectedLayoutText = "1�1 (1 camera)";
                    break;

                case CameraGridLayout.TwoByTwo:
                    GridColumns = 2;
                    GridRows = 2;
                    SelectedLayoutText = "2�2 (4 cameras)";
                    break;

                case CameraGridLayout.ThreeByThree:
                    GridColumns = 3;
                    GridRows = 3;
                    SelectedLayoutText = "3�3 (9 cameras)";
                    break;

                case CameraGridLayout.FourByFour:
                    GridColumns = 4;
                    GridRows = 4;
                    SelectedLayoutText = "4�4 (16 cameras)";
                    break;
            }
        }

        [RelayCommand]
        private void RefreshStreams()
        {
            LoadCameraStreams();
        }

        [RelayCommand]
        private void TakeSnapshot(CameraStreamViewModel? camera)
        {
            if (camera == null) return;
            System.Diagnostics.Debug.WriteLine($"Snapshot taken for {camera.CameraName}");
            // TODO: Implement snapshot capture
        }

        [RelayCommand]
        private void ToggleRecording(CameraStreamViewModel? camera)
        {
            if (camera == null) return;
            camera.IsRecording = !camera.IsRecording;
        }

        [RelayCommand]
        private void ShowCameraSettings(CameraStreamViewModel? camera)
        {
            if (camera == null) return;
            System.Diagnostics.Debug.WriteLine($"Show settings for {camera.CameraName}");
            // TODO: Show camera settings dialog
        }
    }

    public partial class CameraStreamViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _cameraId = string.Empty;

        [ObservableProperty]
        private string _cameraName = string.Empty;

        [ObservableProperty]
        private string _streamUrl = string.Empty;

        [ObservableProperty]
        private string _resolution = "1280�720";

        [ObservableProperty]
        private string _irStatus = "AUTO";

        [ObservableProperty]
        private string _hdrStatus = "AUTO";

        [ObservableProperty]
        private bool _isOnline;

        [ObservableProperty]
        private bool _isRecording;

        [ObservableProperty]
        private int _fps;

        [ObservableProperty]
        private double _bitrate;

        public string StatusText => _isOnline ? "Online" : "Offline";

        public SolidColorBrush StatusColor => _isOnline
    ? new SolidColorBrush(Colors.Green)
      : new SolidColorBrush(Colors.Gray);

        public string RecordingIcon => _isRecording ? "\uE722" : "\uE714"; // Record/Stop icons

        public SolidColorBrush RecordingColor => _isRecording
       ? new SolidColorBrush(Colors.Red)
           : new SolidColorBrush(Colors.Gray);

        public string IrStatusDisplay => $"IR: {_irStatus}";
        public string HdrStatusDisplay => $"HDR: {_hdrStatus}";
        public string ResolutionDisplay => $"Resolution: {_resolution}";
        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string BitrateDisplay => $"{_bitrate:F1} Mbps";
    }
}


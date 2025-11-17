using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Geometry;

namespace Center
{
    /// <summary>
    /// Center window with ArcGIS Map for monitoring all stations
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private MapPoint? _initialViewpoint;
        private DispatcherTimer? _timer;

        public MainWindow()
        {
            InitializeComponent();

            // Set window size for center monitoring
            AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 1000));

            // Initialize timer for clock
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Initialize map after a short delay to ensure UI is ready
            _ = InitializeMapAsync();
        }

        private void Timer_Tick(object? sender, object e)
        {
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private async System.Threading.Tasks.Task InitializeMapAsync()
        {
            // Wait for UI to be ready
            await System.Threading.Tasks.Task.Delay(100);
            InitializeMap();
        }

        private async void InitializeMap()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingRing.IsActive = true;

                // Create map with OpenStreetMap (free, no API key needed)
                var myMap = new Map(BasemapStyle.OSMStandard);

                // Subscribe to map events
                myMap.Loaded += (s, args) =>
                       {
                           DispatcherQueue.TryEnqueue(() =>
                   {
                              LoadingRing.IsActive = false;
                              LoadingOverlay.Visibility = Visibility.Collapsed;
                              System.Diagnostics.Debug.WriteLine("Map loaded successfully");
                          });
                       };

                myMap.LoadStatusChanged += (s, args) =>
          {
              System.Diagnostics.Debug.WriteLine($"Map load status: {args.Status}");

              DispatcherQueue.TryEnqueue(async () =>
         {
                     if (args.Status == Esri.ArcGISRuntime.LoadStatus.Loaded)
                     {
                         LoadingRing.IsActive = false;
                         LoadingOverlay.Visibility = Visibility.Collapsed;
                     }
                     else if (args.Status == Esri.ArcGISRuntime.LoadStatus.FailedToLoad)
                     {
                         LoadingRing.IsActive = false;
                         LoadingOverlay.Visibility = Visibility.Collapsed;

                         var dialog = new ContentDialog
                         {
                             Title = "Lỗi tải bản đồ",
                             Content = $"Không thể tải bản đồ.\n\nLỗi: {myMap.LoadError?.Message}\n\nGiải pháp:\n1. Kiểm tra internet\n2. Lấy API key từ https://developers.arcgis.com/\n3. Cập nhật trong App.xaml.cs",
                             CloseButtonText = "Đóng",
                             XamlRoot = this.Content.XamlRoot
                         };
                         await dialog.ShowAsync();
                     }
                 });
          };

                // Set map to MapView
                MyMapView.Map = myMap;

                // Set initial viewpoint - Vietnam center (16.0544, 108.0717)
                // This covers all stations from North to South Vietnam
                _initialViewpoint = new MapPoint(108.0717, 16.0544, SpatialReferences.Wgs84);

                // Wait for initialization
                await System.Threading.Tasks.Task.Delay(500);

                // Set viewpoint to Vietnam
                await MyMapView.SetViewpointCenterAsync(_initialViewpoint, 1500000); // Scale for whole Vietnam

                // Add sample station markers (you can replace with real data)
                AddStationMarkers();

                // Timeout safety
                await System.Threading.Tasks.Task.Delay(5000);
                if (LoadingRing.IsActive)
                {
                    LoadingRing.IsActive = false;
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LoadingRing.IsActive = false;
                LoadingOverlay.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"Map initialization error: {ex}");

                var dialog = new ContentDialog
                {
                    Title = "Lỗi khởi tạo",
                    Content = $"Không thể khởi tạo bản đồ:\n{ex.Message}",
                    CloseButtonText = "Đóng",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void AddStationMarkers()
        {
            // TODO: Add graphic overlay with station markers
            // This is placeholder - implement based on your station data
            System.Diagnostics.Debug.WriteLine("Station markers would be added here");
        }

        private void BasemapSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyMapView?.Map == null || BasemapSelector.SelectedItem == null)
                return;

            try
            {
                var selectedItem = (ComboBoxItem)BasemapSelector.SelectedItem;
                var tag = selectedItem.Tag?.ToString();

                BasemapStyle basemapStyle = tag switch
                {
                    "OSM" => BasemapStyle.OSMStandard,
                    "OSMLight" => BasemapStyle.OSMLightGray,
                    "OSMDark" => BasemapStyle.OSMDarkGray,
                    "Topographic" => BasemapStyle.ArcGISTopographic,
                    "Streets" => BasemapStyle.ArcGISStreets,
                    "Imagery" => BasemapStyle.ArcGISImagery,
                    _ => BasemapStyle.OSMStandard
                };

                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingRing.IsActive = true;

                MyMapView.Map.Basemap = new Basemap(basemapStyle);

                // Hide loading after basemap change
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
           {
               DispatcherQueue.TryEnqueue(() =>
                     {
                         LoadingRing.IsActive = false;
                         LoadingOverlay.Visibility = Visibility.Collapsed;
                     });
           });
            }
            catch (Exception ex)
            {
                LoadingRing.IsActive = false;
                LoadingOverlay.Visibility = Visibility.Collapsed;
                System.Diagnostics.Debug.WriteLine($"Basemap change error: {ex.Message}");
            }
        }

        private async void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyMapView == null) return;

            try
            {
                var currentScale = MyMapView.MapScale;
                await MyMapView.SetViewpointScaleAsync(currentScale / 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zoom in error: {ex.Message}");
            }
        }

        private async void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyMapView == null) return;

            try
            {
                var currentScale = MyMapView.MapScale;
                await MyMapView.SetViewpointScaleAsync(currentScale * 2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zoom out error: {ex.Message}");
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyMapView == null || _initialViewpoint == null) return;

            try
            {
                // Reset to Vietnam center view
                await MyMapView.SetViewpointCenterAsync(_initialViewpoint, 1500000);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset view error: {ex.Message}");
            }
        }

        private async void LocationButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyMapView == null) return;

            try
            {
                // Example: Zoom to Hanoi (you can change this to actual center location)
                var hanoiLocation = new MapPoint(105.8542, 21.0285, SpatialReferences.Wgs84);
                await MyMapView.SetViewpointCenterAsync(hanoiLocation, 50000);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
            }
        }
    }
}

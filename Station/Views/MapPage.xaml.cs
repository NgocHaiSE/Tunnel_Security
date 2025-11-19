using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Esri.ArcGISRuntime;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Esri.ArcGISRuntime.Geometry;
using Windows.UI;

namespace Station.Views;

/// <summary>
/// Map page with ArcGIS integration
/// </summary>
public sealed partial class MapPage : Page
{
    private MapPoint _initialViewpoint;

    public MapPage()
    {
        InitializeComponent();

        // Subscribe to loaded event to initialize map after page is ready
        this.Loaded += MapPage_Loaded;
    }

    private void MapPage_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeMap();
    }

    private async void InitializeMap()
    {
        try
        {
            LoadingRing.IsActive = true;

            // Create a map with Topographic basemap (lighter and loads faster)
            var myMap = new Map(BasemapStyle.ArcGISTopographic);

            // Subscribe to map loaded event - MUST dispatch to UI thread
            myMap.Loaded += (s, args) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    LoadingRing.IsActive = false;
                    System.Diagnostics.Debug.WriteLine("Map loaded successfully");
                });
            };

            // Subscribe to map load error - MUST dispatch to UI thread
            myMap.LoadStatusChanged += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"Map load status: {args.Status}");

                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (args.Status == Esri.ArcGISRuntime.LoadStatus.Loaded)
                    {
                        LoadingRing.IsActive = false;
                    }
                    else if (args.Status == Esri.ArcGISRuntime.LoadStatus.FailedToLoad)
                    {
                        LoadingRing.IsActive = false;
                        var dialog = new ContentDialog
                        {
                            Title = "Lỗi tải bản đồ",
                            Content = "Không thể tải bản đồ. Vui lòng kiểm tra kết nối internet.\n\nLưu ý: ArcGIS Runtime cần kết nối internet để tải basemap.",
                            CloseButtonText = "Đóng",
                            XamlRoot = this.XamlRoot,
                            // Apply 4K monitoring theme colors
                            Background = new SolidColorBrush(Color.FromArgb(255, 17, 24, 39)), // #111827
                            Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 238, 243)), // #E6EEF3
                            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 31, 36, 41)), // #1F2429
                            RequestedTheme = ElementTheme.Dark
                        };

                        // Style the close button
                        dialog.Resources["ContentDialogButtonStyle"] = CreateDialogButtonStyle();

                        await dialog.ShowAsync();
                    }
                });
            };

            // Set the map to the MapView
            MyMapView.Map = myMap;

            // Set initial viewpoint (Vietnam - Hanoi area)
            // Coordinates: Hanoi center (21.0285, 105.8542)
            //_initialViewpoint = new MapPoint(105.8542, 21.0285, SpatialReferences.Wgs84);

            //// Wait a bit for map to initialize before setting viewpoint
            //await System.Threading.Tasks.Task.Delay(5000);

            //await MyMapView.SetViewpointCenterAsync(_initialViewpoint, 50000);

            //// Extra safety: hide loading after 5 seconds maximum
            //await System.Threading.Tasks.Task.Delay(5000);
            //if (LoadingRing.IsActive)
            //{
            //    LoadingRing.IsActive = false;
            //    System.Diagnostics.Debug.WriteLine("Loading timeout - hiding loading indicator");
            //}
        }
        catch (Exception ex)
        {
            LoadingRing.IsActive = false;
            System.Diagnostics.Debug.WriteLine($"Map initialization error: {ex}");

            var dialog = new ContentDialog
            {
                Title = "Lỗi",
                Content = $"Không thể khởi tạo bản đồ:\n{ex.Message}\n\nChi tiết: {ex.InnerException?.Message}",
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot,
                // Apply 4K monitoring theme colors
                Background = new SolidColorBrush(Color.FromArgb(255, 17, 24, 39)), // #111827
                Foreground = new SolidColorBrush(Color.FromArgb(255, 230, 238, 243)), // #E6EEF3
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 31, 36, 41)), // #1F2429
                RequestedTheme = ElementTheme.Dark
            };

            // Style the close button
            dialog.Resources["ContentDialogButtonStyle"] = CreateDialogButtonStyle();

            await dialog.ShowAsync();
        }
    }

    /// <summary>
    /// Create styled button for ContentDialog using 4K monitoring theme
    /// </summary>
    private Style CreateDialogButtonStyle()
    {
        var style = new Style(typeof(Button));

        // Background color - Accent button color
        style.Setters.Add(new Setter(Button.BackgroundProperty,
            new SolidColorBrush(Color.FromArgb(255, 41, 121, 255)))); // #2979FF

        // Foreground color - Light text
        style.Setters.Add(new Setter(Button.ForegroundProperty,
            new SolidColorBrush(Color.FromArgb(255, 230, 238, 243)))); // #E6EEF3

        // Border
        style.Setters.Add(new Setter(Button.BorderBrushProperty,
     new SolidColorBrush(Color.FromArgb(255, 31, 36, 41)))); // #1F2429

        style.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1)));
        style.Setters.Add(new Setter(Button.CornerRadiusProperty, new CornerRadius(4)));
      style.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(16, 8, 16, 8)));

        return style;
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
  "Imagery" => BasemapStyle.ArcGISImagery,
           "Topographic" => BasemapStyle.ArcGISTopographic,
    "Oceans" => BasemapStyle.ArcGISOceans,
     _ => BasemapStyle.ArcGISStreets
            };

            LoadingRing.IsActive = true;
         MyMapView.Map.Basemap = new Basemap(basemapStyle);

        // Hide loading after basemap change
    System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
       {
     DispatcherQueue.TryEnqueue(() => LoadingRing.IsActive = false);
       });
    }
    catch (Exception ex)
      {
            LoadingRing.IsActive = false;
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
     await MyMapView.SetViewpointCenterAsync(_initialViewpoint, 50000);
        }
   catch (Exception ex)
        {
       System.Diagnostics.Debug.WriteLine($"Reset view error: {ex.Message}");
        }
    }
}

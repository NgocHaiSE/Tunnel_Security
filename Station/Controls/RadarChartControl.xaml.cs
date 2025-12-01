using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Station.Controls
{
    public sealed partial class RadarChartControl : UserControl
    {
        private bool _isInitialized = false;
        private List<RadarDetection> _pendingDetections = new List<RadarDetection>();
        private double _maxDistance = 50.0;

        public RadarChartControl()
        {
            this.InitializeComponent();
            this.Loaded += RadarChartControl_Loaded;
        }

        private async void RadarChartControl_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
        }

        private async Task InitializeWebView()
        {
            try
            {
                await RadarWebView.EnsureCoreWebView2Async();

                // Get the path to the HTML file
                var appPath = AppDomain.CurrentDomain.BaseDirectory;
                var htmlPath = Path.Combine(appPath, "Assets", "RadarChart.html");

                if (File.Exists(htmlPath))
                {
                    RadarWebView.Source = new Uri($"file:///{htmlPath.Replace("\\", "/")}");
                }
                else
                {
                    // Fallback: Load inline HTML if file not found
                    await LoadInlineHtml();
                }

                RadarWebView.NavigationCompleted += RadarWebView_NavigationCompleted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing WebView2: {ex.Message}");
                // Try to load inline HTML as fallback
                await LoadInlineHtml();
            }
        }

        private void RadarWebView_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _isInitialized = true;

            // Update with pending detections if any
            if (_pendingDetections.Count > 0)
            {
                _ = UpdateDetectionsAsync(_pendingDetections);
            }
        }

        public async Task UpdateDetectionsAsync(List<RadarDetection> detections)
        {
            if (!_isInitialized)
            {
                _pendingDetections = detections;
                return;
            }

            try
            {
                var jsonData = JsonSerializer.Serialize(new
                {
                    detections = detections,
                    maxDistance = _maxDistance
                });

                var script = $"if (typeof updateRadarData === 'function') {{ updateRadarData({jsonData}); }}";
                await RadarWebView.ExecuteScriptAsync(script);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating radar detections: {ex.Message}");
            }
        }

        public async Task SetMaxDistanceAsync(double maxDistance)
        {
            _maxDistance = maxDistance;

            if (_isInitialized)
            {
                try
                {
                    var script = $"if (typeof updateRadarData === 'function') {{ updateRadarData({{ maxDistance: {maxDistance} }}); }}";
                    await RadarWebView.ExecuteScriptAsync(script);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting max distance: {ex.Message}");
                }
            }
        }

        public async Task SetAnimationSpeedAsync(double speed)
        {
            if (_isInitialized)
            {
                try
                {
                    var script = $"if (typeof setAnimationSpeed === 'function') {{ setAnimationSpeed({speed}); }}";
                    await RadarWebView.ExecuteScriptAsync(script);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error setting animation speed: {ex.Message}");
                }
            }
        }

        private async Task LoadInlineHtml()
        {
            // Load the HTML content from the Assets folder or use embedded resource
            var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "RadarChart.html");
            
            if (File.Exists(htmlPath))
            {
                var html = await File.ReadAllTextAsync(htmlPath);
                RadarWebView.NavigateToString(html);
            }
        }
    }

    public class RadarDetection
    {
        public double angle { get; set; } // Angle in degrees (0-180)
        public double distance { get; set; } // Distance in cm
        public double intensity { get; set; } // Signal intensity (0-100)
        public string objectType { get; set; } = "Unknown";
    }
}

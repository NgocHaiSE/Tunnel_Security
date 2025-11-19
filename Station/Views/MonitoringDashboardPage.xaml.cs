using Microsoft.UI.Xaml.Controls;
using Station.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using Station.Dialogs;

namespace Station.Views
{
    public sealed partial class MonitoringDashboardPage : Page
    {
        public MonitoringDashboardViewModel ViewModel { get; }

        public MonitoringDashboardPage()
        {
            InitializeComponent();
            ViewModel = (MonitoringDashboardViewModel)DataContext;

            // Initialize WebView2 for security map
            InitializeSecurityMap();
        }

        private async void InitializeSecurityMap()
        {
            try
            {
                // Ensure WebView2 is initialized
                await SecurityMapWebView.EnsureCoreWebView2Async();

                // Set up message handler for communication from HTML to C#
                SecurityMapWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

                // Navigate to the security map HTML
                var htmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "SecurityMap.html");

                if (File.Exists(htmlPath))
                {
                    SecurityMapWebView.Source = new Uri($"file:///{htmlPath.Replace("\\", "/")}");
                }
                else
                {
                    Debug.WriteLine($"Security map HTML not found at: {htmlPath}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing security map: {ex.Message}");
            }
        }

        private void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                var message = args.WebMessageAsJson;
                Debug.WriteLine($"Received message: {message}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var data = JsonSerializer.Deserialize<SecurityMapMessage>(message, options);

                if (data != null)
                {
                    Debug.WriteLine($"Message type: {data.Type}, NodeId: {data.NodeId}");
                    
                    switch (data.Type?.ToLower())
                    {
                        case "mapready":
                            Debug.WriteLine("Security map is ready");
                            // You can send initial data to the map here
                            break;

                        case "viewcamera":
                            HandleViewCamera(data.CameraId, data.NodeId);
                            break;

                        case "managedevice":
                            HandleManageDevice(data.NodeId);
                            break;

                        default:
                            Debug.WriteLine($"Unknown message type: {data.Type}");
                            break;
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to deserialize message");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling web message: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async void HandleViewCamera(string? cameraId, string? nodeId)
        {
            if (string.IsNullOrEmpty(cameraId))
                return;

            Debug.WriteLine($"View camera: {cameraId} for node: {nodeId}");

            try
            {
                // Open PlaybackDialog with camera ID
                var playbackDialog = new Station.Dialogs.PlaybackDialog(cameraId)
                {
                    XamlRoot = this.XamlRoot
                };

                await playbackDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening PlaybackDialog: {ex.Message}");
                
                // Show error dialog
                var errorDialog = new ContentDialog
                {
                    Title = "Lỗi",
                    Content = $"Không thể mở camera: {ex.Message}",
                    CloseButtonText = "Đóng",
                    XamlRoot = this.XamlRoot
                };
                
                await errorDialog.ShowAsync();
            }
        }

        private async void HandleManageDevice(string? nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
                return;

            Debug.WriteLine($"Manage device for node: {nodeId}");

            // Determine device type from nodeId (you can customize this logic)
            string deviceType = GetDeviceTypeFromNodeId(nodeId);
            
            // Show device control dialog
            var dialog = new Station.Dialogs.DeviceControlDialog(nodeId, deviceType)
            {
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private string GetDeviceTypeFromNodeId(string nodeId)
        {
            // Determine device type based on node ID pattern
            // This is a simple implementation - adjust based on your actual node ID format
            if (nodeId.StartsWith("CAM", StringComparison.OrdinalIgnoreCase) || 
                nodeId.Contains("Camera", StringComparison.OrdinalIgnoreCase))
            {
                return "Camera";
            }
            else if (nodeId.StartsWith("SEN", StringComparison.OrdinalIgnoreCase) ||
                     nodeId.Contains("Sensor", StringComparison.OrdinalIgnoreCase))
            {
                return "Sensor";
            }
            else if (nodeId.StartsWith("RAD", StringComparison.OrdinalIgnoreCase) ||
                     nodeId.Contains("Radar", StringComparison.OrdinalIgnoreCase))
            {
                return "Radar";
            }
            
            // Default to Sensor for other types
            return "Sensor";
        }

        /// <summary>
        /// Update node data in the map (can be called from ViewModel)
        /// </summary>
        public async void UpdateNodeInMap(string nodeId, object nodeData)
        {
            try
            {
                var json = JsonSerializer.Serialize(nodeData);
                await SecurityMapWebView.CoreWebView2.ExecuteScriptAsync($"updateNode({json})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating node in map: {ex.Message}");
            }
        }

        /// <summary>
        /// Update multiple nodes in the map
        /// </summary>
        public async void UpdateNodesInMap(object[] nodesData)
        {
            try
            {
                var json = JsonSerializer.Serialize(nodesData);
                await SecurityMapWebView.CoreWebView2.ExecuteScriptAsync($"updateNodes({json})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating nodes in map: {ex.Message}");
            }
        }

        private class SecurityMapMessage
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }
            
            [JsonPropertyName("cameraId")]
            public string? CameraId { get; set; }
            
            [JsonPropertyName("nodeId")]
            public string? NodeId { get; set; }
        }
    }
}

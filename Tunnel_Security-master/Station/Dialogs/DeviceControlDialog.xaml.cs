using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Station.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Station.Dialogs
{
    public sealed partial class DeviceControlDialog : ContentDialog
    {
        private DeviceItemViewModel _device;
        private string _nodeId;
        private string _deviceType;
        private const string BackendBaseUrl = "http://localhost:5280";
        private const string StationId = "ST01";

        public DeviceControlDialog()
        {
            this.InitializeComponent();
        }

        public DeviceControlDialog(DeviceItemViewModel device) : this()
        {
            _device = device;
            _deviceType = device.Type ?? "Sensor";
            _nodeId = device.DeviceId;
            LoadDeviceInfo();
            _ = LoadSensorsDataAsync();
        }

        public DeviceControlDialog(string nodeId, string deviceType) : this()
        {
            _nodeId = nodeId;
            _deviceType = deviceType;
            LoadNodeInfo(nodeId, deviceType);
            _ = LoadSensorsDataAsync();
        }

        private async void LoadNodeInfo(string nodeId, string deviceType)
        {
            DeviceNameText.Text = $"Node {nodeId}";
            DeviceIdText.Text = nodeId;
            await LoadNodeDataFromApi(nodeId);
        }

        private async Task LoadNodeDataFromApi(string nodeId)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync($"{BackendBaseUrl}/api/stations/{StationId}/nodes");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var geojson = JsonDocument.Parse(json);
                    
                    foreach (var feature in geojson.RootElement.GetProperty("features").EnumerateArray())
                    {
                        var props = feature.GetProperty("properties");
                        var id = props.GetProperty("id").GetString();
                        
                        if (id == nodeId)
                        {
                            DeviceNameText.Text = props.GetProperty("name").GetString();
                            BatteryText.Text = $"{props.GetProperty("batteryLevel").GetInt32()}%";
                            RSSIText.Text = $"{props.GetProperty("rssi").GetInt32()} dBm";
                            
                            var sensorCount = props.GetProperty("sensorCount").GetInt32();
                            SensorCountText.Text = sensorCount.ToString();
                            
                            var lastOnline = props.GetProperty("lastOnline").GetDateTime();
                            LastOnlineText.Text = FormatDateTime(lastOnline);
                            
                            var status = props.GetProperty("status").GetInt32();
                            UpdateStatusBadge(status);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading node data: {ex.Message}");
            }
        }

        private async Task LoadSensorsDataAsync()
        {
            try
            {
                SensorHeaderText.Text = "Đang tải...";

                if (string.IsNullOrEmpty(_nodeId))
                {
                    SensorHeaderText.Text = "Không có thông tin node";
                    return;
                }
                
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                
                var url = $"{BackendBaseUrl}/api/stations/{StationId}/nodes/{_nodeId}/sensors";
                System.Diagnostics.Debug.WriteLine($"Loading sensors from: {url}");
                
                var response = await client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Sensors JSON: {json.Substring(0, Math.Min(200, json.Length))}...");
                    
                    // API trả về object {"value": [...], "Count": 5}
                    List<SensorData> sensors;
                    try
                    {
                        var wrapper = JsonSerializer.Deserialize<SensorWrapper>(json, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        });
                        sensors = wrapper?.Value ?? new List<SensorData>();
                    }
                    catch
                    {
                        // Fallback: thử parse trực tiếp như array
                        sensors = JsonSerializer.Deserialize<List<SensorData>>(json, new JsonSerializerOptions 
                        { 
                            PropertyNameCaseInsensitive = true 
                        }) ?? new List<SensorData>();
                    }

                    if (sensors != null && sensors.Count > 0)
                    {
                        SensorHeaderText.Text = $"Đã tìm thấy {sensors.Count} cảm biến";
                        
                        var sensorViewModels = sensors.Select(s => new SensorViewModel
                        {
                            Name = GetSensorDisplayName(s.Type, s.Name),
                            Type = GetSensorTypeName(s.Type),
                            Value = s.CurrentValue?.ToString("0.0") ?? "N/A",
                            Unit = s.Unit ?? "",
                            Icon = GetSensorIcon(s.Type, s.Name),
                            IconBackground = GetSensorIconBackground(s.Type),
                            ValueColor = GetValueColor(s)
                        }).ToList();

                        SensorsItemsControl.ItemsSource = sensorViewModels;
                    }
                    else
                    {
                        SensorHeaderText.Text = "Không có cảm biến";
                    }
                }
                else
                {
                    SensorHeaderText.Text = $"Lỗi API: {response.StatusCode}";
                    System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (HttpRequestException ex)
            {
                SensorHeaderText.Text = "Không kết nối được Backend";
                System.Diagnostics.Debug.WriteLine($"HTTP Error loading sensors: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                SensorHeaderText.Text = "Timeout khi tải dữ liệu";
            }
            catch (Exception ex)
            {
                SensorHeaderText.Text = $"Lỗi: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error loading sensors: {ex.Message}");
            }
        }

        private void LoadDeviceInfo()
        {
            if (_device != null)
            {
                DeviceNameText.Text = _device.Name;
                DeviceIdText.Text = _device.DeviceId;
            }
        }

        private void UpdateStatusBadge(int status)
        {
            var (color, text) = status switch
            {
                0 => ("#10B981", "● Online"),
                1 => ("#F59E0B", "● Warning"),
                2 => ("#EF4444", "● Critical"),
                3 => ("#6B7280", "● Offline"),
                _ => ("#94A3B8", "● Unknown")
            };

            StatusBadge.Background = new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(255,
                    Convert.ToByte(color.Substring(1, 2), 16),
                    Convert.ToByte(color.Substring(3, 2), 16),
                    Convert.ToByte(color.Substring(5, 2), 16)));
            
            ((TextBlock)StatusBadge.Child).Text = text;
        }

        private string FormatDateTime(DateTime dt)
        {
            var diff = DateTime.Now - dt;
            if (diff.TotalMinutes < 1) return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
            return dt.ToString("dd/MM/yyyy HH:mm");
        }

        private string GetSensorTypeName(int type)
        {
            return type switch
            {
                0 => "RADAR",
                1 => "TEMPERATURE",
                2 => "HUMIDITY",
                3 => "VIBRATION",
                4 => "WATERLEVEL",
                5 => "SMOKE",
                _ => "UNKNOWN"
            };
        }

        private string GetSensorDisplayName(int type, string name)
        {
            return type switch
            {
                0 => "Radar biển động",
                1 => "Nhiệt độ",
                2 => "Độ ẩm",
                3 => "Cảm biến rung",
                4 => "Mực nước",
                5 => "Cảm biến khói/lửa",
                _ => name ?? "Cảm biến"
            };
        }

        private string GetSensorIcon(int type, string name)
        {
            return type switch
            {
                0 => "📡",
                1 => "🌡️",
                2 => "💧",
                3 => "📳",
                4 => "🌊",
                5 => "🔥",
                _ => "📊"
            };
        }

        private SolidColorBrush GetSensorIconBackground(int type)
        {
            var color = type switch
            {
                0 => "#3B82F6",  // RADAR
                1 => "#EF4444",  // TEMPERATURE
                2 => "#06B6D4",  // HUMIDITY
                3 => "#8B5CF6",  // VIBRATION
                4 => "#0EA5E9",  // WATERLEVEL
                5 => "#F59E0B",  // SMOKE
                _ => "#6B7280"
            };

            return new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(40,
                    Convert.ToByte(color.Substring(1, 2), 16),
                    Convert.ToByte(color.Substring(3, 2), 16),
                    Convert.ToByte(color.Substring(5, 2), 16)));
        }

        private SolidColorBrush GetValueColor(SensorData sensor)
        {
            if (sensor.CriticalThreshold.HasValue && sensor.CurrentValue >= sensor.CriticalThreshold)
                return new SolidColorBrush(Microsoft.UI.Colors.Red);
            if (sensor.WarningThreshold.HasValue && sensor.CurrentValue >= sensor.WarningThreshold)
                return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11));
            return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 16, 185, 129));
        }

        private async void ViewCameraButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var playbackDialog = new PlaybackDialog(_nodeId ?? "CAM-001");
            playbackDialog.XamlRoot = this.XamlRoot;
            await playbackDialog.ShowAsync();
        }

        private void PowerToggle_Checked(object sender, RoutedEventArgs e)
        {
            ShowNotification("Đã gửi lệnh bật thiết bị", InfoBarSeverity.Success);
        }

        private void PowerToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowNotification("Đã gửi lệnh tắt thiết bị", InfoBarSeverity.Warning);
        }

        private void PowerOnButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNotification("Đã gửi lệnh bật thiết bị", InfoBarSeverity.Success);
        }

        private void PowerOffButton_Click(object sender, RoutedEventArgs e)
        {
            ShowNotification("Đã gửi lệnh tắt thiết bị", InfoBarSeverity.Warning);
        }

        private async void RebootButton_Click(object sender, RoutedEventArgs e)
        {
            RebootButton.IsEnabled = false;
            ShowNotification("Đang khởi động lại thiết bị...", InfoBarSeverity.Informational);
            await Task.Delay(2000);
            ShowNotification("Thiết bị đã khởi động lại thành công", InfoBarSeverity.Success);
            RebootButton.IsEnabled = true;
        }

        private async void EditDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_device != null)
            {
                var editDialog = new EditDeviceDialog(_device);
                editDialog.XamlRoot = this.XamlRoot;
                await editDialog.ShowAsync();
            }
        }

        private async void ShowNotification(string message, InfoBarSeverity severity = InfoBarSeverity.Success)
        {
            NotificationBar.Message = message;
            NotificationBar.Severity = severity;
            NotificationBar.IsOpen = true;
            await Task.Delay(3000);
            NotificationBar.IsOpen = false;
        }
    }

    public class SensorWrapper
    {
        public List<SensorData> Value { get; set; }
        public int Count { get; set; }
    }

    public class SensorData
    {
        public string Id { get; set; }
        public string NodeId { get; set; }
        public int Type { get; set; }  // Changed from string to int
        public string Name { get; set; }
        public string Unit { get; set; }
        public double? WarningThreshold { get; set; }
        public double? CriticalThreshold { get; set; }
        public double? CurrentValue { get; set; }
        public DateTime? LastReading { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class SensorViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string Icon { get; set; }
        public SolidColorBrush IconBackground { get; set; }
        public SolidColorBrush ValueColor { get; set; }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Windows.UI;
using Station.Controls;

namespace Station.Views
{
    public sealed partial class DataPage : Page
    {
        private List<LineData> _lines = new();
        private string _selectedLineId = "all";
        private string _selectedStatus = "all";
        private int _timeRangeHours = 6;
        private string _searchText = "";
        private HashSet<string> _selectedTypes = new() { "radar", "camera", "temperature", "humidity", "light", "water", "vibration" };
        private int _columnsPerRow = 2;

        public DataPage()
        {
            this.InitializeComponent();
            InitializeMockData();
            this.Loaded += DataPage_Loaded;
        }

        private void DataPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSensorCounts();
            UpdateStatusCounts();
            LoadChartsForAllNodes();
        }

        private void InitializeMockData()
        {
            _lines = new List<LineData>
            {
                new LineData
                {
                    Id = "LINE_A",
                    Name = "Tuyến A",
                    Status = "active",
                    Nodes = new List<NodeData>
                    {
                        new NodeData { Id = "S01", Name = "Radar Cổng A1", Type = "radar", Icon = "📡", Status = "normal", Value = 3 },
                        new NodeData { Id = "S02", Name = "Radar Lối Thoát A2", Type = "radar", Icon = "📡", Status = "warning", Value = 15 },
                        new NodeData { Id = "S03", Name = "Camera IR A3", Type = "camera", Icon = "📹", Status = "normal", Value = 1 },
                        new NodeData { Id = "S04", Name = "Nhiệt độ A4", Type = "temperature", Icon = "🌡️", Status = "normal", Value = 28.5 },
                        new NodeData { Id = "S05", Name = "Độ ẩm A5", Type = "humidity", Icon = "💧", Status = "normal", Value = 65 },
                        new NodeData { Id = "S06", Name = "Ánh sáng A6", Type = "light", Icon = "💡", Status = "normal", Value = 174 }
                    }
                },
                new LineData
                {
                    Id = "LINE_B",
                    Name = "Tuyến B",
                    Status = "active",
                    Nodes = new List<NodeData>
                    {
                        new NodeData { Id = "S07", Name = "Radar B1", Type = "radar", Icon = "📡", Status = "normal", Value = 2 },
                        new NodeData { Id = "S08", Name = "Camera IR B2", Type = "camera", Icon = "📹", Status = "normal", Value = 0 },
                        new NodeData { Id = "S09", Name = "Nhiệt độ B3", Type = "temperature", Icon = "🌡️", Status = "warning", Value = 35.2 },
                        new NodeData { Id = "S10", Name = "Độ ẩm B4", Type = "humidity", Icon = "💧", Status = "normal", Value = 70 },
                        new NodeData { Id = "S11", Name = "Mực nước B5", Type = "water", Icon = "🌊", Status = "critical", Value = 25 },
                        new NodeData { Id = "S12", Name = "Rung động B6", Type = "vibration", Icon = "📊", Status = "warning", Value = 0.45 }
                    }
                },
                new LineData
                {
                    Id = "LINE_C",
                    Name = "Tuyến C",
                    Status = "active",
                    Nodes = new List<NodeData>
                    {
                        new NodeData { Id = "S13", Name = "Radar C1", Type = "radar", Icon = "📡", Status = "normal", Value = 1 },
                        new NodeData { Id = "S14", Name = "Camera IR C2", Type = "camera", Icon = "📹", Status = "normal", Value = 0 },
                        new NodeData { Id = "S15", Name = "Nhiệt độ C3", Type = "temperature", Icon = "🌡️", Status = "normal", Value = 27.8 },
                        new NodeData { Id = "S16", Name = "Độ ẩm C4", Type = "humidity", Icon = "💧", Status = "normal", Value = 68 },
                        new NodeData { Id = "S17", Name = "Ánh sáng C5", Type = "light", Icon = "💡", Status = "normal", Value = 165 },
                        new NodeData { Id = "S18", Name = "Mực nước C6", Type = "water", Icon = "🌊", Status = "normal", Value = 8 }
                    }
                }
            };
        }

        private void UpdateSensorCounts()
        {
            var allNodes = _lines.SelectMany(l => l.Nodes).ToList();
            RadarCount.Text = allNodes.Count(n => n.Type == "radar").ToString();
            CameraCount.Text = allNodes.Count(n => n.Type == "camera").ToString();
            TempCount.Text = allNodes.Count(n => n.Type == "temperature").ToString();
            HumidityCount.Text = allNodes.Count(n => n.Type == "humidity").ToString();
            LightCount.Text = allNodes.Count(n => n.Type == "light").ToString();
            WaterCount.Text = allNodes.Count(n => n.Type == "water").ToString();
            VibrationCount.Text = allNodes.Count(n => n.Type == "vibration").ToString();
        }

        private void UpdateStatusCounts()
        {
            var allNodes = _lines.SelectMany(l => l.Nodes).ToList();
            var normalCount = allNodes.Count(n => n.Status == "normal");
            var warningCount = allNodes.Count(n => n.Status == "warning");
            var criticalCount = allNodes.Count(n => n.Status == "critical");

            NormalCountText.Text = $"{normalCount} bình thường";
            WarningCountText.Text = $"{warningCount} cảnh báo";
            CriticalCountText.Text = $"{criticalCount} nghiêm trọng";
        }

        private void LoadChartsForAllNodes()
        {
            if (ChartsPanel == null) return;

            ChartsPanel.Children.Clear();
            var filteredNodes = GetFilteredNodes();

            if (filteredNodes.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                ChartCountText.Text = "0 biểu đồ";
                return;
            }

            EmptyState.Visibility = Visibility.Collapsed;
            ChartCountText.Text = $"{filteredNodes.Count} biểu đồ";

            // Create rows based on layout setting
            var currentRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
            int itemsInRow = 0;

            foreach (var node in filteredNodes)
            {
                var chartCard = CreateChartCard(node);
                currentRow.Children.Add(chartCard);
                itemsInRow++;

                if (itemsInRow >= _columnsPerRow)
                {
                    ChartsPanel.Children.Add(currentRow);
                    currentRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
                    itemsInRow = 0;
                }
            }

            // Add remaining items
            if (currentRow.Children.Count > 0)
            {
                ChartsPanel.Children.Add(currentRow);
            }
        }

        private List<NodeData> GetFilteredNodes()
        {
            var allNodes = _lines.SelectMany(l => l.Nodes).ToList();

            // Filter by line
            if (_selectedLineId != "all")
            {
                var line = _lines.FirstOrDefault(l => l.Id == _selectedLineId);
                allNodes = line?.Nodes ?? new List<NodeData>();
            }

            // Filter by selected types (checkboxes)
            allNodes = allNodes.Where(n => _selectedTypes.Contains(n.Type)).ToList();

            // Filter by status
            if (_selectedStatus != "all")
            {
                allNodes = allNodes.Where(n => n.Status == _selectedStatus).ToList();
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(_searchText))
            {
                allNodes = allNodes.Where(n => 
                    n.Name.ToLower().Contains(_searchText.ToLower()) ||
                    n.Id.ToLower().Contains(_searchText.ToLower())
                ).ToList();
            }

            return allNodes;
        }

        private Border CreateChartCard(NodeData node)
        {
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["BackgroundSecondaryBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                MinWidth = _columnsPerRow == 1 ? 0 : (_columnsPerRow == 2 ? 450 : 300),
                HorizontalAlignment = _columnsPerRow == 1 ? HorizontalAlignment.Stretch : HorizontalAlignment.Left
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(250) });

            // Header
            var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            var title = new TextBlock
            {
                Text = $"{node.Icon} {node.Name}",
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            var subtitle = new TextBlock
            {
                Text = $"Giá trị: {node.Value} {GetUnit(node.Type)}",
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 2, 0, 0)
            };
            titleStack.Children.Add(title);
            titleStack.Children.Add(subtitle);
            Grid.SetColumn(titleStack, 0);

            var statusBadge = new Border
            {
                Background = GetStatusBrush(node.Status),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 4, 10, 4),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = GetStatusText(node.Status),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                }
            };
            Grid.SetColumn(statusBadge, 1);

            header.Children.Add(titleStack);
            header.Children.Add(statusBadge);
            Grid.SetRow(header, 0);

            // Chart
            FrameworkElement chart;
            if (node.Type == "radar")
            {
                // Use WebView2 radar chart for radar sensors
                chart = CreateRadarChart(node);
            }
            else
            {
                // Use line chart for other sensors
                chart = CreateChart(node);
            }
            Grid.SetRow(chart, 1);

            grid.Children.Add(header);
            grid.Children.Add(chart);
            card.Child = grid;

            return card;
        }

        private RadarChartControl CreateRadarChart(NodeData node)
        {
            var random = new Random();
            var detections = new List<RadarDetection>();

            // Generate random detections based on node value
            var detectionCount = (int)(node.Value ?? 0);
            for (int i = 0; i < detectionCount; i++)
            {
                // Generate detections in active zone (60° - 120°)
                detections.Add(new RadarDetection
                {
                    angle = 60 + random.Next(61), // Random angle between 60° and 120°
                    distance = 10 + random.Next(35), // Random distance between 10cm and 45cm
                    intensity = 50 + random.Next(50), // Random intensity
                    objectType = "Person"
                });
            }

            var radarChart = new RadarChartControl();
            
            // Update detections after control is loaded
            radarChart.Loaded += async (s, e) =>
            {
                await radarChart.UpdateDetectionsAsync(detections);
            };

            return radarChart;
        }

        private CartesianChart CreateChart(NodeData node)
        {
            var random = new Random();
            var baseValue = node.Value ?? 0;
            var values = new double[24];

            for (int i = 0; i < 24; i++)
            {
                var variance = (random.NextDouble() - 0.5) * baseValue * 0.2;
                values[i] = Math.Max(0, baseValue + variance);
            }

            var showDataPoints = ChkShowDataPoints?.IsChecked ?? true;
            var smoothLine = ChkSmoothLine?.IsChecked ?? true ? 0.5 : 0;
            var showGrid = ChkShowGrid?.IsChecked ?? true;

            var series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = values,
                    Fill = new LinearGradientPaint(
                        GetChartColor(node.Type, 60),
                        GetChartColor(node.Type, 10),
                        new SKPoint(0, 0),
                        new SKPoint(0, 1)),
                    Stroke = new SolidColorPaint(GetChartColor(node.Type)) { StrokeThickness = 2 },
                    GeometrySize = showDataPoints ? 6 : 0,
                    GeometryStroke = showDataPoints ? new SolidColorPaint(GetChartColor(node.Type)) { StrokeThickness = 2 } : null,
                    GeometryFill = showDataPoints ? new SolidColorPaint(new SKColor(255, 255, 255)) : null,
                    LineSmoothness = smoothLine
                }
            };

            var xAxis = new Axis
            {
                Labels = new[] { "00h", "04h", "08h", "12h", "16h", "20h", "24h" },
                LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                SeparatorsPaint = showGrid ? new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 } : null,
                TextSize = 10
            };

            var yAxis = new Axis
            {
                LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                SeparatorsPaint = showGrid ? new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 } : null,
                TextSize = 10
            };

            return new CartesianChart
            {
                Series = series,
                XAxes = new[] { xAxis },
                YAxes = new[] { yAxis }
            };
        }

        private SKColor GetChartColor(string type, byte alpha = 255)
        {
            return type switch
            {
                "radar" => new SKColor(16, 185, 129, alpha),
                "camera" => new SKColor(59, 130, 246, alpha),
                "temperature" => new SKColor(239, 68, 68, alpha),
                "humidity" => new SKColor(6, 182, 212, alpha),
                "light" => new SKColor(251, 191, 36, alpha),
                "water" => new SKColor(14, 165, 233, alpha),
                "vibration" => new SKColor(245, 158, 11, alpha),
                _ => new SKColor(156, 163, 175, alpha)
            };
        }

        private SolidColorBrush GetStatusBrush(string status)
        {
            return status switch
            {
                "normal" => new SolidColorBrush(Color.FromArgb(255, 34, 197, 94)),
                "warning" => new SolidColorBrush(Color.FromArgb(255, 245, 158, 11)),
                "critical" => new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                _ => new SolidColorBrush(Color.FromArgb(255, 156, 163, 175))
            };
        }

        private string GetUnit(string type)
        {
            return type switch
            {
                "temperature" => "°C",
                "humidity" => "%",
                "light" => "lux",
                "water" => "cm",
                "vibration" => "m/s²",
                "radar" => "phát hiện",
                "camera" => "người",
                _ => ""
            };
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "normal" => "Bình thường",
                "warning" => "Cảnh báo",
                "critical" => "Nghiêm trọng",
                _ => "Không xác định"
            };
        }

        // Event Handlers
        private void LineFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedLineId = LineFilterComboBox.SelectedIndex switch
            {
                0 => "all",
                1 => "LINE_A",
                2 => "LINE_B",
                3 => "LINE_C",
                _ => "all"
            };
            LoadChartsForAllNodes();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedStatus = StatusFilterComboBox.SelectedIndex switch
            {
                0 => "all",
                1 => "normal",
                2 => "warning",
                3 => "critical",
                _ => "all"
            };
            LoadChartsForAllNodes();
        }

        private void TimeRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _timeRangeHours = TimeRangeComboBox.SelectedIndex switch
            {
                0 => 1,
                1 => 6,
                2 => 24,
                3 => 168,
                4 => 720,
                _ => 6
            };
            LoadChartsForAllNodes();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text;
            LoadChartsForAllNodes();
        }

        private void ChartType_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string type)
            {
                if (checkBox.IsChecked == true)
                    _selectedTypes.Add(type);
                else
                    _selectedTypes.Remove(type);

                LoadChartsForAllNodes();
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            ChkRadar.IsChecked = true;
            ChkCamera.IsChecked = true;
            ChkTemperature.IsChecked = true;
            ChkHumidity.IsChecked = true;
            ChkLight.IsChecked = true;
            ChkWater.IsChecked = true;
            ChkVibration.IsChecked = true;
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            ChkRadar.IsChecked = false;
            ChkCamera.IsChecked = false;
            ChkTemperature.IsChecked = false;
            ChkHumidity.IsChecked = false;
            ChkLight.IsChecked = false;
            ChkWater.IsChecked = false;
            ChkVibration.IsChecked = false;
        }

        private void DisplayOption_Changed(object sender, RoutedEventArgs e)
        {
            LoadChartsForAllNodes();
        }

        private void LayoutOption_Changed(object sender, RoutedEventArgs e)
        {
            if (LayoutSingle?.IsChecked == true)
                _columnsPerRow = 1;
            else if (LayoutDouble?.IsChecked == true)
                _columnsPerRow = 2;
            else if (LayoutTriple?.IsChecked == true)
                _columnsPerRow = 3;

            LoadChartsForAllNodes();
        }

        private void RefreshData_Click(object sender, RoutedEventArgs e)
        {
            LoadChartsForAllNodes();
        }

        private void ExportData_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Export data functionality - to be implemented");
        }

        public class LineData
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public List<NodeData> Nodes { get; set; } = new();
        }

        public class NodeData
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public double? Value { get; set; }
        }
    }
}

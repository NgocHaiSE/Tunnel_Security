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
        private HashSet<string> _selectedNodeIds = new();
        private HashSet<string> _selectedTypes = new() { "radar", "camera", "temperature", "humidity", "light", "water", "vibration" };
        private int _columnsPerRow = 2;
        private DispatcherTimer _realtimeTimer;
        private Random _random = new Random();
        private Dictionary<string, List<double>> _sensorHistoricalData = new();
        private Dictionary<string, CartesianChart> _chartInstances = new();
        private Dictionary<string, TextBlock> _sensorValueTexts = new();

        public DataPage()
        {
            this.InitializeComponent();
            InitializeMockData();
            this.Loaded += DataPage_Loaded;
            this.Unloaded += DataPage_Unloaded;
        }

        private void DataPage_Loaded(object sender, RoutedEventArgs e)
        {
            BuildNodeFilterComboBox();
            LoadChartsForAllNodes();
            StartRealtimeUpdates();
        }

        private void DataPage_Unloaded(object sender, RoutedEventArgs e)
        {
            StopRealtimeUpdates();
        }

        private void StartRealtimeUpdates()
        {
            _realtimeTimer = new DispatcherTimer();
            _realtimeTimer.Interval = TimeSpan.FromSeconds(2);
            _realtimeTimer.Tick += RealtimeTimer_Tick;
            _realtimeTimer.Start();
        }

        private void StopRealtimeUpdates()
        {
            if (_realtimeTimer != null)
            {
                _realtimeTimer.Stop();
                _realtimeTimer = null;
            }
        }

        private void RealtimeTimer_Tick(object sender, object e)
        {
            UpdateSensorValues();
            UpdateCharts();
        }

        private void UpdateSensorValues()
        {
            foreach (var line in _lines)
            {
                foreach (var node in line.Nodes)
                {
                    foreach (var sensor in node.Sensors)
                    {
                        if (sensor.Value.HasValue)
                        {
                            // Update sensor value with random variation
                            var baseValue = sensor.Value.Value;
                            var variation = sensor.Type switch
                            {
                                "temperature" => (_random.NextDouble() - 0.5) * 2,
                                "humidity" => (_random.NextDouble() - 0.5) * 4,
                                "light" => (_random.NextDouble() - 0.5) * 10,
                                "water" => (_random.NextDouble() - 0.5) * 2,
                                "vibration" => (_random.NextDouble() - 0.5) * 0.1,
                                "radar" => _random.Next(-1, 2),
                                "camera" => _random.Next(-1, 2),
                                _ => 0
                            };
                            
                            sensor.Value = Math.Max(0, baseValue + variation);
                            
                            // Update historical data
                            if (!_sensorHistoricalData.ContainsKey(sensor.Id))
                            {
                                _sensorHistoricalData[sensor.Id] = new List<double>();
                                for (int i = 0; i < 24; i++)
                                {
                                    _sensorHistoricalData[sensor.Id].Add(baseValue);
                                }
                            }
                            
                            var history = _sensorHistoricalData[sensor.Id];
                            history.RemoveAt(0);
                            history.Add(sensor.Value.Value);
                        }
                    }
                }
            }
        }

        private void UpdateCharts()
        {
            foreach (var kvp in _chartInstances)
            {
                var sensorId = kvp.Key;
                var chart = kvp.Value;
                
                if (_sensorHistoricalData.ContainsKey(sensorId))
                {
                    var sensor = _lines.SelectMany(l => l.Nodes)
                                       .SelectMany(n => n.Sensors)
                                       .FirstOrDefault(s => s.Id == sensorId);
                    
                    if (sensor != null && chart.Series != null)
                    {
                        var seriesArray = chart.Series.ToArray();
                        if (seriesArray.Length > 0)
                        {
                            // Update value text
                            if (_sensorValueTexts.ContainsKey(sensorId))
                            {
                                _sensorValueTexts[sensorId].Text = $"Giá trị: {sensor.Value:F1} {GetUnit(sensor.Type)}";
                            }
                            
                            if (sensor.Type == "vibration" && seriesArray[0] is ColumnSeries<double> columnSeries)
                            {
                                // Update vibration chart with more points
                                var values = new double[120];
                                var baseValue = sensor.Value ?? 0;
                                for (int i = 0; i < 120; i++)
                                {
                                    var variance = (_random.NextDouble() - 0.5) * 2;
                                    values[i] = baseValue + variance * baseValue * 2;
                                }
                                columnSeries.Values = values;
                            }
                            else if (seriesArray[0] is LineSeries<double> lineSeries)
                            {
                                // Update line chart
                                lineSeries.Values = _sensorHistoricalData[sensorId].ToArray();
                            }
                        }
                    }
                }
            }
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
                        new NodeData 
                        { 
                            Id = "NODE_A1", 
                            Name = "Node A1 - Cổng vào", 
                            Status = "normal",
                            Sensors = new List<SensorData>
                            {
                                new SensorData { Id = "S01", Name = "Radar", Type = "radar", Status = "normal", Value = 3 },
                                new SensorData { Id = "S02", Name = "Camera IR", Type = "camera", Status = "normal", Value = 1 },
                                new SensorData { Id = "S03", Name = "Nhiệt độ", Type = "temperature", Status = "normal", Value = 28.5 },
                                new SensorData { Id = "S04", Name = "Độ ẩm", Type = "humidity", Status = "normal", Value = 65 },
                                new SensorData { Id = "S05", Name = "Ánh sáng", Type = "light", Status = "normal", Value = 174 },
                                new SensorData { Id = "S06", Name = "Mực nước", Type = "water", Status = "normal", Value = 5 }
                            }
                        },
                        new NodeData 
                        { 
                            Id = "NODE_A2", 
                            Name = "Node A2 - Giữa tuyến", 
                            Status = "warning",
                            Sensors = new List<SensorData>
                            {
                                new SensorData { Id = "S07", Name = "Radar", Type = "radar", Status = "warning", Value = 15 },
                                new SensorData { Id = "S08", Name = "Camera IR", Type = "camera", Status = "normal", Value = 2 },
                                new SensorData { Id = "S09", Name = "Nhiệt độ", Type = "temperature", Status = "warning", Value = 32.1 },
                                new SensorData { Id = "S10", Name = "Độ ẩm", Type = "humidity", Status = "normal", Value = 68 },
                                new SensorData { Id = "S11", Name = "Ánh sáng", Type = "light", Status = "normal", Value = 165 },
                                new SensorData { Id = "S12", Name = "Rung động", Type = "vibration", Status = "warning", Value = 0.45 }
                            }
                        }
                    }
                },
                new LineData
                {
                    Id = "LINE_B",
                    Name = "Tuyến B",
                    Status = "active",
                    Nodes = new List<NodeData>
                    {
                        new NodeData 
                        { 
                            Id = "NODE_B1", 
                            Name = "Node B1 - Điểm đo 1", 
                            Status = "critical",
                            Sensors = new List<SensorData>
                            {
                                new SensorData { Id = "S13", Name = "Radar", Type = "radar", Status = "normal", Value = 2 },
                                new SensorData { Id = "S14", Name = "Camera IR", Type = "camera", Status = "normal", Value = 0 },
                                new SensorData { Id = "S15", Name = "Nhiệt độ", Type = "temperature", Status = "warning", Value = 35.2 },
                                new SensorData { Id = "S16", Name = "Độ ẩm", Type = "humidity", Status = "normal", Value = 70 },
                                new SensorData { Id = "S17", Name = "Ánh sáng", Type = "light", Status = "normal", Value = 180 },
                                new SensorData { Id = "S18", Name = "Mực nước", Type = "water", Status = "critical", Value = 25 }
                            }
                        },
                        new NodeData 
                        { 
                            Id = "NODE_B2", 
                            Name = "Node B2 - Điểm đo 2", 
                            Status = "normal",
                            Sensors = new List<SensorData>
                            {
                                new SensorData { Id = "S19", Name = "Radar", Type = "radar", Status = "normal", Value = 1 },
                                new SensorData { Id = "S20", Name = "Camera IR", Type = "camera", Status = "normal", Value = 0 },
                                new SensorData { Id = "S21", Name = "Nhiệt độ", Type = "temperature", Status = "normal", Value = 27.5 },
                                new SensorData { Id = "S22", Name = "Độ ẩm", Type = "humidity", Status = "normal", Value = 66 },
                                new SensorData { Id = "S23", Name = "Ánh sáng", Type = "light", Status = "normal", Value = 170 },
                                new SensorData { Id = "S24", Name = "Rung động", Type = "vibration", Status = "normal", Value = 0.12 }
                            }
                        }
                    }
                },
                new LineData
                {
                    Id = "LINE_C",
                    Name = "Tuyến C",
                    Status = "active",
                    Nodes = new List<NodeData>
                    {
                        new NodeData 
                        { 
                            Id = "NODE_C1", 
                            Name = "Node C1 - Khu vực 1", 
                            Status = "normal",
                            Sensors = new List<SensorData>
                            {
                                new SensorData { Id = "S25", Name = "Radar", Type = "radar", Status = "normal", Value = 1 },
                                new SensorData { Id = "S26", Name = "Camera IR", Type = "camera", Status = "normal", Value = 0 },
                                new SensorData { Id = "S27", Name = "Nhiệt độ", Type = "temperature", Status = "normal", Value = 27.8 },
                                new SensorData { Id = "S28", Name = "Độ ẩm", Type = "humidity", Status = "normal", Value = 68 },
                                new SensorData { Id = "S29", Name = "Ánh sáng", Type = "light", Status = "normal", Value = 165 },
                                new SensorData { Id = "S30", Name = "Mực nước", Type = "water", Status = "normal", Value = 8 }
                            }
                        }
                    }
                }
            };
        }

        //private void UpdateSensorCounts()
        //{
        //    var allSensors = _lines.SelectMany(l => l.Nodes).SelectMany(n => n.Sensors).ToList();
        //    RadarCount.Text = allSensors.Count(s => s.Type == "radar").ToString();
        //    CameraCount.Text = allSensors.Count(s => s.Type == "camera").ToString();
        //    TempCount.Text = allSensors.Count(s => s.Type == "temperature").ToString();
        //    HumidityCount.Text = allSensors.Count(s => s.Type == "humidity").ToString();
        //    LightCount.Text = allSensors.Count(s => s.Type == "light").ToString();
        //    WaterCount.Text = allSensors.Count(s => s.Type == "water").ToString();
        //    VibrationCount.Text = allSensors.Count(s => s.Type == "vibration").ToString();
        //}


        private void BuildNodeFilterComboBox()
        {
            if (NodeFilterComboBox == null) return;

            NodeFilterComboBox.Items.Clear();

            // Add "All Nodes" option
            var allItem = new ComboBoxItem
            {
                Content = "Tất cả nodes",
                Tag = "all"
            };
            NodeFilterComboBox.Items.Add(allItem);

            // Get lines based on selected line filter
            var linesToShow = _selectedLineId == "all" 
                ? _lines 
                : _lines.Where(l => l.Id == _selectedLineId).ToList();

            // Add nodes from filtered lines
            foreach (var line in linesToShow)
            {
                foreach (var node in line.Nodes)
                {
                    var nodeItem = new ComboBoxItem
                    {
                        Content = _selectedLineId == "all" 
                            ? $"{line.Name} - {node.Name}" 
                            : node.Name, // If specific line selected, no need to show line name
                        Tag = node.Id
                    };
                    NodeFilterComboBox.Items.Add(nodeItem);
                }
            }

            NodeFilterComboBox.SelectedIndex = 0;
            _selectedNodeIds.Clear(); // Reset node selection when rebuilding
        }

        private void NodeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NodeFilterComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _selectedNodeIds.Clear();
                
                if (tag != "all")
                {
                    _selectedNodeIds.Add(tag);
                }

                LoadChartsForAllNodes();
            }
        }

        private void LoadChartsForAllNodes()
        {
            if (ChartsPanel == null || CameraPanel == null) return;

            ChartsPanel.Children.Clear();
            CameraPanel.Children.Clear();
            _chartInstances.Clear();
            _sensorValueTexts.Clear();
            
            var filteredSensors = GetFilteredSensors();
            
            // Separate cameras from other sensors
            var cameraSensors = filteredSensors.Where(s => s.Type == "camera").ToList();
            var chartSensors = filteredSensors.Where(s => s.Type != "camera").ToList();

            // Handle Charts Panel
            if (chartSensors.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
                ChartCountText.Text = "0 biểu đồ";
            }
            else
            {
                EmptyState.Visibility = Visibility.Collapsed;
                ChartCountText.Text = $"{chartSensors.Count} biểu đồ";

                // Create rows based on layout setting
                var currentRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
                int itemsInRow = 0;

                foreach (var sensor in chartSensors)
                {
                    var chartCard = CreateChartCard(sensor);
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

            // Handle Camera Panel
            if (cameraSensors.Count == 0)
            {
                CameraEmptyState.Visibility = Visibility.Visible;
                CameraCountText.Text = "0 camera";
            }
            else
            {
                CameraEmptyState.Visibility = Visibility.Collapsed;
                CameraCountText.Text = $"{cameraSensors.Count} camera";

                foreach (var sensor in cameraSensors)
                {
                    var cameraCard = CreateCameraCard(sensor);
                    CameraPanel.Children.Add(cameraCard);
                }
            }
        }

        private List<SensorData> GetFilteredSensors()
        {
            // Get all nodes
            var allNodes = _lines.SelectMany(l => l.Nodes).ToList();

            // Filter by line
            if (_selectedLineId != "all")
            {
                var line = _lines.FirstOrDefault(l => l.Id == _selectedLineId);
                allNodes = line?.Nodes ?? new List<NodeData>();
            }

            // Filter by selected nodes
            if (_selectedNodeIds.Count > 0)
            {
                allNodes = allNodes.Where(n => _selectedNodeIds.Contains(n.Id)).ToList();
            }

            // Get all sensors from filtered nodes
            var allSensors = allNodes.SelectMany(n => n.Sensors).ToList();

            // Filter by sensor type (checkboxes in sidebar)
            allSensors = allSensors.Where(s => _selectedTypes.Contains(s.Type)).ToList();

            // Filter by status
            if (_selectedStatus != "all")
            {
                allSensors = allSensors.Where(s => s.Status == _selectedStatus).ToList();
            }

            // Filter by search text
            if (!string.IsNullOrEmpty(_searchText))
            {
                allSensors = allSensors.Where(s => 
                    s.Name.ToLower().Contains(_searchText.ToLower()) ||
                    s.Id.ToLower().Contains(_searchText.ToLower())
                ).ToList();
            }

            return allSensors;
        }

        private Border CreateChartCard(SensorData sensor)
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
                Text = sensor.Name,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            var subtitle = new TextBlock
            {
                Text = $"Giá trị: {sensor.Value:F1} {GetUnit(sensor.Type)}",
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["TextSecondaryBrush"],
                Margin = new Thickness(0, 2, 0, 0)
            };
            
            // Store subtitle reference for realtime updates
            _sensorValueTexts[sensor.Id] = subtitle;
            
            titleStack.Children.Add(title);
            titleStack.Children.Add(subtitle);
            Grid.SetColumn(titleStack, 0);


            header.Children.Add(titleStack);
            Grid.SetRow(header, 0);

            // Chart or Radar View (no camera here)
            FrameworkElement chart;
            if (sensor.Type == "radar")
            {
                // Use WebView2 radar chart for radar sensors
                chart = CreateRadarChart(sensor);
            }
            else
            {
                // Use line chart for other sensors
                chart = CreateChart(sensor);
            }
            Grid.SetRow(chart, 1);

            grid.Children.Add(header);
            grid.Children.Add(chart);
            card.Child = grid;

            return card;
        }

        private RadarChartControl CreateRadarChart(SensorData sensor)
        {
            var random = new Random();
            var detections = new List<RadarDetection>();

            // Generate random detections based on sensor value
            var detectionCount = (int)(sensor.Value ?? 0);
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

        private Grid CreateCameraView(NodeData node)
        {
            var grid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 20, 20, 30)),
                CornerRadius = new CornerRadius(8)
            };

            // Camera feed placeholder with border
            var videoContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 40)),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(8),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 80)),
                BorderThickness = new Thickness(1)
            };

            // Video feed with live indicator
            var videoGrid = new Grid();

            // Placeholder video feed (you can replace this with actual video stream)
            var placeholder = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb(255, 45, 55, 72), Offset = 0 },
                        new GradientStop { Color = Color.FromArgb(255, 30, 40, 55), Offset = 1 }
                    }
                }
            };

            // Camera icon in center
            var cameraIcon = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 8
            };

            var icon = new FontIcon
            {
                Glyph = "\uE714", // Camera icon
                FontSize = 48,
                Foreground = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255))
            };

            var statusText = new TextBlock
            {
                Text = "Camera Feed",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            cameraIcon.Children.Add(icon);
            cameraIcon.Children.Add(statusText);

            // Live indicator
            var liveIndicator = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(12, 12, 0, 0)
            };

            var liveStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            var recordDot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };

            liveStack.Children.Add(recordDot);
            liveIndicator.Child = liveStack;

            // Camera info overlay
            var infoPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 12, 12),
                Spacing = 4
            };

            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var fpsText = new TextBlock
            {
                Text = "30 FPS • 1080p",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            infoPanel.Children.Add(timeText);
            infoPanel.Children.Add(fpsText);

            videoGrid.Children.Add(placeholder);
            videoGrid.Children.Add(cameraIcon);
            videoGrid.Children.Add(liveIndicator);
            videoGrid.Children.Add(infoPanel);

            videoContainer.Child = videoGrid;
            grid.Children.Add(videoContainer);

            return grid;
        }

        private Border CreateCameraCard(SensorData sensor)
        {
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["BackgroundSecondaryBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                MinHeight = 220
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header
            var header = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            var title = new TextBlock
            {
                Text = sensor.Name,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            titleStack.Children.Add(title);
            Grid.SetColumn(titleStack, 0);


            header.Children.Add(titleStack);
            //header.Children.Add(statusBadge);
            Grid.SetRow(header, 0);

            // Camera View (compact version for sidebar)
            var cameraView = CreateCompactCameraView(sensor);
            Grid.SetRow(cameraView, 1);

            grid.Children.Add(header);
            grid.Children.Add(cameraView);
            card.Child = grid;

            return card;
        }

        private Grid CreateCompactCameraView(SensorData sensor)
        {
            var grid = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 20, 20, 30)),
                CornerRadius = new CornerRadius(6)
            };

            // Camera feed placeholder
            var videoContainer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 40)),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(4),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 60, 60, 80)),
                BorderThickness = new Thickness(1)
            };

            var videoGrid = new Grid();

            // Placeholder
            var placeholder = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0),
                    EndPoint = new Windows.Foundation.Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb(255, 45, 55, 72), Offset = 0 },
                        new GradientStop { Color = Color.FromArgb(255, 30, 40, 55), Offset = 1 }
                    }
                }
            };

            // Camera icon
            var cameraIcon = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 6
            };

            var icon = new FontIcon
            {
                Glyph = "\uE714",
                FontSize = 32,
                Foreground = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255))
            };

            var statusText = new TextBlock
            {
                Text = "Camera Feed",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            cameraIcon.Children.Add(icon);
            cameraIcon.Children.Add(statusText);


            var liveStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };

            var recordDot = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = new SolidColorBrush(Microsoft.UI.Colors.White),
                VerticalAlignment = VerticalAlignment.Center
            };


            liveStack.Children.Add(recordDot);

            // Info overlay
            var infoPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 8, 8),
                Spacing = 2
            };

            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                FontSize = 9,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var fpsText = new TextBlock
            {
                Text = "30 FPS",
                FontSize = 8,
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Right
            };

            infoPanel.Children.Add(timeText);
            infoPanel.Children.Add(fpsText);

            videoGrid.Children.Add(placeholder);
            videoGrid.Children.Add(cameraIcon);
            videoGrid.Children.Add(infoPanel);

            videoContainer.Child = videoGrid;
            grid.Children.Add(videoContainer);

            return grid;
        }

        private CartesianChart CreateChart(SensorData sensor)
        {
            var random = new Random();
            var baseValue = sensor.Value ?? 0;
            var showGrid = ChkShowGrid?.IsChecked ?? true;

            // Initialize historical data if not exists
            if (!_sensorHistoricalData.ContainsKey(sensor.Id))
            {
                _sensorHistoricalData[sensor.Id] = new List<double>();
                for (int i = 0; i < 24; i++)
                {
                    var variance = (random.NextDouble() - 0.5) * baseValue * 0.2;
                    _sensorHistoricalData[sensor.Id].Add(Math.Max(0, baseValue + variance));
                }
            }

            // Biểu đồ rung động dùng bar chart với nhiều điểm dữ liệu
            if (sensor.Type == "vibration")
            {
                var dataPoints = 120; // Nhiều điểm dữ liệu để tạo hiệu ứng dày đặc
                var values = new double[dataPoints];

                for (int i = 0; i < dataPoints; i++)
                {
                    // Tạo dao động ngẫu nhiên xung quanh giá trị trung bình
                    var variance = (random.NextDouble() - 0.5) * 2;
                    values[i] = baseValue + variance * baseValue * 2;
                }

                var series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = values,
                        Fill = new SolidColorPaint(GetChartColor(sensor.Type, 180)),
                        Stroke = null,
                        MaxBarWidth = 3,
                        IgnoresBarPosition = true
                    }
                };

                var xAxis = new Axis
                {
                    Labels = Enumerable.Range(0, 7).Select(i => $"{i * 10}s").ToArray(),
                    LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                    SeparatorsPaint = showGrid ? new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 } : null,
                    TextSize = 10,
                    MinLimit = 0
                };

                var yAxis = new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                    SeparatorsPaint = showGrid ? new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 } : null,
                    TextSize = 10,
                    MinLimit = -baseValue * 3,
                    MaxLimit = baseValue * 3
                };

                var chart = new CartesianChart
                {
                    Series = series,
                    XAxes = new[] { xAxis },
                    YAxes = new[] { yAxis }
                };

                _chartInstances[sensor.Id] = chart;
                return chart;
            }
            else
            {
                // Các loại biểu đồ khác giữ nguyên dạng line chart
                var values = _sensorHistoricalData[sensor.Id].ToArray();

                var showDataPoints = ChkShowDataPoints?.IsChecked ?? true;
                var smoothLine = ChkSmoothLine?.IsChecked ?? true ? 0.5 : 0;

                var series = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = values,
                        Fill = new LinearGradientPaint(
                            GetChartColor(sensor.Type, 60),
                            GetChartColor(sensor.Type, 10),
                            new SKPoint(0, 0),
                            new SKPoint(0, 1)),
                        Stroke = new SolidColorPaint(GetChartColor(sensor.Type)) { StrokeThickness = 2 },
                        GeometrySize = showDataPoints ? 6 : 0,
                        GeometryStroke = showDataPoints ? new SolidColorPaint(GetChartColor(sensor.Type)) { StrokeThickness = 2 } : null,
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

                var chart = new CartesianChart
                {
                    Series = series,
                    XAxes = new[] { xAxis },
                    YAxes = new[] { yAxis }
                };

                _chartInstances[sensor.Id] = chart;
                return chart;
            }
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
            
            // Rebuild node dropdown based on selected line
            BuildNodeFilterComboBox();
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
            public string Status { get; set; } = string.Empty;
            public List<SensorData> Sensors { get; set; } = new();
        }

        public class SensorData
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
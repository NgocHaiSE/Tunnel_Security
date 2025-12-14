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

namespace Station.Views
{
    public sealed partial class DataPage : Page
    {
        private List<LineData> _lines = new();
        private string _selectedLineId = "all";
        private string _selectedNodeType = "all";
        private string _selectedStatus = "all";
        private int _timeRangeHours = 6;

        public DataPage()
        {
            this.InitializeComponent();
            InitializeMockData();
            BuildLineTree();
            this.Loaded += DataPage_Loaded;
        }

        private void DataPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadChartsForAllNodes();
        }

        private void InitializeMockData()
        {
            // Mock data: 3 tuyến, mỗi tuyến có 6 nút
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

        private void BuildLineTree()
        {
            LineTreePanel.Children.Clear();

            foreach (var line in _lines)
            {
                var expander = new Expander
                {
                    Header = CreateLineHeader(line),
                    IsExpanded = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var nodesList = new StackPanel { Spacing = 4, Margin = new Thickness(16, 8, 0, 0) };

                foreach (var node in line.Nodes)
                {
                    var nodeButton = new Button
                    {
                        Content = CreateNodeContent(node),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(8, 6, 8, 6),
                        Tag = node
                    };

                    nodeButton.Click += NodeButton_Click;
                    nodesList.Children.Add(nodeButton);
                }

                expander.Content = nodesList;
                LineTreePanel.Children.Add(expander);
            }
        }

        private UIElement CreateLineHeader(LineData line)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameText = new TextBlock
            {
                Text = $"📍 {line.Name}",
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 14
            };
            Grid.SetColumn(nameText, 0);

            var countBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 2, 8, 2),
                Child = new TextBlock
                {
                    Text = $"{line.Nodes.Count} nút",
                    FontSize = 10,
                    //Foreground = new SolidColorBrush(Colors.White)
                }
            };
            Grid.SetColumn(countBadge, 1);

            grid.Children.Add(nameText);
            grid.Children.Add(countBadge);

            return grid;
        }

        private UIElement CreateNodeContent(NodeData node)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new TextBlock
            {
                Text = node.Icon,
                FontSize = 16,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(icon, 0);

            var name = new TextBlock
            {
                Text = node.Name,
                FontSize = 12,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(name, 1);

            var statusDot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = GetStatusBrush(node.Status)
            };
            Grid.SetColumn(statusDot, 2);

            grid.Children.Add(icon);
            grid.Children.Add(name);
            grid.Children.Add(statusDot);

            return grid;
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

        private void NodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is NodeData node)
            {
                LoadChartForNode(node);
            }
        }

        private void LoadChartsForAllNodes()
        {
            if (ChartsPanel == null)
            {
                Debug.WriteLine("ChartsPanel is null, skipping chart load");
                return;
            }

            ChartsPanel.Children.Clear();

            var filteredNodes = GetFilteredNodes();

            foreach (var node in filteredNodes)
            {
                var chartCard = CreateChartCard(node);
                ChartsPanel.Children.Add(chartCard);
            }
        }

        private List<NodeData> GetFilteredNodes()
        {
            var allNodes = _lines.SelectMany(l => l.Nodes).ToList();

            if (_selectedLineId != "all")
            {
                var line = _lines.FirstOrDefault(l => l.Id == _selectedLineId);
                allNodes = line?.Nodes ?? new List<NodeData>();
            }

            if (_selectedNodeType != "all")
            {
                allNodes = allNodes.Where(n => n.Type == _selectedNodeType).ToList();
            }

            if (_selectedStatus != "all")
            {
                allNodes = allNodes.Where(n => n.Status == _selectedStatus).ToList();
            }

            return allNodes;
        }

        private Border CreateChartCard(NodeData node)
        {
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["MonitoringPanelBackgroundBrush"],
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(300) });

            var header = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel();
            var title = new TextBlock
            {
                Text = $"{node.Icon} {node.Name}",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            var subtitle = new TextBlock
            {
                Text = $"Giá trị hiện tại: {node.Value} {GetUnit(node.Type)}",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["MonitoringTextSecondaryBrush"],
                Margin = new Thickness(0, 4, 0, 0)
            };
            titleStack.Children.Add(title);
            titleStack.Children.Add(subtitle);
            Grid.SetColumn(titleStack, 0);

            var statusBadge = new Border
            {
                Background = GetStatusBrush(node.Status),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 4, 12, 4),
                Child = new TextBlock
                {
                    Text = GetStatusText(node.Status),
                    FontSize = 11,
                    //Foreground = new SolidColorBrush(Colors.White)
                }
            };
            Grid.SetColumn(statusBadge, 1);

            header.Children.Add(titleStack);
            header.Children.Add(statusBadge);
            Grid.SetRow(header, 0);

            var chart = CreateChart(node);
            Grid.SetRow(chart, 1);

            grid.Children.Add(header);
            grid.Children.Add(chart);
            card.Child = grid;

            return card;
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
                    Stroke = new SolidColorPaint(GetChartColor(node.Type)) { StrokeThickness = 3 },
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(GetChartColor(node.Type)) { StrokeThickness = 2 },
                    GeometryFill = new SolidColorPaint(new SKColor(255, 255, 255)),
                    LineSmoothness = 0.5
                }
            };

            var xAxis = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[]
            {
                new Axis
                {
                    Labels = new[] { "00h", "04h", "08h", "12h", "16h", "20h", "24h" },
                    LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
                    TextSize = 11
                }
            };

            var yAxis = new LiveChartsCore.Kernel.Sketches.ICartesianAxis[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                    SeparatorsPaint = new SolidColorPaint(new SKColor(226, 232, 240)) { StrokeThickness = 1 },
                    TextSize = 11
                }
            };

            return new CartesianChart
            {
                Series = series,
                XAxes = xAxis,
                YAxes = yAxis
            };
        }

        private SKColor GetChartColor(string type, byte alpha = 255)
        {
            return type switch
            {
                "radar" => new SKColor(16, 185, 129, alpha),
                "camera" => new SKColor(59, 130, 246, alpha),
                "temperature" => new SKColor(239, 68, 68, alpha),
                "humidity" => new SKColor(59, 130, 246, alpha),
                "light" => new SKColor(251, 191, 36, alpha),
                "water" => new SKColor(14, 165, 233, alpha),
                "vibration" => new SKColor(245, 158, 11, alpha),
                _ => new SKColor(156, 163, 175, alpha)
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

        private void LoadChartForNode(NodeData node)
        {
            ChartsPanel.Children.Clear();
            var chartCard = CreateChartCard(node);
            ChartsPanel.Children.Add(chartCard);
        }

        private void LineFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LineFilterComboBox.SelectedIndex == 0)
                _selectedLineId = "all";
            else if (LineFilterComboBox.SelectedIndex == 1)
                _selectedLineId = "LINE_A";
            else if (LineFilterComboBox.SelectedIndex == 2)
                _selectedLineId = "LINE_B";
            else if (LineFilterComboBox.SelectedIndex == 3)
                _selectedLineId = "LINE_C";

            LoadChartsForAllNodes();
        }

        private void NodeTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedNodeType = NodeTypeFilterComboBox.SelectedIndex switch
            {
                0 => "all",
                1 => "radar",
                2 => "camera",
                3 => "temperature",
                4 => "humidity",
                5 => "light",
                6 => "water",
                7 => "vibration",
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
                4 => "offline",
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

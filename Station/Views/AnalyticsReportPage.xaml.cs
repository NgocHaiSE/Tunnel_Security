using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp;
using Station.Services;



namespace Station.Views
{
    public sealed partial class AnalyticsReportPage : Page
    {
        public ObservableCollection<TopNodeStat> TopNodes { get; } =
            new ObservableCollection<TopNodeStat>();

        public ObservableCollection<AlertHistoryRecord> History { get; } =
            new ObservableCollection<AlertHistoryRecord>();
        public IEnumerable<ISeries> AlertsByLineSeries { get; set; } = System.Array.Empty<ISeries>();
        public IEnumerable<ICartesianAxis> LineAxes { get; set; } = System.Array.Empty<ICartesianAxis>();
        public IEnumerable<ICartesianAxis> LineYAxes { get; set; } = System.Array.Empty<ICartesianAxis>();

        public IEnumerable<ISeries> AlertsByHourSeries { get; set; } = System.Array.Empty<ISeries>();
        public IEnumerable<ICartesianAxis> HourAxes { get; set; } = System.Array.Empty<ICartesianAxis>();
        public IEnumerable<ICartesianAxis> HourYAxes { get; set; } = System.Array.Empty<ICartesianAxis>();

        public AnalyticsReportPage()
        {
            this.InitializeComponent();   // lúc này sẽ hết báo lỗi
            TopNodeList.ItemsSource = TopNodes;
            HistoryGrid.ItemsSource = History;
            SetupCharts();
            SeedDemoData();
        }

        private void SeedDemoData()
        {
            TopNodes.Clear();
            TopNodes.Add(new TopNodeStat { Rank = 1, NodeCode = "SEN-002", LineName = "Tuyến A1", AlertCount = 23 });
            TopNodes.Add(new TopNodeStat { Rank = 2, NodeCode = "SEN-015", LineName = "Tuyến B3", AlertCount = 17 });
            TopNodes.Add(new TopNodeStat { Rank = 3, NodeCode = "SEN-021", LineName = "Tuyến C2", AlertCount = 11 });
            TopNodes.Add(new TopNodeStat { Rank = 3, NodeCode = "SEN-022", LineName = "Tuyến B2", AlertCount = 1 });


            History.Clear();
            History.Add(new AlertHistoryRecord { Timestamp = "19:47 09/12/2025", Line = "A1", Node = "SEN-002", AlertType = "Nhiệt độ vượt ngưỡng", Severity = "Cao", Status = "Chờ xử lý" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:45 09/12/2025", Line = "B2", Node = "SEN-010", AlertType = "Rung động bất thường", Severity = "Trung bình", Status = "Đang xử lý" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:40 09/12/2025", Line = "A1", Node = "SEN-002", AlertType = "Độ ẩm vượt ngưỡng", Severity = "Thấp", Status = "Đã đóng" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:35 09/12/2025", Line = "C1", Node = "SEN-030", AlertType = "Mực nước tăng nhanh", Severity = "Cao", Status = "Đang xử lý" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:30 09/12/2025", Line = "B1", Node = "SEN-018", AlertType = "Rung động bất thường", Severity = "Trung bình", Status = "Đã đóng" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:25 09/12/2025", Line = "A2", Node = "SEN-008", AlertType = "Nhiệt độ vượt ngưỡng", Severity = "Cao", Status = "Đang xử lý" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:20 09/12/2025", Line = "C2", Node = "SEN-033", AlertType = "Mất tín hiệu cảm biến", Severity = "Nghiêm trọng", Status = "Chờ xử lý" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:15 09/12/2025", Line = "B2", Node = "SEN-012", AlertType = "Độ ẩm vượt ngưỡng", Severity = "Thấp", Status = "Đã đóng" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:10 09/12/2025", Line = "A1", Node = "SEN-005", AlertType = "Mực nước vượt ngưỡng", Severity = "Trung bình", Status = "Đã đóng" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:05 09/12/2025", Line = "B1", Node = "SEN-017", AlertType = "Nhiệt độ cảm biến bất thường", Severity = "Thấp", Status = "Đã đóng" });
            History.Add(new AlertHistoryRecord { Timestamp = "19:00 09/12/2025", Line = "C1", Node = "SEN-029", AlertType = "Rung động bất thường", Severity = "Cao", Status = "Đang xử lý" });
            History.Add(new AlertHistoryRecord { Timestamp = "18:55 09/12/2025", Line = "A2", Node = "SEN-009", AlertType = "Mực nước tăng nhanh", Severity = "Trung bình", Status = "Đã đóng" });
            History.Add(new AlertHistoryRecord { Timestamp = "18:50 09/12/2025", Line = "C2", Node = "SEN-034", AlertType = "Mất kết nối gateway", Severity = "Nghiêm trọng", Status = "Chờ xử lý" });
        }

        private void SetupCharts()
        {
            // ===== Màu cho trục =====
            var axisLabelColor = new SKColor(226, 232, 240);   // gần trắng
            var axisGridColor = new SKColor(75, 85, 99);      // xám đậm hơn nền

            // ==================================================================
            // 1) BIỂU ĐỒ: Xu hướng cảnh báo theo tuyến
            // ==================================================================
            // Mock data: 5 tuyến với số cảnh báo khác nhau
            var lineNames = new[] { "Tuyến A1", "Tuyến A2", "Tuyến B1", "Tuyến B2", "Tuyến C1" };
            var lineCounts = new[] { 12, 7, 19, 4, 15 };

            AlertsByLineSeries = new ISeries[]
            {
        new ColumnSeries<int>
        {
            Name  = "Số cảnh báo",
            Values = lineCounts,

            // Làm cột nổi bật hơn
            Fill   = new SolidColorPaint(new SKColor(59, 130, 246)),          // xanh dương sáng
            Stroke = new SolidColorPaint(new SKColor(147, 197, 253))          // viền nhạt
            { StrokeThickness = 1 },

            // Hiển thị số ngay trên từng cột
            DataLabelsPaint     = new SolidColorPaint(axisLabelColor),
            DataLabelsPosition  = DataLabelsPosition.Top,
            DataLabelsFormatter = point => point.PrimaryValue.ToString("0")
        }
            };

            LineAxes = new ICartesianAxis[]
            {
        new Axis
        {
            Labels      = lineNames,
            LabelsPaint = new SolidColorPaint(axisLabelColor),
            TextSize    = 13,
            SeparatorsPaint = new SolidColorPaint(axisGridColor)
            {
                StrokeThickness = 1
            }
        }
            };

            LineYAxes = new ICartesianAxis[]
            {
        new Axis
        {
            Name        = "Số cảnh báo",
            LabelsPaint = new SolidColorPaint(axisLabelColor),
            TextSize    = 13,
            SeparatorsPaint = new SolidColorPaint(axisGridColor)
            {
                StrokeThickness = 1
            }
        }
            };

            // ==================================================================
            // 2) BIỂU ĐỒ: Xu hướng theo thời gian trong ngày
            // ==================================================================
            // Mock data: 24 giá trị, cao vào giờ cao điểm
            var hourValues = new int[24];
            var rand = new Random();

            for (int h = 0; h < 24; h++)
            {
                int baseVal;
                if (h >= 6 && h <= 9) baseVal = 8;   // sáng
                else if (h >= 16 && h <= 20) baseVal = 10;  // chiều tối
                else baseVal = 3;   // lúc khác

                hourValues[h] = Math.Max(0, baseVal + rand.Next(-2, 3));
            }

            AlertsByHourSeries = new ISeries[]
            {
        new ColumnSeries<int>
        {
            Name   = "Cảnh báo",
            Values = hourValues,

            Fill   = new SolidColorPaint(new SKColor(34, 197, 94)),       // xanh lá sáng
            Stroke = new SolidColorPaint(new SKColor(134, 239, 172))
            { StrokeThickness = 1 },

            // không bật DataLabels cho 24 cột để đỡ rối – tooltip vẫn có khi hover
        }
            };

            HourAxes = new ICartesianAxis[]
            {
        new Axis
        {
            Labels = Enumerable.Range(0, 24)
                               .Select(h => $"{h}h")
                               .ToArray(),
            LabelsPaint = new SolidColorPaint(axisLabelColor),
            TextSize    = 11,
            SeparatorsPaint = new SolidColorPaint(axisGridColor)
            {
                StrokeThickness = 1
            }
        }
            };

            HourYAxes = new ICartesianAxis[]
            {
        new Axis
        {
            Name        = "Số cảnh báo",
            LabelsPaint = new SolidColorPaint(axisLabelColor),
            TextSize    = 11,
            SeparatorsPaint = new SolidColorPaint(axisGridColor)
            {
                StrokeThickness = 1
            }
        }
            };
        }



        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            var path = ReportExporter.ExportHistoryToExcel(
                History,
                TopNodes,
                "TRM-HN-001",
                "Trạm Nghĩa Đô");

            await ShowExportDoneDialogAsync("Excel", path);
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var path = ReportExporter.ExportHistoryToPdf(
                History,
                TopNodes,
                "TRM-HN-001",
                "Trạm Nghĩa Đô");

            await ShowExportDoneDialogAsync("PDF", path);
        }

        private async Task ShowExportDoneDialogAsync(string kind, string path)
        {
            var dialog = new ContentDialog
            {
                Title = $"Xuất file {kind}",
                Content = $"Đã xuất file {kind} thành công.\nĐường dẫn:\n{path}",
                CloseButtonText = "Đóng",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

    }

    public class TopNodeStat
    {
        public int Rank { get; set; }
        public string NodeCode { get; set; } = "";
        public string LineName { get; set; } = "";
        public int AlertCount { get; set; }
    }

    public class AlertHistoryRecord
    {
        public string Timestamp { get; set; } = "";
        public string Line { get; set; } = "";
        public string Node { get; set; } = "";
        public string AlertType { get; set; } = "";
        public string Severity { get; set; } = "";
        public string Status { get; set; } = "";
    }
}

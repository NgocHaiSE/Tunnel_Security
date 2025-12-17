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
using SkiaSharp;
using Station.Services;

namespace Station.Views
{
    public sealed partial class AnalyticsReportPage : Page
    {
        // ====== LIST / TABLE DATA ======
        public ObservableCollection<TopNodeStat> TopNodes { get; } =
            new ObservableCollection<TopNodeStat>();

        public ObservableCollection<AlertHistoryRecord> History { get; } =
            new ObservableCollection<AlertHistoryRecord>();

        // ====== CHART 1: Xu hướng cảnh báo theo tuyến ======
        public IEnumerable<ISeries> AlertsByLineSeries { get; set; } = Array.Empty<ISeries>();
        public IEnumerable<ICartesianAxis> LineAxes { get; set; } = Array.Empty<ICartesianAxis>();
        public IEnumerable<ICartesianAxis> LineYAxes { get; set; } = Array.Empty<ICartesianAxis>();

        // ====== CHART 2: Xu hướng theo thời gian trong ngày ======
        public IEnumerable<ISeries> AlertsByHourSeries { get; set; } = Array.Empty<ISeries>();
        public IEnumerable<ICartesianAxis> HourAxes { get; set; } = Array.Empty<ICartesianAxis>();
        public IEnumerable<ICartesianAxis> HourYAxes { get; set; } = Array.Empty<ICartesianAxis>();

        // ====== CHART 3: Xu hướng mực nước cao bất thường ======
        public IEnumerable<ISeries> WaterLevelSeries { get; set; } = Array.Empty<ISeries>();
        public IEnumerable<ICartesianAxis> WaterAxes { get; set; } = Array.Empty<ICartesianAxis>();
        public IEnumerable<ICartesianAxis> WaterYAxes { get; set; } = Array.Empty<ICartesianAxis>();

        // ====== CHART 4: Top đoạn cống rủi ro ======
        public IEnumerable<ISeries> SegmentRiskSeries { get; set; } = Array.Empty<ISeries>();
        public IEnumerable<ICartesianAxis> SegmentAxes { get; set; } = Array.Empty<ICartesianAxis>();
        public IEnumerable<ICartesianAxis> SegmentYAxes { get; set; } = Array.Empty<ICartesianAxis>();
        // ====== CHART 4: Top tuyến có thiết bị lỗi / xâm nhập ======
        public IEnumerable<ISeries> TopLinesIssueSeries { get; set; } = Array.Empty<ISeries>();
        public IEnumerable<ICartesianAxis> TopLinesAxes { get; set; } = Array.Empty<ICartesianAxis>();
        public IEnumerable<ICartesianAxis> TopLinesYAxes { get; set; } = Array.Empty<ICartesianAxis>();

        public AnalyticsReportPage()
        {
            InitializeComponent();

            TopNodeList.ItemsSource = TopNodes;
            HistoryGrid.ItemsSource = History;

            SetupCharts();
            SeedDemoData();
        }

        // =====================================================================
        //  DEMO DATA CHO BẢNG / TOP NODE
        // =====================================================================
        private void SeedDemoData()
        {
            TopNodes.Clear();
            TopNodes.Add(new TopNodeStat { Rank = 1, NodeCode = "SEN-002", LineName = "Tuyến A1", AlertCount = 23 });
            TopNodes.Add(new TopNodeStat { Rank = 2, NodeCode = "SEN-015", LineName = "Tuyến B3", AlertCount = 17 });
            TopNodes.Add(new TopNodeStat { Rank = 3, NodeCode = "SEN-021", LineName = "Tuyến C2", AlertCount = 11 });
            TopNodes.Add(new TopNodeStat { Rank = 4, NodeCode = "SEN-022", LineName = "Tuyến B2", AlertCount = 8 });

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

        // =====================================================================
        //  CẤU HÌNH TẤT CẢ CÁC BIỂU ĐỒ
        // =====================================================================
        private void SetupCharts()
        {
            var axisLabelColor = new SKColor(226, 232, 240);   // gần trắng
            var axisGridColor = new SKColor(75, 85, 99);       // xám đậm

            // -----------------------------------------------------------------
            // 1) Xu hướng cảnh báo theo tuyến
            // -----------------------------------------------------------------
            var lineNames = new[] { "Tuyến A1", "Tuyến A2", "Tuyến B1", "Tuyến B2", "Tuyến C1" };
            var lineCounts = new[] { 12, 7, 19, 4, 15 };

            AlertsByLineSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name   = "Số cảnh báo",
                    Values = lineCounts,
                    Fill   = new SolidColorPaint(new SKColor(59,130,246)),
                    Stroke = new SolidColorPaint(new SKColor(147,197,253)) { StrokeThickness = 1 },
                    DataLabelsPaint     = new SolidColorPaint(axisLabelColor),
                    DataLabelsPosition  = DataLabelsPosition.Top,
                    DataLabelsFormatter = p => p.PrimaryValue.ToString("0")
                }
            };

            LineAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labels      = lineNames,
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 13,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };

            LineYAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Name        = "Số cảnh báo",
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 13,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };

            // -----------------------------------------------------------------
            // 2) Xu hướng theo thời gian trong ngày
            // -----------------------------------------------------------------
            var hourValues = new int[24];
            var rand = new Random();

            for (int h = 0; h < 24; h++)
            {
                int baseVal;
                if (h >= 6 && h <= 9) baseVal = 8;          // sáng
                else if (h >= 16 && h <= 20) baseVal = 10;  // chiều tối
                else baseVal = 3;                           // thời gian khác

                hourValues[h] = Math.Max(0, baseVal + rand.Next(-2, 3));
            }

            AlertsByHourSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name   = "Cảnh báo",
                    Values = hourValues,
                    Fill   = new SolidColorPaint(new SKColor(34,197,94)),
                    Stroke = new SolidColorPaint(new SKColor(134,239,172)) { StrokeThickness = 1 }
                }
            };

            HourAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labels = Enumerable.Range(0, 24).Select(h => $"{h}h").ToArray(),
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 11,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };

            HourYAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Name        = "Số cảnh báo",
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 11,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };

            // -----------------------------------------------------------------
            // 3) Xu hướng mực nước cao bất thường
            //     (ví dụ: 10 lần vượt ngưỡng trong ngày)
            // -----------------------------------------------------------------
            var waterEvents = Enumerable.Range(1, 10).Select(i => $"Lần {i}").ToArray();
            var waterLevels = new[] { 5, 7, 6, 9, 8, 10, 7, 6, 9, 8 }; // cm

            WaterLevelSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name   = "Mực nước (cm)",
                    Values = waterLevels,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(new SKColor(56,189,248)) { StrokeThickness = 3 },
                    Fill   = new SolidColorPaint(new SKColor(56,189,248,60)),
                    DataLabelsPaint     = new SolidColorPaint(axisLabelColor),
                    DataLabelsPosition  = DataLabelsPosition.Top,
                    DataLabelsFormatter = p => p.PrimaryValue.ToString("0")
                }
            };

            WaterAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labels      = waterEvents,
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 11,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };

            WaterYAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Name        = "Mực nước (cm)",
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 11,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };

            // -----------------------------------------------------------------
            // 4) Top đoạn cống rủi ro
            //     (tắc/ngập/thết bị hỏng/xâm nhập...)
            // -----------------------------------------------------------------
            var segmentNames = new[]
            {
                "Đoạn A1-03",
                "Đoạn B1-07",
                "Đoạn B2-02",
                "Đoạn C1-01",
                "Đoạn C2-05"
            };

            var segmentScores = new[] { 10, 8, 7, 5, 4 }; // điểm rủi ro tổng hợp

            SegmentRiskSeries = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Name   = "Điểm rủi ro",
                    Values = segmentScores,
                    Fill   = new SolidColorPaint(new SKColor(249,115,22)),   // cam
                    Stroke = new SolidColorPaint(new SKColor(253,186,116)) { StrokeThickness = 1 },
                    DataLabelsPaint     = new SolidColorPaint(axisLabelColor),
                    DataLabelsPosition  = DataLabelsPosition.Top,
                    DataLabelsFormatter = p => p.PrimaryValue.ToString("0")
                }
            };

            SegmentAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Labels      = segmentNames,
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 11,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };

            SegmentYAxes = new ICartesianAxis[]
            {
                new Axis
                {
                    Name        = "Điểm rủi ro",
                    LabelsPaint = new SolidColorPaint(axisLabelColor),
                    TextSize    = 11,
                    SeparatorsPaint = new SolidColorPaint(axisGridColor) { StrokeThickness = 1 }
                }
            };
        }

        // =====================================================================
        //  BUTTON HANDLERS
        // =====================================================================
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
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
            // Ví dụ nếu sau này export PDF
            // var path = ReportExporter.ExportHistoryToPdf(History, TopNodes, "TRM-HN-001", "Trạm Nghĩa Đô");
            // await ShowExportDoneDialogAsync("PDF", path);
        }

        private async Task ShowExportDoneDialogAsync(string kind, string path)
        {
            var dialog = new ContentDialog
            {
                Title = $"Xuất file {kind}",
                Content = $"Đã xuất file {kind} thành công.\nĐường dẫn:\n{path}",
                CloseButtonText = "Đóng",
                XamlRoot = Content.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    // =====================================================================
    //  DTO
    // =====================================================================
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

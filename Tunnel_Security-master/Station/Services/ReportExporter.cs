using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Station.Views;   // để dùng AlertHistoryRecord, TopNodeStat

namespace Station.Services
{
    public static class ReportExporter
    {
        // ===== EXPORT EXCEL =====
        public static string ExportHistoryToExcel(
            IEnumerable<AlertHistoryRecord> history,
            IEnumerable<TopNodeStat> topNodes,
            string stationId,
            string stationName)
        {
            var timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsFolder,
                $"BaoCao_CanhBao_{stationId}_{timeStamp}.xlsx");

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Cảnh báo");

            int row = 1;

            // Header thông tin chung
            ws.Cell(row, 1).Value = $"BÁO CÁO CẢNH BÁO – {stationName} ({stationId})";
            ws.Range(row, 1, row, 6).Merge().Style
                .Font.SetBold().Font.SetFontSize(16);
            row += 2;

            // Một số thống kê đơn giản
            var list = history.ToList();
            int total = list.Count;
            int severe = list.Count(h => h.Severity == "Cao" || h.Severity == "Nghiêm trọng");
            int closed = list.Count(h => h.Status.StartsWith("Đã"));
            int open = total - closed;

            ws.Cell(row, 1).Value = "Tổng số cảnh báo:";
            ws.Cell(row, 2).Value = total;
            row++;
            ws.Cell(row, 1).Value = "Cảnh báo nghiêm trọng:";
            ws.Cell(row, 2).Value = severe;
            row++;
            ws.Cell(row, 1).Value = "Đã xử lý:";
            ws.Cell(row, 2).Value = closed;
            row++;
            ws.Cell(row, 1).Value = "Đang mở:";
            ws.Cell(row, 2).Value = open;
            row += 2;

            // Bảng Top node
            ws.Cell(row, 1).Value = "Top node lỗi nhiều nhất";
            ws.Range(row, 1, row, 4).Merge().Style.Font.SetBold();
            row++;

            ws.Cell(row, 1).Value = "Hạng";
            ws.Cell(row, 2).Value = "Node";
            ws.Cell(row, 3).Value = "Tuyến";
            ws.Cell(row, 4).Value = "Số cảnh báo";
            ws.Range(row, 1, row, 4).Style.Font.SetBold();
            row++;

            foreach (var t in topNodes)
            {
                ws.Cell(row, 1).Value = t.Rank;
                ws.Cell(row, 2).Value = t.NodeCode;
                ws.Cell(row, 3).Value = t.LineName;
                ws.Cell(row, 4).Value = t.AlertCount;
                row++;
            }

            row += 2;

            // Bảng dữ liệu lịch sử
            ws.Cell(row, 1).Value = "Dữ liệu lịch sử cảnh báo";
            ws.Range(row, 1, row, 6).Merge().Style.Font.SetBold();
            row++;

            int headerRow = row;

            ws.Cell(row, 1).Value = "Thời gian";
            ws.Cell(row, 2).Value = "Tuyến";
            ws.Cell(row, 3).Value = "Nút";
            ws.Cell(row, 4).Value = "Loại cảnh báo";
            ws.Cell(row, 5).Value = "Mức độ";
            ws.Cell(row, 6).Value = "Trạng thái xử lý";

            ws.Range(row, 1, row, 6).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.LightGray);
            row++;

            foreach (var h in list)
            {
                ws.Cell(row, 1).Value = h.Timestamp;
                ws.Cell(row, 2).Value = h.Line;
                ws.Cell(row, 3).Value = h.Node;
                ws.Cell(row, 4).Value = h.AlertType;
                ws.Cell(row, 5).Value = h.Severity;
                ws.Cell(row, 6).Value = h.Status;
                row++;
            }

            ws.Columns().AdjustToContents();

            workbook.SaveAs(filePath);
            return filePath;
        }

        // ===== EXPORT PDF =====
        public static string ExportHistoryToPdf(
            IEnumerable<AlertHistoryRecord> history,
            IEnumerable<TopNodeStat> topNodes,
            string stationId,
            string stationName)
        {
            var timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var filePath = Path.Combine(documentsFolder,
                $"BaoCao_CanhBao_{stationId}_{timeStamp}.pdf");

            var list = history.ToList();

            int total = list.Count;
            int severe = list.Count(h => h.Severity == "Cao" || h.Severity == "Nghiêm trọng");
            int closed = list.Count(h => h.Status.StartsWith("Đã"));
            int open = total - closed;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // Tiêu đề
                        col.Item().Text($"BÁO CÁO CẢNH BÁO").FontSize(18).Bold();
                        col.Item().Text($"{stationName} ({stationId})");
                        col.Item().Text($"Thời gian xuất: {DateTime.Now:HH:mm dd/MM/yyyy}");

                        // Tóm tắt
                        col.Item().Row(row =>
                        {
                            row.Spacing(20);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Tổng số cảnh báo: {total}");
                                c.Item().Text($"Cảnh báo nghiêm trọng: {severe}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Đã xử lý: {closed}");
                                c.Item().Text($"Đang mở: {open}");
                            });
                        });

                        // Top node
                        col.Item().PaddingTop(10).Text("Top node lỗi nhiều nhất").Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("#").Bold();
                                h.Cell().Text("Node").Bold();
                                h.Cell().Text("Tuyến").Bold();
                                h.Cell().Text("Số cảnh báo").Bold();
                            });

                            foreach (var t in topNodes)
                            {
                                table.Cell().Text(t.Rank.ToString());
                                table.Cell().Text(t.NodeCode);
                                table.Cell().Text(t.LineName);
                                table.Cell().Text(t.AlertCount.ToString());
                            }
                        });

                        // Lịch sử cảnh báo
                        col.Item().PaddingTop(15).Text("Dữ liệu lịch sử cảnh báo").Bold();

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(120); // thời gian
                                columns.ConstantColumn(40);  // tuyến
                                columns.ConstantColumn(70);  // nút
                                columns.RelativeColumn();    // loại cảnh báo
                                columns.ConstantColumn(70);  // mức độ
                                columns.ConstantColumn(90);  // trạng thái
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Thời gian").Bold();
                                h.Cell().Text("Tuyến").Bold();
                                h.Cell().Text("Nút").Bold();
                                h.Cell().Text("Loại cảnh báo").Bold();
                                h.Cell().Text("Mức độ").Bold();
                                h.Cell().Text("Trạng thái").Bold();
                            });

                            foreach (var item in list)
                            {
                                table.Cell().Text(item.Timestamp);
                                table.Cell().Text(item.Line);
                                table.Cell().Text(item.Node);
                                table.Cell().Text(item.AlertType);
                                table.Cell().Text(item.Severity);
                                table.Cell().Text(item.Status);
                            }
                        });
                    });
                });
            });

            document.GeneratePdf(filePath);
            return filePath;
        }
    }
}

using ClosedXML.Excel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;

namespace Pipe_Hub.BLL.Reports;

/// <summary>
/// Xuất báo cáo doanh thu ra file Excel (.xlsx) bằng ClosedXML (Chương 8).
/// </summary>
public class ExcelReportBuilder : IReportBuilder
{
    public Task BuildAsync(IReadOnlyList<OrderDto> data, string outputPath, string title = "Báo cáo doanh thu")
    {
        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Doanh Thu");

            // ==== 1. Tiêu đề (merge A1:F1) ====
            worksheet.Range("A1:F1").Merge();
            var titleCell = worksheet.Cell("A1");
            titleCell.Value = title;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            worksheet.Row(1).Height = 26;

            // ==== 2. Ngày xuất ====
            worksheet.Range("A2:F2").Merge();
            worksheet.Cell("A2").Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // ==== 3. Header cột (dòng 4) ====
            string[] headers =
            {
                "Mã đơn", "Ngày tạo", "Khách hàng", "Số lượng game", "Tổng tiền", "Trạng thái"
            };
            const int headerRow = 4;

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            }

            // ==== 4. Dữ liệu (từ dòng 5) ====
            int row = headerRow + 1;
            foreach (var order in data)
            {
                worksheet.Cell(row, 1).Value = order.OrderCode;
                worksheet.Cell(row, 2).Value = order.CreatedAt;
                worksheet.Cell(row, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                worksheet.Cell(row, 3).Value = order.Username;
                worksheet.Cell(row, 4).Value = order.Items.Sum(i => i.Quantity);
                worksheet.Cell(row, 5).Value = order.TotalAmount;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(row, 6).Value = order.Status;
                row++;
            }

            // ==== 5. Điều chỉnh độ rộng + lưu file ====
            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(outputPath);
        });
    }
}

using System.Globalization;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Pipe_Hub.BLL.Reports;

/// <summary>
/// Xuất báo cáo doanh thu ra file PDF bằng QuestPDF (Chương 8).
/// </summary>
public class PdfReportBuilder : IReportBuilder
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    static PdfReportBuilder()
    {
        // Bắt buộc khai báo license trước khi generate PDF (QuestPDF ≥ 2024.3).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task BuildAsync(IReadOnlyList<OrderDto> data, string outputPath, string title = "Báo cáo doanh thu")
    {
        return Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ==== Header: tiêu đề + ngày xuất ====
                    page.Header().Column(header =>
                    {
                        header.Item()
                            .Text(title)
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        header.Item()
                            .Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        header.Item()
                            .PaddingTop(8)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten1);
                    });

                    // ==== Table: Mã đơn | Ngày tạo | Khách hàng | Tổng tiền ====
                    page.Content().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1.4f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Mã đơn");
                            header.Cell().Element(HeaderCell).Text("Ngày tạo");
                            header.Cell().Element(HeaderCell).Text("Khách hàng");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Tổng tiền");
                        });

                        foreach (var order in data)
                        {
                            table.Cell().Element(BodyCell).Text(order.OrderCode);
                            table.Cell().Element(BodyCell).Text(order.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Element(BodyCell).Text(order.Username);
                            table.Cell().Element(BodyCell).AlignRight()
                                .Text(order.TotalAmount.ToString("N0", ViCulture));
                        }
                    });
                });
            }).GeneratePdf(outputPath);
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .DefaultTextStyle(x => x.Bold());

    private static IContainer BodyCell(IContainer container) =>
        container
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(4);
}

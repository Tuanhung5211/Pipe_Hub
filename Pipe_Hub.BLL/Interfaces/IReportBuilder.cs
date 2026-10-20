using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Xuất danh sách đơn hàng ra file báo cáo (Chương 8).
/// Mỗi định dạng (Excel, PDF) là một implementation riêng.
/// </summary>
public interface IReportBuilder
{
    /// <summary>Xuất <paramref name="data"/> ra file tại <paramref name="outputPath"/>.</summary>
    Task BuildAsync(IReadOnlyList<OrderDto> data, string outputPath, string title = "Báo cáo doanh thu");
}

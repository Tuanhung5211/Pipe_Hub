using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service tổng hợp dữ liệu cho Dashboard (Chương 6).
/// Chỉ Admin hoặc Publisher được xem.
/// </summary>
public interface IReportService
{
    /// <summary>Doanh thu N tháng gần nhất (Order Completed), sắp xếp tăng dần theo thời gian.</summary>
    Task<IReadOnlyList<RevenueByMonthDto>> GetRevenueByMonthAsync(int months = 12);

    /// <summary>Số người chơi truy cập (distinct UserId) N ngày gần nhất, sắp xếp tăng dần theo ngày.</summary>
    Task<IReadOnlyList<CcuByDayDto>> GetCcuByDayAsync(int days = 7);

    /// <summary>Số lượng game (chưa xóa) theo thể loại, sắp xếp giảm dần.</summary>
    Task<IReadOnlyList<GameCountByGenreDto>> GetGameCountByGenreAsync();

    /// <summary>Lấy danh sách đơn hàng Completed (đã lọc theo ngày + khoảng giá). Chỉ Admin.</summary>
    Task<IReadOnlyList<OrderDto>> GetRevenueDetailsAsync(RevenueFilterDto filter);

    /// <summary>
    /// Lấy doanh thu chi tiết của Publisher hiện tại (chỉ OrderItem có Game.PublisherId = current publisher).
    /// Chỉ Publisher. Filter theo ngày + Subtotal.
    /// </summary>
    Task<IReadOnlyList<PublisherRevenueItemDto>> GetRevenueDetailsForPublisherAsync(RevenueFilterDto filter);
}

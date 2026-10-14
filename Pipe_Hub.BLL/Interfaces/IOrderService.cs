using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Mua game — method chính của D4.5.
    /// Mở transaction, kiểm tra ví/game, trừ tiền, gán key atomic, tạo Order + OrderItem + Transaction.
    /// Trả về OrderDto nếu thành công; Result.Failure nếu thiếu tiền/hết key.
    /// </summary>
    Task<Result<OrderDto>> PurchaseAsync(int gameId, int quantity = 1);

    /// <summary>
    /// Lấy đơn của user hiện tại (Player chỉ xem đơn mình).
    /// </summary>
    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync();

    /// <summary>
    /// Admin: lấy tất cả đơn.
    /// </summary>
    Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync(string? statusFilter = null);

    /// <summary>
    /// Lấy đơn theo Id. Player chỉ xem đơn của mình.
    /// </summary>
    Task<OrderDto> GetByIdAsync(int orderId);
}

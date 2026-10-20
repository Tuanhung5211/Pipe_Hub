namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// 1 dòng doanh thu của Publisher = 1 OrderItem (1 game cụ thể trong 1 đơn).
/// </summary>
public record PublisherRevenueItemDto
{
    public int OrderId { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
    public string BuyerUsername { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}

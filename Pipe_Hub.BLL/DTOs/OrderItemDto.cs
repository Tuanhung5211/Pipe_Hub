namespace Pipe_Hub.BLL.DTOs;

public record OrderItemDto
{
    public int Id { get; init; }
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
}

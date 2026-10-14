namespace Pipe_Hub.BLL.DTOs;

public record OrderDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = new List<OrderItemDto>();
    public IReadOnlyList<string> GameKeys { get; init; } = new List<string>();
}

namespace Pipe_Hub.BLL.DTOs;

public record TopUpRequestDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? PaymentMethod { get; init; }
    public string? PaymentReference { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public string? RejectReason { get; init; }
    public DateTime CreatedAt { get; init; }
}

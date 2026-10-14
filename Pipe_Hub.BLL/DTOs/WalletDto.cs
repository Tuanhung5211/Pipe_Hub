namespace Pipe_Hub.BLL.DTOs;

public record WalletDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public DateTime CreatedAt { get; init; }
}

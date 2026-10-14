namespace Pipe_Hub.BLL.DTOs;

public record ReviewDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string? Content { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

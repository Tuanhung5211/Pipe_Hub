namespace Pipe_Hub.BLL.DTOs;

public record GameKeyDto
{
    public int Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public int? OrderId { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

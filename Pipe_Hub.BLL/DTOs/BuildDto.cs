namespace Pipe_Hub.BLL.DTOs;

public record BuildDto
{
    public int Id { get; init; }
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string? PatchNotes { get; init; }
    public string? FilePath { get; init; }
    public DateTime? ReleasedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

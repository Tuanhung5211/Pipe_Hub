namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho Achievement — UI dùng thay vì Entity.
/// </summary>
public record AchievementDto
{
    public int Id { get; init; }
    public int GameId { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? IconPath { get; init; }
    public DateTime CreatedAt { get; init; }
}

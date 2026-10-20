namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho thành tích mà Player đã đạt được — UI dùng thay vì Entity.
/// </summary>
public record PlayerAchievementDto
{
    public int AchievementId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string GameTitle { get; init; } = string.Empty;
    public DateTime UnlockedAt { get; init; }
}

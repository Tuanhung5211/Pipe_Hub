namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho LogEntry — UI dùng thay vì Entity (audit nghiệp vụ).
/// </summary>
public record LogEntryDto
{
    public int Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? Details { get; init; }
    public int? UserId { get; init; }
    public string? Username { get; init; }
    public string? EntityName { get; init; }
    public int? EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
}

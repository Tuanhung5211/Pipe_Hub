namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho SystemConfig — UI dùng thay vì Entity.
/// </summary>
public record SystemConfigDto
{
    public int Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

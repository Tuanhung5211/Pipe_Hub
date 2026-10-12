namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho User — KHÔNG chứa PasswordHash (bảo mật).
/// </summary>
public record UserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Country { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public bool IsActive { get; init; }
    public int RoleId { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

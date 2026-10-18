namespace Pipe_Hub.BLL.DTOs;

public record UpdateProfileDto
{
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Country { get; init; }
}

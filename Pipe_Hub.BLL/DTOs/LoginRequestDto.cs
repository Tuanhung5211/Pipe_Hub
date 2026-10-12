namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho yêu cầu đăng nhập từ LoginForm.
/// </summary>
public record LoginRequestDto
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

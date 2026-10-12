namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho yêu cầu đăng ký tài khoản Player từ RegisterForm.
/// </summary>
public record RegisterRequestDto
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Country { get; init; }
}

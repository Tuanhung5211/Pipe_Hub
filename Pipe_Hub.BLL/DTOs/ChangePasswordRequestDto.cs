namespace Pipe_Hub.BLL.DTOs;

/// <summary>
/// DTO cho yêu cầu đổi mật khẩu từ ChangePasswordForm.
/// </summary>
public record ChangePasswordRequestDto
{
    public string OldPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

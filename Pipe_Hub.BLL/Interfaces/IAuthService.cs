using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

/// <summary>
/// Service xử lý xác thực và tài khoản.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng nhập. Trả về Result chứa UserDto nếu thành công.
    /// Sai username/password → Result.Failure (không throw).
    /// </summary>
    Task<Result<UserDto>> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Đăng ký tài khoản Player mới.
    /// Vi phạm rule (trùng username/email, password yếu) → Result.Failure.
    /// </summary>
    Task<Result<UserDto>> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Đổi mật khẩu.
    /// Mật khẩu cũ sai → Result.Failure.
    /// </summary>
    Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request);

    /// <summary>
    /// Lấy UserDto theo Id. Ném AppException nếu không tìm thấy.
    /// </summary>
    Task<UserDto> GetByIdAsync(int userId);
}

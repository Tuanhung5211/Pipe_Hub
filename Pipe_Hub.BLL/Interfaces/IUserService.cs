using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IUserService
{
    /// <summary>Lấy profile user hiện tại.</summary>
    Task<UserDto> GetMyProfileAsync();

    /// <summary>Cập nhật profile. Validate email format + unique.</summary>
    Task<Result<UserDto>> UpdateProfileAsync(UpdateProfileDto request);
}

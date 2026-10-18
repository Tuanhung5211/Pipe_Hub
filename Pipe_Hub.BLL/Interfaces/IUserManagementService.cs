using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;

namespace Pipe_Hub.BLL.Interfaces;

public interface IUserManagementService
{
    /// <summary>Lấy tất cả user (Admin only).</summary>
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync();

    /// <summary>Admin tạo user mới. Nếu Role là Player → tạo kèm Wallet (Balance = 0).</summary>
    Task<Result<UserDto>> CreateUserAsync(string username, string email, string password, string fullName, int roleId, bool isActive);

    /// <summary>Admin cập nhật thông tin cơ bản của user (Họ tên, Email, Role, Active).</summary>
    Task<Result<UserDto>> UpdateUserAsync(int userId, string email, string fullName, int roleId, bool isActive);

    /// <summary>Admin đổi Role của user.</summary>
    Task<Result> ChangeRoleAsync(int userId, int roleId);

    /// <summary>Admin khoá/mở khoá user.</summary>
    Task<Result> SetActiveStatusAsync(int userId, bool isActive);

    /// <summary>Lấy danh sách Role để bind ComboBox.</summary>
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync();
}

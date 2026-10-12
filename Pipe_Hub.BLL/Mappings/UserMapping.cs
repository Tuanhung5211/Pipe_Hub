using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Mappings;

/// <summary>
/// Extension method chuyển đổi Entity User → UserDto.
/// KHÔNG map PasswordHash.
/// </summary>
public static class UserMapping
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Country = user.Country,
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            IsActive = user.IsActive,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty,
            CreatedAt = user.CreatedAt
        };
    }
}

using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Mappings;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Service quản lý người dùng dành riêng cho Admin — CRUD User, đổi Role, khoá/mở khoá.
/// Tách biệt với IUserService (chỉ dùng cho chính user đang đăng nhập).
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public UserManagementService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    /// <summary>Chặn mọi thao tác nếu người gọi không phải Admin.</summary>
    private void EnsureAdmin()
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới có quyền quản lý người dùng.");
        }
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync()
    {
        EnsureAdmin();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var users = await context.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Id)
            .ToListAsync();

        return users.Select(u => u.ToDto()).ToList();
    }

    public async Task<Result<UserDto>> CreateUserAsync(string username, string email, string password, string fullName, int roleId, bool isActive)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(fullName))
        {
            return Result<UserDto>.Failure("Vui lòng nhập đầy đủ thông tin.");
        }

        username = username.Trim();
        email = email.Trim();
        fullName = fullName.Trim();

        if (username.Length < 3 || username.Length > 50)
        {
            return Result<UserDto>.Failure("Tên đăng nhập phải từ 3 đến 50 ký tự.");
        }

        if (password.Length < 6)
        {
            return Result<UserDto>.Failure("Mật khẩu phải từ 6 ký tự trở lên.");
        }

        if (email.Length > 100)
        {
            return Result<UserDto>.Failure("Email tối đa 100 ký tự.");
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            return Result<UserDto>.Failure("Email không đúng định dạng.");
        }

        if (fullName.Length > 100)
        {
            return Result<UserDto>.Failure("Họ tên tối đa 100 ký tự.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await context.Users.AnyAsync(u => u.Username == username))
        {
            return Result<UserDto>.Failure("Tên đăng nhập đã tồn tại.");
        }

        if (await context.Users.AnyAsync(u => u.Email == email))
        {
            return Result<UserDto>.Failure("Email đã được sử dụng.");
        }

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

        if (role is null)
        {
            return Result<UserDto>.Failure("Vai trò không tồn tại.");
        }

        // Transaction: tạo User (+ Wallet nếu là Player) cùng lúc.
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var newUser = new User
            {
                Username = username,
                Email = email,
                PasswordHash = await Task.Run(() => PasswordHasher.Hash(password)),
                FullName = fullName,
                RoleId = roleId,
                IsActive = isActive
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();

            if (string.Equals(role.Name, "Player", StringComparison.OrdinalIgnoreCase))
            {
                context.Wallets.Add(new Wallet
                {
                    UserId = newUser.Id,
                    Balance = 0m
                });
                await context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            // Load Role cho DTO
            newUser.Role = role;

            _ = _logService.WriteAsync(
                action: "CreateUser",
                details: $"Admin '{_currentUser.Username}' tạo user '{newUser.Username}' (Id={newUser.Id})",
                userId: _currentUser.UserId,
                entityName: nameof(User),
                entityId: newUser.Id);

            return Result<UserDto>.Success(newUser.ToDto());
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result<UserDto>> UpdateUserAsync(int userId, string email, string fullName, int roleId, bool isActive)
    {
        EnsureAdmin();

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<UserDto>.Failure("Email không được để trống.");
        }

        if (email.Length > 100)
        {
            return Result<UserDto>.Failure("Email tối đa 100 ký tự.");
        }

        if (!MailAddress.TryCreate(email, out _))
        {
            return Result<UserDto>.Failure("Email không đúng định dạng.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<UserDto>.Failure("Họ tên không được để trống.");
        }

        if (fullName.Length > 100)
        {
            return Result<UserDto>.Failure("Họ tên tối đa 100 ký tự.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new AppException($"Không tìm thấy người dùng với Id = {userId}.");
        }

        var emailTaken = await context.Users
            .AnyAsync(u => u.Email == email && u.Id != userId);

        if (emailTaken)
        {
            return Result<UserDto>.Failure("Email đã được sử dụng bởi tài khoản khác.");
        }

        var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);

        if (role is null)
        {
            return Result<UserDto>.Failure("Vai trò không tồn tại.");
        }

        user.Email = email.Trim();
        user.FullName = fullName.Trim();
        user.RoleId = roleId;
        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateUser",
            details: $"Admin '{_currentUser.Username}' cập nhật user '{user.Username}' (Id={user.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(User),
            entityId: user.Id);

        // Load lại Role để map DTO đúng RoleName mới.
        user.Role = role;

        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result> ChangeRoleAsync(int userId, int roleId)
    {
        EnsureAdmin();

        if (userId == _currentUser.UserId)
        {
            throw new AppException("Không thể tự đổi Role của mình.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new AppException($"Không tìm thấy người dùng với Id = {userId}.");
        }

        var roleExists = await context.Roles.AnyAsync(r => r.Id == roleId);

        if (!roleExists)
        {
            return Result.Failure("Vai trò không tồn tại.");
        }

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "ChangeRole",
            details: $"Admin '{_currentUser.Username}' đổi Role của user '{user.Username}' (Id={user.Id}) thành RoleId={roleId}",
            userId: _currentUser.UserId,
            entityName: nameof(User),
            entityId: user.Id);

        return Result.Success();
    }

    public async Task<Result> SetActiveStatusAsync(int userId, bool isActive)
    {
        EnsureAdmin();

        if (userId == _currentUser.UserId)
        {
            throw new AppException("Không thể tự khoá tài khoản của mình.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new AppException($"Không tìm thấy người dùng với Id = {userId}.");
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "SetActiveStatus",
            details: $"Admin '{_currentUser.Username}' {(isActive ? "mở khoá" : "khoá")} tài khoản '{user.Username}' (Id={user.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(User),
            entityId: user.Id);

        return Result.Success();
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync()
    {
        EnsureAdmin();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var roles = await context.Roles
            .OrderBy(r => r.Id)
            .ToListAsync();

        return roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description
        }).ToList();
    }
}

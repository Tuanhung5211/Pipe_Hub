using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Mappings;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Data;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Service xác thực — Login, Register, ChangePassword, GetById.
/// Sử dụng IDbContextFactory để tạo context ngắn hạn (không giữ 1 context suốt app).
/// </summary>
public class AuthService : IAuthService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly ILogService _logService;

    public AuthService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _logService = logService;
    }

    public async Task<Result<UserDto>> LoginAsync(LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<UserDto>.Failure("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null)
        {
            return Result<UserDto>.Failure("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        if (!user.IsActive)
        {
            return Result<UserDto>.Failure("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

        if (!PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result<UserDto>.Failure("Tên đăng nhập hoặc mật khẩu không đúng.");
        }

        // Ghi log — không await để không block login (fire-and-forget an toàn vì LogService không throw)
        _ = _logService.WriteAsync(
            action: "Login",
            details: $"User '{user.Username}' đăng nhập thành công",
            userId: user.Id,
            entityName: nameof(User),
            entityId: user.Id);

        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<UserDto>> RegisterAsync(RegisterRequestDto request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result<UserDto>.Failure("Vui lòng nhập đầy đủ thông tin.");
        }

        if (request.Username.Length < 3 || request.Username.Length > 50)
        {
            return Result<UserDto>.Failure("Tên đăng nhập phải từ 3 đến 50 ký tự.");
        }

        if (request.Password.Length < 6)
        {
            return Result<UserDto>.Failure("Mật khẩu phải từ 6 ký tự trở lên.");
        }

        if (request.Password != request.ConfirmPassword)
        {
            return Result<UserDto>.Failure("Mật khẩu xác nhận không khớp.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Check unique
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return Result<UserDto>.Failure("Tên đăng nhập đã tồn tại.");
        }

        if (await context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return Result<UserDto>.Failure("Email đã được sử dụng.");
        }

        var playerRole = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Player");

        if (playerRole is null)
        {
            throw new AppException("Không tìm thấy vai trò Player trong hệ thống. Liên hệ quản trị viên.");
        }

        // Transaction: tạo User + Wallet cùng lúc
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = PasswordHasher.Hash(request.Password),
                FullName = request.FullName,
                RoleId = playerRole.Id,
                IsActive = true,
                Country = request.Country
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();

            var wallet = new Wallet
            {
                UserId = newUser.Id,
                Balance = 0m
            };
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Load Role cho DTO
            newUser.Role = playerRole;

            _ = _logService.WriteAsync(
                action: "Register",
                details: $"User mới '{newUser.Username}' đăng ký tài khoản",
                userId: newUser.Id,
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

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.OldPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result.Failure("Vui lòng nhập đầy đủ mật khẩu cũ và mật khẩu mới.");
        }

        if (request.NewPassword.Length < 6)
        {
            return Result.Failure("Mật khẩu mới phải từ 6 ký tự trở lên.");
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            return Result.Failure("Mật khẩu xác nhận không khớp.");
        }

        if (request.OldPassword == request.NewPassword)
        {
            return Result.Failure("Mật khẩu mới phải khác mật khẩu cũ.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user is null)
        {
            throw new AppException("Không tìm thấy người dùng.");
        }

        if (!PasswordHasher.Verify(request.OldPassword, user.PasswordHash))
        {
            return Result.Failure("Mật khẩu cũ không đúng.");
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "ChangePassword",
            details: $"User '{user.Username}' đổi mật khẩu",
            userId: user.Id,
            entityName: nameof(User),
            entityId: user.Id);

        return Result.Success();
    }

    public async Task<UserDto> GetByIdAsync(int userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new AppException($"Không tìm thấy người dùng với Id = {userId}.");
        }

        return user.ToDto();
    }
}

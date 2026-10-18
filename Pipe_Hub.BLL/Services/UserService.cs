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

public class UserService : IUserService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public UserService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<UserDto> GetMyProfileAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value);

        if (user is null)
        {
            throw new AppException("Không tìm thấy thông tin người dùng.");
        }

        return user.ToDto();
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(UpdateProfileDto request)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result<UserDto>.Failure("Email không được để trống.");
        }

        if (request.Email.Length > 100)
        {
            return Result<UserDto>.Failure("Email tối đa 100 ký tự.");
        }

        if (!MailAddress.TryCreate(request.Email, out _))
        {
            return Result<UserDto>.Failure("Email không đúng định dạng.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return Result<UserDto>.Failure("Họ tên không được để trống.");
        }

        if (request.FullName.Length > 100)
        {
            return Result<UserDto>.Failure("Họ tên tối đa 100 ký tự.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            throw new AppException("Không tìm thấy thông tin người dùng.");
        }

        var emailTaken = await context.Users
            .AnyAsync(u => u.Email == request.Email && u.Id != userId);

        if (emailTaken)
        {
            return Result<UserDto>.Failure("Email đã được sử dụng bởi tài khoản khác.");
        }

        user.Email = request.Email.Trim();
        user.FullName = request.FullName.Trim();
        user.Country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateProfile",
            details: $"User {_currentUser.Username} cập nhật profile",
            userId: userId,
            entityName: nameof(User),
            entityId: userId);

        return Result<UserDto>.Success(user.ToDto());
    }
}

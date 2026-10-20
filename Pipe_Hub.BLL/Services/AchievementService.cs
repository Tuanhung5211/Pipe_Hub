using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Service quản lý Achievement (thành tích) của game.
/// Chỉ Admin hoặc Publisher sở hữu game mới được CRUD.
/// </summary>
public class AchievementService : IAchievementService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public AchievementService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<IReadOnlyList<AchievementDto>> GetByGameAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        await EnsureCanManageAsync(context, gameId);

        var achievements = await context.Achievements
            .AsNoTracking()
            .Where(a => a.GameId == gameId)
            .Include(a => a.Game)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return achievements.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<PlayerAchievementDto>> GetMyAchievementsAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var userAch = await context.UserAchievements
            .AsNoTracking()
            .Include(ua => ua.Achievement)
            .ThenInclude(a => a.Game)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.UnlockedAt)
            .ToListAsync();

        return userAch.Select(ua => new PlayerAchievementDto
        {
            AchievementId = ua.AchievementId,
            Name = ua.Achievement.Name,
            Description = ua.Achievement.Description,
            GameTitle = ua.Achievement.Game?.Title ?? string.Empty,
            UnlockedAt = ua.UnlockedAt
        }).ToList();
    }

    public async Task<Result<AchievementDto>> CreateAsync(
        int gameId,
        string name,
        string? description,
        string? iconPath)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await EnsureCanManageAsync(context, gameId);

        var validation = Validate(name);
        if (validation is not null)
        {
            return Result<AchievementDto>.Failure(validation);
        }

        var achievement = new Achievement
        {
            GameId = gameId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IconPath = string.IsNullOrWhiteSpace(iconPath) ? null : iconPath.Trim()
        };

        context.Achievements.Add(achievement);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateAchievement",
            details: $"Tạo thành tích '{achievement.Name}' cho game '{game.Title}' (Id={gameId})",
            userId: _currentUser.UserId,
            entityName: nameof(Achievement),
            entityId: achievement.Id);

        return Result<AchievementDto>.Success(ToDto(achievement, game.Title));
    }

    public async Task<Result> UpdateAsync(
        int id,
        string name,
        string? description,
        string? iconPath)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var achievement = await context.Achievements
            .Include(a => a.Game)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (achievement is null)
        {
            throw new AppException($"Không tìm thấy thành tích với Id = {id}.");
        }

        await EnsureCanManageAsync(context, achievement.GameId);

        var validation = Validate(name);
        if (validation is not null)
        {
            return Result.Failure(validation);
        }

        achievement.Name = name.Trim();
        achievement.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        achievement.IconPath = string.IsNullOrWhiteSpace(iconPath) ? null : iconPath.Trim();
        achievement.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateAchievement",
            details: $"Cập nhật thành tích '{achievement.Name}' (Id={achievement.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Achievement),
            entityId: achievement.Id);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var achievement = await context.Achievements
            .Include(a => a.Game)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (achievement is null)
        {
            throw new AppException($"Không tìm thấy thành tích với Id = {id}.");
        }

        await EnsureCanManageAsync(context, achievement.GameId);

        context.Achievements.Remove(achievement);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Result.Failure("Không thể xóa thành tích này vì đang có người chơi đạt được.");
        }

        _ = _logService.WriteAsync(
            action: "DeleteAchievement",
            details: $"Xóa thành tích '{achievement.Name}' (Id={achievement.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Achievement),
            entityId: achievement.Id);

        return Result.Success();
    }

    /// <summary>
    /// Kiểm tra quyền quản lý thành tích của game: game phải tồn tại,
    /// user phải là Admin hoặc Publisher sở hữu game.
    /// </summary>
    private async Task<Game> EnsureCanManageAsync(PipeHubDbContext context, int gameId)
    {
        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (!_currentUser.IsAdmin && !_currentUser.IsPublisher)
        {
            throw new AppException("Chỉ Admin hoặc Publisher mới có quyền quản lý thành tích.");
        }

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền quản lý thành tích của game này.");
        }

        return game;
    }

    /// <summary>
    /// Validate tên thành tích. Trả về message lỗi, hoặc null nếu hợp lệ.
    /// </summary>
    private static string? Validate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Tên thành tích không được để trống.";
        }

        if (name.Trim().Length > 100)
        {
            return "Tên thành tích tối đa 100 ký tự.";
        }

        return null;
    }

    private static AchievementDto ToDto(Achievement achievement)
    {
        return ToDto(achievement, achievement.Game?.Title ?? string.Empty);
    }

    private static AchievementDto ToDto(Achievement achievement, string gameTitle)
    {
        return new AchievementDto
        {
            Id = achievement.Id,
            GameId = achievement.GameId,
            GameTitle = gameTitle,
            Name = achievement.Name,
            Description = achievement.Description,
            IconPath = achievement.IconPath,
            CreatedAt = achievement.CreatedAt
        };
    }
}

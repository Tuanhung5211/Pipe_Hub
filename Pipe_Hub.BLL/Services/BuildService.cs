using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

public class BuildService : IBuildService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public BuildService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<IReadOnlyList<BuildDto>> GetBuildsByGameAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        EnsureCanManage(game);

        var builds = await context.GameBuilds
            .Where(b => b.GameId == gameId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return builds.Select(b => ToDto(b, game.Title)).ToList();
    }

    public async Task<Result<BuildDto>> CreateBuildAsync(int gameId, string version, string channel, string? patchNotes, string? filePath)
    {
        var validation = Validate(version, channel);
        if (validation is not null)
        {
            return Result<BuildDto>.Failure(validation);
        }

        var parsedChannel = Enum.Parse<BuildChannel>(channel.Trim(), ignoreCase: true);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        EnsureCanManage(game);

        var build = new GameBuild
        {
            GameId = gameId,
            Version = version.Trim(),
            Channel = parsedChannel,
            PatchNotes = string.IsNullOrWhiteSpace(patchNotes) ? null : patchNotes.Trim(),
            FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath.Trim(),
            ReleasedAt = DateTime.UtcNow
        };

        context.GameBuilds.Add(build);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateBuild",
            details: $"Tạo build '{build.Version}' cho game '{game.Title}' (Id={gameId})",
            userId: _currentUser.UserId,
            entityName: nameof(GameBuild),
            entityId: build.Id);

        return Result<BuildDto>.Success(ToDto(build, game.Title));
    }

    public async Task<Result> UpdateBuildAsync(int buildId, string version, string channel, string? patchNotes, string? filePath)
    {
        var validation = Validate(version, channel);
        if (validation is not null)
        {
            return Result.Failure(validation);
        }

        var parsedChannel = Enum.Parse<BuildChannel>(channel.Trim(), ignoreCase: true);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var build = await context.GameBuilds
            .Include(b => b.Game)
            .FirstOrDefaultAsync(b => b.Id == buildId);

        if (build is null)
        {
            throw new AppException($"Không tìm thấy build với Id = {buildId}.");
        }

        EnsureCanManage(build.Game);

        build.Version = version.Trim();
        build.Channel = parsedChannel;
        build.PatchNotes = string.IsNullOrWhiteSpace(patchNotes) ? null : patchNotes.Trim();
        build.FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath.Trim();
        build.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateBuild",
            details: $"Cập nhật build '{build.Version}' (Id={build.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(GameBuild),
            entityId: build.Id);

        return Result.Success();
    }

    public async Task<Result> DeleteBuildAsync(int buildId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var build = await context.GameBuilds
            .Include(b => b.Game)
            .FirstOrDefaultAsync(b => b.Id == buildId);

        if (build is null)
        {
            throw new AppException($"Không tìm thấy build với Id = {buildId}.");
        }

        EnsureCanManage(build.Game);

        context.GameBuilds.Remove(build);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Result.Failure("Không thể xóa build này vì đang có dữ liệu ràng buộc.");
        }

        _ = _logService.WriteAsync(
            action: "DeleteBuild",
            details: $"Xóa build '{build.Version}' (Id={build.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(GameBuild),
            entityId: build.Id);

        return Result.Success();
    }

    /// <summary>
    /// Validate version/channel. Trả về message lỗi, hoặc null nếu hợp lệ.
    /// </summary>
    private static string? Validate(string version, string channel)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "Phiên bản không được để trống.";
        }

        if (version.Trim().Length > 50)
        {
            return "Phiên bản tối đa 50 ký tự.";
        }

        if (!Enum.TryParse<BuildChannel>(channel?.Trim(), ignoreCase: true, out _))
        {
            return "Kênh phát hành không hợp lệ (chỉ nhận Stable hoặc Beta).";
        }

        return null;
    }

    /// <summary>
    /// Chặn Player và Publisher không phải chủ sở hữu game.
    /// </summary>
    private void EnsureCanManage(Game game)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (!_currentUser.IsAdmin && !_currentUser.IsPublisher)
        {
            throw new AppException("Chỉ Admin hoặc Publisher mới có quyền thao tác build.");
        }

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền thao tác trên build của game này.");
        }
    }

    private static BuildDto ToDto(GameBuild build, string gameTitle)
    {
        return new BuildDto
        {
            Id = build.Id,
            GameId = build.GameId,
            GameTitle = gameTitle,
            Version = build.Version,
            Channel = build.Channel.ToString(),
            PatchNotes = build.PatchNotes,
            FilePath = build.FilePath,
            ReleasedAt = build.ReleasedAt,
            CreatedAt = build.CreatedAt
        };
    }
}

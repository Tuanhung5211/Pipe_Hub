using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Mappings;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

public class GameService : IGameService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public GameService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<IReadOnlyList<GameDto>> GetAllVisibleAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Games
            .Include(g => g.Publisher)
            .Include(g => g.Genres)
            .Include(g => g.Tags)
            .Include(g => g.Languages)
            .Where(g => !g.IsDeleted);

        // Player chỉ thấy Approved
        if (_currentUser.IsPlayer)
        {
            query = query.Where(g => g.Status == GameStatus.Approved);
        }
        // Publisher chỉ thấy game của mình + game Approved của người khác
        else if (_currentUser.IsPublisher && _currentUser.UserId.HasValue)
        {
            var myId = _currentUser.UserId.Value;
            query = query.Where(g => g.PublisherId == myId || g.Status == GameStatus.Approved);
        }
        // Admin thấy hết

        var games = await query.OrderByDescending(g => g.CreatedAt).ToListAsync();
        return games.Select(g => g.ToDto()).ToList();
    }

    public async Task<GameDto> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games
            .Include(g => g.Publisher)
            .Include(g => g.Genres)
            .Include(g => g.Tags)
            .Include(g => g.Languages)
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {id}.");
        }

        return game.ToDto();
    }

    public async Task<IReadOnlyList<GameDto>> GetByPublisherAsync(int publisherId)
    {
        if (_currentUser.IsPublisher && _currentUser.UserId != publisherId)
        {
            throw new AppException("Bạn không có quyền xem game của nhà phát hành khác.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var games = await context.Games
            .Include(g => g.Publisher)
            .Include(g => g.Genres)
            .Include(g => g.Tags)
            .Include(g => g.Languages)
            .Where(g => g.PublisherId == publisherId && !g.IsDeleted)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return games.Select(g => g.ToDto()).ToList();
    }

    public async Task<Result<GameDto>> CreateAsync(string title, decimal price, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<GameDto>.Failure("Tên game không được để trống.");
        }

        if (title.Length > 200)
        {
            return Result<GameDto>.Failure("Tên game tối đa 200 ký tự.");
        }

        if (price < 0)
        {
            return Result<GameDto>.Failure("Giá không được âm.");
        }

        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (!_currentUser.IsPublisher && !_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Publisher mới được tạo game.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = new Game
        {
            Title = title.Trim(),
            Description = description?.Trim(),
            Price = price,
            Status = GameStatus.Draft,
            PublisherId = _currentUser.UserId.Value,
            IsDeleted = false
        };

        context.Games.Add(game);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "CreateGame",
            details: $"Tạo game '{game.Title}' (Id={game.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Game),
            entityId: game.Id);

        return Result<GameDto>.Success(game.ToDto());
    }

    public async Task<Result<GameDto>> UpdateAsync(int gameId, string title, decimal price, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<GameDto>.Failure("Tên game không được để trống.");
        }

        if (price < 0)
        {
            return Result<GameDto>.Failure("Giá không được âm.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games
            .Include(g => g.Publisher)
            .FirstOrDefaultAsync(g => g.Id == gameId);

        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (game.IsDeleted)
        {
            return Result<GameDto>.Failure("Game đã bị xóa.");
        }

        // Publisher chỉ sửa game của mình
        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền sửa game của nhà phát hành khác.");
        }

        game.Title = title.Trim();
        game.Description = description?.Trim();
        game.Price = price;
        game.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "UpdateGame",
            details: $"Cập nhật game '{game.Title}' (Id={game.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Game),
            entityId: game.Id);

        return Result<GameDto>.Success(game.ToDto());
    }

    public async Task<Result> SubmitForApprovalAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId && !g.IsDeleted);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền gửi duyệt game này.");
        }

        if (game.Status != GameStatus.Draft && game.Status != GameStatus.Rejected)
        {
            return Result.Failure($"Không thể gửi duyệt khi game đang ở trạng thái {game.Status}.");
        }

        game.Status = GameStatus.Pending;
        game.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "SubmitGameForApproval",
            details: $"Gửi duyệt game '{game.Title}' (Id={game.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Game),
            entityId: game.Id);

        return Result.Success();
    }

    public async Task<Result> ApproveAsync(int gameId)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới được duyệt game.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId && !g.IsDeleted);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (game.Status != GameStatus.Pending)
        {
            return Result.Failure($"Chỉ duyệt được game đang Pending. Game hiện tại: {game.Status}.");
        }

        game.Status = GameStatus.Approved;
        game.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "ApproveGame",
            details: $"Duyệt game '{game.Title}' (Id={game.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Game),
            entityId: game.Id);

        return Result.Success();
    }

    public async Task<Result> RejectAsync(int gameId)
    {
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới được từ chối game.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId && !g.IsDeleted);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (game.Status != GameStatus.Pending)
        {
            return Result.Failure($"Chỉ từ chối được game đang Pending. Game hiện tại: {game.Status}.");
        }

        game.Status = GameStatus.Rejected;
        game.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "RejectGame",
            details: $"Từ chối game '{game.Title}' (Id={game.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Game),
            entityId: game.Id);

        return Result.Success();
    }

    public async Task<Result> SoftDeleteAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền xóa game này.");
        }

        if (game.IsDeleted)
        {
            return Result.Failure("Game đã bị xóa từ trước.");
        }

        game.IsDeleted = true;
        game.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "SoftDeleteGame",
            details: $"Xóa mềm game '{game.Title}' (Id={game.Id})",
            userId: _currentUser.UserId,
            entityName: nameof(Game),
            entityId: game.Id);

        return Result.Success();
    }
}

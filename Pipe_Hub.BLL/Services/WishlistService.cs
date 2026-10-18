using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Services;

public class WishlistService : IWishlistService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public WishlistService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<IReadOnlyList<WishlistItemDto>> GetMyWishlistAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var items = await context.Wishlists
            .Include(w => w.Game).ThenInclude(g => g.Publisher)
            .Where(w => w.UserId == userId && !w.Game.IsDeleted)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync();

        return items.Select(w => new WishlistItemDto
        {
            Id = w.Id,
            GameId = w.GameId,
            GameTitle = w.Game.Title,
            PublisherName = w.Game.Publisher?.FullName ?? string.Empty,
            Price = w.Game.Price,
            SalePrice = w.Game.SalePrice,
            CoverImagePath = w.Game.CoverImagePath,
            AddedAt = w.AddedAt
        }).ToList();
    }

    public async Task<bool> IsInWishlistAsync(int gameId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return false;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Wishlists
            .AnyAsync(w => w.UserId == _currentUser.UserId.Value && w.GameId == gameId);
    }

    public async Task<Result> AddAsync(int gameId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games
            .FirstOrDefaultAsync(g => g.Id == gameId && !g.IsDeleted);

        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        var exists = await context.Wishlists
            .AnyAsync(w => w.UserId == userId && w.GameId == gameId);

        if (exists)
        {
            return Result.Failure("Game đã có trong wishlist của bạn.");
        }

        var item = new Wishlist
        {
            UserId = userId,
            GameId = gameId
        };

        context.Wishlists.Add(item);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "AddWishlist",
            details: $"User {_currentUser.Username} thêm '{game.Title}' vào wishlist",
            userId: userId,
            entityName: nameof(Wishlist),
            entityId: item.Id);

        return Result.Success();
    }

    public async Task<Result> RemoveAsync(int gameId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        var userId = _currentUser.UserId.Value;

        await using var context = await _contextFactory.CreateDbContextAsync();

        var item = await context.Wishlists
            .FirstOrDefaultAsync(w => w.GameId == gameId && w.UserId == userId);

        if (item is null)
        {
            throw new AppException("Game không có trong wishlist của bạn.");
        }

        context.Wishlists.Remove(item);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "RemoveWishlist",
            details: $"User {_currentUser.Username} xóa game Id = {gameId} khỏi wishlist",
            userId: userId,
            entityName: nameof(Wishlist),
            entityId: item.Id);

        return Result.Success();
    }
}

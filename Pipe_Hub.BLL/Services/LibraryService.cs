using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;

namespace Pipe_Hub.BLL.Services;

public class LibraryService : ILibraryService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;

    public LibraryService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<LibraryItemDto>> GetMyLibraryAsync()
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        var userId = _currentUser.UserId.Value;
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Bước 1: Lấy danh sách game đã mua (distinct) + giá + ngày mua sớm nhất
        var purchasedItems = await context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.UserId == userId)
            .Select(oi => new
            {
                oi.GameId,
                oi.UnitPrice,
                oi.Order.CreatedAt
            })
            .ToListAsync();

        if (purchasedItems.Count == 0)
            return new List<LibraryItemDto>();

        // Group theo GameId — lấy giá + ngày mua đầu tiên
        var grouped = purchasedItems
            .GroupBy(x => x.GameId)
            .Select(g => new
            {
                GameId = g.Key,
                PurchasePrice = g.OrderBy(x => x.CreatedAt).First().UnitPrice,
                PurchasedAt = g.Min(x => x.CreatedAt)
            })
            .ToList();

        var gameIds = grouped.Select(g => g.GameId).ToList();

        // Bước 2: Load Games (Include Publisher)
        var games = await context.Games
            .Include(g => g.Publisher)
            .Where(g => gameIds.Contains(g.Id))
            .ToListAsync();

        // Bước 3: Playtime sum per game
        var playtimeByGame = await context.PlaySessions
            .Where(ps => ps.UserId == userId && gameIds.Contains(ps.GameId))
            .GroupBy(ps => ps.GameId)
            .Select(g => new
            {
                GameId = g.Key,
                TotalMinutes = g.Sum(ps => ps.DurationMinutes)
            })
            .ToDictionaryAsync(x => x.GameId, x => x.TotalMinutes);

        // Bước 4: Merge
        var result = new List<LibraryItemDto>();
        foreach (var g in grouped)
        {
            var game = games.FirstOrDefault(x => x.Id == g.GameId);
            if (game is null) continue;

            result.Add(new LibraryItemDto
            {
                GameId = g.GameId,
                GameTitle = game.Title,
                PublisherName = game.Publisher?.FullName ?? string.Empty,
                CoverImagePath = game.CoverImagePath,
                PurchasePrice = g.PurchasePrice,
                PurchasedAt = g.PurchasedAt,
                TotalPlayMinutes = playtimeByGame.TryGetValue(g.GameId, out var min) ? min : 0
            });
        }

        return result.OrderByDescending(x => x.PurchasedAt).ToList();
    }

    public async Task<int> GetTotalPlaytimeAsync(int gameId)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        var userId = _currentUser.UserId.Value;
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.PlaySessions
            .Where(ps => ps.UserId == userId && ps.GameId == gameId)
            .SumAsync(ps => (int?)ps.DurationMinutes) ?? 0;
    }
}

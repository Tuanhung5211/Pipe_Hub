using Microsoft.EntityFrameworkCore;
using QRCoder;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

public class KeyService : IKeyService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public KeyService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<Result<IReadOnlyList<GameKeyDto>>> GenerateKeysAsync(int gameId, int count)
    {
        if (!_currentUser.UserId.HasValue)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (count < 1 || count > 1000)
        {
            return Result<IReadOnlyList<GameKeyDto>>.Failure("Số lượng key phải từ 1 đến 1000.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền sinh key cho game này.");
        }

        if (!_currentUser.IsAdmin && !_currentUser.IsPublisher)
        {
            throw new AppException("Chỉ Publisher hoặc Admin mới sinh được key.");
        }

        var prefix = new string(game.Title.Where(char.IsLetterOrDigit).ToArray())
            .ToUpperInvariant();
        if (prefix.Length > 10) prefix = prefix.Substring(0, 10);
        if (string.IsNullOrEmpty(prefix)) prefix = "GAME";

        var newKeys = new List<GameKey>();
        var generated = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            string keyValue;
            int attempts = 0;
            do
            {
                keyValue = $"{prefix}-{GenerateRandomSegment(6)}";
                attempts++;
                if (attempts > 100)
                {
                    return Result<IReadOnlyList<GameKeyDto>>.Failure(
                        "Không sinh được key duy nhất sau 100 lần thử. Vui lòng thử lại.");
                }
            } while (generated.Contains(keyValue) ||
                     await context.GameKeys.AnyAsync(k => k.Key == keyValue));

            generated.Add(keyValue);
            newKeys.Add(new GameKey
            {
                Key = keyValue,
                Status = KeyStatus.Available,
                GameId = gameId
            });
        }

        context.GameKeys.AddRange(newKeys);
        await context.SaveChangesAsync();

        _ = _logService.WriteAsync(
            action: "GenerateKeys",
            details: $"Sinh {count} key cho game '{game.Title}' (Id={gameId})",
            userId: _currentUser.UserId,
            entityName: nameof(GameKey),
            entityId: gameId);

        var result = newKeys.Select(k => new GameKeyDto
        {
            Id = k.Id,
            Key = k.Key,
            Status = k.Status.ToString(),
            GameId = k.GameId,
            GameTitle = game.Title,
            CreatedAt = k.CreatedAt
        }).ToList();

        return Result<IReadOnlyList<GameKeyDto>>.Success(result);
    }

    public async Task<IReadOnlyList<GameKeyDto>> GetKeysByGameAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var game = await context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
        if (game is null)
        {
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");
        }

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
        {
            throw new AppException("Bạn không có quyền xem key của game này.");
        }

        var keys = await context.GameKeys
            .Where(k => k.GameId == gameId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();

        return keys.Select(k => new GameKeyDto
        {
            Id = k.Id,
            Key = k.Key,
            Status = k.Status.ToString(),
            GameId = k.GameId,
            GameTitle = game.Title,
            OrderId = k.OrderId,
            ActivatedAt = k.ActivatedAt,
            CreatedAt = k.CreatedAt
        }).ToList();
    }

    public async Task<byte[]> GenerateQrCodeAsync(int keyId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var key = await context.GameKeys.FirstOrDefaultAsync(k => k.Id == keyId);
        if (key is null)
        {
            throw new AppException($"Không tìm thấy key với Id = {keyId}.");
        }

        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(key.Key, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);

        return qrCode.GetGraphic(20);
    }

    public async Task<int> CountAvailableAsync(int gameId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.GameKeys
            .CountAsync(k => k.GameId == gameId && k.Status == KeyStatus.Available);
    }

    private static string GenerateRandomSegment(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}

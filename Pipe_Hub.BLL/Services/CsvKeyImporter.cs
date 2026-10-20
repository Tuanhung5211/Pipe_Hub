using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

public class CsvKeyImporter : ICsvKeyImporter
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public CsvKeyImporter(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<Result<int>> ImportAsync(
        int gameId,
        string filePath,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        if (!_currentUser.UserId.HasValue)
            throw new AppException("Bạn chưa đăng nhập.");

        if (!File.Exists(filePath))
            return Result<int>.Failure("Không tìm thấy file CSV.");

        if (!string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase))
            return Result<int>.Failure("Chỉ hỗ trợ file .csv.");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var game = await context.Games
            .FirstOrDefaultAsync(g => g.Id == gameId && !g.IsDeleted, ct);
        if (game is null)
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");

        if (!_currentUser.IsAdmin && !_currentUser.IsPublisher)
            throw new AppException("Chỉ Admin hoặc Publisher mới có quyền import key.");

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
            throw new AppException("Bạn không có quyền import key cho game này.");

        var lines = await File.ReadAllLinesAsync(filePath, ct);
        if (lines.Length < 2)
            return Result<int>.Failure("File CSV trống hoặc thiếu header.");

        return await Task.Run(async () =>
        {
            // ============ PASS 1: Parse CSV → HashSet (dedupe trong file) ============
            var parsedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int totalRows = 0;

            using (var reader = new StringReader(string.Join('\n', lines)))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                HeaderValidated = null
            }))
            {
                await csv.ReadAsync();
                csv.ReadHeader();

                var headers = csv.HeaderRecord ?? Array.Empty<string>();
                var keyIndex = Array.FindIndex(headers,
                    h => string.Equals(h, "Key", StringComparison.OrdinalIgnoreCase));
                if (keyIndex < 0)
                    return Result<int>.Failure("File CSV phải có cột 'Key'.");

                var reportEvery = Math.Max(1, lines.Length / 20);

                while (await csv.ReadAsync())
                {
                    ct.ThrowIfCancellationRequested();
                    totalRows++;

                    var rawKey = csv.GetField(keyIndex)?.Trim();
                    if (string.IsNullOrEmpty(rawKey) || rawKey.Length > 100)
                        continue;

                    parsedKeys.Add(rawKey);

                    if (progress != null && totalRows % reportEvery == 0)
                        progress.Report(Math.Min(50, (int)((double)totalRows / lines.Length * 50)));
                }
            }

            if (parsedKeys.Count == 0)
            {
                progress?.Report(100);
                return Result<int>.Success(0);
            }

            // ============ PASS 2: 1 query check trùng global ============
            progress?.Report(50);

            var keyArray = parsedKeys.ToArray();
            var existingSet = await context.GameKeys
                .Where(k => keyArray.Contains(k.Key))
                .Select(k => k.Key)
                .ToHashSetAsync(ct);

            // ============ PASS 3: filter + insert ============
            var newKeys = parsedKeys
                .Where(k => !existingSet.Contains(k))
                .Select(k => new GameKey
                {
                    Key = k,
                    GameId = gameId,
                    Status = KeyStatus.Available
                })
                .ToList();

            try
            {
                if (newKeys.Count > 0)
                {
                    context.GameKeys.AddRange(newKeys);
                    await context.SaveChangesAsync(ct);
                }
            }
            catch (DbUpdateException)
            {
                return Result<int>.Failure("Có lỗi khi lưu vào DB. Vui lòng thử lại.");
            }

            progress?.Report(100);

            _ = _logService.WriteAsync(
                action: "ImportKeys",
                details: $"Import {newKeys.Count}/{totalRows} key cho game '{game.Title}' (skip {existingSet.Count} trùng DB) từ '{Path.GetFileName(filePath)}'",
                userId: _currentUser.UserId,
                entityName: nameof(GameKey),
                entityId: gameId);

            return Result<int>.Success(newKeys.Count);
        }, ct);
    }
}

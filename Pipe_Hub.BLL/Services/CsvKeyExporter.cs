using System.Globalization;
using System.Text;
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

public class CsvKeyExporter : ICsvKeyExporter
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;
    private readonly ILogService _logService;

    public CsvKeyExporter(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser,
        ILogService logService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _logService = logService;
    }

    public async Task<Result<int>> ExportAsync(
        int gameId,
        string outputPath,
        string? statusFilter = null,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        // ==== 1. Validate login + quyền ====
        if (!_currentUser.UserId.HasValue)
            throw new AppException("Bạn chưa đăng nhập.");

        if (string.IsNullOrWhiteSpace(outputPath))
            return Result<int>.Failure("Đường dẫn file export không hợp lệ.");

        if (!string.Equals(Path.GetExtension(outputPath), ".csv", StringComparison.OrdinalIgnoreCase))
            return Result<int>.Failure("Chỉ hỗ trợ file .csv.");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var game = await context.Games
            .FirstOrDefaultAsync(g => g.Id == gameId && !g.IsDeleted, ct);
        if (game is null)
            throw new AppException($"Không tìm thấy game với Id = {gameId}.");

        if (!_currentUser.IsAdmin && !_currentUser.IsPublisher)
            throw new AppException("Chỉ Admin hoặc Publisher mới có quyền export key.");

        if (_currentUser.IsPublisher && game.PublisherId != _currentUser.UserId)
            throw new AppException("Bạn không có quyền export key cho game này.");

        // ==== 2. Build query + filter theo status ====
        var query = context.GameKeys
            .AsNoTracking()
            .Where(k => k.GameId == gameId);

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<KeyStatus>(statusFilter, ignoreCase: true, out var st))
        {
            query = query.Where(k => k.Status == st);
        }

        var keys = await query.OrderBy(k => k.Key).ToListAsync(ct);

        // ==== 3. Ghi file CSV (ghi header luôn, kể cả khi 0 key) ====
        progress?.Report(0);

        int written;
        try
        {
            written = await Task.Run(async () =>
            {
                // BOM (encoderShouldEmitUTF8Identifier: true) để Excel hiển thị đúng tiếng Việt
                await using var writer = new StreamWriter(
                    outputPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                });

                csv.WriteField("Key");
                csv.WriteField("Status");
                csv.WriteField("GameTitle");
                csv.WriteField("ActivatedAt");
                csv.WriteField("CreatedAt");
                await csv.NextRecordAsync();

                int count = 0;
                foreach (var k in keys)
                {
                    ct.ThrowIfCancellationRequested();

                    csv.WriteField(k.Key);
                    csv.WriteField(k.Status.ToString());
                    csv.WriteField(game.Title);
                    csv.WriteField(k.ActivatedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? "");
                    csv.WriteField(k.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
                    await csv.NextRecordAsync();

                    count++;
                }

                await writer.FlushAsync(ct);
                return count;
            }, ct);
        }
        catch (IOException ex)
        {
            return Result<int>.Failure($"Không thể ghi file CSV: {ex.Message}");
        }

        // ==== 4. Báo tiến độ + log ====
        progress?.Report(100);

        _ = _logService.WriteAsync(
            action: "ExportKeys",
            details: $"Export {written} key cho game '{game.Title}' → '{Path.GetFileName(outputPath)}'",
            userId: _currentUser.UserId,
            entityName: nameof(GameKey),
            entityId: gameId);

        return Result<int>.Success(written);
    }
}

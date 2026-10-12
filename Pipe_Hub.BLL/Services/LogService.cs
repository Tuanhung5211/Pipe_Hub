using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Service ghi/đọc LogEntry — audit nghiệp vụ.
/// KHÁC với Serilog (log kỹ thuật).
/// </summary>
public class LogService : ILogService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;

    public LogService(IDbContextFactory<PipeHubDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task WriteAsync(
        string action,
        string? details = null,
        int? userId = null,
        string? entityName = null,
        int? entityId = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var entry = new LogEntry
            {
                Action = action,
                Details = details,
                UserId = userId,
                EntityName = entityName,
                EntityId = entityId
            };

            context.LogEntries.Add(entry);
            await context.SaveChangesAsync();
        }
        catch
        {
            // Log fail không được làm app crash — bỏ qua
            // Serilog đã ghi log kỹ thuật ở Program.cs
        }
    }

    public async Task<List<LogEntry>> GetRecentAsync(int count = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.LogEntries
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<LogEntry>> GetByUserAsync(int userId, int count = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.LogEntries
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}

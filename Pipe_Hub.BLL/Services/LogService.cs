using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Mappings;
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

    public async Task<List<LogEntryDto>> GetRecentAsync(int count = 100)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var entries = await context.LogEntries
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();

        return entries.Select(e => e.ToDto()).ToList();
    }

    public async Task<List<LogEntryDto>> GetByUserAsync(int userId, int count = 50)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var entries = await context.LogEntries
            .Include(l => l.User)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();

        return entries.Select(e => e.ToDto()).ToList();
    }

    public async Task<IReadOnlyList<LogEntryDto>> GetLogsByFilterAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? action,
        int? userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        IQueryable<LogEntry> query = context.LogEntries
            .Include(l => l.User)
            .AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(l => l.Action.Contains(action));
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        var entries = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(1000)
            .ToListAsync();

        return entries.Select(e => e.ToDto()).ToList();
    }
}

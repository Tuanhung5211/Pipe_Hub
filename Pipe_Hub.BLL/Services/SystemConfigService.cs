using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Service đọc/ghi SystemConfig.
/// Cache trong RAM — load lazy lần đầu, invalidate khi Set.
/// </summary>
public class SystemConfigService : ISystemConfigService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly object _lock = new();
    private Dictionary<string, string>? _cache;

    public SystemConfigService(IDbContextFactory<PipeHubDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private async Task<Dictionary<string, string>> GetCacheAsync()
    {
        // Double-check locking pattern
        if (_cache is not null) return _cache;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var items = await context.SystemConfigs.ToListAsync();

        lock (_lock)
        {
            _cache ??= items.ToDictionary(x => x.Key, x => x.Value);
            return _cache;
        }
    }

    public async Task<string?> GetAsync(string key)
    {
        var cache = await GetCacheAsync();
        return cache.TryGetValue(key, out var value) ? value : null;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0m)
    {
        var raw = await GetAsync(key);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;

        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var raw = await GetAsync(key);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : defaultValue;
    }

    public async Task SetAsync(string key, string value, string? description = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.SystemConfigs.FirstOrDefaultAsync(x => x.Key == key);

        if (existing is null)
        {
            context.SystemConfigs.Add(new SystemConfig
            {
                Key = key,
                Value = value,
                Description = description
            });
        }
        else
        {
            existing.Value = value;
            if (description is not null)
            {
                existing.Description = description;
            }
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        InvalidateCache();
    }

    public async Task<IReadOnlyList<SystemConfigDto>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var items = await context.SystemConfigs
            .OrderBy(sc => sc.Key)
            .ToListAsync();

        return items.Select(sc => new SystemConfigDto
        {
            Id = sc.Id,
            Key = sc.Key,
            Value = sc.Value,
            Description = sc.Description,
            UpdatedAt = sc.UpdatedAt
        }).ToList();
    }

    public void InvalidateCache()
    {
        lock (_lock)
        {
            _cache = null;
        }
    }
}

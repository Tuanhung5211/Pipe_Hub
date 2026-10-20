using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.BLL.Services;

/// <summary>
/// Service tổng hợp dữ liệu cho Dashboard (Chương 6 — LiveCharts2).
/// Chỉ Admin hoặc Publisher được xem.
/// </summary>
public class ReportService : IReportService
{
    private readonly IDbContextFactory<PipeHubDbContext> _contextFactory;
    private readonly CurrentUserContext _currentUser;

    public ReportService(
        IDbContextFactory<PipeHubDbContext> contextFactory,
        CurrentUserContext currentUser)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
    }

    /// <summary>Chặn Player (và user chưa đăng nhập) xem Dashboard.</summary>
    private void EnsureCanViewDashboard()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new AppException("Bạn chưa đăng nhập.");
        }

        if (!_currentUser.IsAdmin && !_currentUser.IsPublisher)
        {
            throw new AppException("Chỉ Admin hoặc Publisher mới được xem Dashboard.");
        }
    }

    public async Task<IReadOnlyList<RevenueByMonthDto>> GetRevenueByMonthAsync(int months = 12)
    {
        EnsureCanViewDashboard();

        await using var context = await _contextFactory.CreateDbContextAsync();

        var rows = await context.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed)
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                TotalRevenue = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .Take(months)
            .ToListAsync();

        // Đã lấy N tháng gần nhất theo thứ tự giảm dần → đảo lại để biểu đồ chạy trái → phải
        rows.Reverse();

        return rows
            .Select(r => new RevenueByMonthDto
            {
                Year = r.Year,
                Month = r.Month,
                TotalRevenue = r.TotalRevenue
            })
            .ToList();
    }

    public async Task<IReadOnlyList<CcuByDayDto>> GetCcuByDayAsync(int days = 7)
    {
        EnsureCanViewDashboard();

        var fromDate = DateTime.UtcNow.AddDays(-days);

        await using var context = await _contextFactory.CreateDbContextAsync();

        var rows = await context.PlaySessions
            .AsNoTracking()
            .Where(p => p.StartedAt >= fromDate)
            .GroupBy(p => p.StartedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                UniquePlayers = g.Select(p => p.UserId).Distinct().Count()
            })
            .OrderBy(r => r.Date)
            .ToListAsync();

        return rows
            .Select(r => new CcuByDayDto
            {
                Date = r.Date,
                UniquePlayers = r.UniquePlayers
            })
            .ToList();
    }

    public async Task<IReadOnlyList<GameCountByGenreDto>> GetGameCountByGenreAsync()
    {
        EnsureCanViewDashboard();

        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Genres
            .AsNoTracking()
            .Include(g => g.Games)
            .Where(g => g.Games.Any(ga => !ga.IsDeleted))
            .Select(g => new GameCountByGenreDto
            {
                GenreName = g.Name,
                GameCount = g.Games.Count(ga => !ga.IsDeleted)
            })
            .OrderByDescending(r => r.GameCount)
            .ToListAsync();
    }
}

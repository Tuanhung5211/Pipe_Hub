using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Mappings;
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

        // Lấy ngày lớn nhất trong DB để test (tránh lỗi data cũ quá 7 ngày)
        // Hoặc dùng DateTime.UtcNow.AddDays(-days) nếu data luôn mới.
        // Để an toàn cho seed data, ta lấy mốc thời gian mới nhất trong DB.
        await using var context = await _contextFactory.CreateDbContextAsync();

        var maxDate = await context.PlaySessions.AnyAsync()
            ? await context.PlaySessions.MaxAsync(p => p.StartedAt)
            : DateTime.UtcNow;
        var fromDate = maxDate.AddDays(-days);

        var sessions = await context.PlaySessions
            .AsNoTracking()
            .Where(p => p.StartedAt >= fromDate)
            .ToListAsync();

        var rows = sessions
            .GroupBy(p => p.StartedAt.Date)
            .Select(g => new CcuByDayDto
            {
                Date = g.Key,
                UniquePlayers = g.Select(p => p.UserId).Distinct().Count()
            })
            .OrderBy(r => r.Date)
            .ToList();

        return rows;
    }

    public async Task<IReadOnlyList<GameCountByGenreDto>> GetGameCountByGenreAsync()
    {
        EnsureCanViewDashboard();

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Load Genres + Games lên RAM trước — tránh EF Core dịch truy vấn N-N
        // (Include + Any/Count) sang SQL Server không chính xác.
        var genres = await context.Genres
            .AsNoTracking()
            .Include(g => g.Games)
            .ToListAsync();

        return genres
            .Select(g => new GameCountByGenreDto
            {
                GenreName = g.Name,
                GameCount = g.Games.Count(ga => !ga.IsDeleted)
            })
            .Where(d => d.GameCount > 0)
            .OrderByDescending(d => d.GameCount)
            .ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetRevenueDetailsAsync(RevenueFilterDto filter)
    {
        // Chỉ Admin — dùng EnsureCanViewDashboard sẽ cho cả Publisher, cần check riêng
        if (!_currentUser.IsAdmin)
        {
            throw new AppException("Chỉ Admin mới được xem báo cáo doanh thu chi tiết.");
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var query = context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Game)
            .Include(o => o.Keys)
            .Where(o => o.Status == OrderStatus.Completed);

        if (filter.FromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.ToDate.Value);

        if (filter.MinAmount.HasValue)
            query = query.Where(o => o.TotalAmount >= filter.MinAmount.Value);

        if (filter.MaxAmount.HasValue)
            query = query.Where(o => o.TotalAmount <= filter.MaxAmount.Value);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => o.ToDto()).ToList();
    }
}

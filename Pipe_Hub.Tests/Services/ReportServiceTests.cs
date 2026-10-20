using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.BLL.Services;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;
using Xunit;

namespace Pipe_Hub.Tests.Services;

public class ReportServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IDbContextFactory<PipeHubDbContext> _factory = null!;
    private CurrentUserContext _currentUser = null!;

    private int _adminId;
    private int _playerId;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PipeHubDbContext>()
            .UseSqlite(_connection)
            .Options;
        _factory = new TestDbContextFactory(options);

        var now = DateTime.UtcNow;

        await using (var ctx = _factory.CreateDbContext())
        {
            await ctx.Database.EnsureCreatedAsync();

            var adminRole = new Role { Name = "Admin" };
            var playerRole = new Role { Name = "Player" };
            ctx.Roles.AddRange(adminRole, playerRole);
            await ctx.SaveChangesAsync();

            var admin = new User
            {
                Username = "admin",
                Email = "admin@test.local",
                PasswordHash = "ignored",
                FullName = "Test Admin",
                RoleId = adminRole.Id,
                IsActive = true
            };
            var player = new User
            {
                Username = "player",
                Email = "player@test.local",
                PasswordHash = "ignored",
                FullName = "Test Player",
                RoleId = playerRole.Id,
                IsActive = true
            };
            ctx.Users.AddRange(admin, player);
            await ctx.SaveChangesAsync();
            _adminId = admin.Id;
            _playerId = player.Id;

            ctx.Orders.AddRange(
                new Order
                {
                    OrderCode = "A",
                    TotalAmount = 100_000m,
                    Status = OrderStatus.Completed,
                    CreatedAt = now,
                    UserId = player.Id
                },
                new Order
                {
                    OrderCode = "B",
                    TotalAmount = 200_000m,
                    Status = OrderStatus.Completed,
                    CreatedAt = now.AddDays(-5),
                    UserId = player.Id
                },
                new Order
                {
                    OrderCode = "C",
                    TotalAmount = 300_000m,
                    Status = OrderStatus.Completed,
                    CreatedAt = now.AddDays(-10),
                    UserId = player.Id
                },
                new Order
                {
                    OrderCode = "P",
                    TotalAmount = 50_000m,
                    Status = OrderStatus.Pending,
                    CreatedAt = now,
                    UserId = player.Id
                });
            await ctx.SaveChangesAsync();
        }

        _currentUser = new CurrentUserContext();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private ReportService CreateService() => new(_factory, _currentUser);

    private void SetAdmin() => _currentUser.SetUser(_adminId, "admin", "Test Admin", "Admin");

    private void SetPlayer() => _currentUser.SetUser(_playerId, "player", "Test Player", "Player");

    [Fact]
    public async Task GetRevenueDetailsAsync_WhenAdminAndNoFilter_ReturnsAllCompletedOrders()
    {
        SetAdmin();
        var service = CreateService();

        var result = await service.GetRevenueDetailsAsync(new RevenueFilterDto());

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, o => o.OrderCode == "P");
        Assert.All(result, o => Assert.Equal("Completed", o.Status));
    }

    [Fact]
    public async Task GetRevenueDetailsAsync_WhenFilterFromDate_ReturnsOnlyRecent()
    {
        SetAdmin();
        var service = CreateService();

        var filter = new RevenueFilterDto { FromDate = DateTime.UtcNow.AddDays(-3) };
        var result = await service.GetRevenueDetailsAsync(filter);

        Assert.Single(result);
        Assert.Equal("A", result[0].OrderCode);
    }

    [Fact]
    public async Task GetRevenueDetailsAsync_WhenFilterByAmount_ReturnsInRange()
    {
        SetAdmin();
        var service = CreateService();

        var filter = new RevenueFilterDto { MinAmount = 150_000m, MaxAmount = 250_000m };
        var result = await service.GetRevenueDetailsAsync(filter);

        Assert.Single(result);
        Assert.Equal("B", result[0].OrderCode);
    }

    [Fact]
    public async Task GetRevenueDetailsAsync_WhenFilterCombined_ReturnsIntersection()
    {
        SetAdmin();
        var service = CreateService();

        var filter = new RevenueFilterDto
        {
            FromDate = DateTime.UtcNow.AddDays(-7),
            MinAmount = 150_000m,
            MaxAmount = 200_000m
        };
        var result = await service.GetRevenueDetailsAsync(filter);

        Assert.Single(result);
        Assert.Equal("B", result[0].OrderCode);
    }

    [Fact]
    public async Task GetRevenueDetailsAsync_WhenNotAdmin_ThrowsAppException()
    {
        SetPlayer();
        var service = CreateService();

        await Assert.ThrowsAsync<AppException>(
            () => service.GetRevenueDetailsAsync(new RevenueFilterDto()));
    }

    [Fact]
    public async Task GetRevenueDetailsAsync_WhenNoMatch_ReturnsEmpty()
    {
        SetAdmin();
        var service = CreateService();

        var filter = new RevenueFilterDto { MinAmount = 999_999_999m };
        var result = await service.GetRevenueDetailsAsync(filter);

        Assert.Empty(result);
    }

    private class TestDbContextFactory : IDbContextFactory<PipeHubDbContext>
    {
        private readonly DbContextOptions<PipeHubDbContext> _options;

        public TestDbContextFactory(DbContextOptions<PipeHubDbContext> options)
        {
            _options = options;
        }

        public PipeHubDbContext CreateDbContext() => new(_options);

        public Task<PipeHubDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}

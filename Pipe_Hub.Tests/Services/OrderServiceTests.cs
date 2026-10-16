using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.BLL.Services;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;
using Xunit;

namespace Pipe_Hub.Tests.Services;

public class OrderServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IDbContextFactory<PipeHubDbContext> _factory = null!;
    private CurrentUserContext _currentUser = null!;
    private ILogService _logService = null!;

    private int _userId;
    private int _gameId;
    private int _keyId;

    public async Task InitializeAsync()
    {
        // 1. SQLite in-memory + giữ connection mở
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        // 2. Factory trỏ vào connection đang mở
        var options = new DbContextOptionsBuilder<PipeHubDbContext>()
            .UseSqlite(_connection)
            .Options;

        _factory = new TestDbContextFactory(options);

        // 3. Tạo schema (EnsureCreated — không dùng Migration cho test)
        await using (var ctx = _factory.CreateDbContext())
        {
            await ctx.Database.EnsureCreatedAsync();

            // 4. Seed data tối thiểu
            var playerRole = new Role { Name = "Player" };
            ctx.Roles.Add(playerRole);
            await ctx.SaveChangesAsync();

            var user = new User
            {
                Username = "testplayer",
                Email = "testplayer@test.local",
                PasswordHash = "ignored",
                FullName = "Test Player",
                RoleId = playerRole.Id,
                IsActive = true
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            _userId = user.Id;

            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = 500_000m
            };
            ctx.Wallets.Add(wallet);

            var publisher = new User
            {
                Username = "testpub",
                Email = "testpub@test.local",
                PasswordHash = "ignored",
                FullName = "Test Publisher",
                RoleId = playerRole.Id,
                IsActive = true
            };
            ctx.Users.Add(publisher);
            await ctx.SaveChangesAsync();

            var game = new Game
            {
                Title = "Test Game",
                Description = "Game for unit test",
                Price = 199_000m,
                Status = GameStatus.Approved,
                PublisherId = publisher.Id,
                IsDeleted = false
            };
            ctx.Games.Add(game);
            await ctx.SaveChangesAsync();
            _gameId = game.Id;

            var key = new GameKey
            {
                Key = "TESTGAME-ABC123",
                Status = KeyStatus.Available,
                GameId = game.Id
            };
            ctx.GameKeys.Add(key);
            await ctx.SaveChangesAsync();
            _keyId = key.Id;
        }

        // 5. CurrentUserContext + mock LogService
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(_userId, "testplayer", "Test Player", "Player");

        _logService = Substitute.For<ILogService>();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ===========================================================
    // Test 1: PurchaseAsync khi ví không đủ tiền
    // ===========================================================
    [Fact]
    public async Task PurchaseAsync_WhenBalanceTooLow_ReturnsFailure()
    {
        // Arrange: set Balance về 50,000 (nhỏ hơn 199,000)
        await using (var ctx = _factory.CreateDbContext())
        {
            var wallet = await ctx.Wallets.FirstAsync(w => w.UserId == _userId);
            wallet.Balance = 50_000m;
            await ctx.SaveChangesAsync();
        }

        var service = new OrderService(_factory, _currentUser, _logService);

        // Act
        var result = await service.PurchaseAsync(_gameId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("không đủ", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Verify: Balance không thay đổi + Key vẫn Available
        await using (var ctx = _factory.CreateDbContext())
        {
            var wallet = await ctx.Wallets.FirstAsync(w => w.UserId == _userId);
            Assert.Equal(50_000m, wallet.Balance);

            var key = await ctx.GameKeys.FirstAsync(k => k.Id == _keyId);
            Assert.Equal(KeyStatus.Available, key.Status);
        }
    }

    // ===========================================================
    // Test 2: PurchaseAsync khi game hết key
    // ===========================================================
    [Fact]
    public async Task PurchaseAsync_WhenNoKeyAvailable_ReturnsFailure()
    {
        // Arrange: chuyển key thành Activated (giả lập đã bán cho user khác)
        await using (var ctx = _factory.CreateDbContext())
        {
            var key = await ctx.GameKeys.FirstAsync(k => k.Id == _keyId);
            key.Status = KeyStatus.Activated;
            await ctx.SaveChangesAsync();
        }

        var service = new OrderService(_factory, _currentUser, _logService);

        // Act
        var result = await service.PurchaseAsync(_gameId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("key", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Verify: Balance không thay đổi
        await using (var ctx = _factory.CreateDbContext())
        {
            var wallet = await ctx.Wallets.FirstAsync(w => w.UserId == _userId);
            Assert.Equal(500_000m, wallet.Balance);
        }
    }

    // ===========================================================
    // Test 3: PurchaseAsync thành công
    // ===========================================================
    [Fact]
    public async Task PurchaseAsync_WhenValid_ReturnsSuccess_AndUpdatesData()
    {
        // Arrange
        var service = new OrderService(_factory, _currentUser, _logService);

        // Act
        var result = await service.PurchaseAsync(_gameId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Game", result.Data!.Items[0].GameTitle);
        Assert.Equal(199_000m, result.Data.TotalAmount);
        Assert.Single(result.Data.GameKeys);

        // Verify DB state
        await using (var ctx = _factory.CreateDbContext())
        {
            // Wallet giảm 199,000
            var wallet = await ctx.Wallets.FirstAsync(w => w.UserId == _userId);
            Assert.Equal(301_000m, wallet.Balance);

            // Key chuyển Activated + có OrderId
            var key = await ctx.GameKeys.FirstAsync(k => k.Id == _keyId);
            Assert.Equal(KeyStatus.Activated, key.Status);
            Assert.NotNull(key.OrderId);
            Assert.NotNull(key.ActivatedAt);

            // Order + OrderItem + Transaction tạo mới
            var order = await ctx.Orders.Include(o => o.Items).FirstAsync(o => o.Id == result.Data.Id);
            Assert.Equal(OrderStatus.Completed, order.Status);
            Assert.Single(order.Items);
            Assert.Equal(199_000m, order.Items.First().UnitPrice);

            // Transaction Purchase Amount ÂM
            var txn = await ctx.Transactions
                .FirstAsync(t => t.UserId == _userId && t.Type == TransactionType.Purchase);
            Assert.Equal(-199_000m, txn.Amount);
            Assert.Equal(TransactionStatus.Success, txn.Status);
        }
    }

    // ===========================================================
    // Test 4: PurchaseAsync lần 2 cùng game → Failure "đã sở hữu"
    // ===========================================================
    [Fact]
    public async Task PurchaseAsync_WhenCalledTwice_ReturnsFailure()
    {
        // Arrange: mua lần 1
        var service = new OrderService(_factory, _currentUser, _logService);
        var firstResult = await service.PurchaseAsync(_gameId);
        Assert.True(firstResult.IsSuccess);

        // Act: mua lần 2 cùng game
        var secondResult = await service.PurchaseAsync(_gameId);

        // Assert: lần 2 fail với message "đã sở hữu"
        Assert.False(secondResult.IsSuccess);
        Assert.Contains("sở hữu", secondResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Verify: chỉ có 1 Order, 1 Transaction, 1 key Activated
        await using var ctx = _factory.CreateDbContext();
        var orderCount = await ctx.Orders.CountAsync(o => o.UserId == _userId);
        Assert.Equal(1, orderCount);
    }

    // ===========================================================
    // Custom Factory cho test (dùng connection đang mở)
    // ===========================================================
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

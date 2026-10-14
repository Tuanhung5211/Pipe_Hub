using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.BLL.Services;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;
using Xunit;

namespace Pipe_Hub.Tests.Services;

public class ReviewServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IDbContextFactory<PipeHubDbContext> _factory = null!;
    private CurrentUserContext _currentUser = null!;
    private ILogService _logService = null!;

    private int _playerId;
    private int _otherId;
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
            var publisherRole = new Role { Name = "Publisher" };
            ctx.Roles.AddRange(playerRole, publisherRole);
            await ctx.SaveChangesAsync();

            var player = new User
            {
                Username = "reviewplayer",
                Email = "reviewplayer@test.local",
                PasswordHash = "ignored",
                FullName = "Review Player",
                RoleId = playerRole.Id,
                IsActive = true
            };
            var otherPlayer = new User
            {
                Username = "reviewother",
                Email = "reviewother@test.local",
                PasswordHash = "ignored",
                FullName = "Review Other",
                RoleId = playerRole.Id,
                IsActive = true
            };
            var publisher = new User
            {
                Username = "reviewpub",
                Email = "reviewpub@test.local",
                PasswordHash = "ignored",
                FullName = "Review Publisher",
                RoleId = publisherRole.Id,
                IsActive = true
            };
            ctx.Users.AddRange(player, otherPlayer, publisher);
            await ctx.SaveChangesAsync();
            _playerId = player.Id;
            _otherId = otherPlayer.Id;

            ctx.Wallets.AddRange(
                new Wallet { UserId = player.Id, Balance = 0m },
                new Wallet { UserId = otherPlayer.Id, Balance = 0m },
                new Wallet { UserId = publisher.Id, Balance = 0m });
            await ctx.SaveChangesAsync();

            var game = new Game
            {
                Title = "Review Test Game",
                Description = "Game for review unit test",
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
                Key = "REVIEWGAME-ABC123",
                Status = KeyStatus.Available,
                GameId = game.Id
            };
            ctx.GameKeys.Add(key);
            await ctx.SaveChangesAsync();
            _keyId = key.Id;
        }

        // 5. CurrentUserContext + mock LogService
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(_playerId, "reviewplayer", "Review Player", "Player");

        _logService = Substitute.For<ILogService>();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ===========================================================
    // Helper: tạo Order + GameKey Activated cho user (giả lập đã mua)
    // ===========================================================
    private async Task SetupPurchaseAsync(int userId, string keyValue)
    {
        await using var ctx = _factory.CreateDbContext();
        var order = new Order
        {
            OrderCode = $"ORD-TEST-{Guid.NewGuid():N}",
            UserId = userId,
            TotalAmount = 199_000m,
            Status = OrderStatus.Completed
        };
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();

        var key = new GameKey
        {
            Key = keyValue,
            GameId = _gameId,
            Status = KeyStatus.Activated,
            OrderId = order.Id,
            ActivatedAt = DateTime.UtcNow
        };
        ctx.GameKeys.Add(key);
        await ctx.SaveChangesAsync();
    }

    // ===========================================================
    // Helper: chèn Review trực tiếp (bypass service) cho setup
    // ===========================================================
    private async Task<int> InsertReviewAsync(int userId, int rating, string? content)
    {
        await using var ctx = _factory.CreateDbContext();
        var review = new Review
        {
            UserId = userId,
            GameId = _gameId,
            Rating = rating,
            Content = content
        };
        ctx.Reviews.Add(review);
        await ctx.SaveChangesAsync();
        return review.Id;
    }

    // ===========================================================
    // Test 1: CreateAsync khi chưa mua → Failure chứa "mua"
    // ===========================================================
    [Fact]
    public async Task CreateAsync_WhenNotPurchased_ReturnsFailure()
    {
        // Arrange: không có GameKey Activated cho user
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act
        var result = await service.CreateAsync(_gameId, 5, "Test");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("mua", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ===========================================================
    // Test 2: CreateAsync khi đã mua → Success
    // ===========================================================
    [Fact]
    public async Task CreateAsync_WhenPurchased_ReturnsSuccess()
    {
        // Arrange
        await SetupPurchaseAsync(_playerId, "REVIEW-PURCHASED-001");
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act
        var result = await service.CreateAsync(_gameId, 5, "Test content");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(5, result.Data!.Rating);
        Assert.Equal("Test content", result.Data.Content);
    }

    // ===========================================================
    // Test 3: CreateAsync lần 2 → Failure chứa "đánh giá"
    // ===========================================================
    [Fact]
    public async Task CreateAsync_WhenAlreadyReviewed_ReturnsFailure()
    {
        // Arrange: đã mua + đã có 1 review cũ
        await SetupPurchaseAsync(_playerId, "REVIEW-DUP-001");
        await InsertReviewAsync(_playerId, 4, "Review cũ");
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act
        var result = await service.CreateAsync(_gameId, 5, "Review lần 2");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("đánh giá", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ===========================================================
    // Test 4: CreateAsync rating = 0 → Failure chứa "1 đến 5"
    // ===========================================================
    [Fact]
    public async Task CreateAsync_WhenRatingBelow1_ReturnsFailure()
    {
        // Arrange
        await SetupPurchaseAsync(_playerId, "REVIEW-LOW-001");
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act
        var result = await service.CreateAsync(_gameId, 0, null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("1 đến 5", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ===========================================================
    // Test 5: CreateAsync rating = 6 → Failure
    // ===========================================================
    [Fact]
    public async Task CreateAsync_WhenRatingAbove5_ReturnsFailure()
    {
        // Arrange
        await SetupPurchaseAsync(_playerId, "REVIEW-HIGH-001");
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act
        var result = await service.CreateAsync(_gameId, 6, null);

        // Assert
        Assert.False(result.IsSuccess);
    }

    // ===========================================================
    // Test 6: UpdateAsync review của người khác (không phải Admin) → throws
    // ===========================================================
    [Fact]
    public async Task UpdateAsync_WhenNotOwner_ThrowsAppException()
    {
        // Arrange: review của otherPlayer, currentUser = player (Player)
        var reviewId = await InsertReviewAsync(_otherId, 5, "Của người khác");
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act + Assert
        await Assert.ThrowsAsync<AppException>(
            () => service.UpdateAsync(reviewId, 3, "Hack"));
    }

    // ===========================================================
    // Test 7: UpdateAsync review của chính mình → Success
    // ===========================================================
    [Fact]
    public async Task UpdateAsync_WhenOwner_ReturnsSuccess()
    {
        // Arrange
        var reviewId = await InsertReviewAsync(_playerId, 5, "Ban đầu");
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act
        var result = await service.UpdateAsync(reviewId, 3, "Updated");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data!.Rating);
        Assert.Equal("Updated", result.Data.Content);
    }

    // ===========================================================
    // Test 8: SoftDeleteAsync review của người khác (không Admin) → throws
    // ===========================================================
    [Fact]
    public async Task SoftDeleteAsync_WhenNotOwnerAndNotAdmin_ThrowsAppException()
    {
        // Arrange: review của otherPlayer, currentUser = player
        var reviewId = await InsertReviewAsync(_otherId, 5, "Của người khác");
        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act + Assert
        await Assert.ThrowsAsync<AppException>(
            () => service.SoftDeleteAsync(reviewId));
    }

    // ===========================================================
    // Test 9: GetAverageRatingAsync với 3 review (5, 4, 3) → 4.0
    // ===========================================================
    [Fact]
    public async Task GetAverageRatingAsync_ReturnsCorrectAverage()
    {
        // Arrange: 3 review từ 3 user khác nhau (tránh unique UserId+GameId)
        await using (var ctx = _factory.CreateDbContext())
        {
            var roleId = await ctx.Roles
                .Where(r => r.Name == "Player")
                .Select(r => r.Id)
                .FirstAsync();
            var third = new User
            {
                Username = "reviewthird",
                Email = "reviewthird@test.local",
                PasswordHash = "ignored",
                FullName = "Review Third",
                RoleId = roleId,
                IsActive = true
            };
            ctx.Users.Add(third);
            await ctx.SaveChangesAsync();

            ctx.Reviews.AddRange(
                new Review { UserId = _playerId, GameId = _gameId, Rating = 5 },
                new Review { UserId = _otherId, GameId = _gameId, Rating = 4 },
                new Review { UserId = third.Id, GameId = _gameId, Rating = 3 });
            await ctx.SaveChangesAsync();
        }

        var service = new ReviewService(_factory, _currentUser, _logService);

        // Act
        var avg = await service.GetAverageRatingAsync(_gameId);

        // Assert
        Assert.Equal(4.0, avg);
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

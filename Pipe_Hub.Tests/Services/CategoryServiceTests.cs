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

public class CategoryServiceTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IDbContextFactory<PipeHubDbContext> _factory = null!;
    private CurrentUserContext _currentUser = null!;
    private ILogService _logService = null!;

    private int _adminUserId;
    private int _genreId;
    private int _tagId;

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

        // 3. Tạo schema + seed tối thiểu
        await using (var ctx = _factory.CreateDbContext())
        {
            await ctx.Database.EnsureCreatedAsync();

            var adminRole = new Role { Name = "Admin" };
            var playerRole = new Role { Name = "Player" };
            ctx.Roles.AddRange(adminRole, playerRole);
            await ctx.SaveChangesAsync();

            var admin = new User
            {
                Username = "admincat",
                Email = "admincat@test.local",
                PasswordHash = "ignored",
                FullName = "Category Admin",
                RoleId = adminRole.Id,
                IsActive = true
            };
            ctx.Users.Add(admin);
            await ctx.SaveChangesAsync();
            _adminUserId = admin.Id;

            var genre = new Genre { Name = "Action", Description = "Hành động" };
            var tag = new Tag { Name = "Multiplayer" };
            ctx.Genres.Add(genre);
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync();
            _genreId = genre.Id;
            _tagId = tag.Id;
        }

        // 4. CurrentUserContext (mặc định là Admin) + mock LogService
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(_adminUserId, "admincat", "Category Admin", "Admin");

        _logService = Substitute.For<ILogService>();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    // ===========================================================
    // Test 1: CreateGenreAsync khi là Admin → Success
    // ===========================================================
    [Fact]
    public async Task CreateGenreAsync_WhenAdmin_ReturnsSuccess()
    {
        // Arrange
        var service = new CategoryService(_factory, _currentUser, _logService);

        // Act
        var result = await service.CreateGenreAsync("RPG", "Nhập vai");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("RPG", result.Data!.Name);
        Assert.Equal("Nhập vai", result.Data.Description);
    }

    // ===========================================================
    // Test 2: CreateGenreAsync trùng tên → AppException
    // (CategoryService throw AppException khi trùng tên — theo D8.3)
    // ===========================================================
    [Fact]
    public async Task CreateGenreAsync_WhenDuplicateName_ThrowsAppException()
    {
        // Arrange: "Action" đã được seed sẵn
        var service = new CategoryService(_factory, _currentUser, _logService);

        // Act + Assert
        await Assert.ThrowsAsync<AppException>(
            () => service.CreateGenreAsync("Action", null));
    }

    // ===========================================================
    // Test 3: DeleteGenreAsync khi Genre đang được Game dùng → Failure
    // ===========================================================
    [Fact]
    public async Task DeleteGenreAsync_WhenInUseByGame_ReturnsFailure()
    {
        // Arrange: tạo 1 Game gán vào Genre đang xét
        await using (var ctx = _factory.CreateDbContext())
        {
            var game = new Game
            {
                Title = "In-use Game",
                Description = "Game đang dùng genre",
                Price = 100_000m,
                Status = GameStatus.Draft,
                PublisherId = _adminUserId,
                IsDeleted = false
            };

            var genre = await ctx.Genres.FirstAsync(g => g.Id == _genreId);
            game.Genres.Add(genre);

            ctx.Games.Add(game);
            await ctx.SaveChangesAsync();
        }

        var service = new CategoryService(_factory, _currentUser, _logService);

        // Act
        var result = await service.DeleteGenreAsync(_genreId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("đang được sử dụng", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // ===========================================================
    // Test 4: DeleteGenreAsync khi không bị dùng → Success
    // ===========================================================
    [Fact]
    public async Task DeleteGenreAsync_WhenNotInUse_ReturnsSuccess()
    {
        // Arrange
        var service = new CategoryService(_factory, _currentUser, _logService);

        // Act
        var result = await service.DeleteGenreAsync(_genreId);

        // Assert
        Assert.True(result.IsSuccess);

        await using var ctx = _factory.CreateDbContext();
        Assert.False(await ctx.Genres.AnyAsync(g => g.Id == _genreId));
    }

    // ===========================================================
    // Test 5: CreateTagAsync khi không phải Admin → AppException
    // ===========================================================
    [Fact]
    public async Task CreateTagAsync_WhenNotAdmin_ThrowsAppException()
    {
        // Arrange: hạ quyền current user xuống Player
        _currentUser.SetUser(_adminUserId, "admincat", "Category Admin", "Player");
        var service = new CategoryService(_factory, _currentUser, _logService);

        // Act + Assert
        await Assert.ThrowsAsync<AppException>(
            () => service.CreateTagAsync("Indie"));
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

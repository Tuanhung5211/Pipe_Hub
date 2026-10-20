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

public class CsvKeyExporterTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IDbContextFactory<PipeHubDbContext> _factory = null!;
    private CurrentUserContext _currentUser = null!;
    private ILogService _logService = null!;
    private int _gameId;
    private int _publisherId;
    private readonly List<string> _tempFiles = new();

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PipeHubDbContext>()
            .UseSqlite(_connection).Options;
        _factory = new TestDbContextFactory(options);

        await using (var ctx = _factory.CreateDbContext())
        {
            await ctx.Database.EnsureCreatedAsync();

            var pubRole = new Role { Name = "Publisher" };
            ctx.Roles.Add(pubRole);
            await ctx.SaveChangesAsync();

            var pub = new User
            {
                Username = "csvpubexp",
                Email = "csvpubexp@test.local",
                PasswordHash = "ignored",
                FullName = "CSV Publisher Export",
                RoleId = pubRole.Id,
                IsActive = true
            };
            ctx.Users.Add(pub);
            await ctx.SaveChangesAsync();
            _publisherId = pub.Id;

            var game = new Game
            {
                Title = "CSV Export Game",
                Price = 150_000m,
                Status = GameStatus.Approved,
                PublisherId = pub.Id,
                IsDeleted = false
            };
            ctx.Games.Add(game);
            await ctx.SaveChangesAsync();
            _gameId = game.Id;
        }

        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(_publisherId, "csvpubexp", "CSV Publisher Export", "Publisher");
        _logService = Substitute.For<ILogService>();
    }

    public async Task DisposeAsync()
    {
        foreach (var f in _tempFiles)
        {
            try { if (File.Exists(f)) File.Delete(f); } catch { }
        }
        await _connection.DisposeAsync();
    }

    private string NewTempCsvPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"csvexp_{Guid.NewGuid():N}.csv");
        _tempFiles.Add(path);
        return path;
    }

    private async Task SeedKeyAsync(string key, KeyStatus status, DateTime? activatedAt = null)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.GameKeys.Add(new GameKey
        {
            Key = key,
            GameId = _gameId,
            Status = status,
            ActivatedAt = activatedAt
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task ExportAsync_WhenNoKeys_ReturnsSuccessZero()
    {
        var path = NewTempCsvPath();
        var service = new CsvKeyExporter(_factory, _currentUser, _logService);

        var result = await service.ExportAsync(_gameId, path);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Data);
        Assert.True(File.Exists(path));
        var lines = await File.ReadAllLinesAsync(path);
        Assert.Single(lines);
        Assert.Contains("Key,Status,GameTitle", lines[0]);
    }

    [Fact]
    public async Task ExportAsync_WhenNoStatusFilter_ExportsOnlyAvailable()
    {
        await SeedKeyAsync("EXP-KEY-001", KeyStatus.Available);
        await SeedKeyAsync("EXP-KEY-002", KeyStatus.Available);
        await SeedKeyAsync("EXP-KEY-003", KeyStatus.Activated, new DateTime(2026, 1, 2, 3, 4, 5));

        var path = NewTempCsvPath();
        var service = new CsvKeyExporter(_factory, _currentUser, _logService);

        var result = await service.ExportAsync(_gameId, path);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data);

        var lines = await File.ReadAllLinesAsync(path);
        Assert.Equal(3, lines.Length);
        Assert.Contains("Key,Status,GameTitle", lines[0]);
        Assert.DoesNotContain(lines.Skip(1), l => l.Contains("Activated"));
    }

    [Fact]
    public async Task ExportAsync_WithStatusFilter_ExportsOnlyMatching()
    {
        await SeedKeyAsync("EXP-FLT-001", KeyStatus.Available);
        await SeedKeyAsync("EXP-FLT-002", KeyStatus.Available);
        await SeedKeyAsync("EXP-FLT-003", KeyStatus.Sold);

        var path = NewTempCsvPath();
        var service = new CsvKeyExporter(_factory, _currentUser, _logService);

        var result = await service.ExportAsync(_gameId, path, "Available");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data);

        var lines = await File.ReadAllLinesAsync(path);
        Assert.Equal(3, lines.Length);
    }

    private class TestDbContextFactory : IDbContextFactory<PipeHubDbContext>
    {
        private readonly DbContextOptions<PipeHubDbContext> _options;
        public TestDbContextFactory(DbContextOptions<PipeHubDbContext> options) => _options = options;
        public PipeHubDbContext CreateDbContext() => new(_options);
        public Task<PipeHubDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }
}

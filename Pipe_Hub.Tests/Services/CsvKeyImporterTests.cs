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

public class CsvKeyImporterTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private IDbContextFactory<PipeHubDbContext> _factory = null!;
    private CurrentUserContext _currentUser = null!;
    private ILogService _logService = null!;
    private int _gameId;
    private int _otherGameId;
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
                Username = "csvpub",
                Email = "csvpub@test.local",
                PasswordHash = "ignored",
                FullName = "CSV Publisher",
                RoleId = pubRole.Id,
                IsActive = true
            };
            ctx.Users.Add(pub);
            await ctx.SaveChangesAsync();
            _publisherId = pub.Id;

            var g1 = new Game
            {
                Title = "CSV Test Game",
                Price = 100_000m,
                Status = GameStatus.Approved,
                PublisherId = pub.Id,
                IsDeleted = false
            };
            var g2 = new Game
            {
                Title = "Other Game",
                Price = 200_000m,
                Status = GameStatus.Approved,
                PublisherId = pub.Id,
                IsDeleted = false
            };
            ctx.Games.AddRange(g1, g2);
            await ctx.SaveChangesAsync();
            _gameId = g1.Id;
            _otherGameId = g2.Id;
        }

        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(_publisherId, "csvpub", "CSV Publisher", "Publisher");
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

    private string CreateTempCsv(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"csvtest_{Guid.NewGuid():N}.csv");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    [Fact]
    public async Task ImportAsync_WhenFileNotFound_ReturnsFailure()
    {
        var service = new CsvKeyImporter(_factory, _currentUser, _logService);
        var result = await service.ImportAsync(_gameId, @"C:\nonexistent\keys.csv");
        Assert.False(result.IsSuccess);
        Assert.Contains("không tìm thấy", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportAsync_WhenNotCsvExtension_ReturnsFailure()
    {
        var path = Path.Combine(Path.GetTempPath(), $"csvtest_{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "Key\nABC-001");
        _tempFiles.Add(path);

        var service = new CsvKeyImporter(_factory, _currentUser, _logService);
        var result = await service.ImportAsync(_gameId, path);
        Assert.False(result.IsSuccess);
        Assert.Contains(".csv", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportAsync_WhenValidFile_ImportsAllValidKeys()
    {
        var path = CreateTempCsv("Key\nSKY-001\nSKY-002\nSKY-003");
        var service = new CsvKeyImporter(_factory, _currentUser, _logService);
        var result = await service.ImportAsync(_gameId, path);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Data);

        await using var ctx = _factory.CreateDbContext();
        Assert.Equal(3, await ctx.GameKeys.CountAsync(k => k.GameId == _gameId));
    }

    [Fact]
    public async Task ImportAsync_WhenDuplicateInSameGame_SkipsDuplicates()
    {
        await using (var seed = _factory.CreateDbContext())
        {
            seed.GameKeys.Add(new GameKey
            {
                Key = "EXISTING-001",
                GameId = _gameId,
                Status = KeyStatus.Available
            });
            await seed.SaveChangesAsync();
        }

        var path = CreateTempCsv("Key\nEXISTING-001\nNEW-KEY-001\nEXISTING-001");
        var service = new CsvKeyImporter(_factory, _currentUser, _logService);
        var result = await service.ImportAsync(_gameId, path);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data);

        await using var check = _factory.CreateDbContext();
        Assert.Equal(2, await check.GameKeys.CountAsync(k => k.GameId == _gameId));
    }

    [Fact]
    public async Task ImportAsync_WhenKeyExistsInOtherGame_SkipsGlobally()
    {
        await using (var seed = _factory.CreateDbContext())
        {
            seed.GameKeys.Add(new GameKey
            {
                Key = "CROSS-GAME-001",
                GameId = _otherGameId,
                Status = KeyStatus.Available
            });
            await seed.SaveChangesAsync();
        }

        var path = CreateTempCsv("Key\nCROSS-GAME-001\nUNIQUE-NEW-001");
        var service = new CsvKeyImporter(_factory, _currentUser, _logService);
        var result = await service.ImportAsync(_gameId, path);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Data);

        await using var check = _factory.CreateDbContext();
        Assert.Equal(1, await check.GameKeys.CountAsync(k => k.GameId == _gameId));
        Assert.Equal(1, await check.GameKeys.CountAsync(k => k.GameId == _otherGameId));
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

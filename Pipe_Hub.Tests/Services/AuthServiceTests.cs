using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.BLL.Services;
using Pipe_Hub.DAL.Data;
using Pipe_Hub.DAL.Entities;
using Xunit;

namespace Pipe_Hub.Tests.Services;

/// <summary>
/// Unit test cho AuthService: Login, Register, ChangePassword, GetById.
/// Pattern giống OrderServiceTests/ReviewServiceTests: SQLite in-memory + mock ILogService.
/// </summary>
public class AuthServiceTests : IAsyncLifetime
{
    private const string ValidPassword = "player123";

    private SqliteConnection _connection = null!;
    private IDbContextFactory<PipeHubDbContext> _factory = null!;
    private CurrentUserContext _currentUser = null!;
    private ILogService _logService = null!;

    private int _playerId;

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
                Username = "player1",
                Email = "player1@test.local",
                PasswordHash = PasswordHasher.Hash(ValidPassword),
                FullName = "Player One",
                RoleId = playerRole.Id,
                IsActive = true
            };
            var locked = new User
            {
                Username = "locked",
                Email = "locked@test.local",
                PasswordHash = PasswordHasher.Hash(ValidPassword),
                FullName = "Locked User",
                RoleId = playerRole.Id,
                IsActive = false
            };
            var publisher = new User
            {
                Username = "publisher1",
                Email = "publisher1@test.local",
                PasswordHash = PasswordHasher.Hash(ValidPassword),
                FullName = "Publisher One",
                RoleId = publisherRole.Id,
                IsActive = true
            };
            ctx.Users.AddRange(player, locked, publisher);
            await ctx.SaveChangesAsync();
            _playerId = player.Id;
        }

        // 5. CurrentUserContext + mock LogService
        _currentUser = new CurrentUserContext();
        _currentUser.SetUser(_playerId, "player1", "Player One", "Player");

        _logService = Substitute.For<ILogService>();
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private AuthService CreateService() => new(_factory, _logService, _currentUser);

    // ===========================================================
    // LoginAsync
    // ===========================================================

    // Test 1: Login thiếu username/password → Failure
    [Fact]
    public async Task LoginAsync_WhenUsernameEmpty_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "",
            Password = ValidPassword
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("đầy đủ", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 2: Login username không tồn tại → Failure
    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "ghost",
            Password = ValidPassword
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("không đúng", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 3: Login sai password → Failure (message giống user-not-found)
    [Fact]
    public async Task LoginAsync_WhenPasswordWrong_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "player1",
            Password = "wrong-password"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("không đúng", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 4: Login user bị khóa (IsActive = false) → Failure
    [Fact]
    public async Task LoginAsync_WhenUserInactive_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "locked",
            Password = ValidPassword
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("khóa", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 5: Login thành công → trả UserDto + RoleName map đúng (permission)
    [Fact]
    public async Task LoginAsync_WhenValid_ReturnsSuccess_WithRoleName()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "player1",
            Password = ValidPassword
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(_playerId, result.Data!.Id);
        Assert.Equal("player1", result.Data.Username);
        Assert.Equal("Player One", result.Data.FullName);
        Assert.Equal("Player", result.Data.RoleName); // permission theo Role
    }

    // Test 6: Login user Role Publisher → RoleName "Publisher"
    [Fact]
    public async Task LoginAsync_WhenPublisher_ReturnsPublisherRoleName()
    {
        var service = CreateService();

        var result = await service.LoginAsync(new LoginRequestDto
        {
            Username = "publisher1",
            Password = ValidPassword
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Publisher", result.Data!.RoleName);
    }

    // ===========================================================
    // RegisterAsync
    // ===========================================================

    // Test 7: Register thiếu field → Failure
    [Fact]
    public async Task RegisterAsync_WhenMissingFields_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "newplayer",
            Email = "newplayer@test.local",
            Password = "secret123",
            ConfirmPassword = "secret123",
            FullName = "   " // thiếu
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("đầy đủ", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 8: Register password < 6 ký tự → Failure
    [Fact]
    public async Task RegisterAsync_WhenPasswordTooShort_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "newplayer",
            Email = "newplayer@test.local",
            Password = "12345",
            ConfirmPassword = "12345",
            FullName = "New Player"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("6 ký tự", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 9: Register confirm password không khớp → Failure
    [Fact]
    public async Task RegisterAsync_WhenConfirmMismatch_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "newplayer",
            Email = "newplayer@test.local",
            Password = "secret123",
            ConfirmPassword = "secret124",
            FullName = "New Player"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("không khớp", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 10: Register trùng username → Failure
    [Fact]
    public async Task RegisterAsync_WhenUsernameExists_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "player1",
            Email = "brandnew@test.local",
            Password = "secret123",
            ConfirmPassword = "secret123",
            FullName = "New Player"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("đã tồn tại", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 11: Register trùng email → Failure
    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "brandnew",
            Email = "player1@test.local",
            Password = "secret123",
            ConfirmPassword = "secret123",
            FullName = "New Player"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Email đã được sử dụng", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 12: Register hợp lệ → Success + tạo User role Player + Wallet 0 + hash password
    [Fact]
    public async Task RegisterAsync_WhenValid_ReturnsSuccess_AndCreatesPlayerWithWallet()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequestDto
        {
            Username = "newplayer",
            Email = "newplayer@test.local",
            Password = "secret123",
            ConfirmPassword = "secret123",
            FullName = "New Player",
            Country = "VN"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal("newplayer", result.Data!.Username);
        Assert.Equal("Player", result.Data.RoleName);

        // Verify DB: User + Wallet được tạo, password lưu dạng hash
        await using var ctx = _factory.CreateDbContext();
        var created = await ctx.Users
            .Include(u => u.Role)
            .FirstAsync(u => u.Username == "newplayer");

        Assert.Equal("Player", created.Role.Name);
        Assert.True(created.IsActive);
        Assert.NotEqual("secret123", created.PasswordHash);
        Assert.True(PasswordHasher.Verify("secret123", created.PasswordHash));

        var wallet = await ctx.Wallets.FirstAsync(w => w.UserId == created.Id);
        Assert.Equal(0m, wallet.Balance);
    }

    // ===========================================================
    // ChangePasswordAsync
    // ===========================================================

    // Test 13: ChangePassword thiếu field → Failure
    [Fact]
    public async Task ChangePasswordAsync_WhenMissingFields_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            OldPassword = "",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("đầy đủ", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 14: ChangePassword mật khẩu mới < 6 ký tự → Failure
    [Fact]
    public async Task ChangePasswordAsync_WhenNewPasswordTooShort_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            OldPassword = ValidPassword,
            NewPassword = "12345",
            ConfirmNewPassword = "12345"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("6 ký tự", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 15: ChangePassword confirm không khớp → Failure
    [Fact]
    public async Task ChangePasswordAsync_WhenConfirmMismatch_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            OldPassword = ValidPassword,
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass124"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("không khớp", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 16: ChangePassword mật khẩu mới trùng mật khẩu cũ → Failure
    [Fact]
    public async Task ChangePasswordAsync_WhenNewSameAsOld_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            OldPassword = ValidPassword,
            NewPassword = ValidPassword,
            ConfirmNewPassword = ValidPassword
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("khác", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 17: ChangePassword mật khẩu cũ sai → Failure
    [Fact]
    public async Task ChangePasswordAsync_WhenOldPasswordWrong_ReturnsFailure()
    {
        var service = CreateService();

        var result = await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            OldPassword = "wrong-old",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("cũ", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    // Test 18: ChangePassword hợp lệ → Success + hash mới dùng được
    [Fact]
    public async Task ChangePasswordAsync_WhenValid_UpdatesPasswordHash()
    {
        const string newPassword = "newpass456";
        var service = CreateService();

        var result = await service.ChangePasswordAsync(new ChangePasswordRequestDto
        {
            OldPassword = ValidPassword,
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword
        });

        Assert.True(result.IsSuccess);

        // Verify: hash mới lưu đúng, mật khẩu mới login được, mật khẩu cũ không còn
        await using (var ctx = _factory.CreateDbContext())
        {
            var user = await ctx.Users.FirstAsync(u => u.Id == _playerId);
            Assert.True(PasswordHasher.Verify(newPassword, user.PasswordHash));
            Assert.False(PasswordHasher.Verify(ValidPassword, user.PasswordHash));
            Assert.NotNull(user.UpdatedAt);
        }

        var loginResult = await service.LoginAsync(new LoginRequestDto
        {
            Username = "player1",
            Password = newPassword
        });
        Assert.True(loginResult.IsSuccess);
    }

    // ===========================================================
    // GetByIdAsync
    // ===========================================================

    // Test 19: GetById user tồn tại → trả DTO + RoleName
    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        var service = CreateService();

        var dto = await service.GetByIdAsync(_playerId);

        Assert.Equal(_playerId, dto.Id);
        Assert.Equal("player1", dto.Username);
        Assert.Equal("Player", dto.RoleName);
    }

    // Test 20: GetById user không tồn tại → throws AppException
    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ThrowsAppException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(999_999));
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

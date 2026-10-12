using Microsoft.EntityFrameworkCore;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PipeHubDbContext context)
    {
        // IDEMPOTENT CHECK
        if (await context.Roles.AnyAsync())
        {
            return;
        }

        // 1. ROLES
        var roleAdmin = new Role { Name = "Admin", Description = "Quản trị viên hệ thống" };
        var rolePublisher = new Role { Name = "Publisher", Description = "Nhà phát hành game" };
        var rolePlayer = new Role { Name = "Player", Description = "Người chơi" };
        context.Roles.AddRange(roleAdmin, rolePublisher, rolePlayer);
        await context.SaveChangesAsync();

        // 2. USERS
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        var userPasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123");

        var admin = new User
        {
            Username = "admin",
            Email = "admin@pipehub.local",
            PasswordHash = adminPasswordHash,
            FullName = "System Administrator",
            RoleId = roleAdmin.Id,
            IsActive = true,
            Country = "Vietnam"
        };

        var pub1 = new User
        {
            Username = "publisher1",
            Email = "pub1@pipehub.local",
            PasswordHash = userPasswordHash,
            FullName = "Nhà phát hành Alpha",
            RoleId = rolePublisher.Id,
            IsActive = true,
            Country = "Vietnam"
        };

        var pub2 = new User
        {
            Username = "publisher2",
            Email = "pub2@pipehub.local",
            PasswordHash = userPasswordHash,
            FullName = "Nhà phát hành Beta",
            RoleId = rolePublisher.Id,
            IsActive = true,
            Country = "Japan"
        };

        var player1 = new User
        {
            Username = "player1",
            Email = "player1@pipehub.local",
            PasswordHash = userPasswordHash,
            FullName = "Người chơi Một",
            RoleId = rolePlayer.Id,
            IsActive = true,
            Country = "Vietnam",
            Latitude = 21.0285,
            Longitude = 105.8542
        };

        var player2 = new User
        {
            Username = "player2",
            Email = "player2@pipehub.local",
            PasswordHash = userPasswordHash,
            FullName = "Người chơi Hai",
            RoleId = rolePlayer.Id,
            IsActive = true,
            Country = "Vietnam",
            Latitude = 10.8231,
            Longitude = 106.6297
        };

        var player3 = new User
        {
            Username = "player3",
            Email = "player3@pipehub.local",
            PasswordHash = userPasswordHash,
            FullName = "Người chơi Ba",
            RoleId = rolePlayer.Id,
            IsActive = true,
            Country = "United States",
            Latitude = 40.7128,
            Longitude = -74.0060
        };

        context.Users.AddRange(admin, pub1, pub2, player1, player2, player3);
        await context.SaveChangesAsync();

        // 3. WALLETS
        var wallets = new[]
        {
            new Wallet { UserId = player1.Id, Balance = 500_000m },
            new Wallet { UserId = player2.Id, Balance = 300_000m },
            new Wallet { UserId = player3.Id, Balance = 1_000_000m }
        };
        context.Wallets.AddRange(wallets);
        await context.SaveChangesAsync();

        // 4. GENRES
        var genres = new[]
        {
            new Genre { Name = "Action", Description = "Hành động" },
            new Genre { Name = "RPG", Description = "Nhập vai" },
            new Genre { Name = "Strategy", Description = "Chiến thuật" },
            new Genre { Name = "Sports", Description = "Thể thao" }
        };
        context.Genres.AddRange(genres);
        await context.SaveChangesAsync();

        // 5. TAGS
        var tags = new[]
        {
            new Tag { Name = "Singleplayer" },
            new Tag { Name = "Multiplayer" },
            new Tag { Name = "OpenWorld" },
            new Tag { Name = "Indie" }
        };
        context.Tags.AddRange(tags);
        await context.SaveChangesAsync();

        // 6. LANGUAGES
        var languages = new[]
        {
            new Language { Name = "English", Code = "en" },
            new Language { Name = "Vietnamese", Code = "vi" },
            new Language { Name = "Chinese", Code = "zh" },
            new Language { Name = "Japanese", Code = "ja" }
        };
        context.Languages.AddRange(languages);
        await context.SaveChangesAsync();

        // 7. GAMES
        var games = new List<Game>
        {
            new Game { Title = "Sky Adventure", Description = "Game phiêu lưu trên không", Price = 199_000m, Status = GameStatus.Approved, PublisherId = pub1.Id, IsDeleted = false },
            new Game { Title = "Dungeon Master", Description = "RPG trong hầm ngục", Price = 299_000m, SalePrice = 199_000m, SaleStartAt = DateTime.UtcNow.AddDays(-5), SaleEndAt = DateTime.UtcNow.AddDays(5), Status = GameStatus.Approved, PublisherId = pub1.Id, IsDeleted = false },
            new Game { Title = "Cyber Racer", Description = "Đua xe tương lai", Price = 249_000m, Status = GameStatus.Approved, PublisherId = pub2.Id, IsDeleted = false },
            new Game { Title = "Kingdom Builder", Description = "Xây dựng vương quốc", Price = 349_000m, Status = GameStatus.Approved, PublisherId = pub2.Id, IsDeleted = false },
            new Game { Title = "Pixel Warrior", Description = "Game indie pixel", Price = 99_000m, Status = GameStatus.Approved, PublisherId = pub1.Id, IsDeleted = false },
            new Game { Title = "Space Explorer", Description = "Khám phá vũ trụ", Price = 399_000m, Status = GameStatus.Pending, PublisherId = pub2.Id, IsDeleted = false }
        };
        context.Games.AddRange(games);
        await context.SaveChangesAsync();

        // Gán Genre/Tag/Language cho game (N-N)
        games[0].Genres.Add(genres[0]);
        games[0].Tags.Add(tags[2]);
        games[0].Languages.Add(languages[0]);
        games[0].Languages.Add(languages[1]);

        games[1].Genres.Add(genres[1]);
        games[1].Tags.Add(tags[0]);
        games[1].Languages.Add(languages[0]);

        games[2].Genres.Add(genres[0]);
        games[2].Tags.Add(tags[1]);
        games[2].Languages.Add(languages[0]);

        games[3].Genres.Add(genres[2]);
        games[3].Tags.Add(tags[0]);
        games[3].Languages.Add(languages[0]);

        games[4].Genres.Add(genres[0]);
        games[4].Tags.Add(tags[3]);
        games[4].Languages.Add(languages[1]);

        await context.SaveChangesAsync();

        // 8. GAME KEYS
        var keys = new List<GameKey>();
        var rng = new Random(42);

        foreach (var game in games.Where(g => g.Status == GameStatus.Approved))
        {
            for (int i = 0; i < 5; i++)
            {
                keys.Add(new GameKey
                {
                    Key = $"{game.Title.Replace(" ", "").ToUpper()}-{rng.Next(100000, 999999)}",
                    Status = KeyStatus.Available,
                    GameId = game.Id
                });
            }
        }
        context.GameKeys.AddRange(keys);
        await context.SaveChangesAsync();

        // 9. SYSTEM CONFIG
        var configs = new[]
        {
            new SystemConfig { Key = "CommissionRate", Value = "0.3", Description = "Hoa hồng sàn (30%)" },
            new SystemConfig { Key = "MinTopUpAmount", Value = "10000", Description = "Số tiền nạp tối thiểu" },
            new SystemConfig { Key = "MaxTopUpAmount", Value = "50000000", Description = "Số tiền nạp tối đa" },
            new SystemConfig { Key = "SiteName", Value = "Pipe Hub", Description = "Tên hệ thống" }
        };
        context.SystemConfigs.AddRange(configs);
        await context.SaveChangesAsync();

        // 10. PLAY SESSIONS mẫu
        var sessions = new List<PlaySession>();
        var now = DateTime.UtcNow;
        for (int d = 0; d < 7; d++)
        {
            sessions.Add(new PlaySession
            {
                UserId = player1.Id,
                GameId = games[0].Id,
                StartedAt = now.AddDays(-d).AddHours(-2),
                EndedAt = now.AddDays(-d),
                DurationMinutes = 120
            });
        }
        context.PlaySessions.AddRange(sessions);
        await context.SaveChangesAsync();

        // 11. WISHLIST mẫu
        var wishlists = new[]
        {
            new Wishlist { UserId = player1.Id, GameId = games[2].Id },
            new Wishlist { UserId = player1.Id, GameId = games[3].Id },
            new Wishlist { UserId = player2.Id, GameId = games[1].Id }
        };
        context.Wishlists.AddRange(wishlists);
        await context.SaveChangesAsync();

        // 12. REVIEWS mẫu
        var reviews = new[]
        {
            new Review { UserId = player1.Id, GameId = games[0].Id, Rating = 5, Content = "Game rất hay!" },
            new Review { UserId = player2.Id, GameId = games[0].Id, Rating = 4, Content = "Ổn, nhưng hơi ngắn" },
            new Review { UserId = player1.Id, GameId = games[1].Id, Rating = 5, Content = "RPG đỉnh cao" }
        };
        context.Reviews.AddRange(reviews);
        await context.SaveChangesAsync();
    }
}

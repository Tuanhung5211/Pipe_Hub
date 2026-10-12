using Microsoft.EntityFrameworkCore;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PipeHubDbContext context)
    {
        // IDEMPOTENT CHECK — nếu có Order thì coi như đã seed
        if (await context.Orders.AnyAsync())
        {
            return;
        }

        // BỌC TRANSACTION — nếu lỗi bất kỳ chỗ nào → rollback toàn bộ
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            // ===== 1. ROLES =====
            var roleAdmin = new Role { Name = "Admin", Description = "Quản trị viên hệ thống" };
            var rolePublisher = new Role { Name = "Publisher", Description = "Nhà phát hành game" };
            var rolePlayer = new Role { Name = "Player", Description = "Người chơi" };
            context.Roles.AddRange(roleAdmin, rolePublisher, rolePlayer);
            await context.SaveChangesAsync();

            // ===== 2. USERS =====
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            var userPasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123");

            var admin = new User { Username = "admin", Email = "admin@pipehub.local", PasswordHash = adminPasswordHash, FullName = "System Administrator", RoleId = roleAdmin.Id, IsActive = true, Country = "Vietnam" };
            var pub1 = new User { Username = "publisher1", Email = "pub1@pipehub.local", PasswordHash = userPasswordHash, FullName = "Nhà phát hành Alpha", RoleId = rolePublisher.Id, IsActive = true, Country = "Vietnam" };
            var pub2 = new User { Username = "publisher2", Email = "pub2@pipehub.local", PasswordHash = userPasswordHash, FullName = "Nhà phát hành Beta", RoleId = rolePublisher.Id, IsActive = true, Country = "Japan" };
            var player1 = new User { Username = "player1", Email = "player1@pipehub.local", PasswordHash = userPasswordHash, FullName = "Người chơi Một", RoleId = rolePlayer.Id, IsActive = true, Country = "Vietnam", Latitude = 21.0285, Longitude = 105.8542 };
            var player2 = new User { Username = "player2", Email = "player2@pipehub.local", PasswordHash = userPasswordHash, FullName = "Người chơi Hai", RoleId = rolePlayer.Id, IsActive = true, Country = "Vietnam", Latitude = 10.8231, Longitude = 106.6297 };
            var player3 = new User { Username = "player3", Email = "player3@pipehub.local", PasswordHash = userPasswordHash, FullName = "Người chơi Ba", RoleId = rolePlayer.Id, IsActive = true, Country = "United States", Latitude = 40.7128, Longitude = -74.0060 };

            context.Users.AddRange(admin, pub1, pub2, player1, player2, player3);
            await context.SaveChangesAsync();

            // ===== 3. WALLETS =====
            var wallet1 = new Wallet { UserId = player1.Id, Balance = 500_000m };
            var wallet2 = new Wallet { UserId = player2.Id, Balance = 300_000m };
            var wallet3 = new Wallet { UserId = player3.Id, Balance = 1_000_000m };
            context.Wallets.AddRange(wallet1, wallet2, wallet3);
            await context.SaveChangesAsync();

            // ===== 4. GENRES / TAGS / LANGUAGES =====
            var genres = new[]
            {
                new Genre { Name = "Action", Description = "Hành động" },
                new Genre { Name = "RPG", Description = "Nhập vai" },
                new Genre { Name = "Strategy", Description = "Chiến thuật" },
                new Genre { Name = "Sports", Description = "Thể thao" }
            };
            var tags = new[]
            {
                new Tag { Name = "Singleplayer" },
                new Tag { Name = "Multiplayer" },
                new Tag { Name = "OpenWorld" },
                new Tag { Name = "Indie" }
            };
            var languages = new[]
            {
                new Language { Name = "English", Code = "en" },
                new Language { Name = "Vietnamese", Code = "vi" },
                new Language { Name = "Chinese", Code = "zh" },
                new Language { Name = "Japanese", Code = "ja" }
            };
            context.Genres.AddRange(genres);
            context.Tags.AddRange(tags);
            context.Languages.AddRange(languages);
            await context.SaveChangesAsync();

            // ===== 5. GAMES =====
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

            // Gán N-N
            games[0].Genres.Add(genres[0]); games[0].Tags.Add(tags[2]); games[0].Languages.Add(languages[0]); games[0].Languages.Add(languages[1]);
            games[1].Genres.Add(genres[1]); games[1].Tags.Add(tags[0]); games[1].Languages.Add(languages[0]);
            games[2].Genres.Add(genres[0]); games[2].Tags.Add(tags[1]); games[2].Languages.Add(languages[0]);
            games[3].Genres.Add(genres[2]); games[3].Tags.Add(tags[0]); games[3].Languages.Add(languages[0]);
            games[4].Genres.Add(genres[0]); games[4].Tags.Add(tags[3]); games[4].Languages.Add(languages[1]);
            await context.SaveChangesAsync();

            // ===== 6. GAME BUILDS =====
            var builds = new List<GameBuild>
            {
                new GameBuild { GameId = games[0].Id, Version = "1.0.0", Channel = BuildChannel.Stable, PatchNotes = "Initial release", ReleasedAt = DateTime.UtcNow.AddDays(-30) },
                new GameBuild { GameId = games[0].Id, Version = "1.1.0-beta", Channel = BuildChannel.Beta, PatchNotes = "Beta build", ReleasedAt = DateTime.UtcNow.AddDays(-5) },
                new GameBuild { GameId = games[1].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = DateTime.UtcNow.AddDays(-20) },
                new GameBuild { GameId = games[2].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = DateTime.UtcNow.AddDays(-15) },
                new GameBuild { GameId = games[3].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = DateTime.UtcNow.AddDays(-10) },
                new GameBuild { GameId = games[4].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = DateTime.UtcNow.AddDays(-5) }
            };
            context.GameBuilds.AddRange(builds);
            await context.SaveChangesAsync();

            // ===== 7. GAME KEYS =====
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

            // ===== 8. ORDERS + ORDER ITEMS + TRANSACTIONS + WALLET SYNC =====
            // Đơn 1: player1 mua Sky Adventure (199k)
            var order1 = new Order
            {
                OrderCode = "ORD-2026-0001",
                UserId = player1.Id,
                TotalAmount = 199_000m,
                Status = OrderStatus.Completed,
                Note = "Mua Sky Adventure"
            };
            context.Orders.Add(order1);
            await context.SaveChangesAsync();

            var orderItem1 = new OrderItem
            {
                OrderId = order1.Id,
                GameId = games[0].Id,
                Quantity = 1,
                UnitPrice = 199_000m
            };
            context.OrderItems.Add(orderItem1);

            // Gán key cho đơn 1
            var keyForOrder1 = keys.First(k => k.GameId == games[0].Id);
            keyForOrder1.Status = KeyStatus.Sold;
            keyForOrder1.OrderId = order1.Id;
            keyForOrder1.ActivatedAt = DateTime.UtcNow.AddDays(-10);

            // Trừ ví player1 (500k - 199k = 301k)
            wallet1.Balance = 301_000m;
            context.Transactions.Add(new Transaction
            {
                UserId = player1.Id,
                WalletId = wallet1.Id,
                Amount = -199_000m,
                Type = TransactionType.Purchase,
                Status = TransactionStatus.Success,
                Description = "Mua game Sky Adventure - đơn ORD-2026-0001"
            });

            // Đơn 2: player2 mua Dungeon Master (sale 199k)
            var order2 = new Order
            {
                OrderCode = "ORD-2026-0002",
                UserId = player2.Id,
                TotalAmount = 199_000m,
                Status = OrderStatus.Completed,
                Note = "Mua Dungeon Master (sale)"
            };
            context.Orders.Add(order2);
            await context.SaveChangesAsync();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = order2.Id,
                GameId = games[1].Id,
                Quantity = 1,
                UnitPrice = 199_000m
            });

            var keyForOrder2 = keys.First(k => k.GameId == games[1].Id);
            keyForOrder2.Status = KeyStatus.Sold;
            keyForOrder2.OrderId = order2.Id;
            keyForOrder2.ActivatedAt = DateTime.UtcNow.AddDays(-5);

            wallet2.Balance = 101_000m; // 300k - 199k
            context.Transactions.Add(new Transaction
            {
                UserId = player2.Id,
                WalletId = wallet2.Id,
                Amount = -199_000m,
                Type = TransactionType.Purchase,
                Status = TransactionStatus.Success,
                Description = "Mua game Dungeon Master - đơn ORD-2026-0002"
            });

            // Transaction nạp ví ban đầu (đồng bộ với Balance ban đầu)
            context.Transactions.Add(new Transaction
            {
                UserId = player1.Id,
                WalletId = wallet1.Id,
                Amount = 500_000m,
                Type = TransactionType.TopUp,
                Status = TransactionStatus.Success,
                Description = "Nạp ví lần đầu"
            });
            context.Transactions.Add(new Transaction
            {
                UserId = player2.Id,
                WalletId = wallet2.Id,
                Amount = 300_000m,
                Type = TransactionType.TopUp,
                Status = TransactionStatus.Success,
                Description = "Nạp ví lần đầu"
            });
            context.Transactions.Add(new Transaction
            {
                UserId = player3.Id,
                WalletId = wallet3.Id,
                Amount = 1_000_000m,
                Type = TransactionType.TopUp,
                Status = TransactionStatus.Success,
                Description = "Nạp ví lần đầu"
            });

            await context.SaveChangesAsync();

            // ===== 9. TOPUP REQUESTS (2 pending + 1 approved) =====
            context.TopUpRequests.AddRange(
                new TopUpRequest { UserId = player1.Id, Amount = 100_000m, Status = TopUpStatus.Pending, PaymentMethod = "VNPay", PaymentReference = "VNP-2026-001" },
                new TopUpRequest { UserId = player2.Id, Amount = 200_000m, Status = TopUpStatus.Pending, PaymentMethod = "MoMo", PaymentReference = "MOMO-2026-001" },
                new TopUpRequest { UserId = player3.Id, Amount = 500_000m, Status = TopUpStatus.Approved, PaymentMethod = "VNPay", PaymentReference = "VNP-2026-000", ApprovedAt = DateTime.UtcNow.AddDays(-3) }
            );
            await context.SaveChangesAsync();

            // ===== 10. SYSTEM CONFIG =====
            context.SystemConfigs.AddRange(
                new SystemConfig { Key = "CommissionRate", Value = "0.3", Description = "Hoa hồng sàn (30%)" },
                new SystemConfig { Key = "MinTopUpAmount", Value = "10000", Description = "Số tiền nạp tối thiểu" },
                new SystemConfig { Key = "MaxTopUpAmount", Value = "50000000", Description = "Số tiền nạp tối đa" },
                new SystemConfig { Key = "SiteName", Value = "Pipe Hub", Description = "Tên hệ thống" }
            );
            await context.SaveChangesAsync();

            // ===== 11. PLAY SESSIONS (7 ngày cho Dashboard CCU) =====
            var now = DateTime.UtcNow;
            var sessions = new List<PlaySession>();
            for (int d = 0; d < 7; d++)
            {
                sessions.Add(new PlaySession { UserId = player1.Id, GameId = games[0].Id, StartedAt = now.AddDays(-d).AddHours(-2), EndedAt = now.AddDays(-d), DurationMinutes = 120 });
                if (d < 3)
                {
                    sessions.Add(new PlaySession { UserId = player2.Id, GameId = games[1].Id, StartedAt = now.AddDays(-d).AddHours(-3), EndedAt = now.AddDays(-d).AddHours(-1), DurationMinutes = 120 });
                }
            }
            context.PlaySessions.AddRange(sessions);
            await context.SaveChangesAsync();

            // ===== 12. WISHLISTS =====
            context.Wishlists.AddRange(
                new Wishlist { UserId = player1.Id, GameId = games[2].Id },
                new Wishlist { UserId = player1.Id, GameId = games[3].Id },
                new Wishlist { UserId = player2.Id, GameId = games[3].Id }
            );
            await context.SaveChangesAsync();

            // ===== 13. ACHIEVEMENTS + USER ACHIEVEMENTS =====
            var achievements = new[]
            {
                new Achievement { GameId = games[0].Id, Name = "First Flight", Description = "Hoàn thành màn đầu tiên" },
                new Achievement { GameId = games[0].Id, Name = "Sky Master", Description = "Hoàn thành toàn bộ game" },
                new Achievement { GameId = games[1].Id, Name = "Dungeon Slayer", Description = "Đánh bại trùm cuối" }
            };
            context.Achievements.AddRange(achievements);
            await context.SaveChangesAsync();

            context.UserAchievements.AddRange(
                new UserAchievement { UserId = player1.Id, AchievementId = achievements[0].Id },
                new UserAchievement { UserId = player1.Id, AchievementId = achievements[1].Id },
                new UserAchievement { UserId = player2.Id, AchievementId = achievements[2].Id }
            );
            await context.SaveChangesAsync();

            // ===== 14. REVIEWS (chỉ player đã mua mới review) =====
            context.Reviews.AddRange(
                new Review { UserId = player1.Id, GameId = games[0].Id, Rating = 5, Content = "Game rất hay!" },
                new Review { UserId = player2.Id, GameId = games[1].Id, Rating = 5, Content = "RPG đỉnh cao" }
            );
            await context.SaveChangesAsync();

            // ===== COMMIT TRANSACTION =====
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

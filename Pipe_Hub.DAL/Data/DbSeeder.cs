using Microsoft.EntityFrameworkCore;
using Pipe_Hub.DAL.Entities;
using Pipe_Hub.DAL.Enums;

namespace Pipe_Hub.DAL.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(PipeHubDbContext context)
    {
        // IDEMPOTENT CHECK — Roles là bảng đầu tiên được seed
        // (transaction all-or-nothing nên check bảng đầu là đủ)
        if (await context.Roles.AnyAsync())
        {
            return;
        }

        var now = DateTime.UtcNow;

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

            // ===== 3. WALLETS (Balance ban đầu = số tiền nạp đầu) =====
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
                new Game { Title = "Dungeon Master", Description = "RPG trong hầm ngục", Price = 299_000m, SalePrice = 199_000m, SaleStartAt = now.AddDays(-60), SaleEndAt = now.AddDays(5), Status = GameStatus.Approved, PublisherId = pub1.Id, IsDeleted = false },
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
                new GameBuild { GameId = games[0].Id, Version = "1.0.0", Channel = BuildChannel.Stable, PatchNotes = "Initial release", ReleasedAt = now.AddDays(-80) },
                new GameBuild { GameId = games[0].Id, Version = "1.1.0-beta", Channel = BuildChannel.Beta, PatchNotes = "Beta build", ReleasedAt = now.AddDays(-40) },
                new GameBuild { GameId = games[1].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = now.AddDays(-70) },
                new GameBuild { GameId = games[2].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = now.AddDays(-65) },
                new GameBuild { GameId = games[3].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = now.AddDays(-30) },
                new GameBuild { GameId = games[4].Id, Version = "1.0.0", Channel = BuildChannel.Stable, ReleasedAt = now.AddDays(-25) }
            };
            context.GameBuilds.AddRange(builds);
            await context.SaveChangesAsync();

            // ===== 7. GAME KEYS (25 key Available) =====
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

            // ===== 8. TOPUP BAN ĐẦU (đồng bộ với Balance ban đầu, -90 ngày) =====
            var topUpDate = now.AddDays(-90);
            context.Transactions.Add(new Transaction
            {
                UserId = player1.Id, WalletId = wallet1.Id, Amount = 500_000m,
                Type = TransactionType.TopUp, Status = TransactionStatus.Success,
                Description = "Nạp ví lần đầu", CreatedAt = topUpDate
            });
            context.Transactions.Add(new Transaction
            {
                UserId = player2.Id, WalletId = wallet2.Id, Amount = 300_000m,
                Type = TransactionType.TopUp, Status = TransactionStatus.Success,
                Description = "Nạp ví lần đầu", CreatedAt = topUpDate
            });
            context.Transactions.Add(new Transaction
            {
                UserId = player3.Id, WalletId = wallet3.Id, Amount = 1_000_000m,
                Type = TransactionType.TopUp, Status = TransactionStatus.Success,
                Description = "Nạp ví lần đầu", CreatedAt = topUpDate
            });
            await context.SaveChangesAsync();

            // ===== 9. ĐƠN ORD-2026-0001 (player1, Sky Adventure, -50 ngày) =====
            var order1Date = now.AddDays(-50);
            var order1 = new Order
            {
                OrderCode = "ORD-2026-0001", UserId = player1.Id, TotalAmount = 199_000m,
                Status = OrderStatus.Completed, Note = "Mua Sky Adventure", CreatedAt = order1Date
            };
            context.Orders.Add(order1);
            await context.SaveChangesAsync();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = order1.Id, GameId = games[0].Id, Quantity = 1,
                UnitPrice = 199_000m, CreatedAt = order1Date
            });

            var keyForOrder1 = keys.First(k => k.GameId == games[0].Id);
            keyForOrder1.Status = KeyStatus.Activated;
            keyForOrder1.OrderId = order1.Id;
            keyForOrder1.ActivatedAt = order1Date.AddHours(1);

            wallet1.Balance = 301_000m; // 500k - 199k
            context.Transactions.Add(new Transaction
            {
                UserId = player1.Id, WalletId = wallet1.Id, Amount = -199_000m,
                Type = TransactionType.Purchase, Status = TransactionStatus.Success,
                Description = "Mua game Sky Adventure - đơn ORD-2026-0001", CreatedAt = order1Date
            });
            await context.SaveChangesAsync();

            // ===== 10. ĐƠN ORD-2026-0002 (player2, Dungeon Master sale, -12 ngày) =====
            var order2Date = now.AddDays(-12);
            var order2 = new Order
            {
                OrderCode = "ORD-2026-0002", UserId = player2.Id, TotalAmount = 199_000m,
                Status = OrderStatus.Completed, Note = "Mua Dungeon Master (sale)", CreatedAt = order2Date
            };
            context.Orders.Add(order2);
            await context.SaveChangesAsync();

            context.OrderItems.Add(new OrderItem
            {
                OrderId = order2.Id, GameId = games[1].Id, Quantity = 1,
                UnitPrice = 199_000m, CreatedAt = order2Date
            });

            var keyForOrder2 = keys.First(k => k.GameId == games[1].Id);
            keyForOrder2.Status = KeyStatus.Activated;
            keyForOrder2.OrderId = order2.Id;
            keyForOrder2.ActivatedAt = order2Date.AddHours(1);

            wallet2.Balance = 101_000m; // 300k - 199k
            context.Transactions.Add(new Transaction
            {
                UserId = player2.Id, WalletId = wallet2.Id, Amount = -199_000m,
                Type = TransactionType.Purchase, Status = TransactionStatus.Success,
                Description = "Mua game Dungeon Master - đơn ORD-2026-0002", CreatedAt = order2Date
            });
            await context.SaveChangesAsync();

            // ===== 11. 3 ĐƠN CỦA PLAYER3 =====
            var player3Orders = new[]
            {
                (Code: "ORD-2026-0003", Game: games[2], Price: 249_000m, DaysAgo: 70),
                (Code: "ORD-2026-0004", Game: games[3], Price: 349_000m, DaysAgo: 35),
                (Code: "ORD-2026-0005", Game: games[4], Price: 99_000m, DaysAgo: 20)
            };

            foreach (var (Code, Game, Price, DaysAgo) in player3Orders)
            {
                var orderDate = now.AddDays(-DaysAgo);
                var order = new Order
                {
                    OrderCode = Code, UserId = player3.Id, TotalAmount = Price,
                    Status = OrderStatus.Completed, Note = $"Mua {Game.Title}", CreatedAt = orderDate
                };
                context.Orders.Add(order);
                await context.SaveChangesAsync();

                context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id, GameId = Game.Id, Quantity = 1,
                    UnitPrice = Price, CreatedAt = orderDate
                });

                var keyForOrder = keys.First(k => k.GameId == Game.Id && k.OrderId == null);
                keyForOrder.Status = KeyStatus.Activated;
                keyForOrder.OrderId = order.Id;
                keyForOrder.ActivatedAt = orderDate.AddHours(1);

                context.Transactions.Add(new Transaction
                {
                    UserId = player3.Id, WalletId = wallet3.Id, Amount = -Price,
                    Type = TransactionType.Purchase, Status = TransactionStatus.Success,
                    Description = $"Mua game {Game.Title} - đơn {Code}", CreatedAt = orderDate
                });
                await context.SaveChangesAsync();
            }

            // Balance cuối wallet3 = 1M + 500k(top-up duyệt) - 249k - 349k - 99k = 803k
            wallet3.Balance = 803_000m;

            // ===== 12. TOPUP REQUESTS (2 pending + 1 approved) =====
            var approvedTopUpDate = now.AddDays(-3);
            context.TopUpRequests.AddRange(
                new TopUpRequest { UserId = player1.Id, Amount = 100_000m, Status = TopUpStatus.Pending, PaymentMethod = "VNPay", PaymentReference = "VNP-2026-001" },
                new TopUpRequest { UserId = player2.Id, Amount = 200_000m, Status = TopUpStatus.Pending, PaymentMethod = "MoMo", PaymentReference = "MOMO-2026-001" },
                new TopUpRequest { UserId = player3.Id, Amount = 500_000m, Status = TopUpStatus.Approved, PaymentMethod = "VNPay", PaymentReference = "VNP-2026-000", ApprovedAt = approvedTopUpDate }
            );
            await context.SaveChangesAsync();

            // Transaction TopUp cho yêu cầu đã duyệt của player3
            context.Transactions.Add(new Transaction
            {
                UserId = player3.Id, WalletId = wallet3.Id, Amount = 500_000m,
                Type = TransactionType.TopUp, Status = TransactionStatus.Success,
                Description = "Nạp ví VNP-2026-000", CreatedAt = approvedTopUpDate
            });
            await context.SaveChangesAsync();

            // ===== 13. SYSTEM CONFIG =====
            context.SystemConfigs.AddRange(
                new SystemConfig { Key = "CommissionRate", Value = "0.3", Description = "Hoa hồng sàn (30%)" },
                new SystemConfig { Key = "MinTopUpAmount", Value = "10000", Description = "Số tiền nạp tối thiểu" },
                new SystemConfig { Key = "MaxTopUpAmount", Value = "50000000", Description = "Số tiền nạp tối đa" },
                new SystemConfig { Key = "SiteName", Value = "Pipe Hub", Description = "Tên hệ thống" }
            );
            await context.SaveChangesAsync();

            // ===== 14. PLAY SESSIONS (7 ngày cho Dashboard CCU) =====
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

            // ===== 15. WISHLISTS =====
            context.Wishlists.AddRange(
                new Wishlist { UserId = player1.Id, GameId = games[2].Id },
                new Wishlist { UserId = player1.Id, GameId = games[3].Id },
                new Wishlist { UserId = player2.Id, GameId = games[3].Id }
            );
            await context.SaveChangesAsync();

            // ===== 16. ACHIEVEMENTS + USER ACHIEVEMENTS =====
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

            // ===== 17. REVIEWS (chỉ player đã mua mới review) =====
            context.Reviews.AddRange(
                new Review { UserId = player1.Id, GameId = games[0].Id, Rating = 5, Content = "Game rất hay!" },
                new Review { UserId = player2.Id, GameId = games[1].Id, Rating = 5, Content = "RPG đỉnh cao" },
                new Review { UserId = player3.Id, GameId = games[2].Id, Rating = 4, Content = "Đua rất đã tay" }
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

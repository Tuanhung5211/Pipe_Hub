using Microsoft.EntityFrameworkCore;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Data;

public class PipeHubDbContext : DbContext
{
    public PipeHubDbContext(DbContextOptions<PipeHubDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<GameBuild> GameBuilds => Set<GameBuild>();
    public DbSet<GameKey> GameKeys => Set<GameKey>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TopUpRequest> TopUpRequests => Set<TopUpRequest>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<PlaySession> PlaySessions => Set<PlaySession>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PipeHubDbContext).Assembly);

        // SQLite (unit test) không hỗ trợ rowversion — dùng byte[] thường
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        {
            modelBuilder.Entity<Pipe_Hub.DAL.Entities.Wallet>()
                .Property(w => w.RowVersion)
                .IsConcurrencyToken(false)
                .ValueGeneratedNever()
                .HasDefaultValue(Array.Empty<byte>());
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class GameKeyConfiguration : IEntityTypeConfiguration<GameKey>
{
    public void Configure(EntityTypeBuilder<GameKey> builder)
    {
        builder.ToTable("GameKeys");

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(k => k.Status)
            .HasConversion<int>();

        builder.HasIndex(k => k.Key).IsUnique();
        builder.HasIndex(k => k.Status);

        // GameKey → Game
        builder.HasOne(k => k.Game)
            .WithMany(g => g.Keys)
            .HasForeignKey(k => k.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // GameKey → Order (nullable)
        builder.HasOne(k => k.Order)
            .WithMany(o => o.Keys)
            .HasForeignKey(k => k.OrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

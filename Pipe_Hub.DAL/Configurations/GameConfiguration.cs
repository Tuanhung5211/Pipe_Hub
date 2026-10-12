using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("Games");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasMaxLength(4000);

        builder.Property(g => g.CoverImagePath)
            .HasMaxLength(500);

        builder.Property(g => g.Price)
            .HasPrecision(18, 2);

        builder.Property(g => g.SalePrice)
            .HasPrecision(18, 2);

        builder.Property(g => g.Status)
            .HasConversion<int>();

        builder.HasIndex(g => g.Title);
        builder.HasIndex(g => g.Status);

        // Game → Publisher (User) — Restrict
        builder.HasOne(g => g.Publisher)
            .WithMany()
            .HasForeignKey(g => g.PublisherId)
            .OnDelete(DeleteBehavior.Restrict);

        // Game ↔ Genre (N-N)
        builder.HasMany(g => g.Genres)
            .WithMany(ge => ge.Games)
            .UsingEntity(j => j.ToTable("GameGenres"));

        // Game ↔ Tag (N-N)
        builder.HasMany(g => g.Tags)
            .WithMany(t => t.Games)
            .UsingEntity(j => j.ToTable("GameTags"));

        // Game ↔ Language (N-N)
        builder.HasMany(g => g.Languages)
            .WithMany(l => l.Games)
            .UsingEntity(j => j.ToTable("GameLanguages"));
    }
}

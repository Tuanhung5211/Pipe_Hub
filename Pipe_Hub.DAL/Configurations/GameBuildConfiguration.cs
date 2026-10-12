using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class GameBuildConfiguration : IEntityTypeConfiguration<GameBuild>
{
    public void Configure(EntityTypeBuilder<GameBuild> builder)
    {
        builder.ToTable("GameBuilds");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Version)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Channel)
            .HasConversion<int>();

        builder.Property(b => b.PatchNotes)
            .HasMaxLength(4000);

        builder.Property(b => b.FilePath)
            .HasMaxLength(500);

        // GameBuild → Game (many-to-1)
        builder.HasOne(b => b.Game)
            .WithMany(g => g.Builds)
            .HasForeignKey(b => b.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

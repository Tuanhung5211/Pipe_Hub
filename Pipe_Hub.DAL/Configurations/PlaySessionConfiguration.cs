using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class PlaySessionConfiguration : IEntityTypeConfiguration<PlaySession>
{
    public void Configure(EntityTypeBuilder<PlaySession> builder)
    {
        builder.ToTable("PlaySessions");

        builder.HasKey(ps => ps.Id);

        builder.HasIndex(ps => ps.UserId);
        builder.HasIndex(ps => ps.GameId);
        builder.HasIndex(ps => ps.StartedAt);

        // PlaySession → User (Cascade — session thuộc user)
        builder.HasOne(ps => ps.User)
            .WithMany(u => u.PlaySessions)
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // PlaySession → Game (Cascade — session không quan trọng lịch sử game)
        builder.HasOne(ps => ps.Game)
            .WithMany()
            .HasForeignKey(ps => ps.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

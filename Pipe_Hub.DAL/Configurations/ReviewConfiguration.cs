using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", t =>
        {
            t.HasCheckConstraint("CK_Review_Rating", "[Rating] >= 1 AND [Rating] <= 5");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Content)
            .HasMaxLength(2000);

        // Unique composite — mỗi user chỉ review 1 game 1 lần
        // HasFilter loại review đã xóa mềm → user có thể viết lại sau khi xóa
        builder.HasIndex(r => new { r.UserId, r.GameId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(r => r.GameId);
        builder.HasIndex(r => r.CreatedAt);

        // Review → User (Restrict — giữ lịch sử)
        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Review → Game (Restrict — giữ lịch sử)
        builder.HasOne(r => r.Game)
            .WithMany(g => g.Reviews)
            .HasForeignKey(r => r.GameId)
            .OnDelete(DeleteBehavior.Restrict);

        // Soft delete — query mặc định lọc IsDeleted = false
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

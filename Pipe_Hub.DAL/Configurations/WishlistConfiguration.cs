using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.ToTable("Wishlists");

        builder.HasKey(w => w.Id);

        // Unique composite — mỗi user thêm 1 game 1 lần
        builder.HasIndex(w => new { w.UserId, w.GameId }).IsUnique();
        builder.HasIndex(w => w.CreatedAt);

        // Wishlist → User (Cascade — wishlist thuộc user)
        builder.HasOne(w => w.User)
            .WithMany(u => u.Wishlists)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Wishlist → Game (Cascade — wishlist không quan trọng)
        builder.HasOne(w => w.Game)
            .WithMany(g => g.Wishlists)
            .HasForeignKey(w => w.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Type)
            .HasConversion<int>();

        builder.Property(t => t.Status)
            .HasConversion<int>();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.CreatedAt);
        builder.HasIndex(t => t.Status);

        // Transaction → User (many-to-1) — Restrict (giữ lịch sử)
        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction → Wallet (many-to-1, nullable) — Restrict
        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class TopUpRequestConfiguration : IEntityTypeConfiguration<TopUpRequest>
{
    public void Configure(EntityTypeBuilder<TopUpRequest> builder)
    {
        builder.ToTable("TopUpRequests");

        builder.HasKey(tr => tr.Id);

        builder.Property(tr => tr.Amount)
            .HasPrecision(18, 2);

        builder.Property(tr => tr.Status)
            .HasConversion<int>();

        builder.Property(tr => tr.PaymentMethod)
            .HasMaxLength(50);

        builder.Property(tr => tr.PaymentReference)
            .HasMaxLength(200);

        builder.Property(tr => tr.RejectReason)
            .HasMaxLength(500);

        builder.HasIndex(tr => tr.UserId);
        builder.HasIndex(tr => tr.Status);
        builder.HasIndex(tr => tr.CreatedAt);

        // TopUpRequest → User (many-to-1) — Restrict
        // User KHÔNG có navigation ngược → dùng WithMany() không tham số
        builder.HasOne(tr => tr.User)
            .WithMany()
            .HasForeignKey(tr => tr.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

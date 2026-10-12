using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("LogEntries");

        builder.HasKey(le => le.Id);

        builder.Property(le => le.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(le => le.Details)
            .HasMaxLength(2000);

        builder.Property(le => le.EntityName)
            .HasMaxLength(100);

        builder.HasIndex(le => le.UserId);
        builder.HasIndex(le => le.CreatedAt);
        builder.HasIndex(le => le.EntityName);

        // LogEntry → User (nullable) — SetNull (giữ log khi xóa user)
        builder.HasOne(le => le.User)
            .WithMany()
            .HasForeignKey(le => le.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

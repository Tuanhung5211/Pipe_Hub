using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pipe_Hub.DAL.Entities;

namespace Pipe_Hub.DAL.Configurations;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("SystemConfigs");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sc => sc.Value)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(sc => sc.Description)
            .HasMaxLength(500);

        // Unique Key — không có 2 config cùng key
        builder.HasIndex(sc => sc.Key).IsUnique();
    }
}

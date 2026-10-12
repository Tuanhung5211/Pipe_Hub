\---

name: pipehub-fluent-api

description: "Pattern chuẩn để viết Fluent API Configuration trong dự án Pipe\_Hub. Dùng khi tạo mới hoặc sửa file trong Pipe\_Hub.DAL/Configurations/. Bao gồm quy tắc DeleteBehavior, HasQueryFilter, HasPrecision, index, quan hệ N-N, check constraint. Áp dụng khi làm task D2.x, D3+, hoặc bất kỳ khi nào thêm/sửa entity cần config mới."

\---



\# Pipe\_Hub — Fluent API Configuration Pattern



\## 1. Nguyên tắc cốt lõi



\- \*\*Mỗi entity = 1 file config riêng\*\* trong `Pipe\_Hub.DAL/Configurations/`

\- \*\*Tên file\*\*: `{EntityName}Configuration.cs` (VD: `UserConfiguration.cs`)

\- \*\*Namespace\*\*: `Pipe\_Hub.DAL.Configurations`

\- \*\*Class\*\*: `public class XxxConfiguration : IEntityTypeConfiguration<Xxx>`

\- \*\*DbContext\*\* tự load qua `modelBuilder.ApplyConfigurationsFromAssembly()` — KHÔNG cần đăng ký thủ công

\- \*\*KHÔNG hard-code schema\*\* — dùng `ToTable("TableName")` với PascalCase



\## 2. Khung chuẩn của 1 config file



```csharp

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Pipe\_Hub.DAL.Entities;



namespace Pipe\_Hub.DAL.Configurations;



public class XxxConfiguration : IEntityTypeConfiguration<Xxx>

{

&#x20;   public void Configure(EntityTypeBuilder<Xxx> builder)

&#x20;   {

&#x20;       builder.ToTable("Xxxs");

&#x20;       builder.HasKey(x => x.Id);



&#x20;       // 1. Property config

&#x20;       builder.Property(x => x.Name)

&#x20;           .IsRequired()

&#x20;           .HasMaxLength(100);



&#x20;       builder.Property(x => x.Description)

&#x20;           .HasMaxLength(500);



&#x20;       // 2. Index

&#x20;       builder.HasIndex(x => x.Name).IsUnique();



&#x20;       // 3. Relations

&#x20;       builder.HasOne(x => x.User)

&#x20;           .WithMany(u => u.Xxxs)

&#x20;           .HasForeignKey(x => x.UserId)

&#x20;           .OnDelete(DeleteBehavior.Restrict);

&#x20;   }

}


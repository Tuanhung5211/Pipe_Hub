using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.DAL.Data;

namespace Pipe_Hub.BLL.Extensions;

public static class DatabaseInitializer
{
    public static void MigrateAndSeed(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<PipeHubDbContext>>();
        using var db = factory.CreateDbContext();

        // Apply migration chưa apply (nếu đã apply → bỏ qua)
        db.Database.Migrate();

        // Seed data (idempotent — chạy lại không tạo trùng)
        DbSeeder.SeedAsync(db).GetAwaiter().GetResult();
    }
}

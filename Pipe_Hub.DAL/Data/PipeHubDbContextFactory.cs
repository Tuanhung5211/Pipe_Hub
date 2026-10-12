using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pipe_Hub.DAL.Data;

public class PipeHubDbContextFactory : IDesignTimeDbContextFactory<PipeHubDbContext>
{
    public PipeHubDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PipeHubDbContext>();

        // Connection string tạm — CHỈ dùng cho design-time (dotnet ef)
        // Runtime sẽ đọc từ appsettings.json qua DI
        const string connectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=PipeHubDb;Trusted_Connection=True;TrustServerCertificate=True";

        optionsBuilder.UseSqlServer(connectionString);

        return new PipeHubDbContext(optionsBuilder.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.DAL.Data;

namespace Pipe_Hub.BLL.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPipeHubServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PipeHub")
            ?? throw new InvalidOperationException(
                "Connection string 'PipeHub' not found in appsettings.json.");

        // DbContextFactory — không giữ 1 context suốt app (WinForms pattern)
        services.AddDbContextFactory<PipeHubDbContext>(options =>
            options.UseSqlServer(connectionString));

        // TODO D3: Đăng ký Services (IAuthService, IGameService, ...)
        // TODO D3: Đăng ký Forms (LoginForm, MainForm, ...)

        return services;
    }
}

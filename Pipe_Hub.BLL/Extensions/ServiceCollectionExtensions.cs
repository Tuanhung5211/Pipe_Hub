using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.BLL.Services;
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

        // ===== State classes (Singleton) =====
        services.AddSingleton<CurrentUserContext>();
        services.AddSingleton<ISystemConfigService, SystemConfigService>();

        // ===== Services (Transient — không state) =====
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<ILogService, LogService>();

        // ===== Services nhóm Game/Commerce (Transient) =====
        services.AddTransient<IGameService, GameService>();
        services.AddTransient<IWalletService, WalletService>();
        services.AddTransient<IOrderService, OrderService>();
        services.AddTransient<IKeyService, KeyService>();
        services.AddTransient<IReviewService, ReviewService>();
        services.AddTransient<IWishlistService, WishlistService>();
        services.AddTransient<ILibraryService, LibraryService>();
        services.AddTransient<IUserService, UserService>();

        // ===== Services nhóm Admin (Transient) =====
        services.AddTransient<IUserManagementService, UserManagementService>();
        services.AddTransient<ICategoryService, CategoryService>();

        // ===== Services nhóm Publisher (Transient) =====
        services.AddTransient<IBuildService, BuildService>();
        services.AddTransient<ICsvKeyImporter, CsvKeyImporter>();
        services.AddTransient<ICsvKeyExporter, CsvKeyExporter>();

        // ===== Services nhóm Reporting (Transient) =====
        services.AddTransient<IReportService, ReportService>();

        return services;
    }
}

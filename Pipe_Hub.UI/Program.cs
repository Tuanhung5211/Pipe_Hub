using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pipe_Hub.BLL.Extensions;
using Serilog;

namespace Pipe_Hub.UI;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 1. Serilog — cấu hình TRƯỚC khi làm gì khác
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Pipe_Hub", "logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        // 2. Global exception handler — TRƯỚC khi tạo control
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (s, e) =>
        {
            Log.Error(e.Exception, "UI Thread Exception");
            MessageBox.Show(
                "Đã xảy ra lỗi. Vui lòng kiểm tra log để biết chi tiết.",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "AppDomain Unhandled Exception");
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Error(e.Exception, "Unobserved Task Exception");
            e.SetObserved();
        };

        try
        {
            // 3. Host + DI
            var host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    // Đăng ký DbContextFactory + Services qua BLL
                    services.AddPipeHubServices(context.Configuration);

                    // Đăng ký Forms (UI)
                    services.AddTransient<Pipe_Hub.UI.Forms.Auth.LoginForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Auth.RegisterForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Auth.ChangePasswordForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Main.MainForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Player.StoreForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Player.WalletForm>();
                    services.AddTransient<Pipe_Hub.UI.Controls.ReviewPanel>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Player.LibraryForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Player.OrderHistoryForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Player.ProfileForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Player.PlayerAchievementForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Admin.UserManagementForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Admin.UserEditForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Admin.CategoryForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Admin.ApprovalForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Admin.TopUpApprovalForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.GameManagementForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.BuildForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.KeyGeneratorForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.PricingForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.StorePageForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Admin.LogViewerForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Admin.SystemConfigForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.PublisherReportForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.AchievementForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Publisher.MarketingPaymentForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Shared.DashboardForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Shared.ReportForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Shared.ChatForm>();
                    services.AddTransient<Pipe_Hub.UI.Forms.Shared.MapForm>();
                })
                .Build();

            // 4. Migrate + Seed (qua BLL — UI không chạm DbContext trực tiếp)
            DatabaseInitializer.MigrateAndSeed(host.Services);

            // 5. Chạy app
            ApplicationConfiguration.Initialize();
            Application.Run(host.Services.GetRequiredService<Pipe_Hub.UI.Forms.Auth.LoginForm>());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");

            if (ex is Microsoft.Data.SqlClient.SqlException)
            {
                MessageBox.Show(
                    "Không thể kết nối cơ sở dữ liệu.\n\n" +
                    "Vui lòng kiểm tra:\n" +
                    "- SQL Server LocalDB đã được cài đặt chưa?\n" +
                    "- Connection string trong appsettings.json đúng chưa?\n\n" +
                    $"Chi tiết: {ex.Message}",
                    "Lỗi kết nối cơ sở dữ liệu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show(
                    "Ứng dụng không thể khởi động.\n\n" +
                    $"Chi tiết: {ex.Message}\n\n" +
                    "Vui lòng kiểm tra file log để biết thêm.",
                    "Lỗi khởi động",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

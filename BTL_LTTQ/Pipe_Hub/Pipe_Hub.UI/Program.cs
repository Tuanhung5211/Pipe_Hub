using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Pipe_Hub.UI;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // 1. Cấu hình Serilog TRƯỚC khi làm gì khác
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Pipe_Hub", "logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .CreateLogger();

        // 2. Bắt lỗi toàn cục — đặt TRƯỚC khi tạo control
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
            // 3. Cấu hình DI
            var host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    // TODO D2: Đăng ký DbContext
                    // TODO D3: Đăng ký Services (IAuthService, IGameService, ...)
                    // TODO D3: Đăng ký Forms (LoginForm, MainForm, ...)
                })
                .Build();

            // 4. Chạy app
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

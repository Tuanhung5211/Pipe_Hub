using Pipe_Hub.BLL.Common;
using Serilog;

namespace Pipe_Hub.UI.Helpers;

/// <summary>
/// Bọc gọi service async: AppException → MessageBox nghiệp vụ;
/// lỗi lạ → ghi log + thông báo chung (không lộ chi tiết kỹ thuật).
/// </summary>
public static class UiExtensions
{
    public static async Task RunSafeAsync(this Control owner, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (AppException ex)
        {
            MessageBox.Show(owner, ex.Message, "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (OperationCanceledException)
        {
            // Người dùng hủy thao tác — bỏ qua
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi không lường trước trong {Owner}", owner.GetType().Name);
            MessageBox.Show(owner,
                "Đã xảy ra lỗi. Vui lòng thử lại hoặc kiểm tra file log.",
                "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

using System.Net.Mail;

namespace Pipe_Hub.UI.Helpers;

/// <summary>
/// Helper cho ErrorProvider và kiểm tra email.
/// </summary>
public static class ErrorProviderHelper
{
    /// <summary>
    /// Gán/clear lỗi cho control. Trả về isValid để dùng trong Validating.
    /// </summary>
    public static bool Check(this ErrorProvider ep, Control control, bool isValid, string message)
    {
        ep.SetError(control, isValid ? "" : message);
        return isValid;
    }

    /// <summary>
    /// Kiểm tra email hợp lệ bằng MailAddress (không regex tự chế).
    /// </summary>
    public static bool IsValidEmail(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return MailAddress.TryCreate(text.Trim(), out _);
    }
}

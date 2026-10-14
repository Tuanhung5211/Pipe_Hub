namespace Pipe_Hub.UI.Helpers;

/// <summary>
/// Helper đăng ký phím tắt cho form — dùng KeyPreview.
/// </summary>
public static class ShortcutHelper
{
    /// <summary>
    /// Đăng ký phím tắt cho form.
    /// Ví dụ: form.RegisterShortcut(Keys.Control | Keys.F, () => txtSearch.Focus());
    /// </summary>
    public static void RegisterShortcut(this Form form, Keys key, Action action)
    {
        form.KeyPreview = true;
        form.KeyDown += (s, e) =>
        {
            if (e.KeyCode == key || (e.KeyCode | e.Modifiers) == key)
            {
                action();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };
    }
}

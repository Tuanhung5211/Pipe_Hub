using System.Text.RegularExpressions;

namespace Pipe_Hub.UI.Helpers;

public static class TextBoxHelper
{
    // Lọc Username khi Paste (chỉ cho phép chữ, số, _ @ . -)
    public static void SanitizeUsername(object? sender, EventArgs e)
    {
        if (sender is not TextBox txt) return;
        string pattern = @"[^a-zA-Z0-9_@\.\-]";
        var cleanText = Regex.Replace(txt.Text, pattern, "");

        if (txt.Text != cleanText)
        {
            int deletedCount = txt.Text.Length - cleanText.Length;
            int pos = txt.SelectionStart;
            txt.Text = cleanText;
            txt.SelectionStart = Math.Max(0, pos - deletedCount);
        }
    }

    // Lọc Password khi Paste (không cho phép dấu cách, ký tự có dấu)
    public static void SanitizePassword(object? sender, EventArgs e)
    {
        if (sender is not TextBox txt) return;
        string pattern = @"[^a-zA-Z0-9!@#$%^&*()_+=\[\]{};':""\\|,.<>\/?-]";
        var cleanText = Regex.Replace(txt.Text, pattern, "");

        if (txt.Text != cleanText)
        {
            int deletedCount = txt.Text.Length - cleanText.Length;
            int pos = txt.SelectionStart;
            txt.Text = cleanText;
            txt.SelectionStart = Math.Max(0, pos - deletedCount);
        }
    }
}

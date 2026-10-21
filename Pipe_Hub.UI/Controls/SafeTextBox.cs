using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Pipe_Hub.UI.Controls;

public class SafeTextBox : TextBox
{
    private const int WM_CHAR = 0x0102;

    // Mặc định không cho phép dấu cách
    [DefaultValue(false)]
    public bool AllowSpace { get; set; } = false;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CHAR)
        {
            char c = (char)m.WParam;

            // Cho phép các phím điều hướng (Backspace, Enter, Tab...)
            if (c == '\b' || c == '\r' || c == '\n' || c == '\t')
            {
                base.WndProc(ref m);
                return;
            }

            // Chặn ký tự có dấu (mã > 127) VÀ chặn dấu cách
            if (c > 127 || (!AllowSpace && c == ' '))
            {
                return;
            }
        }
        base.WndProc(ref m);
    }
}

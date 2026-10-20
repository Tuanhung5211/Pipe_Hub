using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Shared;

/// <summary>
/// Khung chat với trợ lý ảo (rule-based FAQ). Mở dạng MDI Child từ MainForm.
/// </summary>
public class ChatForm : Form
{
    private readonly IChatService _chatService;

    private FlowLayoutPanel _flowMessages = null!;
    private TextBox _txtInput = null!;
    private Button _btnSend = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    public ChatForm(IChatService chatService)
    {
        _chatService = chatService;
        InitializeComponents();

        // Tin nhắn chào mừng của Bot
        AppendMessage("Xin chào! Tôi là trợ lý ảo. Bạn cần giúp gì?", isUser: false);
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Trợ lý ảo Pipe Hub";
        ClientSize = new Size(600, 700);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        // ===== Khung chứa tin nhắn =====
        _flowMessages = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.White,
            Padding = new Padding(5)
        };

        // Căn lại bong bóng của User khi form đổi kích thước
        _flowMessages.Resize += (s, e) =>
        {
            foreach (Control ctrl in _flowMessages.Controls)
            {
                if (ctrl.Tag?.ToString() == "user")
                {
                    var rightMargin = Math.Max(10, _flowMessages.ClientSize.Width - ctrl.PreferredSize.Width - 25);
                    ctrl.Margin = new Padding(rightMargin, 10, 10, 10);
                }
            }
        };

        // ===== Panel nhập tin (Bottom, Height 60) =====
        var pnlInput = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(245, 245, 245)
        };

        _btnSend = new Button
        {
            Text = "Gửi",
            Dock = DockStyle.Right,
            Width = 80,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSend.Click += BtnSend_Click;

        _txtInput = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11F)
        };
        _txtInput.KeyDown += TxtInput_KeyDown;

        pnlInput.Controls.Add(_txtInput);
        pnlInput.Controls.Add(_btnSend);

        // ===== Thanh trạng thái =====
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel("Sẵn sàng")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusStrip.Items.Add(_lblStatus);

        Controls.Add(_flowMessages);
        Controls.Add(pnlInput);
        Controls.Add(_statusStrip);
    }

    /// <summary>
    /// Thêm 1 bong bóng chat: User → phải (LightBlue), Bot → trái (LightGray).
    /// </summary>
    private void AppendMessage(string text, bool isUser)
    {
        var lblMessage = new Label
        {
            Text = text,
            MaximumSize = new Size(400, 0),
            AutoSize = true,
            Padding = new Padding(10),
            BackColor = isUser ? Color.LightBlue : Color.LightGray,
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 10F)
        };

        var bubble = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = isUser ? DockStyle.Right : DockStyle.Left,
            Margin = new Padding(10)
        };
        bubble.Tag = isUser ? "user" : "bot";
        bubble.Controls.Add(lblMessage);

        _flowMessages.Controls.Add(bubble);

        // FlowLayoutPanel xếp con theo cell (Dock Right/Left không tự căn phải),
        // nên đẩy bong bóng của User sang phải bằng Margin trái.
        if (isUser)
        {
            var rightMargin = Math.Max(10, _flowMessages.ClientSize.Width - bubble.PreferredSize.Width - 25);
            bubble.Margin = new Padding(rightMargin, 10, 10, 10);
        }

        bubble.PerformLayout();
        _flowMessages.ScrollControlIntoView(bubble);
    }

    private async void BtnSend_Click(object? sender, EventArgs e)
    {
        await SendMessageAsync();
    }

    private async void TxtInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true; // Không phát tiếng "ding"
        await SendMessageAsync();
    }

    private async Task SendMessageAsync()
    {
        var text = _txtInput.Text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        AppendMessage(text, isUser: true);
        _txtInput.Clear();

        _btnSend.Enabled = false;
        _lblStatus.Text = "Đang trả lời...";

        try
        {
            await this.RunSafeAsync(async () =>
            {
                var reply = await _chatService.GetResponseAsync(text);
                AppendMessage(reply, isUser: false);
            });
        }
        finally
        {
            _btnSend.Enabled = true;
            _lblStatus.Text = "Sẵn sàng";
            _txtInput.Focus();
        }
    }
}

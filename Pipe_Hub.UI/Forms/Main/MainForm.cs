using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Auth;

namespace Pipe_Hub.UI.Forms.Main;

public class MainForm : Form
{
    private readonly CurrentUserContext _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private MenuStrip _menuStrip = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private ToolStripStatusLabel _lblRole = null!;

    public bool LogoutRequested { get; private set; }

    public MainForm(CurrentUserContext currentUser, IServiceProvider serviceProvider)
    {
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        InitializeComponents();
        BuildMenuByRole();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Pipe Hub — Hệ thống quản lý phát hành game";
        WindowState = FormWindowState.Maximized;
        IsMdiContainer = true;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);

        _menuStrip = new MenuStrip
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 10F)
        };
        MainMenuStrip = _menuStrip;

        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };

        _lblStatus = new ToolStripStatusLabel
        {
            Text = $"Xin chào, {_currentUser.FullName ?? _currentUser.Username}",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblRole = new ToolStripStatusLabel
        {
            Text = $"Vai trò: {_currentUser.RoleName}",
            ForeColor = Color.FromArgb(0, 120, 215),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        _statusStrip.Items.AddRange(new ToolStripItem[] { _lblStatus, _lblRole });

        Controls.Add(_menuStrip);
        Controls.Add(_statusStrip);
    }

    private void BuildMenuByRole()
    {
        _menuStrip.Items.Clear();

        // Menu Hệ thống — chung
        var menuSystem = new ToolStripMenuItem("Hệ thống");

        var menuChangePassword = new ToolStripMenuItem("Đổi mật khẩu")
        {
            ShortcutKeys = Keys.Control | Keys.P
        };
        menuChangePassword.Click += MenuChangePassword_Click;
        menuSystem.DropDownItems.Add(menuChangePassword);

        menuSystem.DropDownItems.Add(new ToolStripSeparator());

        var menuLogout = new ToolStripMenuItem("Đăng xuất")
        {
            ShortcutKeys = Keys.Control | Keys.Q
        };
        menuLogout.Click += MenuLogout_Click;
        menuSystem.DropDownItems.Add(menuLogout);

        menuSystem.DropDownItems.Add(new ToolStripSeparator());

        var menuExit = new ToolStripMenuItem("Thoát")
        {
            ShortcutKeys = Keys.Alt | Keys.F4
        };
        menuExit.Click += (s, e) => Close();
        menuSystem.DropDownItems.Add(menuExit);

        _menuStrip.Items.Add(menuSystem);

        // Menu theo role
        if (_currentUser.IsAdmin)
        {
            BuildAdminMenu();
        }
        else if (_currentUser.IsPublisher)
        {
            BuildPublisherMenu();
        }
        else if (_currentUser.IsPlayer)
        {
            BuildPlayerMenu();
        }
    }

    private void BuildAdminMenu()
    {
        var menuAdmin = new ToolStripMenuItem("Quản trị");
        AddPlaceholderItem(menuAdmin, "Quản lý người dùng", "D8");
        AddPlaceholderItem(menuAdmin, "Quản lý thể loại", "D8");
        AddPlaceholderItem(menuAdmin, "Duyệt game", "D8");
        AddPlaceholderItem(menuAdmin, "Duyệt nạp tiền", "D8");
        AddPlaceholderItem(menuAdmin, "Xem log hệ thống", "D9");
        AddPlaceholderItem(menuAdmin, "Cấu hình hệ thống", "D9");
        _menuStrip.Items.Add(menuAdmin);

        var menuShared = new ToolStripMenuItem("Báo cáo");
        AddPlaceholderItem(menuShared, "Dashboard", "D12");
        AddPlaceholderItem(menuShared, "Báo cáo doanh thu", "D13");
        _menuStrip.Items.Add(menuShared);
    }

    private void BuildPublisherMenu()
    {
        var menuPublisher = new ToolStripMenuItem("Nhà phát hành");
        AddPlaceholderItem(menuPublisher, "Quản lý game", "D9");
        AddPlaceholderItem(menuPublisher, "Bản phát hành", "D10");
        AddPlaceholderItem(menuPublisher, "Định giá", "D10");
        AddPlaceholderItem(menuPublisher, "Sinh key", "D11");
        AddPlaceholderItem(menuPublisher, "Trang game", "D10");
        AddPlaceholderItem(menuPublisher, "Thành tích", "D11");
        AddPlaceholderItem(menuPublisher, "Thanh toán Marketing", "D16");
        AddPlaceholderItem(menuPublisher, "Bản đồ người chơi", "D21");
        _menuStrip.Items.Add(menuPublisher);

        var menuShared = new ToolStripMenuItem("Báo cáo");
        AddPlaceholderItem(menuShared, "Dashboard", "D12");
        AddPlaceholderItem(menuShared, "Báo cáo doanh thu", "D13");
        _menuStrip.Items.Add(menuShared);
    }

    private void BuildPlayerMenu()
    {
        var menuPlayer = new ToolStripMenuItem("Người chơi");
        AddPlaceholderItem(menuPlayer, "Cửa hàng game", "D5");
        AddPlaceholderItem(menuPlayer, "Thư viện game", "D7");
        AddPlaceholderItem(menuPlayer, "Ví của tôi", "D5");
        AddPlaceholderItem(menuPlayer, "Lịch sử đơn hàng", "D7");
        AddPlaceholderItem(menuPlayer, "Hồ sơ cá nhân", "D7");
        AddPlaceholderItem(menuPlayer, "Bản đồ cộng đồng", "D21");
        _menuStrip.Items.Add(menuPlayer);

        var menuChat = new ToolStripMenuItem("Trợ giúp");
        AddPlaceholderItem(menuChat, "Chatbot", "D18");
        _menuStrip.Items.Add(menuChat);
    }

    private void AddPlaceholderItem(ToolStripMenuItem parentMenu, string text, string targetDay)
    {
        var item = new ToolStripMenuItem(text);
        item.Click += (s, e) =>
        {
            MessageBox.Show(
                $"Chức năng này sẽ được triển khai ở {targetDay}.",
                "Thông báo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };
        parentMenu.DropDownItems.Add(item);
    }

    private void MenuChangePassword_Click(object? sender, EventArgs e)
    {
        var changePassForm = _serviceProvider.GetRequiredService<ChangePasswordForm>();
        changePassForm.ShowDialog(this);
    }

    private void MenuLogout_Click(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            "Bạn có chắc chắn muốn đăng xuất?",
            "Đăng xuất",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        foreach (var child in MdiChildren)
        {
            child.Close();
        }

        LogoutRequested = true;
        _currentUser.Clear();
        Close();
    }
}

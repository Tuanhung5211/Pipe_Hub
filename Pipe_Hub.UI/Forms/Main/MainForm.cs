using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Admin;
using Pipe_Hub.UI.Forms.Auth;
using Pipe_Hub.UI.Forms.Player;
using Pipe_Hub.UI.Forms.Publisher;
using Pipe_Hub.UI.Forms.Shared;

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

        var menuUser = new ToolStripMenuItem("Quản lý người dùng");
        menuUser.Click += (s, e) => OpenMdiChild<UserManagementForm>();
        menuAdmin.DropDownItems.Add(menuUser);

        var menuCategory = new ToolStripMenuItem("Quản lý thể loại");
        menuCategory.Click += (s, e) => OpenMdiChild<CategoryForm>();
        menuAdmin.DropDownItems.Add(menuCategory);

        var menuApproveGame = new ToolStripMenuItem("Duyệt game");
        menuApproveGame.Click += (s, e) => OpenMdiChild<ApprovalForm>();
        menuAdmin.DropDownItems.Add(menuApproveGame);

        var menuApproveTopUp = new ToolStripMenuItem("Duyệt nạp tiền");
        menuApproveTopUp.Click += (s, e) => OpenMdiChild<TopUpApprovalForm>();
        menuAdmin.DropDownItems.Add(menuApproveTopUp);

        var menuLog = new ToolStripMenuItem("Xem log hệ thống");
        menuLog.Click += (s, e) => OpenMdiChild<LogViewerForm>();
        menuAdmin.DropDownItems.Add(menuLog);

        var menuConfig = new ToolStripMenuItem("Cấu hình hệ thống");
        menuConfig.Click += (s, e) => OpenMdiChild<SystemConfigForm>();
        menuAdmin.DropDownItems.Add(menuConfig);
        _menuStrip.Items.Add(menuAdmin);

        var menuShared = new ToolStripMenuItem("Báo cáo");
        var menuDashboard = new ToolStripMenuItem("Dashboard");
        menuDashboard.Click += (s, e) => OpenMdiChild<DashboardForm>();
        menuShared.DropDownItems.Add(menuDashboard);

        var menuRevenue = new ToolStripMenuItem("Báo cáo doanh thu");
        menuRevenue.Click += (s, e) => OpenMdiChild<ReportForm>();
        menuShared.DropDownItems.Add(menuRevenue);

        _menuStrip.Items.Add(menuShared);
    }

    private void BuildPublisherMenu()
    {
        var menuPublisher = new ToolStripMenuItem("Nhà phát hành");

        var menuGameManage = new ToolStripMenuItem("Quản lý game");
        menuGameManage.Click += (s, e) => OpenMdiChild<GameManagementForm>();
        menuPublisher.DropDownItems.Add(menuGameManage);

        var menuBuild = new ToolStripMenuItem("Bản phát hành (Build)");
        menuBuild.Click += (s, e) => OpenMdiChild<BuildForm>();
        menuPublisher.DropDownItems.Add(menuBuild);

        var menuPricing = new ToolStripMenuItem("Cấu hình giá");
        menuPricing.Click += (s, e) => OpenMdiChild<PricingForm>();
        menuPublisher.DropDownItems.Add(menuPricing);

        var menuKeyGen = new ToolStripMenuItem("Sinh key");
        menuKeyGen.Click += (s, e) => OpenMdiChild<KeyGeneratorForm>();
        menuPublisher.DropDownItems.Add(menuKeyGen);

        var menuStorePage = new ToolStripMenuItem("Trang game");
        menuStorePage.Click += (s, e) => OpenMdiChild<StorePageForm>();
        menuPublisher.DropDownItems.Add(menuStorePage);
        var menuAchievement = new ToolStripMenuItem("Thành tích");
        menuAchievement.Click += (s, e) => OpenMdiChild<AchievementForm>();
        menuPublisher.DropDownItems.Add(menuAchievement);
        var menuMarketing = new ToolStripMenuItem("Thanh toán Marketing");
        menuMarketing.Click += (s, e) => OpenMdiChild<MarketingPaymentForm>();
        menuPublisher.DropDownItems.Add(menuMarketing);
        AddPlaceholderItem(menuPublisher, "Bản đồ người chơi", "D21");
        _menuStrip.Items.Add(menuPublisher);

        var menuShared = new ToolStripMenuItem("Báo cáo");
        var menuDashboard = new ToolStripMenuItem("Dashboard");
        menuDashboard.Click += (s, e) => OpenMdiChild<DashboardForm>();
        menuShared.DropDownItems.Add(menuDashboard);
        var menuPublisherReport = new ToolStripMenuItem("Báo cáo doanh thu");
        menuPublisherReport.Click += (s, e) => OpenMdiChild<PublisherReportForm>();
        menuShared.DropDownItems.Add(menuPublisherReport);
        _menuStrip.Items.Add(menuShared);
    }

    private void BuildPlayerMenu()
    {
        var menuPlayer = new ToolStripMenuItem("Người chơi");

        var menuStore = new ToolStripMenuItem("Cửa hàng game")
        {
            ShortcutKeys = Keys.Control | Keys.S
        };
        menuStore.Click += (s, e) => OpenMdiChild<StoreForm>();
        menuPlayer.DropDownItems.Add(menuStore);

        var menuLibrary = new ToolStripMenuItem("Thư viện game")
        {
            ShortcutKeys = Keys.Control | Keys.L
        };
        menuLibrary.Click += (s, e) => OpenMdiChild<LibraryForm>();
        menuPlayer.DropDownItems.Add(menuLibrary);

        var menuWallet = new ToolStripMenuItem("Ví của tôi")
        {
            ShortcutKeys = Keys.Control | Keys.W
        };
        menuWallet.Click += (s, e) => OpenMdiChild<WalletForm>();
        menuPlayer.DropDownItems.Add(menuWallet);

        var menuOrders = new ToolStripMenuItem("Lịch sử đơn hàng");
        menuOrders.Click += (s, e) => OpenMdiChild<OrderHistoryForm>();
        menuPlayer.DropDownItems.Add(menuOrders);

        var menuProfile = new ToolStripMenuItem("Hồ sơ cá nhân");
        menuProfile.Click += (s, e) => OpenMdiChild<ProfileForm>();
        menuPlayer.DropDownItems.Add(menuProfile);

        AddPlaceholderItem(menuPlayer, "Bản đồ cộng đồng", "D21");
        _menuStrip.Items.Add(menuPlayer);

        var menuChat = new ToolStripMenuItem("Trợ giúp");
        var menuChatbot = new ToolStripMenuItem("Trợ lý ảo");
        menuChatbot.Click += (s, e) => OpenMdiChild<ChatForm>();
        menuChat.DropDownItems.Add(menuChatbot);
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

    /// <summary>
    /// Mở form con dạng MDI Child. Nếu đã mở → activate thay vì tạo mới.
    /// </summary>
    private void OpenMdiChild<TForm>() where TForm : Form
    {
        // Check form đã mở chưa
        var existing = MdiChildren.OfType<TForm>().FirstOrDefault();
        if (existing is not null)
        {
            if (existing.WindowState == FormWindowState.Minimized)
                existing.WindowState = FormWindowState.Normal;
            existing.Activate();
            return;
        }

        // Tạo mới qua DI
        var form = _serviceProvider.GetRequiredService<TForm>();
        form.MdiParent = this;
        form.Show();
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

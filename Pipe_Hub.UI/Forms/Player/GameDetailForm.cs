using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Player;

public class GameDetailForm : Form
{
    private readonly IGameService _gameService;
    private readonly IOrderService _orderService;
    private readonly IWalletService _walletService;
    private readonly CurrentUserContext _currentUser;
    private readonly int _gameId;

    private Label _lblTitle = null!;
    private Label _lblPublisher = null!;
    private Label _lblPrice = null!;
    private Label _lblSalePrice = null!;
    private TextBox _txtDescription = null!;
    private Label _lblGenres = null!;
    private Label _lblTags = null!;
    private Label _lblLanguages = null!;
    private Label _lblBalance = null!;
    private Button _btnBuy = null!;
    private Button _btnClose = null!;

    public GameDetailForm(
        IGameService gameService,
        IOrderService orderService,
        IWalletService walletService,
        CurrentUserContext currentUser,
        int gameId)
    {
        _gameService = gameService;
        _orderService = orderService;
        _walletService = walletService;
        _currentUser = currentUser;
        _gameId = gameId;

        InitializeComponents();

        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Chi tiết game";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(700, 550);
        MinimumSize = new Size(600, 500);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;

        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true
        };

        // Title
        _lblTitle = new Label
        {
            Text = "Đang tải...",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            Location = new Point(20, 20),
            AutoSize = true,
            MaximumSize = new Size(660, 0)
        };

        _lblPublisher = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.Gray,
            Location = new Point(20, 60),
            AutoSize = true
        };

        // Price
        var lblPriceTitle = new Label
        {
            Text = "Giá:",
            Location = new Point(20, 100),
            Size = new Size(80, 25),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        _lblPrice = new Label
        {
            Text = "—",
            Location = new Point(100, 100),
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215)
        };

        _lblSalePrice = new Label
        {
            Text = "",
            Location = new Point(240, 100),
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.Green
        };

        // Description
        var lblDescTitle = new Label
        {
            Text = "Mô tả:",
            Location = new Point(20, 140),
            Size = new Size(80, 25),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        _txtDescription = new TextBox
        {
            Location = new Point(20, 165),
            Size = new Size(660, 120),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        // Genres
        var lblGenresTitle = new Label
        {
            Text = "Thể loại:",
            Location = new Point(20, 295),
            Size = new Size(80, 25),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        _lblGenres = new Label
        {
            Text = "—",
            Location = new Point(110, 295),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F)
        };

        // Tags
        var lblTagsTitle = new Label
        {
            Text = "Tags:",
            Location = new Point(20, 325),
            Size = new Size(80, 25),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        _lblTags = new Label
        {
            Text = "—",
            Location = new Point(110, 325),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F)
        };

        // Languages
        var lblLangsTitle = new Label
        {
            Text = "Ngôn ngữ:",
            Location = new Point(20, 355),
            Size = new Size(80, 25),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        _lblLanguages = new Label
        {
            Text = "—",
            Location = new Point(110, 355),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F)
        };

        // Balance info
        _lblBalance = new Label
        {
            Text = "Số dư ví: đang tải...",
            Location = new Point(20, 400),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215)
        };

        // Buttons
        _btnBuy = new Button
        {
            Text = "Mua ngay",
            Location = new Point(20, 440),
            Size = new Size(140, 40),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold)
        };
        _btnBuy.Click += BtnBuy_Click;

        _btnClose = new Button
        {
            Text = "Đóng",
            Location = new Point(170, 440),
            Size = new Size(100, 40)
        };
        _btnClose.Click += (s, e) => Close();

        pnlMain.Controls.AddRange(new Control[]
        {
            _lblTitle, _lblPublisher,
            lblPriceTitle, _lblPrice, _lblSalePrice,
            lblDescTitle, _txtDescription,
            lblGenresTitle, _lblGenres,
            lblTagsTitle, _lblTags,
            lblLangsTitle, _lblLanguages,
            _lblBalance,
            _btnBuy, _btnClose
        });

        Controls.Add(pnlMain);
        CancelButton = _btnClose;
    }

    private async Task LoadDataAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var game = await _gameService.GetByIdAsync(_gameId);

            Text = $"Chi tiết game — {game.Title}";
            _lblTitle.Text = game.Title;
            _lblPublisher.Text = $"Nhà phát hành: {game.PublisherName}";

            _lblPrice.Text = $"{game.Price:N0} đ";

            var now = DateTime.UtcNow;
            var isOnSale = game.SalePrice.HasValue
                        && game.SaleStartAt.HasValue
                        && game.SaleEndAt.HasValue
                        && game.SaleStartAt.Value <= now
                        && game.SaleEndAt.Value >= now;

            if (isOnSale)
            {
                _lblPrice.Font = new Font("Segoe UI", 10F, FontStyle.Strikeout);
                _lblPrice.ForeColor = Color.Gray;
                _lblSalePrice.Text = $"→ {game.SalePrice!.Value:N0} đ (SALE)";
            }
            else
            {
                _lblSalePrice.Text = "";
            }

            _txtDescription.Text = game.Description ?? "(không có mô tả)";
            _lblGenres.Text = game.GenreNames.Count > 0
                ? string.Join(", ", game.GenreNames) : "—";
            _lblTags.Text = game.TagNames.Count > 0
                ? string.Join(", ", game.TagNames) : "—";
            _lblLanguages.Text = game.LanguageNames.Count > 0
                ? string.Join(", ", game.LanguageNames) : "—";

            // Load balance
            var wallet = await _walletService.GetMyWalletAsync();
            _lblBalance.Text = $"Số dư ví: {wallet.Balance:N0} đ";

            // Disable Buy nếu game không Approved
            if (game.Status != "Approved")
            {
                _btnBuy.Enabled = false;
                _btnBuy.Text = "Chưa mở bán";
            }
        });
    }

    private async void BtnBuy_Click(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            "Bạn có chắc chắn muốn mua game này?",
            "Xác nhận mua",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnBuy.Enabled = false;
                _btnBuy.Text = "Đang xử lý...";

                var result = await _orderService.PurchaseAsync(_gameId);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(
                        result.ErrorMessage ?? "Mua game thất bại.",
                        "Mua game",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _btnBuy.Enabled = true;
                    _btnBuy.Text = "Mua ngay";
                    return;
                }

                var order = result.Data!;
                var key = order.GameKeys.FirstOrDefault() ?? "(không có key)";

                MessageBox.Show(
                    $"Mua game thành công!\n\n" +
                    $"Mã đơn: {order.OrderCode}\n" +
                    $"Tổng tiền: {order.TotalAmount:N0} đ\n\n" +
                    $"Key của bạn:\n{key}\n\n" +
                    $"Key đã được lưu vào thư viện của bạn.",
                    "Mua thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _btnBuy.Text = "Đã mua";
                _btnBuy.Enabled = false;

                // Reload balance
                await LoadDataAsync();
            }
            finally
            {
                if (_btnBuy.Enabled == false && _btnBuy.Text == "Đang xử lý...")
                {
                    _btnBuy.Text = "Mua ngay";
                    _btnBuy.Enabled = true;
                }
            }
        });
    }
}

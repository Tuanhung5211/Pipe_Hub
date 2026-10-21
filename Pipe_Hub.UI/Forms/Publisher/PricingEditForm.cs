using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Dialog cấu hình giá bán / giá sale theo thời gian cho 1 game.
/// Chỉ dùng ở chế độ Sửa (existingGame bắt buộc khác null).
/// </summary>
public class PricingEditForm : Form
{
    private readonly IGameService _gameService;
    private readonly GameDto _existingGame;

    private Label _lblGameTitle = null!;
    private NumericUpDown _nudPrice = null!;
    private CheckBox _chkIsOnSale = null!;
    private NumericUpDown _nudSalePrice = null!;
    private DateTimePicker _dtpSaleStart = null!;
    private DateTimePicker _dtpSaleEnd = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public PricingEditForm(IGameService gameService, GameDto existingGame)
    {
        _gameService = gameService;
        _existingGame = existingGame;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = "Pipe Hub — Cấu hình giá";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblHeader = new Label
        {
            Text = "CẤU HÌNH GIÁ & SALE",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(460, 32)
        };

        var controls = new List<Control> { lblHeader };

        int y = 65;
        const int labelX = 30, inputX = 160, inputW = 270, rowH = 34;

        // Tên game (chỉ đọc)
        controls.Add(new Label { Text = "Tên game:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _lblGameTitle = new Label
        {
            Text = _existingGame.Title,
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        controls.Add(_lblGameTitle);
        y += rowH;

        // Giá gốc
        controls.Add(new Label { Text = "Giá gốc (đ):", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _nudPrice = new NumericUpDown
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            Minimum = 0,
            Maximum = 1_000_000_000,
            DecimalPlaces = 0,
            ThousandsSeparator = true
        };
        // Clamp để tránh ArgumentOutOfRangeException nếu dữ liệu cũ vượt Maximum.
        _nudPrice.Value = Math.Clamp(_existingGame.Price, _nudPrice.Minimum, _nudPrice.Maximum);
        controls.Add(_nudPrice);
        y += rowH;

        // Checkbox bật/tắt sale
        _chkIsOnSale = new CheckBox
        {
            Text = "Đang sale",
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            Checked = _existingGame.SalePrice.HasValue
        };
        controls.Add(_chkIsOnSale);
        y += rowH;

        // Giá sale
        controls.Add(new Label { Text = "Giá sale (đ):", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _nudSalePrice = new NumericUpDown
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            Minimum = 0,
            Maximum = 1_000_000_000,
            DecimalPlaces = 0,
            ThousandsSeparator = true
        };
        _nudSalePrice.Value = Math.Clamp(
            _existingGame.SalePrice ?? 0m,
            _nudSalePrice.Minimum,
            _nudSalePrice.Maximum);
        controls.Add(_nudSalePrice);
        y += rowH;

        // Thời gian sale
        // DB lưu UTC → đổi sang Local để hiển thị trên DateTimePicker.
        var now = DateTime.Now;
        var saleStart = _existingGame.SaleStartAt.HasValue
            ? DateTime.SpecifyKind(_existingGame.SaleStartAt.Value, DateTimeKind.Utc).ToLocalTime()
            : now;
        var saleEnd = _existingGame.SaleEndAt.HasValue
            ? DateTime.SpecifyKind(_existingGame.SaleEndAt.Value, DateTimeKind.Utc).ToLocalTime()
            : saleStart.AddDays(7);
        if (saleEnd <= saleStart) saleEnd = saleStart.AddDays(7);

        controls.Add(new Label { Text = "Bắt đầu:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _dtpSaleStart = new DateTimePicker
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy HH:mm",
            ShowUpDown = true,
            Value = saleStart
        };
        controls.Add(_dtpSaleStart);
        y += rowH;

        controls.Add(new Label { Text = "Kết thúc:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _dtpSaleEnd = new DateTimePicker
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy HH:mm",
            ShowUpDown = true,
            Value = saleEnd
        };
        controls.Add(_dtpSaleEnd);
        y += rowH + 8;

        // Bật/tắt các control sale theo checkbox
        _chkIsOnSale.CheckedChanged += (s, e) => UpdateSaleInputsEnabled();
        UpdateSaleInputsEnabled();

        // Buttons
        _btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(inputX, y),
            Size = new Size(120, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(inputX + 130, y),
            Size = new Size(120, 32)
        };
        _btnCancel.Click += (s, e) => Close();

        controls.Add(_btnSave);
        controls.Add(_btnCancel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        ClientSize = new Size(460, y + 60);
        Controls.AddRange(controls.ToArray());
    }

    private void UpdateSaleInputsEnabled()
    {
        var onSale = _chkIsOnSale.Checked;
        _nudSalePrice.Enabled = onSale;
        _dtpSaleStart.Enabled = onSale;
        _dtpSaleEnd.Enabled = onSale;
    }

    private bool ValidatePricing()
    {
        _errorProvider.SetError(_nudSalePrice, "");
        _errorProvider.SetError(_dtpSaleEnd, "");

        if (!_chkIsOnSale.Checked) return true;

        if (_nudSalePrice.Value <= 0 || _nudSalePrice.Value >= _nudPrice.Value)
        {
            _errorProvider.SetError(_nudSalePrice, "Giá sale phải lớn hơn 0 và nhỏ hơn giá gốc.");
            MessageBox.Show("Giá sale phải lớn hơn 0 và nhỏ hơn giá gốc.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _nudSalePrice.Focus();
            return false;
        }

        if (_dtpSaleStart.Value >= _dtpSaleEnd.Value)
        {
            _errorProvider.SetError(_dtpSaleEnd, "Thời gian kết thúc phải sau thời gian bắt đầu.");
            MessageBox.Show("Thời gian bắt đầu sale phải trước thời gian kết thúc.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _dtpSaleEnd.Focus();
            return false;
        }

        return true;
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidatePricing()) return;

        var price = _nudPrice.Value;
        decimal? salePrice = null;
        DateTime? saleStart = null;
        DateTime? saleEnd = null;

        if (_chkIsOnSale.Checked)
        {
            salePrice = _nudSalePrice.Value;
            // DateTimePicker trả về Kind = Unspecified (giờ Local) → đổi sang UTC trước khi lưu.
            saleStart = _dtpSaleStart.Value.ToUniversalTime();
            saleEnd = _dtpSaleEnd.Value.ToUniversalTime();
        }

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                var result = await _gameService.UpdatePricingAsync(
                    _existingGame.Id, price, salePrice, saleStart, saleEnd);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Cập nhật giá thất bại.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "Lưu";
            }
        });
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Cho phép đóng form dù validation đang fail
        AutoValidate = AutoValidate.Disable;
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _errorProvider?.Dispose();
        }
        base.Dispose(disposing);
    }
}

using System.ComponentModel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Dialog thêm mới / chỉnh sửa game dành cho Publisher.
/// existingGame == null → chế độ Thêm mới; existingGame != null → chế độ Sửa.
/// D9.1 chỉ hỗ trợ Title / Price / Description (Genre/Tag/Language làm ở D10).
/// </summary>
public class GameEditForm : Form
{
    private readonly IGameService _gameService;
    private readonly GameDto? _existingGame;
    private readonly bool _isEdit;

    private TextBox _txtTitle = null!;
    private TextBox _txtDescription = null!;
    private NumericUpDown _nudPrice = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public GameEditForm(IGameService gameService, GameDto? existingGame = null)
    {
        _gameService = gameService;
        _existingGame = existingGame;
        _isEdit = existingGame is not null;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = _isEdit ? "Pipe Hub — Sửa game" : "Pipe Hub — Thêm game";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblTitle = new Label
        {
            Text = _isEdit ? "SỬA GAME" : "THÊM GAME",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(440, 32)
        };

        var controls = new List<Control> { lblTitle };

        int y = 65;
        const int labelX = 30, inputX = 160, inputW = 250, rowH = 34;

        // Tên game
        controls.Add(new Label { Text = "Tên game:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtTitle = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            MaxLength = 200,
            Text = _existingGame?.Title ?? string.Empty
        };
        _txtTitle.Validating += TxtTitle_Validating;
        controls.Add(_txtTitle);
        y += rowH;

        // Mô tả (multiline)
        controls.Add(new Label { Text = "Mô tả:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtDescription = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 90),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = _existingGame?.Description ?? string.Empty
        };
        controls.Add(_txtDescription);
        y += 90 + 8;

        // Giá
        controls.Add(new Label { Text = "Giá (đ):", Location = new Point(labelX, y), Size = new Size(125, 25) });
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
        _nudPrice.Value = Math.Clamp(_existingGame?.Price ?? 0m, _nudPrice.Minimum, _nudPrice.Maximum);
        _nudPrice.Validating += NudPrice_Validating;
        controls.Add(_nudPrice);
        y += rowH + 8;

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

        ClientSize = new Size(440, y + 60);
        Controls.AddRange(controls.ToArray());
    }

    private void TxtTitle_Validating(object? sender, CancelEventArgs e)
    {
        var title = _txtTitle.Text.Trim();
        e.Cancel = !_errorProvider.Check(_txtTitle,
            !string.IsNullOrWhiteSpace(title),
            "Vui lòng nhập tên game.");
    }

    private void NudPrice_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_nudPrice,
            _nudPrice.Value >= 0,
            "Giá không được âm.");
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        var title = _txtTitle.Text.Trim();
        var price = _nudPrice.Value;
        var description = _txtDescription.Text.Trim();

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                var result = _isEdit
                    ? await _gameService.UpdateAsync(_existingGame!.Id, title, price, description)
                    : await _gameService.CreateAsync(title, price, description);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Lưu game thất bại.", "Thông báo",
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

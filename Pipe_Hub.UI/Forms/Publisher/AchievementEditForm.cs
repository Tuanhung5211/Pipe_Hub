using System.ComponentModel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Dialog thêm mới / chỉnh sửa Achievement của 1 game.
/// existing == null → chế độ Thêm mới; existing != null → chế độ Sửa.
/// </summary>
public class AchievementEditForm : Form
{
    private readonly IAchievementService _service;
    private readonly int _gameId;
    private readonly AchievementDto? _existing;
    private readonly bool _isEdit;

    private TextBox _txtName = null!;
    private TextBox _txtDescription = null!;
    private TextBox _txtIconPath = null!;
    private Button _btnBrowse = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public AchievementEditForm(IAchievementService service, int gameId, AchievementDto? existing = null)
    {
        _service = service;
        _gameId = gameId;
        _existing = existing;
        _isEdit = existing is not null;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = _isEdit ? "Pipe Hub — Sửa thành tích" : "Pipe Hub — Thêm thành tích";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblTitle = new Label
        {
            Text = _isEdit ? "SỬA THÀNH TÍCH" : "THÊM THÀNH TÍCH",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(500, 32)
        };

        var controls = new List<Control> { lblTitle };

        const int labelX = 25, inputX = 130, inputW = 340;

        // Tên thành tích
        controls.Add(new Label { Text = "Tên:", Location = new Point(labelX, 65), Size = new Size(100, 25) });
        _txtName = new TextBox
        {
            Location = new Point(inputX, 62),
            Size = new Size(inputW, 25),
            MaxLength = 100,
            Text = _existing?.Name ?? string.Empty
        };
        _txtName.Validating += TxtName_Validating;
        controls.Add(_txtName);

        // Mô tả
        controls.Add(new Label { Text = "Mô tả:", Location = new Point(labelX, 100), Size = new Size(100, 25) });
        _txtDescription = new TextBox
        {
            Location = new Point(inputX, 97),
            Size = new Size(inputW, 80),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            MaxLength = 500,
            Text = _existing?.Description ?? string.Empty
        };
        controls.Add(_txtDescription);

        // Icon path
        controls.Add(new Label { Text = "Icon:", Location = new Point(labelX, 190), Size = new Size(100, 25) });
        _txtIconPath = new TextBox
        {
            Location = new Point(inputX, 187),
            Size = new Size(230, 25),
            MaxLength = 500,
            Text = _existing?.IconPath ?? string.Empty
        };
        controls.Add(_txtIconPath);

        _btnBrowse = new Button
        {
            Text = "Chọn ảnh...",
            Location = new Point(inputX + 240, 186),
            Size = new Size(100, 27)
        };
        _btnBrowse.Click += BtnBrowse_Click;
        controls.Add(_btnBrowse);

        // Buttons
        _btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(inputX, 235),
            Size = new Size(120, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(inputX + 130, 235),
            Size = new Size(120, 32)
        };
        _btnCancel.Click += (s, e) => Close();

        controls.Add(_btnSave);
        controls.Add(_btnCancel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        ClientSize = new Size(500, 300);
        Controls.AddRange(controls.ToArray());
    }

    private void TxtName_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtName,
            !string.IsNullOrWhiteSpace(_txtName.Text),
            "Vui lòng nhập tên thành tích.");
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Image Files|*.png;*.jpg;*.jpeg",
            Title = "Chọn ảnh icon cho thành tích"
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
        {
            _txtIconPath.Text = ofd.FileName;
        }
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        var name = _txtName.Text.Trim();
        var description = _txtDescription.Text.Trim();
        var iconPath = _txtIconPath.Text.Trim();

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                var result = _isEdit
                    ? await _service.UpdateAsync(_existing!.Id, name, description, iconPath)
                    : await _service.CreateAsync(_gameId, name, description, iconPath);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Lưu thành tích thất bại.", "Thông báo",
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

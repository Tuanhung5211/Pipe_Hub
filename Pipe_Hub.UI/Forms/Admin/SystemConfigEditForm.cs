using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Dialog sửa giá trị 1 SystemConfig (Admin).
/// Key chỉ đọc — không cho đổi tên key; chỉ sửa Value + Description.
/// </summary>
public class SystemConfigEditForm : Form
{
    private readonly ISystemConfigService _configService;
    private readonly SystemConfigDto _existingConfig;

    private TextBox _txtKey = null!;
    private TextBox _txtValue = null!;
    private TextBox _txtDescription = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public SystemConfigEditForm(ISystemConfigService configService, SystemConfigDto existingConfig)
    {
        _configService = configService;
        _existingConfig = existingConfig;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = "Pipe Hub — Cấu hình hệ thống";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(480, 320);
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblHeader = new Label
        {
            Text = "CẤU HÌNH HỆ THỐNG",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 12),
            Size = new Size(480, 32)
        };

        var controls = new List<Control> { lblHeader };

        const int labelX = 25, inputX = 130, inputW = 320;

        // Key (chỉ đọc)
        controls.Add(new Label { Text = "Khóa:", Location = new Point(labelX, 65), Size = new Size(100, 25) });
        _txtKey = new TextBox
        {
            Location = new Point(inputX, 62),
            Size = new Size(inputW, 25),
            ReadOnly = true,
            BackColor = Color.FromArgb(240, 240, 240),
            Text = _existingConfig.Key
        };
        controls.Add(_txtKey);

        // Value
        controls.Add(new Label { Text = "Giá trị:", Location = new Point(labelX, 100), Size = new Size(100, 25) });
        _txtValue = new TextBox
        {
            Location = new Point(inputX, 97),
            Size = new Size(inputW, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = _existingConfig.Value
        };
        controls.Add(_txtValue);

        // Description
        controls.Add(new Label { Text = "Mô tả:", Location = new Point(labelX, 170), Size = new Size(100, 25) });
        _txtDescription = new TextBox
        {
            Location = new Point(inputX, 167),
            Size = new Size(inputW, 60),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = _existingConfig.Description ?? string.Empty
        };
        controls.Add(_txtDescription);

        // Buttons
        _btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(inputX, 248),
            Size = new Size(120, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(inputX + 130, 248),
            Size = new Size(120, 32)
        };
        _btnCancel.Click += (s, e) => Close();

        controls.Add(_btnSave);
        controls.Add(_btnCancel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        Controls.AddRange(controls.ToArray());
    }

    private bool ValidateConfig()
    {
        _errorProvider.SetError(_txtValue, "");

        if (string.IsNullOrWhiteSpace(_txtValue.Text))
        {
            _errorProvider.SetError(_txtValue, "Giá trị không được để trống.");
            _txtValue.Focus();
            return false;
        }

        return true;
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateConfig()) return;

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                await _configService.SetAsync(
                    _existingConfig.Key,
                    _txtValue.Text.Trim(),
                    _txtDescription.Text.Trim());

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

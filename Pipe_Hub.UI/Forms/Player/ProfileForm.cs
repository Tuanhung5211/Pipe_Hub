using System.ComponentModel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;
using Pipe_Hub.UI.Models;

namespace Pipe_Hub.UI.Forms.Player;

public class ProfileForm : Form
{
    private readonly IUserService _userService;

    private TextBox _txtUsername = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtFullName = null!;
    private TextBox _txtCountry = null!;
    private Label _lblRole = null!;
    private Button _btnSave = null!;
    private Button _btnReload = null!;
    private Button _btnClose = null!;
    private ErrorProvider _errorProvider = null!;

    private BindingSource _bindingSource = null!;
    private ProfileBindingModel _model = null!;
    private UserDto? _currentUser;

    public ProfileForm(IUserService userService)
    {
        _userService = userService;
        InitializeComponents();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Hồ sơ cá nhân";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 340);
        MinimumSize = new Size(450, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        // Binding model + source
        _model = new ProfileBindingModel();
        _bindingSource = new BindingSource { DataSource = _model };

        var lblTitle = new Label
        {
            Text = "HỒ SƠ CÁ NHÂN",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(ClientSize.Width, 30)
        };

        int y = 65;
        int labelX = 40, inputX = 150, inputW = 300, rowH = 35;

        // Username (readonly, không bind)
        var lblUsername = new Label
        {
            Text = "Tên đăng nhập:",
            Location = new Point(labelX, y),
            Size = new Size(110, 25)
        };
        _txtUsername = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            ReadOnly = true,
            BackColor = Color.FromArgb(240, 240, 240)
        };
        y += rowH;

        // Email (bind)
        var lblEmail = new Label
        {
            Text = "Email:",
            Location = new Point(labelX, y),
            Size = new Size(110, 25)
        };
        _txtEmail = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25)
        };
        _txtEmail.DataBindings.Add(
            nameof(TextBox.Text),
            _bindingSource,
            nameof(ProfileBindingModel.Email),
            formattingEnabled: true,
            updateMode: DataSourceUpdateMode.OnPropertyChanged);
        _txtEmail.Validating += TxtEmail_Validating;
        y += rowH;

        // FullName (bind)
        var lblFullName = new Label
        {
            Text = "Họ tên:",
            Location = new Point(labelX, y),
            Size = new Size(110, 25)
        };
        _txtFullName = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25)
        };
        _txtFullName.DataBindings.Add(
            nameof(TextBox.Text),
            _bindingSource,
            nameof(ProfileBindingModel.FullName),
            formattingEnabled: true,
            updateMode: DataSourceUpdateMode.OnPropertyChanged);
        _txtFullName.Validating += TxtFullName_Validating;
        y += rowH;

        // Country (bind)
        var lblCountry = new Label
        {
            Text = "Quốc gia:",
            Location = new Point(labelX, y),
            Size = new Size(110, 25)
        };
        _txtCountry = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25)
        };
        _txtCountry.DataBindings.Add(
            nameof(TextBox.Text),
            _bindingSource,
            nameof(ProfileBindingModel.Country),
            formattingEnabled: true,
            updateMode: DataSourceUpdateMode.OnPropertyChanged);
        y += rowH;

        // Role (readonly)
        var lblRoleTitle = new Label
        {
            Text = "Vai trò:",
            Location = new Point(labelX, y),
            Size = new Size(110, 25)
        };
        _lblRole = new Label
        {
            Location = new Point(inputX, y),
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215)
        };
        y += rowH + 15;

        // Buttons
        _btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(inputX, y),
            Size = new Size(90, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += BtnSave_Click;

        _btnReload = new Button
        {
            Text = "Tải lại",
            Location = new Point(inputX + 100, y),
            Size = new Size(90, 32)
        };
        _btnReload.Click += async (s, e) => await LoadDataAsync();

        _btnClose = new Button
        {
            Text = "Đóng",
            Location = new Point(inputX + 200, y),
            Size = new Size(90, 32)
        };
        _btnClose.Click += (s, e) => Close();

        AcceptButton = _btnSave;
        CancelButton = _btnClose;

        Controls.AddRange(new Control[]
        {
            lblTitle,
            lblUsername, _txtUsername,
            lblEmail, _txtEmail,
            lblFullName, _txtFullName,
            lblCountry, _txtCountry,
            lblRoleTitle, _lblRole,
            _btnSave, _btnReload, _btnClose
        });
    }

    private async Task LoadDataAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var profile = await _userService.GetMyProfileAsync();
            _currentUser = profile;

            _txtUsername.Text = profile.Username;
            _lblRole.Text = profile.RoleName;

            // Load vào binding model → tự động đẩy lên UI qua binding 2 chiều
            _model.LoadFrom(profile);
            _bindingSource.ResetBindings(false);
        });
    }

    private void TxtEmail_Validating(object? sender, CancelEventArgs e)
    {
        var email = _model.Email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            e.Cancel = true;
            _errorProvider.SetError(_txtEmail, "Vui lòng nhập email.");
            return;
        }

        if (!ErrorProviderHelper.IsValidEmail(email))
        {
            e.Cancel = true;
            _errorProvider.SetError(_txtEmail, "Email không hợp lệ.");
            return;
        }

        _errorProvider.SetError(_txtEmail, "");
    }

    private void TxtFullName_Validating(object? sender, CancelEventArgs e)
    {
        var fullName = _model.FullName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            e.Cancel = true;
            _errorProvider.SetError(_txtFullName, "Vui lòng nhập họ tên.");
            return;
        }

        _errorProvider.SetError(_txtFullName, "");
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                var dto = new UpdateProfileDto
                {
                    Email = _model.Email.Trim(),
                    FullName = _model.FullName.Trim(),
                    Country = string.IsNullOrWhiteSpace(_model.Country)
                        ? null
                        : _model.Country.Trim()
                };

                var result = await _userService.UpdateProfileAsync(dto);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(
                        result.ErrorMessage ?? "Cập nhật thất bại.",
                        "Hồ sơ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(
                    "Cập nhật hồ sơ thành công!",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Reload để refresh model + UI
                await LoadDataAsync();
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "Lưu";
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bindingSource?.Dispose();
            _errorProvider?.Dispose();
        }
        base.Dispose(disposing);
    }
}

using System.ComponentModel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Auth;

public class RegisterForm : Form
{
    private readonly IAuthService _authService;

    private TextBox _txtUsername = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtPassword = null!;
    private TextBox _txtConfirmPassword = null!;
    private TextBox _txtFullName = null!;
    private TextBox _txtCountry = null!;
    private Button _btnRegister = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public RegisterForm(IAuthService authService)
    {
        _authService = authService;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = "Pipe Hub — Đăng ký tài khoản";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 380);
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblTitle = new Label
        {
            Text = "ĐĂNG KÝ",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(ClientSize.Width, 35)
        };

        int y = 70;
        int labelX = 40, inputX = 160, inputW = 220, rowH = 32;

        var lblUsername = new Label { Text = "Tên đăng nhập:", Location = new Point(labelX, y), Size = new Size(120, 25) };
        _txtUsername = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25) };
        _txtUsername.Validating += TxtUsername_Validating;
        y += rowH;

        var lblEmail = new Label { Text = "Email:", Location = new Point(labelX, y), Size = new Size(120, 25) };
        _txtEmail = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25) };
        _txtEmail.Validating += TxtEmail_Validating;
        y += rowH;

        var lblPassword = new Label { Text = "Mật khẩu:", Location = new Point(labelX, y), Size = new Size(120, 25) };
        _txtPassword = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25), UseSystemPasswordChar = true };
        _txtPassword.Validating += TxtPassword_Validating;
        _txtPassword.TextChanged += (s, e) =>
        {
            if (_txtConfirmPassword.Text.Length > 0)
                TxtConfirmPassword_Validating(_txtConfirmPassword, new CancelEventArgs());
        };
        y += rowH;

        var lblConfirm = new Label { Text = "Xác nhận mật khẩu:", Location = new Point(labelX, y), Size = new Size(120, 25) };
        _txtConfirmPassword = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25), UseSystemPasswordChar = true };
        _txtConfirmPassword.Validating += TxtConfirmPassword_Validating;
        y += rowH;

        var lblFullName = new Label { Text = "Họ tên:", Location = new Point(labelX, y), Size = new Size(120, 25) };
        _txtFullName = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25) };
        _txtFullName.Validating += TxtFullName_Validating;
        y += rowH;

        var lblCountry = new Label { Text = "Quốc gia:", Location = new Point(labelX, y), Size = new Size(120, 25) };
        _txtCountry = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25) };
        y += rowH + 10;

        _btnRegister = new Button
        {
            Text = "Đăng ký",
            Location = new Point(inputX, y),
            Size = new Size(105, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnRegister.Click += BtnRegister_Click;

        _btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(inputX + 115, y),
            Size = new Size(105, 32)
        };
        _btnCancel.Click += (s, e) => Close();

        AcceptButton = _btnRegister;
        CancelButton = _btnCancel;

        Controls.AddRange(new Control[]
        {
            lblTitle, lblUsername, _txtUsername, lblEmail, _txtEmail,
            lblPassword, _txtPassword, lblConfirm, _txtConfirmPassword,
            lblFullName, _txtFullName, lblCountry, _txtCountry,
            _btnRegister, _btnCancel
        });
    }

    private void TxtUsername_Validating(object? sender, CancelEventArgs e)
    {
        var username = _txtUsername.Text.Trim();
        e.Cancel = !_errorProvider.Check(_txtUsername,
            !string.IsNullOrWhiteSpace(username) && username.Length >= 3 && username.Length <= 50,
            "Tên đăng nhập phải từ 3 đến 50 ký tự.");
    }

    private void TxtEmail_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtEmail,
            ErrorProviderHelper.IsValidEmail(_txtEmail.Text),
            "Email không hợp lệ.");
    }

    private void TxtPassword_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtPassword,
            !string.IsNullOrWhiteSpace(_txtPassword.Text) && _txtPassword.Text.Length >= 6,
            "Mật khẩu phải từ 6 ký tự trở lên.");
    }

    private void TxtConfirmPassword_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtConfirmPassword,
            _txtConfirmPassword.Text == _txtPassword.Text,
            "Mật khẩu xác nhận không khớp.");
    }

    private void TxtFullName_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtFullName,
            !string.IsNullOrWhiteSpace(_txtFullName.Text.Trim()),
            "Vui lòng nhập họ tên.");
    }

    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        var username = _txtUsername.Text.Trim();
        var email = _txtEmail.Text.Trim();
        var password = _txtPassword.Text;
        var confirmPassword = _txtConfirmPassword.Text;
        var fullName = _txtFullName.Text.Trim();
        var country = _txtCountry.Text.Trim();

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnRegister.Enabled = false;
                _btnRegister.Text = "Đang xử lý...";

                var request = new RegisterRequestDto
                {
                    Username = username,
                    Email = email,
                    Password = password,
                    ConfirmPassword = confirmPassword,
                    FullName = fullName,
                    Country = string.IsNullOrWhiteSpace(country) ? null : country
                };

                var result = await _authService.RegisterAsync(request);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(
                        result.ErrorMessage ?? "Đăng ký thất bại.",
                        "Đăng ký",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(
                    "Đăng ký thành công! Bạn có thể đăng nhập ngay.",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                _btnRegister.Enabled = true;
                _btnRegister.Text = "Đăng ký";
            }
        });
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Cho phép đóng form dù validation đang fail (tránh bug không đóng được)
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

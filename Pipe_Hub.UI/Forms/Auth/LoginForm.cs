using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Main;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Auth;

public class LoginForm : Form
{
    private readonly IAuthService _authService;
    private readonly CurrentUserContext _currentUser;
    private readonly IServiceProvider _serviceProvider;

    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private Button _btnLogin = null!;
    private Button _btnRegister = null!;
    private ErrorProvider _errorProvider = null!;
    private Label _lblTitle = null!;

    public LoginForm(
        IAuthService authService,
        CurrentUserContext currentUser,
        IServiceProvider serviceProvider)
    {
        _authService = authService;
        _currentUser = currentUser;
        _serviceProvider = serviceProvider;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = "Pipe Hub — Đăng nhập";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 280);
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        _lblTitle = new Label
        {
            Text = "PIPE HUB",
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 20),
            Size = new Size(ClientSize.Width, 40)
        };

        var lblUsername = new Label
        {
            Text = "Tên đăng nhập:",
            Location = new Point(50, 90),
            Size = new Size(120, 25)
        };

        _txtUsername = new TextBox
        {
            Location = new Point(170, 90),
            Size = new Size(180, 25)
        };
        _txtUsername.Validating += TxtUsername_Validating;

        var lblPassword = new Label
        {
            Text = "Mật khẩu:",
            Location = new Point(50, 130),
            Size = new Size(120, 25)
        };

        _txtPassword = new TextBox
        {
            Location = new Point(170, 130),
            Size = new Size(180, 25),
            UseSystemPasswordChar = true
        };
        _txtPassword.Validating += TxtPassword_Validating;

        _btnLogin = new Button
        {
            Text = "Đăng nhập",
            Location = new Point(170, 180),
            Size = new Size(90, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnLogin.Click += BtnLogin_Click;

        _btnRegister = new Button
        {
            Text = "Đăng ký",
            Location = new Point(260, 180),
            Size = new Size(90, 32)
        };
        _btnRegister.Click += BtnRegister_Click;

        AcceptButton = _btnLogin;

        Controls.AddRange(new Control[]
        {
            _lblTitle, lblUsername, _txtUsername, lblPassword, _txtPassword,
            _btnLogin, _btnRegister
        });
    }

    private void TxtUsername_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtUsername,
            !string.IsNullOrWhiteSpace(_txtUsername.Text.Trim()),
            "Vui lòng nhập tên đăng nhập.");
    }

    private void TxtPassword_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtPassword,
            !string.IsNullOrWhiteSpace(_txtPassword.Text),
            "Vui lòng nhập mật khẩu.");
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnLogin.Enabled = false;
                _btnLogin.Text = "Đang đăng nhập...";

                var request = new LoginRequestDto
                {
                    Username = username,
                    Password = password
                };

                var result = await _authService.LoginAsync(request);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(
                        result.ErrorMessage ?? "Đăng nhập thất bại.",
                        "Đăng nhập",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    _txtPassword.Clear();
                    _txtPassword.Focus();
                    return;
                }

                var user = result.Data!;
                _currentUser.SetUser(user.Id, user.Username, user.FullName, user.RoleName);

                var mainForm = _serviceProvider.GetRequiredService<MainForm>();
                Hide();
                mainForm.ShowDialog();
                var logout = mainForm.LogoutRequested;
                mainForm.Dispose();
                _currentUser.Clear();
                if (logout) { _txtPassword.Clear(); _txtUsername.Clear(); Show(); _txtUsername.Focus(); }
                else { Close(); }
            }
            finally
            {
                _btnLogin.Enabled = true;
                _btnLogin.Text = "Đăng nhập";
            }
        });
    }

    private void BtnRegister_Click(object? sender, EventArgs e)
    {
        var registerForm = _serviceProvider.GetRequiredService<RegisterForm>();
        registerForm.ShowDialog(this);
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

using System.ComponentModel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Auth;

public class ChangePasswordForm : Form
{
    private readonly IAuthService _authService;
    private readonly CurrentUserContext _currentUser;

    private TextBox _txtOldPassword = null!;
    private TextBox _txtNewPassword = null!;
    private TextBox _txtConfirmNewPassword = null!;
    private Button _btnChange = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public ChangePasswordForm(IAuthService authService, CurrentUserContext currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = "Pipe Hub — Đổi mật khẩu";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(400, 260);
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblTitle = new Label
        {
            Text = "ĐỔI MẬT KHẨU",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(ClientSize.Width, 30)
        };

        int y = 70;
        int labelX = 40, inputX = 180, inputW = 180;

        var lblOld = new Label { Text = "Mật khẩu cũ:", Location = new Point(labelX, y), Size = new Size(130, 25) };
        _txtOldPassword = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25), UseSystemPasswordChar = true };
        _txtOldPassword.Validating += TxtOldPassword_Validating;
        y += 32;

        var lblNew = new Label { Text = "Mật khẩu mới:", Location = new Point(labelX, y), Size = new Size(130, 25) };
        _txtNewPassword = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25), UseSystemPasswordChar = true };
        _txtNewPassword.Validating += TxtNewPassword_Validating;
        _txtNewPassword.TextChanged += (s, e) =>
        {
            if (_txtConfirmNewPassword.Text.Length > 0)
                TxtConfirmNewPassword_Validating(_txtConfirmNewPassword, new CancelEventArgs());
        };
        y += 32;

        var lblConfirm = new Label { Text = "Xác nhận mật khẩu mới:", Location = new Point(labelX, y), Size = new Size(140, 25) };
        _txtConfirmNewPassword = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25), UseSystemPasswordChar = true };
        _txtConfirmNewPassword.Validating += TxtConfirmNewPassword_Validating;
        y += 42;

        _btnChange = new Button
        {
            Text = "Đổi mật khẩu",
            Location = new Point(inputX, y),
            Size = new Size(110, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnChange.Click += BtnChange_Click;

        _btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(inputX + 120, y),
            Size = new Size(60, 32)
        };
        _btnCancel.Click += (s, e) => Close();

        AcceptButton = _btnChange;
        CancelButton = _btnCancel;

        Controls.AddRange(new Control[]
        {
            lblTitle,
            lblOld, _txtOldPassword,
            lblNew, _txtNewPassword,
            lblConfirm, _txtConfirmNewPassword,
            _btnChange, _btnCancel
        });
    }

    private void TxtOldPassword_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtOldPassword,
            !string.IsNullOrWhiteSpace(_txtOldPassword.Text),
            "Vui lòng nhập mật khẩu cũ.");
    }

    private void TxtNewPassword_Validating(object? sender, CancelEventArgs e)
    {
        var newPassword = _txtNewPassword.Text;
        e.Cancel = !_errorProvider.Check(_txtNewPassword,
            !string.IsNullOrWhiteSpace(newPassword) && newPassword.Length >= 6 && newPassword != _txtOldPassword.Text,
            newPassword == _txtOldPassword.Text
                ? "Mật khẩu mới phải khác mật khẩu cũ."
                : "Mật khẩu mới phải từ 6 ký tự trở lên.");
    }

    private void TxtConfirmNewPassword_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtConfirmNewPassword,
            _txtConfirmNewPassword.Text == _txtNewPassword.Text,
            "Mật khẩu xác nhận không khớp.");
    }

    private async void BtnChange_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        var oldPassword = _txtOldPassword.Text;
        var newPassword = _txtNewPassword.Text;
        var confirmNew = _txtConfirmNewPassword.Text;

        if (!_currentUser.UserId.HasValue)
        {
            MessageBox.Show(
                "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnChange.Enabled = false;
                _btnChange.Text = "Đang xử lý...";

                var request = new ChangePasswordRequestDto
                {
                    OldPassword = oldPassword,
                    NewPassword = newPassword,
                    ConfirmNewPassword = confirmNew
                };

                var result = await _authService.ChangePasswordAsync(request);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(
                        result.ErrorMessage ?? "Đổi mật khẩu thất bại.",
                        "Đổi mật khẩu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(
                    "Đổi mật khẩu thành công!",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                _btnChange.Enabled = true;
                _btnChange.Text = "Đổi mật khẩu";
            }
        });
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

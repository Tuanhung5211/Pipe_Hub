using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;

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
        y += 32;

        var lblNew = new Label { Text = "Mật khẩu mới:", Location = new Point(labelX, y), Size = new Size(130, 25) };
        _txtNewPassword = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25), UseSystemPasswordChar = true };
        y += 32;

        var lblConfirm = new Label { Text = "Xác nhận mật khẩu mới:", Location = new Point(labelX, y), Size = new Size(140, 25) };
        _txtConfirmNewPassword = new TextBox { Location = new Point(inputX, y), Size = new Size(inputW, 25), UseSystemPasswordChar = true };
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

    private async void BtnChange_Click(object? sender, EventArgs e)
    {
        _errorProvider.Clear();

        var oldPassword = _txtOldPassword.Text;
        var newPassword = _txtNewPassword.Text;
        var confirmNew = _txtConfirmNewPassword.Text;

        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            _errorProvider.SetError(_txtOldPassword, "Vui lòng nhập mật khẩu cũ.");
            return;
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            _errorProvider.SetError(_txtNewPassword, "Vui lòng nhập mật khẩu mới.");
            return;
        }

        if (newPassword.Length < 6)
        {
            _errorProvider.SetError(_txtNewPassword, "Mật khẩu mới phải từ 6 ký tự trở lên.");
            return;
        }

        if (newPassword != confirmNew)
        {
            _errorProvider.SetError(_txtConfirmNewPassword, "Mật khẩu xác nhận không khớp.");
            return;
        }

        if (newPassword == oldPassword)
        {
            _errorProvider.SetError(_txtNewPassword, "Mật khẩu mới phải khác mật khẩu cũ.");
            return;
        }

        if (!_currentUser.UserId.HasValue)
        {
            MessageBox.Show(
                "Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

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
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Đã xảy ra lỗi khi đổi mật khẩu: {ex.Message}",
                "Lỗi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _btnChange.Enabled = true;
            _btnChange.Text = "Đổi mật khẩu";
        }
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

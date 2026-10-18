using System.ComponentModel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Dialog thêm mới / chỉnh sửa user dành cho Admin.
/// existingUser == null → chế độ Thêm mới (nhập Username + Password);
/// existingUser != null → chế độ Sửa (Username readonly, ẩn Password).
/// </summary>
public class UserEditForm : Form
{
    private readonly IUserManagementService _userService;
    private readonly UserDto? _existingUser;
    private readonly bool _isEdit;

    private TextBox _txtUsername = null!;
    private TextBox _txtEmail = null!;
    private TextBox _txtFullName = null!;
    private TextBox _txtPassword = null!;
    private ComboBox _cboRole = null!;
    private CheckBox _chkIsActive = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public UserEditForm(IUserManagementService userService, UserDto? existingUser = null)
    {
        _userService = userService;
        _existingUser = existingUser;
        _isEdit = existingUser is not null;

        InitializeComponents();

        Load += async (s, e) => await LoadRolesAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = _isEdit ? "Pipe Hub — Sửa người dùng" : "Pipe Hub — Thêm người dùng";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblTitle = new Label
        {
            Text = _isEdit ? "SỬA NGƯỜI DÙNG" : "THÊM NGƯỜI DÙNG",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(440, 32)
        };

        var controls = new List<Control> { lblTitle };

        int y = 65;
        const int labelX = 30, inputX = 160, inputW = 250, rowH = 34;

        // Username
        controls.Add(new Label { Text = "Tên đăng nhập:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtUsername = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            ReadOnly = _isEdit,
            BackColor = _isEdit ? SystemColors.Control : Color.White
        };
        if (_isEdit && _existingUser is not null)
        {
            _txtUsername.Text = _existingUser.Username;
        }
        _txtUsername.Validating += TxtUsername_Validating;
        controls.Add(_txtUsername);
        y += rowH;

        // Email
        controls.Add(new Label { Text = "Email:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtEmail = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25)
        };
        if (_isEdit && _existingUser is not null)
        {
            _txtEmail.Text = _existingUser.Email;
        }
        _txtEmail.Validating += TxtEmail_Validating;
        controls.Add(_txtEmail);
        y += rowH;

        // FullName
        controls.Add(new Label { Text = "Họ tên:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtFullName = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25)
        };
        if (_isEdit && _existingUser is not null)
        {
            _txtFullName.Text = _existingUser.FullName;
        }
        _txtFullName.Validating += TxtFullName_Validating;
        controls.Add(_txtFullName);
        y += rowH;

        // Password — chỉ hiện khi Thêm mới
        if (!_isEdit)
        {
            controls.Add(new Label { Text = "Mật khẩu:", Location = new Point(labelX, y), Size = new Size(125, 25) });
            _txtPassword = new TextBox
            {
                Location = new Point(inputX, y),
                Size = new Size(inputW, 25),
                UseSystemPasswordChar = true
            };
            _txtPassword.Validating += TxtPassword_Validating;
            controls.Add(_txtPassword);
            y += rowH;
        }

        // Role
        controls.Add(new Label { Text = "Vai trò:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _cboRole = new ComboBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        controls.Add(_cboRole);
        y += rowH;

        // IsActive
        _chkIsActive = new CheckBox
        {
            Text = "Hoạt động",
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            Checked = _existingUser?.IsActive ?? true
        };
        controls.Add(_chkIsActive);
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

    private async Task LoadRolesAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var roles = await _userService.GetAllRolesAsync();
            _cboRole.DataSource = roles.ToList();
            _cboRole.DisplayMember = "Name";
            _cboRole.ValueMember = "Id";

            if (_isEdit && _existingUser is not null)
            {
                _cboRole.SelectedValue = _existingUser.RoleId;
            }
        });
    }

    private void TxtUsername_Validating(object? sender, CancelEventArgs e)
    {
        if (_isEdit) return;

        var username = _txtUsername.Text.Trim();
        e.Cancel = !_errorProvider.Check(_txtUsername,
            username.Length >= 3 && username.Length <= 50,
            "Tên đăng nhập phải từ 3 đến 50 ký tự.");
    }

    private void TxtEmail_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtEmail,
            ErrorProviderHelper.IsValidEmail(_txtEmail.Text),
            "Email không hợp lệ.");
    }

    private void TxtFullName_Validating(object? sender, CancelEventArgs e)
    {
        e.Cancel = !_errorProvider.Check(_txtFullName,
            !string.IsNullOrWhiteSpace(_txtFullName.Text.Trim()),
            "Vui lòng nhập họ tên.");
    }

    private void TxtPassword_Validating(object? sender, CancelEventArgs e)
    {
        if (_isEdit) return;

        e.Cancel = !_errorProvider.Check(_txtPassword,
            !string.IsNullOrWhiteSpace(_txtPassword.Text) && _txtPassword.Text.Length >= 6,
            "Mật khẩu phải từ 6 ký tự trở lên.");
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        if (_cboRole.SelectedItem is not RoleDto role)
        {
            MessageBox.Show("Vui lòng chọn vai trò.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var username = _txtUsername.Text.Trim();
        var email = _txtEmail.Text.Trim();
        var fullName = _txtFullName.Text.Trim();
        var isActive = _chkIsActive.Checked;

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                var result = _isEdit
                    ? await _userService.UpdateUserAsync(_existingUser!.Id, email, fullName, role.Id, isActive)
                    : await _userService.CreateUserAsync(username, email, _txtPassword.Text, fullName, role.Id, isActive);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Lưu thất bại.", "Thông báo",
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

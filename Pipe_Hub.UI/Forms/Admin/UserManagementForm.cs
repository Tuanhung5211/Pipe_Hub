using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Form quản lý người dùng (Admin) — kế thừa BaseCrudForm.
/// Nút "Xóa" được đổi thành "Khoá/Mở khoá" (không xóa user thật).
/// </summary>
public class UserManagementForm : BaseCrudForm
{
    private readonly IUserManagementService _userService;

    private List<UserDto> _allUsers = new();

    public UserManagementForm(IUserManagementService userService)
    {
        _userService = userService;

        Text = "Quản lý người dùng";
        _btnDelete.Text = "Khoá/Mở khoá";

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override async void OnAddClicked()
    {
        using var dialog = new UserEditForm(_userService);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override async void OnEditClicked()
    {
        var selected = GetSelectedUser();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 người dùng.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new UserEditForm(_userService, selected);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override async void OnDeleteClicked()
    {
        var selected = GetSelectedUser();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 người dùng.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var action = selected.IsActive ? "khoá" : "mở khoá";

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn {action} tài khoản '{selected.Username}'?",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _userService.SetActiveStatusAsync(selected.Id, !selected.IsActive);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? $"{action} tài khoản thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await OnRefreshClickedAsync();
        });
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var users = await _userService.GetAllUsersAsync();
            _allUsers = users.ToList();
            BindToGrid(_allUsers);
            SetCountLabel(_allUsers.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allUsers);
            SetCountLabel(_allUsers.Count);
            return;
        }

        var filtered = _allUsers.Where(u =>
            u.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            u.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<UserDto> users)
    {
        _grid.DataSource = null;
        _grid.Columns.Clear();
        _grid.AutoGenerateColumns = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colId",
            DataPropertyName = "Id",
            Visible = false
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tên đăng nhập",
            DataPropertyName = "Username",
            FillWeight = 18
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Email",
            DataPropertyName = "Email",
            FillWeight = 22
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Họ tên",
            DataPropertyName = "FullName",
            FillWeight = 20
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Vai trò",
            DataPropertyName = "RoleName",
            FillWeight = 15
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Trạng thái",
            Name = "colActive",
            FillWeight = 12
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 13,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        foreach (var user in users)
        {
            _grid.Rows.Add(
                user.Id,
                user.Username,
                user.Email,
                user.FullName,
                user.RoleName,
                user.IsActive ? "Hoạt động" : "Bị khóa",
                user.CreatedAt);
        }
    }

    private UserDto? GetSelectedUser()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _allUsers.FirstOrDefault(u => u.Id == id);
    }
}

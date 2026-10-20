using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Form quản lý cấu hình hệ thống (Admin) — chỉ sửa giá trị, không thêm/xóa.
/// DB chỉ có 4 key seed cố định (CommissionRate, MinTopUpAmount, MaxTopUpAmount, SiteName).
/// </summary>
public class SystemConfigForm : BaseCrudForm
{
    private readonly ISystemConfigService _configService;

    private List<SystemConfigDto> _allConfigs = new();

    public SystemConfigForm(ISystemConfigService configService)
    {
        _configService = configService;

        Text = "Cấu hình hệ thống";
        _btnAdd.Visible = false;
        _btnDelete.Visible = false;
        _btnEdit.Text = "Sửa giá trị";

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override void OnAddClicked()
    {
        // Chỉ sửa — không thêm key mới.
    }

    protected override async void OnEditClicked()
    {
        var selected = GetSelectedConfig();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 cấu hình để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SystemConfigEditForm(_configService, selected);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override void OnDeleteClicked()
    {
        // Chỉ sửa — không xóa cấu hình.
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var configs = await _configService.GetAllAsync();

            _allConfigs = configs.ToList();

            BindToGrid(_allConfigs);
            SetCountLabel(_allConfigs.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allConfigs);
            SetCountLabel(_allConfigs.Count);
            return;
        }

        var filtered = _allConfigs
            .Where(c => c.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                     || (c.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<SystemConfigDto> configs)
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
            HeaderText = "Khóa",
            DataPropertyName = "Key",
            FillWeight = 20
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá trị",
            DataPropertyName = "Value",
            FillWeight = 30
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mô tả",
            DataPropertyName = "Description",
            FillWeight = 30
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Cập nhật",
            DataPropertyName = "UpdatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        foreach (var config in configs)
        {
            _grid.Rows.Add(
                config.Id,
                config.Key,
                config.Value,
                (object?)config.Description ?? DBNull.Value,
                config.UpdatedAt.HasValue
                    ? config.UpdatedAt.Value.ToString("dd/MM/yyyy HH:mm")
                    : "—");
        }
    }

    private SystemConfigDto? GetSelectedConfig()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _allConfigs.FirstOrDefault(c => c.Id == id);
    }
}

using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Form xem nhật ký hệ thống (Audit Log) cho Admin — chỉ xem, không thêm/sửa/xóa.
/// Kế thừa BaseCrudForm, thêm bộ lọc: Từ ngày, Đến ngày, Hành động.
/// </summary>
public class LogViewerForm : BaseCrudForm
{
    private readonly ILogService _logService;

    private readonly DateTimePicker _dtpFrom = new();
    private readonly DateTimePicker _dtpTo = new();
    private readonly ToolStripTextBox _txtAction = new();
    private readonly ToolStripButton _btnFilter = new("Lọc");

    private List<LogEntryDto> _currentLogs = new();

    public LogViewerForm(ILogService logService)
    {
        _logService = logService;

        Text = "Nhật ký hệ thống (Audit Log)";
        _btnAdd.Visible = false;
        _btnEdit.Visible = false;
        _btnDelete.Visible = false;

        InitializeFilterBar();

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    private void InitializeFilterBar()
    {
        // ==== Bộ lọc ====
        _dtpFrom.Format = DateTimePickerFormat.Custom;
        _dtpFrom.CustomFormat = "dd/MM/yyyy";
        _dtpFrom.ShowUpDown = false;
        _dtpFrom.Width = 110;
        _dtpFrom.Value = DateTime.Today.AddMonths(-1);

        _dtpTo.Format = DateTimePickerFormat.Custom;
        _dtpTo.CustomFormat = "dd/MM/yyyy";
        _dtpTo.ShowUpDown = false;
        _dtpTo.Width = 110;
        _dtpTo.Value = DateTime.Today;

        _txtAction.Width = 150;
        _txtAction.ToolTipText = "Hành động (chứa)";

        _btnFilter.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _btnFilter.Click += async (s, e) => await OnRefreshClickedAsync();

        // Chèn trước nút "Làm mới"
        var refreshIndex = _toolStrip.Items.IndexOf(_btnRefresh);
        if (refreshIndex < 0) refreshIndex = _toolStrip.Items.Count;

        _toolStrip.Items.Insert(refreshIndex, new ToolStripLabel("Từ:"));
        _toolStrip.Items.Insert(refreshIndex + 1, new ToolStripControlHost(_dtpFrom));
        _toolStrip.Items.Insert(refreshIndex + 2, new ToolStripLabel("Đến:"));
        _toolStrip.Items.Insert(refreshIndex + 3, new ToolStripControlHost(_dtpTo));
        _toolStrip.Items.Insert(refreshIndex + 4, new ToolStripLabel("Hành động:"));
        _toolStrip.Items.Insert(refreshIndex + 5, _txtAction);
        _toolStrip.Items.Insert(refreshIndex + 6, _btnFilter);
    }

    protected override void OnAddClicked()
    {
        // Chỉ xem — không thêm.
    }

    protected override void OnEditClicked()
    {
        // Chỉ xem — không sửa.
    }

    protected override void OnDeleteClicked()
    {
        // Chỉ xem — không xóa.
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var fromDate = _dtpFrom.Value.Date;
            var toDate = _dtpTo.Value.Date.AddDays(1).AddTicks(-1);
            var action = _txtAction.Text.Trim();

            var logs = await _logService.GetLogsByFilterAsync(
                fromDate,
                toDate,
                string.IsNullOrWhiteSpace(action) ? null : action,
                null);

            _currentLogs = logs.ToList();

            BindToGrid(_currentLogs);
            SetCountLabel(_currentLogs.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_currentLogs);
            SetCountLabel(_currentLogs.Count);
            return;
        }

        var filtered = _currentLogs
            .Where(l => l.Action.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<LogEntryDto> logs)
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
            HeaderText = "Thời gian",
            DataPropertyName = "CreatedAt",
            FillWeight = 18,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm:ss" }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Người dùng",
            DataPropertyName = "Username",
            FillWeight = 16
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Hành động",
            DataPropertyName = "Action",
            FillWeight = 26
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Đối tượng",
            DataPropertyName = "EntityName",
            FillWeight = 15
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Chi tiết",
            DataPropertyName = "Details",
            FillWeight = 40
        });

        foreach (var log in logs)
        {
            _grid.Rows.Add(
                log.Id,
                log.CreatedAt,
                (object?)log.Username ?? DBNull.Value,
                log.Action,
                (object?)log.EntityName ?? DBNull.Value,
                (object?)log.Details ?? DBNull.Value);
        }
    }
}

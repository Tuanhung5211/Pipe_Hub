using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Form xem lịch sử gửi email (LogEntry với Action = "SendEmail") cho Admin — chỉ xem, không thêm/sửa/xóa.
/// Kế thừa BaseCrudForm để dùng lại ToolStrip + DataGridView + StatusStrip.
/// </summary>
public class NotificationForm : BaseCrudForm
{
    private readonly ILogService _logService;

    private List<LogEntryDto> _currentLogs = new();

    public NotificationForm(ILogService logService)
    {
        _logService = logService;

        Text = "Lịch sử gửi Email";
        _btnAdd.Visible = false;
        _btnEdit.Visible = false;
        _btnDelete.Visible = false;

        Load += async (s, e) => await OnRefreshClickedAsync();
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
            var logs = await _logService.GetLogsByFilterAsync(null, null, "SendEmail", null);

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
            .Where(l => ContainsIgnoreCase(l.Details, keyword)
                     || ContainsIgnoreCase(l.Username, keyword))
            .ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private static bool ContainsIgnoreCase(string? value, string keyword)
        => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false;

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
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm:ss" }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Người dùng",
            DataPropertyName = "Username",
            FillWeight = 18
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Hành động",
            DataPropertyName = "Action",
            FillWeight = 20
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Chi tiết",
            DataPropertyName = "Details",
            FillWeight = 50
        });

        foreach (var log in logs)
        {
            _grid.Rows.Add(
                log.Id,
                log.CreatedAt,
                (object?)log.Username ?? DBNull.Value,
                log.Action,
                (object?)log.Details ?? DBNull.Value);
        }
    }
}

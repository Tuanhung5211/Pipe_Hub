using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Form duyệt yêu cầu nạp tiền (Admin) — kế thừa BaseCrudForm, chỉ hiển thị yêu cầu Pending.
/// Nút "Sửa" → Duyệt (ApproveTopUpAsync), nút "Xóa" → Từ chối (RejectTopUpAsync).
/// </summary>
public class TopUpApprovalForm : BaseCrudForm
{
    private readonly IWalletService _walletService;

    private List<TopUpRequestDto> _pendingRequests = new();

    public TopUpApprovalForm(IWalletService walletService)
    {
        _walletService = walletService;

        Text = "Duyệt nạp tiền";
        _btnAdd.Visible = false;
        _btnEdit.Text = "Duyệt";
        _btnDelete.Text = "Từ chối";

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override void OnAddClicked()
    {
        // Admin không tạo yêu cầu nạp ở màn duyệt.
    }

    protected override async void OnEditClicked()
    {
        var selected = GetSelectedRequest();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 yêu cầu để duyệt.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Duyệt yêu cầu nạp {selected.Amount:N0} đ của '{selected.Username}'?",
            "Xác nhận duyệt",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _walletService.ApproveTopUpAsync(selected.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Duyệt yêu cầu nạp thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Đã duyệt yêu cầu nạp tiền.", "Thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            await OnRefreshClickedAsync();
        });
    }

    protected override async void OnDeleteClicked()
    {
        var selected = GetSelectedRequest();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 yêu cầu để từ chối.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Từ chối yêu cầu nạp {selected.Amount:N0} đ của '{selected.Username}'?",
            "Xác nhận từ chối",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _walletService.RejectTopUpAsync(selected.Id, "Admin từ chối");

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Từ chối yêu cầu nạp thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Đã từ chối yêu cầu nạp tiền.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            await OnRefreshClickedAsync();
        });
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var requests = await _walletService.GetAllTopUpRequestsAsync();
            _pendingRequests = requests.Where(r => r.Status == "Pending").ToList();
            BindToGrid(_pendingRequests);
            SetCountLabel(_pendingRequests.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_pendingRequests);
            SetCountLabel(_pendingRequests.Count);
            return;
        }

        var filtered = _pendingRequests.Where(r =>
            r.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<TopUpRequestDto> requests)
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
            HeaderText = "Người dùng",
            DataPropertyName = "Username",
            FillWeight = 25
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Số tiền",
            DataPropertyName = "Amount",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Phương thức",
            DataPropertyName = "PaymentMethod",
            FillWeight = 25
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 30,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        foreach (var request in requests)
        {
            _grid.Rows.Add(
                request.Id,
                request.Username,
                request.Amount,
                (object?)request.PaymentMethod ?? DBNull.Value,
                request.CreatedAt);
        }
    }

    private TopUpRequestDto? GetSelectedRequest()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _pendingRequests.FirstOrDefault(r => r.Id == id);
    }
}

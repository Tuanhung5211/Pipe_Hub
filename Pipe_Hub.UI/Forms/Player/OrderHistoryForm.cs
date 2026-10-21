using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Player;

public class OrderHistoryForm : BaseCrudForm
{
    private readonly IOrderService _orderService;

    private List<OrderDto> _allOrders = new();

    public OrderHistoryForm(IOrderService orderService)
    {
        _orderService = orderService;

        Text = "Lịch sử đơn hàng";
        _btnAdd.Visible = false;
        _btnEdit.Text = "Xem chi tiết";
        _btnEdit.Visible = true;
        _btnDelete.Visible = false;

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override void OnAddClicked()
    {
        // Player không tạo đơn
    }

    protected override void OnEditClicked()
    {
        var selected = GetSelectedOrder();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 đơn hàng.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ShowOrderDetail(selected);
    }

    protected override void OnDeleteClicked()
    {
        // Player không xóa đơn
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var orders = await _orderService.GetMyOrdersAsync();
            _allOrders = orders.ToList();
            BindToGrid(_allOrders);
            SetCountLabel(_allOrders.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allOrders);
            SetCountLabel(_allOrders.Count);
            return;
        }

        var filtered = _allOrders.Where(o =>
            o.OrderCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            o.Status.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            (o.Note?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<OrderDto> orders)
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
            HeaderText = "Mã đơn",
            DataPropertyName = "OrderCode",
            FillWeight = 25
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày mua",
            DataPropertyName = "CreatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Số game",
            Name = "colItemCount",
            FillWeight = 10,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tổng tiền",
            DataPropertyName = "TotalAmount",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Trạng thái",
            DataPropertyName = "Status",
            FillWeight = 15
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ghi chú",
            DataPropertyName = "Note",
            FillWeight = 15
        });

        foreach (var order in orders)
        {
            _grid.Rows.Add(
                order.Id,
                order.OrderCode,
                order.CreatedAt.ToLocalTime(),
                order.Items.Count,
                order.TotalAmount,
                order.Status,
                (object?)order.Note ?? DBNull.Value);
        }
    }

    private OrderDto? GetSelectedOrder()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var row = _grid.SelectedRows[0];
        var idCell = row.Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _allOrders.FirstOrDefault(o => o.Id == id);
    }

    private void ShowOrderDetail(OrderDto order)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Mã đơn: {order.OrderCode}");
        sb.AppendLine($"Ngày mua: {order.CreatedAt:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Trạng thái: {order.Status}");
        sb.AppendLine($"Tổng tiền: {order.TotalAmount:N0} đ");
        sb.AppendLine();
        sb.AppendLine("Chi tiết game:");

        foreach (var item in order.Items)
        {
            sb.AppendLine($"  • {item.GameTitle}  —  {item.UnitPrice:N0} đ x {item.Quantity}  =  {item.Subtotal:N0} đ");
        }

        if (order.GameKeys.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Key đã nhận:");
            foreach (var key in order.GameKeys)
            {
                sb.AppendLine($"  • {key}");
            }
        }

        MessageBox.Show(sb.ToString(), $"Đơn hàng {order.OrderCode}",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Form duyệt game (Admin) — kế thừa BaseCrudForm, chỉ hiển thị game Pending.
/// Nút "Sửa" → Duyệt (ApproveAsync), nút "Xóa" → Từ chối (RejectAsync).
/// </summary>
public class ApprovalForm : BaseCrudForm
{
    private readonly IGameService _gameService;

    private List<GameDto> _pendingGames = new();

    public ApprovalForm(IGameService gameService)
    {
        _gameService = gameService;

        Text = "Duyệt game";
        _btnAdd.Visible = false;
        _btnEdit.Text = "Duyệt";
        _btnDelete.Text = "Từ chối";

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override void OnAddClicked()
    {
        // Admin không tạo game ở màn duyệt.
    }

    protected override async void OnEditClicked()
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game để duyệt.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Duyệt game '{selected.Title}'?",
            "Xác nhận duyệt",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _gameService.ApproveAsync(selected.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Duyệt game thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show($"Đã duyệt game '{selected.Title}'.", "Thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            await OnRefreshClickedAsync();
        });
    }

    protected override async void OnDeleteClicked()
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game để từ chối.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Từ chối game '{selected.Title}'?",
            "Xác nhận từ chối",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _gameService.RejectAsync(selected.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Từ chối game thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show($"Đã từ chối game '{selected.Title}'.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            await OnRefreshClickedAsync();
        });
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var games = await _gameService.GetAllVisibleAsync();
            _pendingGames = games.Where(g => g.Status == "Pending").ToList();
            BindToGrid(_pendingGames);
            SetCountLabel(_pendingGames.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_pendingGames);
            SetCountLabel(_pendingGames.Count);
            return;
        }

        var filtered = _pendingGames.Where(g =>
            g.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            g.PublisherName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<GameDto> games)
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
            HeaderText = "Tên game",
            DataPropertyName = "Title",
            FillWeight = 40
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Nhà phát hành",
            DataPropertyName = "PublisherName",
            FillWeight = 25
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá",
            DataPropertyName = "Price",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        foreach (var game in games)
        {
            _grid.Rows.Add(game.Id, game.Title, game.PublisherName, game.Price, game.CreatedAt);
        }
    }

    private GameDto? GetSelectedGame()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _pendingGames.FirstOrDefault(g => g.Id == id);
    }
}

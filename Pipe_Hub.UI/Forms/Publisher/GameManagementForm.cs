using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Form quản lý game của Publisher — kế thừa BaseCrudForm.
/// Ngoài Thêm/Sửa/Xóa còn có nút "Gửi duyệt" (Draft/Rejected → Pending).
/// </summary>
public class GameManagementForm : BaseCrudForm
{
    private readonly IGameService _gameService;
    private readonly CurrentUserContext _currentUser;
    private readonly ToolStripButton _btnSubmit;

    private List<GameDto> _allGames = new();

    public GameManagementForm(IGameService gameService, CurrentUserContext currentUser)
    {
        _gameService = gameService;
        _currentUser = currentUser;

        Text = "Quản lý game";
        _btnAdd.Text = "Thêm game";
        _btnEdit.Text = "Sửa";

        _btnSubmit = new ToolStripButton("Gửi duyệt")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text
        };
        _btnSubmit.Click += async (s, e) => await SubmitSelectedAsync();

        // Chèn ngay sau nút "Xóa", trước separator.
        _toolStrip.Items.Insert(_toolStrip.Items.IndexOf(_btnDelete) + 1, _btnSubmit);

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override async void OnAddClicked()
    {
        using var dialog = new GameEditForm(_gameService);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override async void OnEditClicked()
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (selected.IsDeleted)
        {
            MessageBox.Show("Game đã bị xóa, không thể sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new GameEditForm(_gameService, selected);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override async void OnDeleteClicked()
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa game '{selected.Title}'?",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _gameService.SoftDeleteAsync(selected.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Xóa game thất bại.", "Thông báo",
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
            if (_currentUser.UserId is not int publisherId)
            {
                MessageBox.Show("Bạn chưa đăng nhập.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var games = await _gameService.GetByPublisherAsync(publisherId);
            _allGames = games.ToList();
            BindToGrid(_allGames);
            SetCountLabel(_allGames.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allGames);
            SetCountLabel(_allGames.Count);
            return;
        }

        var filtered = _allGames
            .Where(g => g.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private async Task SubmitSelectedAsync()
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game để gửi duyệt.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn gửi duyệt game '{selected.Title}'? Sau khi gửi, bạn sẽ không thể sửa cho đến khi Admin duyệt.",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _gameService.SubmitForApprovalAsync(selected.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Gửi duyệt game thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await OnRefreshClickedAsync();
        });
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
            FillWeight = 35
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá (đ)",
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
            HeaderText = "Trạng thái",
            DataPropertyName = "Status",
            FillWeight = 15
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
            _grid.Rows.Add(
                game.Id,
                game.Title,
                game.Price,
                game.Status,
                game.CreatedAt);
        }
    }

    private GameDto? GetSelectedGame()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _allGames.FirstOrDefault(g => g.Id == id);
    }
}

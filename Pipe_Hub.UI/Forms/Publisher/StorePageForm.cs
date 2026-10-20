using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Form chỉnh sửa "trang game" (mô tả + ảnh bìa) của Publisher — kế thừa BaseCrudForm.
/// Chỉ có chức năng Sửa (ẩn Thêm/Xóa).
/// </summary>
public class StorePageForm : BaseCrudForm
{
    private readonly IGameService _gameService;
    private readonly CurrentUserContext _currentUser;

    private List<GameDto> _allGames = new();

    public StorePageForm(IGameService gameService, CurrentUserContext currentUser)
    {
        _gameService = gameService;
        _currentUser = currentUser;

        Text = "Trang game";
        _btnAdd.Visible = false;
        _btnDelete.Visible = false;
        _btnEdit.Text = "Sửa trang game";

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override void OnAddClicked()
    {
        // Không dùng — nút Thêm đã bị ẩn.
    }

    protected override async void OnEditClicked()
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game để sửa trang.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new StorePageEditForm(_gameService, selected);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override void OnDeleteClicked()
    {
        // Không dùng — nút Xóa đã bị ẩn.
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
            FillWeight = 25
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Có ảnh bìa",
            DataPropertyName = "HasCover",
            FillWeight = 12,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Có mô tả",
            DataPropertyName = "HasDescription",
            FillWeight = 12,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter
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
            HeaderText = "Cập nhật gần nhất",
            DataPropertyName = "UpdatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        foreach (var game in games)
        {
            var hasCover = string.IsNullOrEmpty(game.CoverImagePath) ? "✗" : "✓";
            var hasDescription = (game.Description?.Length ?? 0) > 50 ? "✓" : "✗";

            _grid.Rows.Add(
                game.Id,
                game.Title,
                hasCover,
                hasDescription,
                game.Status,
                game.UpdatedAt ?? game.CreatedAt);
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

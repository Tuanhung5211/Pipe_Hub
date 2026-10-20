using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Form cấu hình giá bán / giá sale cho game của Publisher — kế thừa BaseCrudForm.
/// Chỉ có chức năng Sửa giá (ẩn Thêm/Xóa).
/// </summary>
public class PricingForm : BaseCrudForm
{
    private readonly IGameService _gameService;
    private readonly CurrentUserContext _currentUser;

    private List<GameDto> _allGames = new();

    public PricingForm(IGameService gameService, CurrentUserContext currentUser)
    {
        _gameService = gameService;
        _currentUser = currentUser;

        Text = "Cấu hình giá";
        _btnEdit.Text = "Sửa giá";
        _btnAdd.Visible = false;
        _btnDelete.Visible = false;

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
            MessageBox.Show("Vui lòng chọn 1 game để sửa giá.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new PricingEditForm(_gameService, selected);
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
            FillWeight = 30
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá gốc (đ)",
            DataPropertyName = "Price",
            FillWeight = 14,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá sale (đ)",
            DataPropertyName = "SalePrice",
            FillWeight = 14,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Sale từ",
            DataPropertyName = "SaleStartAt",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Sale đến",
            DataPropertyName = "SaleEndAt",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Trạng thái",
            DataPropertyName = "Status",
            FillWeight = 12
        });

        foreach (var game in games)
        {
            _grid.Rows.Add(
                game.Id,
                game.Title,
                game.Price,
                (object?)game.SalePrice ?? DBNull.Value,
                (object?)game.SaleStartAt ?? DBNull.Value,
                (object?)game.SaleEndAt ?? DBNull.Value,
                game.Status);
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

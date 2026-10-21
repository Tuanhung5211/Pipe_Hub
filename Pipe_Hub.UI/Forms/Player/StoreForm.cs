using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Player;

public class StoreForm : BaseCrudForm
{
    private readonly IGameService _gameService;
    private readonly IServiceProvider _serviceProvider;

    private List<GameDto> _allGames = new();

    public StoreForm(IGameService gameService, IServiceProvider serviceProvider)
    {
        _gameService = gameService;
        _serviceProvider = serviceProvider;

        Text = "Cửa hàng game";
        _btnAdd.Visible = false;
        _btnEdit.Visible = true;
        _btnEdit.Text = "Chi tiết / Mua";
        _btnDelete.Visible = false;

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    protected override void OnAddClicked()
    {
        // Player không có quyền thêm game
    }

    protected override void OnEditClicked()
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // TODO D5.5: mở GameDetailForm với selected.Id
        // GameDetailForm có tham số gameId nên không đăng ký DI thường → dùng ActivatorUtilities
        using var detailForm = ActivatorUtilities.CreateInstance<GameDetailForm>(
            _serviceProvider, selected.Id);
        detailForm.ShowDialog(this);

        // Reload sau khi đóng detail (balance có thể đã thay đổi)
        _ = OnRefreshClickedAsync();
    }

    protected override void OnDeleteClicked()
    {
        // Player không có quyền xóa
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var games = await _gameService.GetAllVisibleAsync();
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

        var filtered = _allGames.Where(g =>
            g.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            g.PublisherName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            g.GenreNames.Any(n => n.Contains(keyword, StringComparison.OrdinalIgnoreCase))
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
            FillWeight = 25
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Nhà phát hành",
            DataPropertyName = "PublisherName",
            FillWeight = 15
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá",
            DataPropertyName = "Price",
            FillWeight = 12,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá sale",
            DataPropertyName = "SalePrice",
            FillWeight = 12,
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
            FillWeight = 12
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Thể loại",
            DataPropertyName = "GenresText",
            FillWeight = 24
        });

        _grid.DataSource = new SortableBindingList<GameDto>(games);
    }

    private GameDto? GetSelectedGame()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var selectedRow = _grid.SelectedRows[0];
        var idCell = selectedRow.Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _allGames.FirstOrDefault(g => g.Id == id);
    }
}

using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Form quản lý Achievement (thành tích) của game — kế thừa BaseCrudForm.
/// ToolStrip có ComboBox chọn game; grid hiển thị thành tích của game đang chọn.
/// </summary>
public class AchievementForm : BaseCrudForm
{
    private readonly IAchievementService _achievementService;
    private readonly IGameService _gameService;
    private readonly CurrentUserContext _currentUser;

    private readonly ToolStripComboBox _cboGames;

    private List<AchievementDto> _allAchievements = new();
    private bool _loadingGames;

    public AchievementForm(
        IAchievementService achievementService,
        IGameService gameService,
        CurrentUserContext currentUser)
    {
        _achievementService = achievementService;
        _gameService = gameService;
        _currentUser = currentUser;

        Text = "Quản lý thành tích";

        _cboGames = new ToolStripComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 220
        };
        _cboGames.ComboBox.SelectedIndexChanged += async (s, e) =>
        {
            if (_loadingGames) return;
            await OnRefreshClickedAsync();
        };

        // Chèn "Game:" + ComboBox chọn game lên đầu ToolStrip.
        _toolStrip.Items.Insert(0, new ToolStripLabel("Game:"));
        _toolStrip.Items.Insert(1, _cboGames);

        Load += async (s, e) => await LoadGamesAsync();
    }

    private async Task LoadGamesAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            if (_currentUser.UserId is not int userId)
            {
                MessageBox.Show("Bạn chưa đăng nhập.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var games = _currentUser.IsAdmin
                ? await _gameService.GetAllVisibleAsync()
                : await _gameService.GetByPublisherAsync(userId);

            _loadingGames = true;
            try
            {
                _cboGames.ComboBox.DataSource = games.ToList();
                _cboGames.ComboBox.DisplayMember = "Title";
                _cboGames.ComboBox.ValueMember = "Id";
            }
            finally
            {
                _loadingGames = false;
            }

            await OnRefreshClickedAsync();
        });
    }

    protected override async void OnAddClicked()
    {
        var game = GetSelectedGame();
        if (game is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game trước.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new AchievementEditForm(_achievementService, game.Id);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override async void OnEditClicked()
    {
        var game = GetSelectedGame();
        if (game is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game trước.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selected = GetSelectedAchievement();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 thành tích để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new AchievementEditForm(_achievementService, game.Id, selected);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await OnRefreshClickedAsync();
        }
    }

    protected override async void OnDeleteClicked()
    {
        var game = GetSelectedGame();
        if (game is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game trước.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selected = GetSelectedAchievement();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 thành tích để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa thành tích '{selected.Name}' của game '{game.Title}'?",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _achievementService.DeleteAsync(selected.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Xóa thành tích thất bại.", "Thông báo",
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
            var game = GetSelectedGame();
            if (game is null)
            {
                _allAchievements = new List<AchievementDto>();
                BindToGrid(_allAchievements);
                SetCountLabel(0);
                return;
            }

            var achievements = await _achievementService.GetByGameAsync(game.Id);

            _allAchievements = achievements.ToList();

            BindToGrid(_allAchievements);
            SetCountLabel(_allAchievements.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allAchievements);
            SetCountLabel(_allAchievements.Count);
            return;
        }

        var filtered = _allAchievements
            .Where(a => a.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<AchievementDto> achievements)
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
            HeaderText = "Tên",
            DataPropertyName = "Name",
            FillWeight = 25
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mô tả",
            DataPropertyName = "Description",
            FillWeight = 35
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "IconPath",
            DataPropertyName = "IconPath",
            FillWeight = 20
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        foreach (var achievement in achievements)
        {
            _grid.Rows.Add(
                achievement.Id,
                achievement.Name,
                (object?)achievement.Description ?? DBNull.Value,
                (object?)achievement.IconPath ?? DBNull.Value,
                achievement.CreatedAt);
        }
    }

    private GameDto? GetSelectedGame()
    {
        return _cboGames.ComboBox.SelectedItem as GameDto;
    }

    private AchievementDto? GetSelectedAchievement()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _allAchievements.FirstOrDefault(a => a.Id == id);
    }
}

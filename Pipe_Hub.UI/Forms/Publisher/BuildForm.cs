using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Form quản lý GameBuild (bản phát hành) của Publisher — kế thừa BaseCrudForm.
/// ToolStrip có thêm ComboBox chọn game; grid hiển thị build của game đang chọn.
/// </summary>
public class BuildForm : BaseCrudForm
{
    private readonly IBuildService _buildService;
    private readonly IGameService _gameService;
    private readonly CurrentUserContext _currentUser;

    private readonly ToolStripComboBox _cboGames;

    private List<BuildDto> _allBuilds = new();
    private bool _loadingGames;

    public BuildForm(IBuildService buildService, IGameService gameService, CurrentUserContext currentUser)
    {
        _buildService = buildService;
        _gameService = gameService;
        _currentUser = currentUser;

        Text = "Quản lý build";

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
            if (_currentUser.UserId is not int publisherId)
            {
                MessageBox.Show("Bạn chưa đăng nhập.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var games = await _gameService.GetByPublisherAsync(publisherId);

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

        using var dialog = new BuildEditForm(_buildService, game.Id);
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

        var selected = GetSelectedBuild();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 build để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new BuildEditForm(_buildService, game.Id, selected);
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

        var selected = GetSelectedBuild();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 build để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"Bạn có chắc muốn xóa build '{selected.Version}' của game '{game.Title}'?",
            "Xác nhận",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _buildService.DeleteBuildAsync(selected.Id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Xóa build thất bại.", "Thông báo",
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
                _allBuilds = new List<BuildDto>();
                BindToGrid(_allBuilds);
                SetCountLabel(0);
                return;
            }

            var builds = await _buildService.GetBuildsByGameAsync(game.Id);
            _allBuilds = builds.ToList();
            BindToGrid(_allBuilds);
            SetCountLabel(_allBuilds.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allBuilds);
            SetCountLabel(_allBuilds.Count);
            return;
        }

        var filtered = _allBuilds
            .Where(b => b.Version.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private void BindToGrid(List<BuildDto> builds)
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
            HeaderText = "Phiên bản",
            DataPropertyName = "Version",
            FillWeight = 30
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Kênh",
            DataPropertyName = "Channel",
            FillWeight = 20
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Phát hành",
            DataPropertyName = "ReleasedAt",
            FillWeight = 25,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 25,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        foreach (var build in builds)
        {
            _grid.Rows.Add(
                build.Id,
                build.Version,
                build.Channel,
                (object?)build.ReleasedAt ?? DBNull.Value,
                build.CreatedAt);
        }
    }

    private GameDto? GetSelectedGame()
    {
        return _cboGames.ComboBox.SelectedItem as GameDto;
    }

    private BuildDto? GetSelectedBuild()
    {
        if (_grid.SelectedRows.Count == 0) return null;

        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;

        var id = (int)idCell;
        return _allBuilds.FirstOrDefault(b => b.Id == id);
    }
}

using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Player;

/// <summary>
/// Player xem lại các thành tích mình đã đạt được (read-only).
/// </summary>
public class PlayerAchievementForm : Form
{
    private readonly IAchievementService _achievementService;

    private ToolStrip _toolStrip = null!;
    private ToolStripButton _btnRefresh = null!;
    private ToolStripTextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    private readonly DebounceTimer _searchDebounce = new(300);
    private List<PlayerAchievementDto> _allAchievements = new();

    public PlayerAchievementForm(IAchievementService achievementService)
    {
        _achievementService = achievementService;
        InitializeComponents();
        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Thành tích của tôi";
        ClientSize = new Size(900, 550);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(700, 400);

        // ===== ToolStrip =====
        _toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(4) };

        _btnRefresh = new ToolStripButton("Làm mới") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnRefresh.Click += async (s, e) => await OnRefreshClickedAsync();

        _txtSearch = new ToolStripTextBox { Width = 200, ToolTipText = "Tìm theo tên thành tích" };
        _txtSearch.TextChanged += (s, e) =>
        {
            _searchDebounce.Debounce(() =>
            {
                if (IsDisposed) return;
                OnSearchChanged(_txtSearch.Text);
            });
        };

        _toolStrip.Items.Add(_btnRefresh);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(new ToolStripLabel("Tìm:"));
        _toolStrip.Items.Add(_txtSearch);

        // ===== Grid =====
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        // ===== StatusStrip =====
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel("0 thành tích") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_lblStatus);

        Controls.Add(_grid);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        this.RegisterShortcut(Keys.Control | Keys.F, () => _txtSearch.Focus());
        this.RegisterShortcut(Keys.F5, async () => await OnRefreshClickedAsync());
    }

    private async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            _allAchievements = (await _achievementService.GetMyAchievementsAsync()).ToList();
            BindToGrid(_allAchievements);
            UpdateStatus(_allAchievements.Count);
        });
    }

    private void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allAchievements);
            UpdateStatus(_allAchievements.Count);
            return;
        }

        var filtered = _allAchievements.Where(a =>
            a.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            a.GameTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        BindToGrid(filtered);
        UpdateStatus(filtered.Count);
    }

    private void UpdateStatus(int count)
    {
        _lblStatus.Text = $"{count} thành tích";
    }

    private void BindToGrid(List<PlayerAchievementDto> items)
    {
        _grid.DataSource = null;
        _grid.Columns.Clear();
        _grid.AutoGenerateColumns = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tên thành tích",
            DataPropertyName = "Name",
            FillWeight = 30
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Game",
            DataPropertyName = "GameTitle",
            FillWeight = 25
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mô tả",
            DataPropertyName = "Description",
            FillWeight = 30
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày đạt",
            DataPropertyName = "UnlockedAt",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.DataSource = new SortableBindingList<PlayerAchievementDto>(items);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchDebounce?.Dispose();
        }
        base.Dispose(disposing);
    }
}

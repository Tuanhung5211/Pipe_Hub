using Microsoft.Extensions.DependencyInjection;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Player;

public class LibraryForm : Form
{
    private readonly ILibraryService _libraryService;
    private readonly IWishlistService _wishlistService;
    private readonly IServiceProvider _serviceProvider;

    private ToolStrip _toolStrip = null!;
    private ToolStripButton _btnRefresh = null!;
    private ToolStripTextBox _txtSearch = null!;
    private TabControl _tabs = null!;
    private DataGridView _gridLibrary = null!;
    private DataGridView _gridWishlist = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private Button _btnRemoveWishlist = null!;

    private readonly DebounceTimer _searchDebounce = new(300);
    private List<LibraryItemDto> _allLibrary = new();
    private List<WishlistItemDto> _allWishlist = new();

    public LibraryForm(
        ILibraryService libraryService,
        IWishlistService wishlistService,
        IServiceProvider serviceProvider)
    {
        _libraryService = libraryService;
        _wishlistService = wishlistService;
        _serviceProvider = serviceProvider;

        InitializeComponents();
        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Thư viện game";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(700, 400);
        Font = new Font("Segoe UI", 9F);

        // ===== ToolStrip =====
        _toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(4)
        };

        _btnRefresh = new ToolStripButton("Làm mới")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text
        };
        _btnRefresh.Click += async (s, e) => await OnRefreshClickedAsync();

        _txtSearch = new ToolStripTextBox
        {
            Width = 200,
            ToolTipText = "Tìm kiếm (Ctrl+F)"
        };
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

        // ===== TabControl =====
        _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };

        // Tab 1: Thư viện
        var tabLibrary = new TabPage("Thư viện") { Padding = new Padding(3) };
        _gridLibrary = CreateGrid();
        _gridLibrary.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0) OnLibraryDoubleClick();
        };
        tabLibrary.Controls.Add(_gridLibrary);

        // Tab 2: Wishlist
        var tabWishlist = new TabPage("Wishlist") { Padding = new Padding(3) };

        var pnlWishlist = new Panel { Dock = DockStyle.Fill };

        _btnRemoveWishlist = new Button
        {
            Text = "Xóa khỏi wishlist",
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnRemoveWishlist.Click += async (s, e) => await BtnRemoveWishlist_ClickAsync();

        _gridWishlist = CreateGrid();
        _gridWishlist.Dock = DockStyle.Fill;

        pnlWishlist.Controls.Add(_gridWishlist);
        pnlWishlist.Controls.Add(_btnRemoveWishlist);
        tabWishlist.Controls.Add(pnlWishlist);

        _tabs.TabPages.Add(tabLibrary);
        _tabs.TabPages.Add(tabWishlist);

        // ===== StatusStrip =====
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel
        {
            Text = "0 mục",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusStrip.Items.Add(_lblStatus);

        Controls.Add(_tabs);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        // Phím tắt
        this.RegisterShortcut(Keys.Control | Keys.F, () => _txtSearch.Focus());
        this.RegisterShortcut(Keys.F5, async () => await OnRefreshClickedAsync());
    }

    private DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeColumns = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None
        };
    }

    private async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            _allLibrary = (await _libraryService.GetMyLibraryAsync()).ToList();
            _allWishlist = (await _wishlistService.GetMyWishlistAsync()).ToList();

            BindLibraryGrid(_allLibrary);
            BindWishlistGrid(_allWishlist);
            UpdateStatus();
        });
    }

    private void BindLibraryGrid(List<LibraryItemDto> items)
    {
        _gridLibrary.DataSource = null;
        _gridLibrary.Columns.Clear();
        _gridLibrary.AutoGenerateColumns = false;

        _gridLibrary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tên game",
            DataPropertyName = "GameTitle",
            FillWeight = 35
        });

        _gridLibrary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Nhà phát hành",
            DataPropertyName = "PublisherName",
            FillWeight = 20
        });

        _gridLibrary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá mua",
            DataPropertyName = "PurchasePrice",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _gridLibrary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày mua",
            DataPropertyName = "PurchasedAt",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _gridLibrary.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Playtime",
            DataPropertyName = "TotalPlayMinutes",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _gridLibrary.DataSource = new SortableBindingList<LibraryItemDto>(items);
    }

    private void BindWishlistGrid(List<WishlistItemDto> items)
    {
        _gridWishlist.DataSource = null;
        _gridWishlist.Columns.Clear();
        _gridWishlist.AutoGenerateColumns = false;

        _gridWishlist.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tên game",
            DataPropertyName = "GameTitle",
            FillWeight = 35
        });

        _gridWishlist.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Nhà phát hành",
            DataPropertyName = "PublisherName",
            FillWeight = 20
        });

        _gridWishlist.Columns.Add(new DataGridViewTextBoxColumn
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

        _gridWishlist.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Giá sale",
            DataPropertyName = "SalePrice",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _gridWishlist.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày thêm",
            DataPropertyName = "AddedAt",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _gridWishlist.DataSource = new SortableBindingList<WishlistItemDto>(items);
    }

    private void UpdateStatus()
    {
        _lblStatus.Text = $"Thư viện: {_allLibrary.Count} game | Wishlist: {_allWishlist.Count} game";
    }

    private void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindLibraryGrid(_allLibrary);
            BindWishlistGrid(_allWishlist);
            UpdateStatus();
            return;
        }

        var filteredLib = _allLibrary.Where(x =>
            x.GameTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            x.PublisherName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        var filteredWish = _allWishlist.Where(x =>
            x.GameTitle.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            x.PublisherName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        BindLibraryGrid(filteredLib);
        BindWishlistGrid(filteredWish);
        _lblStatus.Text = $"Thư viện: {filteredLib.Count} | Wishlist: {filteredWish.Count}";
    }

    private void OnLibraryDoubleClick()
    {
        if (_gridLibrary.SelectedRows.Count == 0) return;

        var row = _gridLibrary.SelectedRows[0];
        if (row.Cells.Count == 0) return;

        // Lấy GameId từ data source
        var item = row.DataBoundItem as LibraryItemDto;
        if (item is null) return;

        var detailForm = ActivatorUtilities.CreateInstance<GameDetailForm>(
            _serviceProvider, item.GameId);
        detailForm.ShowDialog(this);

        _ = OnRefreshClickedAsync();
    }

    private async Task BtnRemoveWishlist_ClickAsync()
    {
        if (_gridWishlist.SelectedRows.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn 1 game trong wishlist.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var row = _gridWishlist.SelectedRows[0];
        var item = row.DataBoundItem as WishlistItemDto;
        if (item is null) return;

        var confirm = MessageBox.Show(
            $"Xóa '{item.GameTitle}' khỏi wishlist?",
            "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _wishlistService.RemoveAsync(item.GameId);
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Không thể xóa.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Đã xóa khỏi wishlist.", "Thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await OnRefreshClickedAsync();
        });
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

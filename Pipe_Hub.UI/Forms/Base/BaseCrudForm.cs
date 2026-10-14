using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Base;

/// <summary>
/// Form cha cho các form CRUD.
/// Layout: ToolStrip (Thêm/Sửa/Xóa/Làm mới/Search) + DataGridView + StatusStrip.
/// Subclass override abstract method để cung cấp data + handler.
/// </summary>
public abstract class BaseCrudForm : Form
{
    protected ToolStrip _toolStrip = null!;
    protected ToolStripButton _btnAdd = null!;
    protected ToolStripButton _btnEdit = null!;
    protected ToolStripButton _btnDelete = null!;
    protected ToolStripButton _btnRefresh = null!;
    protected ToolStripTextBox _txtSearch = null!;
    protected DataGridView _grid = null!;
    protected StatusStrip _statusStrip = null!;
    protected ToolStripStatusLabel _lblCount = null!;

    protected readonly DebounceTimer _searchDebounce = new(300);

    protected BaseCrudForm()
    {
        InitializeBaseComponents();
    }

    private void InitializeBaseComponents()
    {
        Text = "Quản lý dữ liệu";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 600);
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(700, 400);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        // ==== ToolStrip ====
        _toolStrip = new ToolStrip
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(4)
        };

        _btnAdd = new ToolStripButton("Thêm")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text
        };
        _btnAdd.Click += (s, e) => OnAddClicked();

        _btnEdit = new ToolStripButton("Sửa")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text
        };
        _btnEdit.Click += (s, e) => OnEditClicked();

        _btnDelete = new ToolStripButton("Xóa")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text
        };
        _btnDelete.Click += (s, e) => OnDeleteClicked();

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

        _toolStrip.Items.Add(_btnAdd);
        _toolStrip.Items.Add(_btnEdit);
        _toolStrip.Items.Add(_btnDelete);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(_btnRefresh);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(new ToolStripLabel("Tìm:"));
        _toolStrip.Items.Add(_txtSearch);

        // ==== DataGridView ====
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
        _grid.CellDoubleClick += (s, e) =>
        {
            if (e.RowIndex >= 0) OnEditClicked();
        };

        // ==== StatusStrip ====
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblCount = new ToolStripStatusLabel
        {
            Text = "0 mục",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusStrip.Items.Add(_lblCount);

        Controls.Add(_grid);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        // Phím tắt
        this.RegisterShortcut(Keys.Control | Keys.F, () => _txtSearch.Focus());
        this.RegisterShortcut(Keys.F5, async () => await OnRefreshClickedAsync());
    }

    protected void SetCountLabel(int count)
    {
        _lblCount.Text = $"{count} mục";
    }

    // ============ ABSTRACT METHODS ============

    /// <summary>Subclass cung cấp handler cho nút Thêm.</summary>
    protected abstract void OnAddClicked();

    /// <summary>Subclass cung cấp handler cho nút Sửa.</summary>
    protected abstract void OnEditClicked();

    /// <summary>Subclass cung cấp handler cho nút Xóa.</summary>
    protected abstract void OnDeleteClicked();

    /// <summary>Subclass tự load lại data + bind vào _grid.</summary>
    protected abstract Task OnRefreshClickedAsync();

    /// <summary>Subclass filter data theo từ khóa search.</summary>
    protected abstract void OnSearchChanged(string keyword);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchDebounce?.Dispose();
        }
        base.Dispose(disposing);
    }
}

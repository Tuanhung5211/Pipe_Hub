using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Admin;

/// <summary>
/// Form quản lý danh mục (Genre / Tag / Language) cho Admin.
/// Dùng TabControl 3 tab, mỗi tab có 1 grid + panel nhập liệu riêng
/// (không dùng BaseCrudForm vì form cha chỉ hỗ trợ 1 grid).
/// </summary>
public class CategoryForm : Form
{
    private readonly ICategoryService _categoryService;

    private ToolStrip _toolStrip = null!;
    private ToolStripButton _btnRefresh = null!;
    private TabControl _tabs = null!;

    // ===== Tab Genre =====
    private DataGridView _gridGenre = null!;
    private TextBox _txtGenreName = null!;
    private TextBox _txtGenreDescription = null!;
    private int? _selectedGenreId;

    // ===== Tab Tag =====
    private DataGridView _gridTag = null!;
    private TextBox _txtTagName = null!;
    private int? _selectedTagId;

    // ===== Tab Language =====
    private DataGridView _gridLanguage = null!;
    private TextBox _txtLanguageName = null!;
    private TextBox _txtLanguageCode = null!;
    private int? _selectedLanguageId;

    public CategoryForm(ICategoryService categoryService)
    {
        _categoryService = categoryService;

        InitializeComponents();

        Load += async (s, e) => await RefreshAllAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Quản lý danh mục";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(700, 450);
        Font = new Font("Segoe UI", 9F);

        // ==== ToolStrip ====
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
        _btnRefresh.Click += async (s, e) => await RefreshAllAsync();
        _toolStrip.Items.Add(_btnRefresh);

        // ==== Tabs ====
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F)
        };
        _tabs.TabPages.Add(BuildGenreTab());
        _tabs.TabPages.Add(BuildTagTab());
        _tabs.TabPages.Add(BuildLanguageTab());

        Controls.Add(_tabs);
        Controls.Add(_toolStrip);
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
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
    }

    private static Button CreateActionButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(90, 28)
        };
    }

    // =====================================================================
    // TAB: THỂ LOẠI
    // =====================================================================

    private TabPage BuildGenreTab()
    {
        var tab = new TabPage("Thể loại") { Padding = new Padding(3) };

        _gridGenre = CreateGrid();
        _gridGenre.SelectionChanged += (s, e) => SyncGenreSelection();

        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 130,
            Padding = new Padding(10)
        };

        panel.Controls.Add(new Label { Text = "Tên thể loại:", Location = new Point(10, 15), Size = new Size(80, 25) });
        _txtGenreName = new TextBox { Location = new Point(100, 12), Size = new Size(250, 25) };
        panel.Controls.Add(_txtGenreName);

        panel.Controls.Add(new Label { Text = "Mô tả:", Location = new Point(10, 50), Size = new Size(80, 25) });
        _txtGenreDescription = new TextBox { Location = new Point(100, 47), Size = new Size(250, 25) };
        panel.Controls.Add(_txtGenreDescription);

        var btnAdd = CreateActionButton("Thêm", 100, 85);
        btnAdd.Click += BtnGenreAdd_Click;
        var btnEdit = CreateActionButton("Sửa", 200, 85);
        btnEdit.Click += BtnGenreEdit_Click;
        var btnDelete = CreateActionButton("Xóa", 300, 85);
        btnDelete.Click += BtnGenreDelete_Click;

        panel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

        tab.Controls.Add(_gridGenre);
        tab.Controls.Add(panel);

        return tab;
    }

    private async Task LoadGenresAsync()
    {
        var genres = await _categoryService.GetGenresAsync();

        _selectedGenreId = null;
        _txtGenreName.Clear();
        _txtGenreDescription.Clear();

        _gridGenre.DataSource = null;
        _gridGenre.Columns.Clear();
        _gridGenre.AutoGenerateColumns = false;

        _gridGenre.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colGenreId",
            DataPropertyName = "Id",
            Visible = false
        });
        _gridGenre.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colGenreName",
            HeaderText = "Tên thể loại",
            DataPropertyName = "Name",
            FillWeight = 40
        });
        _gridGenre.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colGenreDescription",
            HeaderText = "Mô tả",
            DataPropertyName = "Description",
            FillWeight = 60
        });

        foreach (var genre in genres)
        {
            _gridGenre.Rows.Add(genre.Id, genre.Name, (object?)genre.Description ?? DBNull.Value);
        }

        if (_gridGenre.Rows.Count > 0)
        {
            _gridGenre.Rows[0].Selected = true;
        }
        SyncGenreSelection();
    }

    private void SyncGenreSelection()
    {
        if (_gridGenre.SelectedRows.Count == 0)
        {
            _selectedGenreId = null;
            return;
        }

        var row = _gridGenre.SelectedRows[0];
        if (row.Cells["colGenreId"].Value is not int id)
        {
            _selectedGenreId = null;
            return;
        }

        _selectedGenreId = id;
        _txtGenreName.Text = row.Cells["colGenreName"].Value?.ToString() ?? string.Empty;
        _txtGenreDescription.Text = row.Cells["colGenreDescription"].Value?.ToString() ?? string.Empty;
    }

    private async void BtnGenreAdd_Click(object? sender, EventArgs e)
    {
        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.CreateGenreAsync(_txtGenreName.Text, _txtGenreDescription.Text);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Thêm thể loại thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadGenresAsync();
        });
    }

    private async void BtnGenreEdit_Click(object? sender, EventArgs e)
    {
        if (_selectedGenreId is not int id)
        {
            MessageBox.Show("Vui lòng chọn 1 thể loại để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.UpdateGenreAsync(id, _txtGenreName.Text, _txtGenreDescription.Text);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Cập nhật thể loại thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadGenresAsync();
        });
    }

    private async void BtnGenreDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedGenreId is not int id)
        {
            MessageBox.Show("Vui lòng chọn 1 thể loại để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Bạn có chắc muốn xóa thể loại này?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.DeleteGenreAsync(id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Xóa thể loại thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadGenresAsync();
        });
    }

    // =====================================================================
    // TAB: THẺ
    // =====================================================================

    private TabPage BuildTagTab()
    {
        var tab = new TabPage("Thẻ") { Padding = new Padding(3) };

        _gridTag = CreateGrid();
        _gridTag.SelectionChanged += (s, e) => SyncTagSelection();

        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 95,
            Padding = new Padding(10)
        };

        panel.Controls.Add(new Label { Text = "Tên thẻ:", Location = new Point(10, 15), Size = new Size(80, 25) });
        _txtTagName = new TextBox { Location = new Point(100, 12), Size = new Size(250, 25) };
        panel.Controls.Add(_txtTagName);

        var btnAdd = CreateActionButton("Thêm", 100, 50);
        btnAdd.Click += BtnTagAdd_Click;
        var btnEdit = CreateActionButton("Sửa", 200, 50);
        btnEdit.Click += BtnTagEdit_Click;
        var btnDelete = CreateActionButton("Xóa", 300, 50);
        btnDelete.Click += BtnTagDelete_Click;

        panel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

        tab.Controls.Add(_gridTag);
        tab.Controls.Add(panel);

        return tab;
    }

    private async Task LoadTagsAsync()
    {
        var tags = await _categoryService.GetTagsAsync();

        _selectedTagId = null;
        _txtTagName.Clear();

        _gridTag.DataSource = null;
        _gridTag.Columns.Clear();
        _gridTag.AutoGenerateColumns = false;

        _gridTag.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTagId",
            DataPropertyName = "Id",
            Visible = false
        });
        _gridTag.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colTagName",
            HeaderText = "Tên thẻ",
            DataPropertyName = "Name",
            FillWeight = 100
        });

        foreach (var tag in tags)
        {
            _gridTag.Rows.Add(tag.Id, tag.Name);
        }

        if (_gridTag.Rows.Count > 0)
        {
            _gridTag.Rows[0].Selected = true;
        }
        SyncTagSelection();
    }

    private void SyncTagSelection()
    {
        if (_gridTag.SelectedRows.Count == 0)
        {
            _selectedTagId = null;
            return;
        }

        var row = _gridTag.SelectedRows[0];
        if (row.Cells["colTagId"].Value is not int id)
        {
            _selectedTagId = null;
            return;
        }

        _selectedTagId = id;
        _txtTagName.Text = row.Cells["colTagName"].Value?.ToString() ?? string.Empty;
    }

    private async void BtnTagAdd_Click(object? sender, EventArgs e)
    {
        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.CreateTagAsync(_txtTagName.Text);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Thêm thẻ thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadTagsAsync();
        });
    }

    private async void BtnTagEdit_Click(object? sender, EventArgs e)
    {
        if (_selectedTagId is not int id)
        {
            MessageBox.Show("Vui lòng chọn 1 thẻ để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.UpdateTagAsync(id, _txtTagName.Text);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Cập nhật thẻ thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadTagsAsync();
        });
    }

    private async void BtnTagDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedTagId is not int id)
        {
            MessageBox.Show("Vui lòng chọn 1 thẻ để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Bạn có chắc muốn xóa thẻ này?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.DeleteTagAsync(id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Xóa thẻ thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadTagsAsync();
        });
    }

    // =====================================================================
    // TAB: NGÔN NGỮ
    // =====================================================================

    private TabPage BuildLanguageTab()
    {
        var tab = new TabPage("Ngôn ngữ") { Padding = new Padding(3) };

        _gridLanguage = CreateGrid();
        _gridLanguage.SelectionChanged += (s, e) => SyncLanguageSelection();

        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 130,
            Padding = new Padding(10)
        };

        panel.Controls.Add(new Label { Text = "Tên ngôn ngữ:", Location = new Point(10, 15), Size = new Size(80, 25) });
        _txtLanguageName = new TextBox { Location = new Point(100, 12), Size = new Size(250, 25) };
        panel.Controls.Add(_txtLanguageName);

        panel.Controls.Add(new Label { Text = "Mã (Code):", Location = new Point(10, 50), Size = new Size(80, 25) });
        _txtLanguageCode = new TextBox { Location = new Point(100, 47), Size = new Size(250, 25) };
        panel.Controls.Add(_txtLanguageCode);

        var btnAdd = CreateActionButton("Thêm", 100, 85);
        btnAdd.Click += BtnLanguageAdd_Click;
        var btnEdit = CreateActionButton("Sửa", 200, 85);
        btnEdit.Click += BtnLanguageEdit_Click;
        var btnDelete = CreateActionButton("Xóa", 300, 85);
        btnDelete.Click += BtnLanguageDelete_Click;

        panel.Controls.AddRange(new Control[] { btnAdd, btnEdit, btnDelete });

        tab.Controls.Add(_gridLanguage);
        tab.Controls.Add(panel);

        return tab;
    }

    private async Task LoadLanguagesAsync()
    {
        var languages = await _categoryService.GetLanguagesAsync();

        _selectedLanguageId = null;
        _txtLanguageName.Clear();
        _txtLanguageCode.Clear();

        _gridLanguage.DataSource = null;
        _gridLanguage.Columns.Clear();
        _gridLanguage.AutoGenerateColumns = false;

        _gridLanguage.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLanguageId",
            DataPropertyName = "Id",
            Visible = false
        });
        _gridLanguage.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLanguageName",
            HeaderText = "Tên ngôn ngữ",
            DataPropertyName = "Name",
            FillWeight = 60
        });
        _gridLanguage.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLanguageCode",
            HeaderText = "Mã",
            DataPropertyName = "Code",
            FillWeight = 40
        });

        foreach (var language in languages)
        {
            _gridLanguage.Rows.Add(language.Id, language.Name, (object?)language.Code ?? DBNull.Value);
        }

        if (_gridLanguage.Rows.Count > 0)
        {
            _gridLanguage.Rows[0].Selected = true;
        }
        SyncLanguageSelection();
    }

    private void SyncLanguageSelection()
    {
        if (_gridLanguage.SelectedRows.Count == 0)
        {
            _selectedLanguageId = null;
            return;
        }

        var row = _gridLanguage.SelectedRows[0];
        if (row.Cells["colLanguageId"].Value is not int id)
        {
            _selectedLanguageId = null;
            return;
        }

        _selectedLanguageId = id;
        _txtLanguageName.Text = row.Cells["colLanguageName"].Value?.ToString() ?? string.Empty;
        _txtLanguageCode.Text = row.Cells["colLanguageCode"].Value?.ToString() ?? string.Empty;
    }

    private async void BtnLanguageAdd_Click(object? sender, EventArgs e)
    {
        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.CreateLanguageAsync(_txtLanguageName.Text, _txtLanguageCode.Text);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Thêm ngôn ngữ thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadLanguagesAsync();
        });
    }

    private async void BtnLanguageEdit_Click(object? sender, EventArgs e)
    {
        if (_selectedLanguageId is not int id)
        {
            MessageBox.Show("Vui lòng chọn 1 ngôn ngữ để sửa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.UpdateLanguageAsync(id, _txtLanguageName.Text, _txtLanguageCode.Text);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Cập nhật ngôn ngữ thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadLanguagesAsync();
        });
    }

    private async void BtnLanguageDelete_Click(object? sender, EventArgs e)
    {
        if (_selectedLanguageId is not int id)
        {
            MessageBox.Show("Vui lòng chọn 1 ngôn ngữ để xóa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show("Bạn có chắc muốn xóa ngôn ngữ này?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _categoryService.DeleteLanguageAsync(id);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Xóa ngôn ngữ thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await LoadLanguagesAsync();
        });
    }

    // =====================================================================

    private async Task RefreshAllAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            await LoadGenresAsync();
            await LoadTagsAsync();
            await LoadLanguagesAsync();
        });
    }
}

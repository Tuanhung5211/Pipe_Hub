using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Security;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Form quản lý GameKey cho Publisher:
/// - Sinh key ngẫu nhiên (NumericUpDown)
/// - Import CSV (Task.Run + ProgressBar + CancellationToken)
/// - Export CSV (SaveFileDialog + Task.Run + ProgressBar)
/// - Xem QR của key (QRCoder)
/// </summary>
public class KeyGeneratorForm : BaseCrudForm
{
    private readonly IKeyService _keyService;
    private readonly IGameService _gameService;
    private readonly ICsvKeyImporter _importer;
    private readonly ICsvKeyExporter _exporter;
    private readonly CurrentUserContext _currentUser;

    private readonly ToolStripComboBox _cboGames;
    private readonly ToolStripButton _btnCancelImport;
    private readonly ToolStripProgressBar _progressBar;
    private readonly ToolStripButton _btnExport;

    private List<GameKeyDto> _allKeys = new();
    private bool _loadingGames;
    private CancellationTokenSource? _activeCts;

    public KeyGeneratorForm(
        IKeyService keyService,
        IGameService gameService,
        ICsvKeyImporter importer,
        ICsvKeyExporter exporter,
        CurrentUserContext currentUser)
    {
        _keyService = keyService;
        _gameService = gameService;
        _importer = importer;
        _exporter = exporter;
        _currentUser = currentUser;

        Text = "Sinh key";
        _btnAdd.Text = "Sinh key ngẫu nhiên";
        _btnEdit.Text = "Import CSV";
        _btnDelete.Text = "Xem QR";

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

        _btnCancelImport = new ToolStripButton("Hủy Import")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Visible = false
        };
        _btnCancelImport.Click += (s, e) => _activeCts?.Cancel();

        _progressBar = new ToolStripProgressBar
        {
            Width = 200,
            Style = ProgressBarStyle.Continuous,
            Visible = false
        };

        _btnExport = new ToolStripButton("Export CSV")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text
        };
        _btnExport.Click += async (s, e) => await ExportCsvAsync();

        // Chèn vào ToolStrip: Game combo + nút Export + Hủy + ProgressBar
        _toolStrip.Items.Insert(0, new ToolStripLabel("Game:"));
        _toolStrip.Items.Insert(1, _cboGames);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(_btnExport);
        _toolStrip.Items.Add(_btnCancelImport);
        _toolStrip.Items.Add(_progressBar);

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
            MessageBox.Show("Vui lòng chọn 1 game.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int count = PromptKeyCount();
        if (count <= 0) return;

        await this.RunSafeAsync(async () =>
        {
            var result = await _keyService.GenerateKeysAsync(game.Id, count);
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Sinh key thất bại.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show($"Đã sinh {result.Data!.Count} key cho game '{game.Title}'.",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await OnRefreshClickedAsync();
        });
    }

    protected override async void OnEditClicked()
    {
        var game = GetSelectedGame();
        if (game is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game trước khi import.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Chọn file CSV chứa danh sách key",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            FilterIndex = 1,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        await ImportCsvAsync(game.Id, dialog.FileName);
    }

    protected override async void OnDeleteClicked()
    {
        var selected = GetSelectedKey();
        if (selected is null)
        {
            MessageBox.Show("Vui lòng chọn 1 key để xem QR.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        await this.RunSafeAsync(async () =>
        {
            var qrBytes = await _keyService.GenerateQrCodeAsync(selected.Id);
            ShowQrDialog(selected.Key, qrBytes);
        });
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            var game = GetSelectedGame();
            if (game is null)
            {
                _allKeys = new List<GameKeyDto>();
                BindToGrid(_allKeys);
                SetCountLabel(0);
                return;
            }

            var keys = await _keyService.GetKeysByGameAsync(game.Id);
            _allKeys = keys.ToList();
            BindToGrid(_allKeys);
            SetCountLabel(_allKeys.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindToGrid(_allKeys);
            SetCountLabel(_allKeys.Count);
            return;
        }

        var filtered = _allKeys.Where(k =>
            k.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            k.Status.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        BindToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private async Task ImportCsvAsync(int gameId, string filePath)
    {
        _activeCts = new CancellationTokenSource();
        var cts = _activeCts;

        SetUiBusy(true);

        try
        {
            var progress = new Progress<int>(v =>
            {
                if (IsDisposed) return;
                _progressBar.Value = Math.Clamp(v, 0, 100);
                _lblCount.Text = $"Đang import... {v}%";
            });

            var result = await Task.Run(async () =>
            {
                return await _importer.ImportAsync(gameId, filePath, progress, cts.Token);
            }, cts.Token);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Import thất bại.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                $"Import xong!\n\nĐã thêm: {result.Data} key",
                "Kết quả Import", MessageBoxButtons.OK, MessageBoxIcon.Information);

            await OnRefreshClickedAsync();
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Đã hủy import.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            SetCountLabel(_allKeys.Count);
        }
        finally
        {
            SetUiBusy(false);
            _activeCts?.Dispose();
            _activeCts = null;
        }
    }

    private async Task ExportCsvAsync()
    {
        var game = GetSelectedGame();
        if (game is null)
        {
            MessageBox.Show("Vui lòng chọn 1 game trước khi export.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Lưu danh sách key ra CSV",
            Filter = "CSV Files (*.csv)|*.csv",
            FileName = $"{game.Title}_keys_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _activeCts = new CancellationTokenSource();
        var cts = _activeCts;
        SetUiBusy(true);

        try
        {
            var progress = new Progress<int>(v =>
            {
                if (IsDisposed) return;
                _progressBar.Value = Math.Clamp(v, 0, 100);
            });

            var result = await Task.Run(async () =>
            {
                return await _exporter.ExportAsync(game.Id, dialog.FileName, null, progress, cts.Token);
            }, cts.Token);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Export thất bại.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                $"Export xong!\n\nĐã ghi {result.Data} key vào:\n{dialog.FileName}",
                "Kết quả Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Đã hủy export.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            SetUiBusy(false);
            _activeCts?.Dispose();
            _activeCts = null;
        }
    }

    private void SetUiBusy(bool busy)
    {
        foreach (ToolStripItem item in _toolStrip.Items)
        {
            if (item == _btnCancelImport || item == _progressBar) continue;
            item.Enabled = !busy;
        }
        _btnCancelImport.Visible = busy;
        _btnCancelImport.Enabled = busy;
        _progressBar.Visible = busy;
        if (busy) _progressBar.Value = 0;
    }

    private int PromptKeyCount()
    {
        using var dialog = new Form
        {
            Text = "Sinh key ngẫu nhiên",
            ClientSize = new Size(320, 130),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9F)
        };

        var lbl = new Label
        {
            Text = "Số lượng key (1 - 1000):",
            Location = new Point(20, 20),
            Size = new Size(180, 25)
        };
        var nud = new NumericUpDown
        {
            Location = new Point(210, 18),
            Size = new Size(90, 25),
            Minimum = 1,
            Maximum = 1000,
            Value = 100
        };
        var btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(130, 80),
            Size = new Size(80, 30)
        };
        var btnCancel = new Button
        {
            Text = "Hủy",
            DialogResult = DialogResult.Cancel,
            Location = new Point(220, 80),
            Size = new Size(80, 30)
        };

        dialog.Controls.AddRange(new Control[] { lbl, nud, btnOk, btnCancel });
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        return dialog.ShowDialog(this) == DialogResult.OK ? (int)nud.Value : 0;
    }

    private void ShowQrDialog(string keyValue, byte[] qrBytes)
    {
        using var dialog = new Form
        {
            Text = "QR Code",
            ClientSize = new Size(360, 420),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9F)
        };

        using var ms = new MemoryStream(qrBytes);
        var pic = new PictureBox
        {
            Image = Image.FromStream(ms),
            SizeMode = PictureBoxSizeMode.Zoom,
            Location = new Point(30, 20),
            Size = new Size(300, 300),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblKey = new Label
        {
            Text = keyValue,
            Location = new Point(30, 330),
            Size = new Size(300, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Consolas", 11F, FontStyle.Bold)
        };

        var btnClose = new Button
        {
            Text = "Đóng",
            DialogResult = DialogResult.OK,
            Location = new Point(140, 380),
            Size = new Size(80, 30)
        };

        dialog.Controls.AddRange(new Control[] { pic, lblKey, btnClose });
        dialog.AcceptButton = btnClose;
        dialog.CancelButton = btnClose;
        dialog.ShowDialog(this);
    }

    private void BindToGrid(List<GameKeyDto> keys)
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
            HeaderText = "Key",
            DataPropertyName = "Key",
            FillWeight = 40,
            DefaultCellStyle = new DataGridViewCellStyle { Font = new Font("Consolas", 9F) }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Trạng thái",
            DataPropertyName = "Status",
            FillWeight = 15
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Kích hoạt lúc",
            DataPropertyName = "ActivatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.DataSource = new SortableBindingList<GameKeyDto>(keys);
    }

    private GameDto? GetSelectedGame()
        => _cboGames.ComboBox.SelectedItem as GameDto;

    private GameKeyDto? GetSelectedKey()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        var idCell = _grid.SelectedRows[0].Cells["colId"].Value;
        if (idCell is null) return null;
        var id = (int)idCell;
        return _allKeys.FirstOrDefault(k => k.Id == id);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _activeCts?.Cancel();
            _activeCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}

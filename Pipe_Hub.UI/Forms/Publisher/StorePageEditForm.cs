using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Dialog chỉnh sửa trang game: mô tả dài + ảnh bìa.
/// Ảnh bìa có thể chọn bằng hộp thoại hoặc kéo-thả từ File Explorer.
/// </summary>
public class StorePageEditForm : Form
{
    private static readonly string[] AllowedExtensions =
    {
        ".jpg", ".jpeg", ".png", ".bmp", ".gif"
    };

    private readonly IGameService _gameService;
    private readonly GameDto _existingGame;

    private string? _selectedImagePath;

    private Label _lblGameTitle = null!;
    private TextBox _txtDescription = null!;
    private PictureBox _picCover = null!;
    private Label _lblImagePath = null!;
    private Button _btnBrowse = null!;
    private Button _btnClearImage = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public StorePageEditForm(IGameService gameService, GameDto existingGame)
    {
        _gameService = gameService;
        _existingGame = existingGame;

        InitializeComponents();
        LoadExistingData();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = "Pipe Hub — Trang game";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        var lblHeader = new Label
        {
            Text = "SỬA TRANG GAME",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(640, 32)
        };

        var controls = new List<Control> { lblHeader };

        // Tên game (chỉ đọc)
        controls.Add(new Label
        {
            Text = "Tên game:",
            Location = new Point(30, 58),
            Size = new Size(110, 25)
        });
        _lblGameTitle = new Label
        {
            Text = _existingGame.Title,
            Location = new Point(145, 58),
            Size = new Size(465, 25),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        controls.Add(_lblGameTitle);

        // Mô tả chi tiết (cột trái)
        controls.Add(new Label
        {
            Text = "Mô tả chi tiết:",
            Location = new Point(30, 96),
            Size = new Size(270, 22)
        });
        _txtDescription = new TextBox
        {
            Location = new Point(30, 118),
            Size = new Size(270, 150),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            MaxLength = 4000
        };
        controls.Add(_txtDescription);

        // Ảnh bìa (cột phải, hỗ trợ kéo-thả)
        controls.Add(new Label
        {
            Text = "Ảnh bìa:",
            Location = new Point(320, 96),
            Size = new Size(300, 22)
        });
        _picCover = new PictureBox
        {
            Location = new Point(320, 118),
            Size = new Size(300, 300),
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.LightGray,
            AllowDrop = true
        };
        _picCover.DragEnter += PicCover_DragEnter;
        _picCover.DragDrop += PicCover_DragDrop;
        controls.Add(_picCover);

        _lblImagePath = new Label
        {
            Text = "(chưa chọn)",
            Location = new Point(320, 422),
            Size = new Size(300, 20),
            ForeColor = Color.Gray,
            AutoEllipsis = true
        };
        controls.Add(_lblImagePath);

        // Nút chọn / xóa ảnh
        var pnlImageButtons = new Panel
        {
            Location = new Point(320, 446),
            Size = new Size(300, 34)
        };
        _btnBrowse = new Button
        {
            Text = "Chọn ảnh...",
            Location = new Point(0, 1),
            Size = new Size(145, 32)
        };
        _btnBrowse.Click += BtnBrowse_Click;

        _btnClearImage = new Button
        {
            Text = "Xóa ảnh",
            Location = new Point(155, 1),
            Size = new Size(145, 32)
        };
        _btnClearImage.Click += BtnClearImage_Click;

        pnlImageButtons.Controls.Add(_btnBrowse);
        pnlImageButtons.Controls.Add(_btnClearImage);
        controls.Add(pnlImageButtons);

        // Nút Lưu / Hủy
        _btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(195, 492),
            Size = new Size(120, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(325, 492),
            Size = new Size(120, 32)
        };
        _btnCancel.Click += (s, e) => Close();

        controls.Add(_btnSave);
        controls.Add(_btnCancel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        ClientSize = new Size(640, 560);
        Controls.AddRange(controls.ToArray());
    }

    private void LoadExistingData()
    {
        _txtDescription.Text = _existingGame.Description ?? string.Empty;

        var coverPath = _existingGame.CoverImagePath;
        if (string.IsNullOrWhiteSpace(coverPath))
        {
            _lblImagePath.Text = "(chưa chọn)";
            return;
        }

        _selectedImagePath = coverPath;
        _lblImagePath.Text = Path.GetFileName(coverPath);

        if (File.Exists(coverPath))
        {
            LoadImagePreview(coverPath);
        }
    }

    private static bool IsValidImageFile(string path)
    {
        if (!File.Exists(path)) return false;

        var ext = Path.GetExtension(path);
        return AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    private void LoadImagePreview(string path)
    {
        try
        {
            _picCover.Image?.Dispose();
            _picCover.Image = Image.FromFile(path);
        }
        catch
        {
            _picCover.Image = null;
            MessageBox.Show("Không thể đọc file ảnh đã chọn.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetSelectedImage(string path)
    {
        _selectedImagePath = path;
        LoadImagePreview(path);
        _lblImagePath.Text = Path.GetFileName(path);
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh bìa",
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*",
            FilterIndex = 1,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        SetSelectedImage(dialog.FileName);
    }

    private void BtnClearImage_Click(object? sender, EventArgs e)
    {
        _selectedImagePath = null;
        _picCover.Image?.Dispose();
        _picCover.Image = null;
        _lblImagePath.Text = "(chưa chọn)";
    }

    private void PicCover_DragEnter(object? sender, DragEventArgs e)
    {
        var files = e.Data?.GetData(DataFormats.FileDrop) as string[];
        e.Effect = files is { Length: > 0 } && IsValidImageFile(files[0])
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void PicCover_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return;
        }

        if (!IsValidImageFile(files[0]))
        {
            MessageBox.Show("Chỉ hỗ trợ ảnh JPG/PNG/BMP/GIF.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetSelectedImage(files[0]);
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_selectedImagePath) && !File.Exists(_selectedImagePath))
        {
            MessageBox.Show("File ảnh không tồn tại.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var description = _txtDescription.Text;
        var coverImagePath = _selectedImagePath;

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                var result = await _gameService.UpdateStorePageAsync(
                    _existingGame.Id, description, coverImagePath);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Cập nhật trang game thất bại.", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            finally
            {
                _btnSave.Enabled = true;
                _btnSave.Text = "Lưu";
            }
        });
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Cho phép đóng form dù validation đang fail
        AutoValidate = AutoValidate.Disable;
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Giải phóng GDI handle của ảnh preview
            _picCover?.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}

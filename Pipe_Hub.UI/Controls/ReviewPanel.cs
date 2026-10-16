using Pipe_Hub.BLL.Common;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;
using Serilog;

namespace Pipe_Hub.UI.Controls;

/// <summary>
/// UserControl hiển thị + CRUD review cho 1 game.
/// Ctor rỗng cho Designer — gọi LoadAsync(service, gameId) để khởi tạo runtime.
/// </summary>
public class ReviewPanel : UserControl
{
    private IReviewService? _reviewService;
    private int _gameId;
    private bool _hasPurchased;
    private ReviewDto? _myReview;

    private Label _lblHeader = null!;
    private Label _lblAverage = null!;
    private DataGridView _grid = null!;
    private Button _btnAddEdit = null!;
    private Button _btnDelete = null!;
    private Label _lblStatus = null!;

    public ReviewPanel()
    {
        InitializeComponents();
    }

    public async Task LoadAsync(IReviewService reviewService, int gameId)
    {
        _reviewService = reviewService;
        _gameId = gameId;
        await RefreshAsync();
    }

    private void InitializeComponents()
    {
        Dock = DockStyle.Fill;
        Padding = new Padding(10);
        BackColor = Color.White;

        _lblHeader = new Label
        {
            Text = "Đánh giá",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            Dock = DockStyle.Top,
            Height = 25
        };

        _lblAverage = new Label
        {
            Text = "Chưa có đánh giá",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.Gray,
            Dock = DockStyle.Top,
            Height = 20
        };

        var pnlButtons = new Panel { Dock = DockStyle.Top, Height = 35 };

        _btnAddEdit = new Button
        {
            Text = "Thêm đánh giá",
            Size = new Size(120, 28),
            Location = new Point(0, 3),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnAddEdit.Click += BtnAddEdit_Click;

        _btnDelete = new Button
        {
            Text = "Xóa",
            Size = new Size(80, 28),
            Location = new Point(130, 3),
            Visible = false
        };
        _btnDelete.Click += BtnDelete_Click;

        pnlButtons.Controls.AddRange(new Control[] { _btnAddEdit, _btnDelete });

        _grid = new DataGridView
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

        // Custom cell format cho rating → hiển thị sao (đăng ký 1 lần duy nhất)
        _grid.CellFormatting += (s, e) =>
        {
            if (e.ColumnIndex == 1 && e.Value is int rating)
            {
                e.Value = new string('★', rating) + new string('☆', 5 - rating);
                e.FormattingApplied = true;
            }
        };

        _lblStatus = new Label
        {
            Text = "Đang tải...",
            Dock = DockStyle.Bottom,
            Height = 20,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8F)
        };

        Controls.Add(_grid);
        Controls.Add(_lblStatus);
        Controls.Add(pnlButtons);
        Controls.Add(_lblAverage);
        Controls.Add(_lblHeader);
    }

    public async Task RefreshAsync()
    {
        if (_reviewService is null) return;

        try
        {
            var reviews = await _reviewService.GetByGameAsync(_gameId);
            var avg = await _reviewService.GetAverageRatingAsync(_gameId);
            _myReview = await _reviewService.GetMyReviewForGameAsync(_gameId);

            BindGrid(reviews);

            if (reviews.Count == 0)
                _lblAverage.Text = "Chưa có đánh giá";
            else
                _lblAverage.Text = $"★ {avg:F1}/5 ({reviews.Count} đánh giá)";

            UpdateButtons();
            _lblStatus.Text = $"{reviews.Count} đánh giá";
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ReviewPanel.RefreshAsync error");
            MessageBox.Show("Không thể tải đánh giá.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindGrid(IReadOnlyList<ReviewDto> reviews)
    {
        _grid.DataSource = null;
        _grid.Columns.Clear();
        _grid.AutoGenerateColumns = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Người đánh giá",
            DataPropertyName = "Username",
            FillWeight = 20
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Sao",
            DataPropertyName = "Rating",
            FillWeight = 10,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            CellTemplate = new DataGridViewTextBoxCell()
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Nội dung",
            DataPropertyName = "Content",
            FillWeight = 50
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày",
            DataPropertyName = "CreatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.DataSource = reviews.ToList();
    }

    private void UpdateButtons()
    {
        if (!_hasPurchased)
        {
            _btnAddEdit.Enabled = false;
            _btnAddEdit.Text = "Cần mua game";
            _btnDelete.Visible = false;
            return;
        }

        _btnAddEdit.Enabled = true;

        if (_myReview is null)
        {
            _btnAddEdit.Text = "Thêm đánh giá";
            _btnDelete.Visible = false;
        }
        else
        {
            _btnAddEdit.Text = "Sửa đánh giá";
            _btnDelete.Visible = true;
        }
    }

    public void SetPurchased(bool purchased)
    {
        _hasPurchased = purchased;
        UpdateButtons();
    }

    private async void BtnAddEdit_Click(object? sender, EventArgs e)
    {
        if (_reviewService is null) return;

        var dialog = BuildReviewDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var rating = (int)dialog.Controls["numRating"]!.Tag!;
        var content = (string)dialog.Controls["txtContent"]!.Tag!;

        try
        {
            Result<ReviewDto> result;

            if (_myReview is null)
            {
                result = await _reviewService.CreateAsync(_gameId, rating, content);
            }
            else
            {
                result = await _reviewService.UpdateAsync(_myReview.Id, rating, content);
            }

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Không thể lưu đánh giá.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show("Lưu đánh giá thành công!", "Thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshAsync();
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (_reviewService is null || _myReview is null) return;

        var confirm = MessageBox.Show(
            "Bạn có chắc muốn xóa đánh giá này?",
            "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        try
        {
            var result = await _reviewService.SoftDeleteAsync(_myReview.Id);
            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Không thể xóa.",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            MessageBox.Show("Đã xóa đánh giá.", "Thành công",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            await RefreshAsync();
        }
        catch (AppException ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private Form BuildReviewDialog()
    {
        var dialog = new Form
        {
            Text = _myReview is null ? "Thêm đánh giá" : "Sửa đánh giá",
            ClientSize = new Size(400, 280),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            Font = new Font("Segoe UI", 9F)
        };

        var lblRating = new Label
        {
            Text = "Đánh giá (1-5 sao):",
            Location = new Point(20, 20),
            Size = new Size(150, 25)
        };

        var numRating = new NumericUpDown
        {
            Name = "numRating",
            Location = new Point(180, 20),
            Size = new Size(80, 25),
            Minimum = 1,
            Maximum = 5,
            Value = _myReview?.Rating ?? 5,
            Tag = _myReview?.Rating ?? 5
        };
        numRating.ValueChanged += (s, e) => numRating.Tag = (int)numRating.Value;

        var lblContent = new Label
        {
            Text = "Nội dung:",
            Location = new Point(20, 60),
            Size = new Size(150, 25)
        };

        var txtContent = new TextBox
        {
            Name = "txtContent",
            Location = new Point(20, 85),
            Size = new Size(340, 120),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            MaxLength = 2000,
            Text = _myReview?.Content ?? string.Empty,
            Tag = _myReview?.Content ?? string.Empty
        };
        txtContent.TextChanged += (s, e) => txtContent.Tag = txtContent.Text;

        var btnOk = new Button
        {
            Text = "Lưu",
            Location = new Point(200, 215),
            Size = new Size(80, 30),
            DialogResult = DialogResult.OK
        };

        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(290, 215),
            Size = new Size(70, 30),
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.AddRange(new Control[] {
            lblRating, numRating, lblContent, txtContent, btnOk, btnCancel
        });
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;

        return dialog;
    }
}

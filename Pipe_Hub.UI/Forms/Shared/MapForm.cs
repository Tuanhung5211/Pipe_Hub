using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Shared;

/// <summary>
/// Bản đồ cộng đồng (GDI+): vẽ vị trí Player dựa trên Latitude/Longitude.
/// </summary>
public class MapForm : Form
{
    private readonly IUserService _userService;

    private ToolStrip _toolStrip = null!;
    private ToolStripButton _btnRefresh = null!;
    private Panel _mapPanel = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;
    private ToolTip _toolTip = null!;

    private List<PlayerLocationDto> _players = new();
    private readonly Dictionary<Point, PlayerLocationDto> _hitMap = new();

    public MapForm(IUserService userService)
    {
        _userService = userService;
        InitializeComponents();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Bản đồ cộng đồng";
        ClientSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        // ===== ToolStrip =====
        _toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(4) };
        _btnRefresh = new ToolStripButton("Làm mới") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnRefresh.Click += async (s, e) => await LoadDataAsync();
        _toolStrip.Items.Add(_btnRefresh);

        // ===== Map Panel =====
        _mapPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 30, 45) // Dark blue
        };
        _mapPanel.Paint += MapPanel_Paint;
        _mapPanel.MouseMove += MapPanel_MouseMove;

        // ===== ToolTip =====
        _toolTip = new ToolTip { InitialDelay = 100, ReshowDelay = 50 };

        // ===== StatusStrip =====
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_lblStatus);

        Controls.Add(_mapPanel);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        this.RegisterShortcut(Keys.F5, async () => await LoadDataAsync());
    }

    private async Task LoadDataAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            _btnRefresh.Enabled = false;
            _lblStatus.Text = "Đang tải dữ liệu...";

            try
            {
                _players = (await _userService.GetPlayersWithLocationAsync()).ToList();
                _lblStatus.Text = $"Đã tải {_players.Count} người chơi.";
                _mapPanel.Invalidate(); // Vẽ lại
            }
            finally
            {
                _btnRefresh.Enabled = true;
            }
        });
    }

    private void MapPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int width = _mapPanel.Width;
        int height = _mapPanel.Height;
        if (width <= 0 || height <= 0) return;

        // 1. Vẽ lưới kinh độ/vĩ độ (mỗi 30 độ)
        using var gridPen = new Pen(Color.FromArgb(60, 100, 100, 100), 1);
        // Kinh độ (-180 -> 180)
        for (int lon = -180; lon <= 180; lon += 30)
        {
            float x = (lon + 180f) / 360f * width;
            g.DrawLine(gridPen, x, 0, x, height);
        }
        // Vĩ độ (-90 -> 90)
        for (int lat = -90; lat <= 90; lat += 30)
        {
            float y = (90f - lat) / 180f * height;
            g.DrawLine(gridPen, 0, y, width, y);
        }

        // 2. Vẽ các chấm đỏ cho Player
        _hitMap.Clear();
        using var playerBrush = new SolidBrush(Color.FromArgb(255, 235, 59, 90)); // Red
        using var borderPen = new Pen(Color.White, 1);

        foreach (var p in _players)
        {
            // Công thức quy đổi toạ độ ra pixel
            float x = (float)((p.Longitude + 180.0) / 360.0 * width);
            float y = (float)((90.0 - p.Latitude) / 180.0 * height);

            // Vẽ chấm tròn (bán kính 6px)
            g.FillEllipse(playerBrush, x - 6, y - 6, 12, 12);
            g.DrawEllipse(borderPen, x - 6, y - 6, 12, 12);

            // Lưu vào hit map để xử lý hover
            _hitMap[new Point((int)x, (int)y)] = p;
        }
    }

    private void MapPanel_MouseMove(object? sender, MouseEventArgs e)
    {
        foreach (var kvp in _hitMap)
        {
            var center = kvp.Key;
            var rect = new Rectangle(center.X - 6, center.Y - 6, 12, 12);

            if (rect.Contains(e.Location))
            {
                var p = kvp.Value;
                _toolTip.SetToolTip(_mapPanel, $"{p.FullName} ({p.Username})\nQuốc gia: {p.Country ?? "N/A"}");
                return;
            }
        }
        _toolTip.SetToolTip(_mapPanel, string.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _toolTip?.Dispose();
        }
        base.Dispose(disposing);
    }
}

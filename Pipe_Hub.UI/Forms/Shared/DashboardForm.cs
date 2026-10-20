using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;
using SkiaSharp;

namespace Pipe_Hub.UI.Forms.Shared;

/// <summary>
/// Dashboard hiển thị 3 biểu đồ (Ch.6): doanh thu 12 tháng (Bar), CCU 7 ngày (Line), 
/// phân bố game theo thể loại (Pie). Dùng LiveCharts2.
/// </summary>
public class DashboardForm : Form
{
    private readonly IReportService _reportService;

    private ToolStrip _toolStrip = null!;
    private ToolStripButton _btnRefresh = null!;
    private ToolStripLabel _lblLastUpdate = null!;
    private CartesianChart _chartRevenue = null!;
    private CartesianChart _chartCcu = null!;
    private PieChart _chartGenre = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    public DashboardForm(IReportService reportService)
    {
        _reportService = reportService;
        InitializeComponents();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "Dashboard — Tổng quan";
        ClientSize = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        // ===== ToolStrip =====
        _toolStrip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(4) };
        _btnRefresh = new ToolStripButton("Làm mới") { DisplayStyle = ToolStripItemDisplayStyle.Text };
        _btnRefresh.Click += async (s, e) => await LoadDataAsync();
        _lblLastUpdate = new ToolStripLabel("(chưa tải)") { ForeColor = Color.Gray };
        _toolStrip.Items.Add(_btnRefresh);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(_lblLastUpdate);

        // ===== Layout 2x2 =====
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        // Revenue chart (top-left)
        var pnlRevenue = BuildChartPanel("Doanh thu theo tháng");
        _chartRevenue = new CartesianChart { Dock = DockStyle.Fill };
        pnlRevenue.Controls.Add(_chartRevenue);

        // CCU chart (top-right)
        var pnlCcu = BuildChartPanel("Số người chơi (CCU) 7 ngày");
        _chartCcu = new CartesianChart { Dock = DockStyle.Fill };
        pnlCcu.Controls.Add(_chartCcu);

        // Genre chart (bottom, span 2)
        var pnlGenre = BuildChartPanel("Số game theo thể loại");
        _chartGenre = new PieChart { Dock = DockStyle.Fill };
        pnlGenre.Controls.Add(_chartGenre);

        layout.Controls.Add(pnlRevenue, 0, 0);
        layout.Controls.Add(pnlCcu, 1, 0);
        layout.Controls.Add(pnlGenre, 0, 1);
        layout.SetColumnSpan(pnlGenre, 2);

        // ===== StatusStrip =====
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_lblStatus);

        Controls.Add(layout);
        Controls.Add(_toolStrip);
        Controls.Add(_statusStrip);

        this.RegisterShortcut(Keys.F5, async () => await LoadDataAsync());
    }

    private Panel BuildChartPanel(string title)
    {
        var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5), BackColor = Color.White };
        var lbl = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 25,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215)
        };
        pnl.Controls.Add(lbl);
        return pnl;
    }

    private async Task LoadDataAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            _btnRefresh.Enabled = false;
            _lblStatus.Text = "Đang tải dữ liệu...";

            try
            {
                var revenue = await _reportService.GetRevenueByMonthAsync(12);
                var ccu = await _reportService.GetCcuByDayAsync(7);
                var genres = await _reportService.GetGameCountByGenreAsync();

                BindRevenueChart(revenue);
                BindCcuChart(ccu);
                BindGenreChart(genres);

                _lblLastUpdate.Text = $"Cập nhật: {DateTime.Now:HH:mm:ss dd/MM/yyyy}";
                _lblStatus.Text = $"Đã tải: {revenue.Count} tháng | {ccu.Count} ngày | {genres.Count} thể loại";
            }
            finally
            {
                _btnRefresh.Enabled = true;
            }
        });
    }

    private void BindRevenueChart(IReadOnlyList<RevenueByMonthDto> data)
    {
        if (data.Count == 0)
        {
            _chartRevenue.Series = Array.Empty<ISeries>();
            return;
        }

        _chartRevenue.Series = new ISeries[]
        {
            new ColumnSeries<decimal>
            {
                Values = data.Select(d => d.TotalRevenue).ToArray(),
                Name = "Doanh thu (đ)",
                DataLabelsPaint = new SolidColorPaint(SKColors.DarkGreen),
                DataLabelsPosition = DataLabelsPosition.Top,
                DataLabelsSize = 10
            }
        };

        _chartRevenue.XAxes = new[]
        {
            new Axis { Labels = data.Select(d => d.Label).ToArray() }
        };

        _chartRevenue.YAxes = new[]
        {
            new Axis { Labeler = value => value.ToString("N0"), MinLimit = 0 }
        };
    }

    private void BindCcuChart(IReadOnlyList<CcuByDayDto> data)
    {
        if (data.Count == 0)
        {
            _chartCcu.Series = Array.Empty<ISeries>();
            return;
        }

        _chartCcu.Series = new ISeries[]
        {
            new LineSeries<int>
            {
                Values = data.Select(d => d.UniquePlayers).ToArray(),
                Name = "Người chơi",
                GeometrySize = 10,
                LineSmoothness = 0.5,
                Fill = new SolidColorPaint(SKColors.LightBlue.WithAlpha(60))
            }
        };

        _chartCcu.XAxes = new[]
        {
            new Axis { Labels = data.Select(d => d.Label).ToArray() }
        };

        _chartCcu.YAxes = new[]
        {
            new Axis { Labeler = value => value.ToString("N0"), MinLimit = 0 }
        };
    }

    private void BindGenreChart(IReadOnlyList<GameCountByGenreDto> data)
    {
        if (data.Count == 0)
        {
            _chartGenre.Series = Array.Empty<ISeries>();
            return;
        }

        var series = data.Select(d => new PieSeries<int>
        {
            Values = new[] { d.GameCount },
            Name = d.GenreName,
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 12,
            DataLabelsPosition = PolarLabelsPosition.Middle
        }).ToArray();

        _chartGenre.Series = series;
    }
}

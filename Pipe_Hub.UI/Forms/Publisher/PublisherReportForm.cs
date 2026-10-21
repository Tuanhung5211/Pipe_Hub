using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Reports; // namespace chứa ExcelReportBuilder/PdfReportBuilder
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Báo cáo doanh thu riêng cho Publisher — chỉ thấy OrderItem thuộc game của mình.
/// Doanh thu tính per OrderItem (Quantity × UnitPrice), không phải per Order.
/// </summary>
public class PublisherReportForm : Form
{
    private readonly IReportService _reportService;
    private readonly ExcelReportBuilder _excelBuilder;
    private readonly PdfReportBuilder _pdfBuilder;

    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Button _btnSearch = null!;
    private DataGridView _grid = null!;
    private Button _btnExportExcel = null!;
    private Button _btnExportPdf = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    private List<PublisherRevenueItemDto> _currentData = new();

    public PublisherReportForm(
        IReportService reportService,
        ExcelReportBuilder excelBuilder,
        PdfReportBuilder pdfBuilder)
    {
        _reportService = reportService;
        _excelBuilder = excelBuilder;
        _pdfBuilder = pdfBuilder;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Báo cáo doanh thu của tôi";
        ClientSize = new Size(1000, 600);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        // ===== Top Filter Panel =====
        var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };

        pnlFilter.Controls.Add(new Label { Text = "Từ ngày:", Location = new Point(10, 15), Size = new Size(60, 25) });
        _dtpFrom = new DateTimePicker
        {
            Location = new Point(75, 12),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddMonths(-1)
        };
        pnlFilter.Controls.Add(_dtpFrom);

        pnlFilter.Controls.Add(new Label { Text = "Đến ngày:", Location = new Point(210, 15), Size = new Size(65, 25) });
        _dtpTo = new DateTimePicker
        {
            Location = new Point(280, 12),
            Size = new Size(120, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };
        pnlFilter.Controls.Add(_dtpTo);

        _btnSearch = new Button
        {
            Text = "Tìm kiếm",
            Location = new Point(415, 10),
            Size = new Size(100, 30),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSearch.Click += async (s, e) => await LoadDataAsync();
        pnlFilter.Controls.Add(_btnSearch);

        // ===== Center Grid =====
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = Color.White
        };
        SetupGridColumns();

        // ===== Bottom Action Panel =====
        var pnlActions = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };

        _btnExportExcel = new Button
        {
            Text = "Xuất Excel",
            Location = new Point(10, 10),
            Size = new Size(120, 30),
            BackColor = Color.FromArgb(40, 167, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnExportExcel.Click += async (s, e) => await ExportExcelAsync();

        _btnExportPdf = new Button
        {
            Text = "Xuất PDF",
            Location = new Point(140, 10),
            Size = new Size(120, 30),
            BackColor = Color.FromArgb(220, 53, 69),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnExportPdf.Click += async (s, e) => await ExportPdfAsync();

        pnlActions.Controls.Add(_btnExportExcel);
        pnlActions.Controls.Add(_btnExportPdf);

        // ===== StatusStrip =====
        _statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
        _lblStatus = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        _statusStrip.Items.Add(_lblStatus);

        Controls.Add(_grid);
        Controls.Add(pnlActions);
        Controls.Add(pnlFilter);
        Controls.Add(_statusStrip);

        Load += async (s, e) => await LoadDataAsync();
    }

    private void SetupGridColumns()
    {
        _grid.AutoGenerateColumns = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mã đơn",
            DataPropertyName = "OrderCode",
            FillWeight = 15
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 15
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tên game",
            DataPropertyName = "GameTitle",
            FillWeight = 25
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Khách hàng",
            DataPropertyName = "BuyerUsername",
            FillWeight = 18
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "SL",
            DataPropertyName = "Quantity",
            FillWeight = 7,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Đơn giá",
            DataPropertyName = "UnitPrice",
            FillWeight = 10,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Thành tiền",
            DataPropertyName = "Subtotal",
            FillWeight = 10,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });
    }

    private async Task LoadDataAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            _btnSearch.Enabled = false;
            SetStatus("Đang tải...");
            try
            {
                var filter = new RevenueFilterDto
                {
                    FromDate = _dtpFrom.Value.Date.ToUniversalTime(),
                    ToDate = _dtpTo.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime() // Hết ngày (Local → UTC)
                };

                var data = await _reportService.GetRevenueDetailsForPublisherAsync(filter);
                _currentData = data.ToList();

                BindToGrid(_currentData);

                var total = _currentData.Sum(d => d.Subtotal);
                SetStatus($"Tìm thấy {_currentData.Count} dòng. Tổng doanh thu: {total:N0} đ");
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        });
    }

    private void BindToGrid(List<PublisherRevenueItemDto> data)
    {
        _grid.Rows.Clear();

        foreach (var item in data)
        {
            _grid.Rows.Add(
                item.OrderCode,
                item.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy"),
                item.GameTitle,
                item.BuyerUsername,
                item.Quantity,
                item.UnitPrice,
                item.Subtotal);
        }
    }

    private async Task ExportExcelAsync()
    {
        if (_currentData.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"BaoCaoDoanhThuPublisher_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        await this.RunSafeAsync(async () =>
        {
            SetStatus("Đang xuất Excel...");
            await _excelBuilder.BuildAsync(TransformToOrderDto(), sfd.FileName, "Báo cáo doanh thu Publisher");
            SetStatus("Xuất Excel thành công.");
            MessageBox.Show("Xuất file Excel thành công!", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task ExportPdfAsync()
    {
        if (_currentData.Count == 0)
        {
            MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"BaoCaoDoanhThuPublisher_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        await this.RunSafeAsync(async () =>
        {
            SetStatus("Đang xuất PDF...");
            await _pdfBuilder.BuildAsync(TransformToOrderDto(), sfd.FileName, "Báo cáo doanh thu Publisher");
            SetStatus("Xuất PDF thành công.");
            MessageBox.Show("Xuất file PDF thành công!", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    /// <summary>
    /// Tái sử dụng Excel/PDF builder (nhận OrderDto): mỗi dòng PublisherRevenueItemDto
    /// được đóng gói thành 1 OrderDto chỉ chứa đúng 1 OrderItem tương ứng.
    /// </summary>
    private List<OrderDto> TransformToOrderDto()
    {
        return _currentData.Select(p => new OrderDto
        {
            OrderCode = p.OrderCode,
            CreatedAt = p.CreatedAt,
            Username = p.BuyerUsername,
            TotalAmount = p.Subtotal,
            Status = "Completed",
            Items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    GameId = p.GameId,
                    GameTitle = p.GameTitle,
                    Quantity = p.Quantity,
                    UnitPrice = p.UnitPrice,
                    Subtotal = p.Subtotal
                }
            },
            GameKeys = new List<string>()
        }).ToList();
    }

    private void SetStatus(string text)
    {
        _lblStatus.Text = text;
    }
}

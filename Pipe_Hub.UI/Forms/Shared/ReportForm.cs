using System.Drawing.Printing;
using System.Text;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.BLL.Reports; // namespace chứa ExcelReportBuilder/PdfReportBuilder
using Pipe_Hub.DAL.AdoNet;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Shared;

public class ReportForm : Form
{
    private readonly IReportService _reportService;
    private readonly ExcelReportBuilder _excelBuilder;
    private readonly PdfReportBuilder _pdfBuilder;
    private readonly IRevenueAdoRepository _adoRepo;

    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private NumericUpDown _nudMinAmount = null!;
    private NumericUpDown _nudMaxAmount = null!;
    private Button _btnSearch = null!;
    private DataGridView _grid = null!;
    private Button _btnExportExcel = null!;
    private Button _btnExportPdf = null!;
    private Button _btnPrintPreview = null!;
    private Button _btnCompare = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _lblStatus = null!;

    private List<OrderDto> _currentData = new();

    public ReportForm(
        IReportService reportService,
        ExcelReportBuilder excelBuilder,
        PdfReportBuilder pdfBuilder,
        IRevenueAdoRepository adoRepo)
    {
        _reportService = reportService;
        _excelBuilder = excelBuilder;
        _pdfBuilder = pdfBuilder;
        _adoRepo = adoRepo;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Báo cáo doanh thu chi tiết";
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

        pnlFilter.Controls.Add(new Label { Text = "Giá từ:", Location = new Point(415, 15), Size = new Size(50, 25) });
        _nudMinAmount = new NumericUpDown
        {
            Location = new Point(465, 12),
            Size = new Size(100, 25),
            Maximum = 1_000_000_000,
            ThousandsSeparator = true
        };
        pnlFilter.Controls.Add(_nudMinAmount);

        pnlFilter.Controls.Add(new Label { Text = "Đến:", Location = new Point(575, 15), Size = new Size(40, 25) });
        _nudMaxAmount = new NumericUpDown
        {
            Location = new Point(615, 12),
            Size = new Size(100, 25),
            Maximum = 1_000_000_000,
            ThousandsSeparator = true
        };
        pnlFilter.Controls.Add(_nudMaxAmount);

        _btnSearch = new Button
        {
            Text = "Tìm kiếm",
            Location = new Point(730, 10),
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

        _btnPrintPreview = new Button
        {
            Text = "In / Print Preview",
            Location = new Point(270, 10),
            Size = new Size(140, 30)
        };
        _btnPrintPreview.Click += (s, e) => PrintPreview();

        _btnCompare = new Button
        {
            Text = "Đối chiếu EF vs ADO",
            Location = new Point(420, 10),
            Size = new Size(160, 30),
            BackColor = Color.FromArgb(255, 193, 7),
            ForeColor = Color.Black
        };
        _btnCompare.Click += BtnCompare_Click;

        pnlActions.Controls.Add(_btnExportExcel);
        pnlActions.Controls.Add(_btnExportPdf);
        pnlActions.Controls.Add(_btnPrintPreview);
        pnlActions.Controls.Add(_btnCompare);

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
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mã đơn", DataPropertyName = "OrderCode", FillWeight = 15 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ngày tạo", DataPropertyName = "CreatedAt", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Khách hàng", DataPropertyName = "Username", FillWeight = 20 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Tổng tiền", DataPropertyName = "TotalAmount", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Trạng thái", DataPropertyName = "Status", FillWeight = 15 });
    }

    private async Task LoadDataAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            _btnSearch.Enabled = false;
            _lblStatus.Text = "Đang tải...";
            try
            {
                var filter = new RevenueFilterDto
                {
                    FromDate = _dtpFrom.Value.Date.ToUniversalTime(),
                    ToDate = _dtpTo.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime(), // Hết ngày (Local → UTC)
                    MinAmount = _nudMinAmount.Value > 0 ? _nudMinAmount.Value : null,
                    MaxAmount = _nudMaxAmount.Value > 0 ? _nudMaxAmount.Value : null
                };

                var data = await _reportService.GetRevenueDetailsAsync(filter);
                _currentData = data.ToList();

                // Grid hiển thị giờ Local thay vì UTC
                var displayData = _currentData
                    .Select(o => o with { CreatedAt = o.CreatedAt.ToLocalTime() })
                    .ToList();
                _grid.DataSource = new SortableBindingList<OrderDto>(displayData);
                _lblStatus.Text = $"Tìm thấy {_currentData.Count} đơn hàng.";
            }
            finally
            {
                _btnSearch.Enabled = true;
            }
        });
    }

    private async Task ExportExcelAsync()
    {
        if (_currentData.Count == 0) { MessageBox.Show("Không có dữ liệu để xuất."); return; }

        using var sfd = new SaveFileDialog { Filter = "Excel Files|*.xlsx", FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd}.xlsx" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        await this.RunSafeAsync(async () =>
        {
            _lblStatus.Text = "Đang xuất Excel...";
            await _excelBuilder.BuildAsync(_currentData, sfd.FileName, "Báo cáo doanh thu chi tiết");
            _lblStatus.Text = "Xuất Excel thành công.";
            MessageBox.Show("Xuất file Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async Task ExportPdfAsync()
    {
        if (_currentData.Count == 0) { MessageBox.Show("Không có dữ liệu để xuất."); return; }

        using var sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd}.pdf" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        await this.RunSafeAsync(async () =>
        {
            _lblStatus.Text = "Đang xuất PDF...";
            await _pdfBuilder.BuildAsync(_currentData, sfd.FileName, "Báo cáo doanh thu chi tiết");
            _lblStatus.Text = "Xuất PDF thành công.";
            MessageBox.Show("Xuất file PDF thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private async void BtnCompare_Click(object? sender, EventArgs e)
    {
        await this.RunSafeAsync(async () =>
        {
            // 1. Lấy dữ liệu từ EF (qua IReportService)
            var efData = await _reportService.GetRevenueByMonthAsync(12);

            // 2. Lấy dữ liệu từ ADO (qua IRevenueAdoRepository)
            var adoData = await _adoRepo.GetRevenueByMonthAsync(12);
            adoData.Reverse(); // ADO trả về DESC, đảo lại để so sánh với EF (ASC)

            // 3. So sánh
            var sb = new StringBuilder();
            sb.AppendLine("KẾT QUẢ ĐỐI CHIẾU EF CORE vs ADO.NET");
            sb.AppendLine("=====================================");
            sb.AppendLine();

            if (efData.Count != adoData.Count)
            {
                sb.AppendLine($"[!] SỐ THÁNG KHÁC NHAU: EF = {efData.Count}, ADO = {adoData.Count}");
            }
            else
            {
                bool isMatch = true;
                for (int i = 0; i < efData.Count; i++)
                {
                    var ef = efData[i];
                    var ado = adoData[i];
                    if (ef.Year != ado.Year || ef.Month != ado.Month || ef.TotalRevenue != ado.TotalRevenue)
                    {
                        isMatch = false;
                        sb.AppendLine($"[X] Lệch tại {ef.Label}: EF = {ef.TotalRevenue:N0}, ADO = {ado.TotalRevenue:N0}");
                    }
                }

                if (isMatch)
                {
                    sb.AppendLine($"[OK] HOÀN TOÀN KHỚP ({efData.Count} tháng)");
                    sb.AppendLine($"  - Tổng EF: {efData.Sum(x => x.TotalRevenue):N0}");
                    sb.AppendLine($"  - Tổng ADO: {adoData.Sum(x => x.TotalRevenue):N0}");
                }
            }

            MessageBox.Show(sb.ToString(), "Đối chiếu EF vs ADO", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private void PrintPreview()
    {
        if (_currentData.Count == 0) { MessageBox.Show("Không có dữ liệu để in."); return; }

        int rowIndex = 0; // Con trỏ dòng đang vẽ, sống ngoài PrintPage
        int pageNumber = 0; // Số trang in hiện tại, reset mỗi lần bắt đầu in

        using var printDoc = new PrintDocument();

        printDoc.BeginPrint += (_, _) =>
        {
            rowIndex = 0;
            pageNumber = 0;
        };

        printDoc.PrintPage += (s, e) =>
        {
            pageNumber++;

            var g = e.Graphics;
            if (g is null) { e.HasMorePages = false; return; }

            using var fontTitle = new Font("Arial", 16, FontStyle.Bold);
            using var fontHeader = new Font("Arial", 10, FontStyle.Bold);
            using var fontBody = new Font("Arial", 10);

            float y = 50;

            // ==== HEADER: vẽ ở mọi trang ====
            g.DrawString("BÁO CÁO DOANH THU CHI TIẾT", fontTitle, Brushes.Black, 150, y);
            y += 40;
            g.DrawString($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}  —  Trang {pageNumber}",
                fontBody, Brushes.Black, 50, y);
            y += 30;

            // Header cột
            g.DrawString("Mã đơn", fontHeader, Brushes.Black, 50, y);
            g.DrawString("Khách hàng", fontHeader, Brushes.Black, 200, y);
            g.DrawString("Ngày tạo", fontHeader, Brushes.Black, 350, y);
            g.DrawString("Tổng tiền", fontHeader, Brushes.Black, 500, y);
            y += 25;
            g.DrawLine(Pens.Black, 50, y, 600, y);
            y += 10;

            // ==== BODY: bắt đầu từ rowIndex ====
            while (rowIndex < _currentData.Count)
            {
                var item = _currentData[rowIndex];

                g.DrawString(item.OrderCode, fontBody, Brushes.Black, 50, y);
                g.DrawString(item.Username, fontBody, Brushes.Black, 200, y);
                g.DrawString(item.CreatedAt.ToString("dd/MM/yyyy"), fontBody, Brushes.Black, 350, y);
                g.DrawString(item.TotalAmount.ToString("N0"), fontBody, Brushes.Black, 500, y);
                y += 25;
                rowIndex++;

                if (y > e.PageBounds.Height - 60)
                {
                    e.HasMorePages = true;
                    return; // using sẽ dispose font tự động
                }
            }

            e.HasMorePages = false;
        };

        using var preview = new PrintPreviewDialog { Document = printDoc, Width = 800, Height = 600 };
        preview.ShowDialog(this);
    }
}

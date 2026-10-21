using System.Diagnostics;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Publisher mua gói dịch vụ Marketing và thanh toán qua VNPay Sandbox.
/// Form chỉ gọi REST API (giả lập) — không lưu giao dịch vào DB.
/// </summary>
public class MarketingPaymentForm : Form
{
    private static readonly PackageItem[] Packages =
    {
        new("Gói Cơ Bản", 500_000m),
        new("Gói Tiêu Chuẩn", 1_000_000m),
        new("Gói Cao Cấp", 2_000_000m)
    };

    private readonly IPaymentService _paymentService;

    private ComboBox _cboPackages = null!;
    private Label _lblPrice = null!;
    private Button _btnPay = null!;
    private Button _btnSimulateCallback = null!;
    private Label _lblStatus = null!;

    public MarketingPaymentForm(IPaymentService paymentService)
    {
        _paymentService = paymentService;
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Thanh toán Marketing";
        ClientSize = new Size(600, 400);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9F);

        // ===== Tiêu đề =====
        var lblTitle = new Label
        {
            Text = "THANH TOÁN DỊCH VỤ MARKETING",
            Location = new Point(20, 20),
            AutoSize = true,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 90, 158)
        };

        // ===== Chọn gói =====
        var lblPackage = new Label
        {
            Text = "Chọn gói quảng cáo:",
            Location = new Point(20, 75),
            AutoSize = true
        };

        _cboPackages = new ComboBox
        {
            Location = new Point(20, 100),
            Size = new Size(540, 25),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 10F)
        };
        _cboPackages.Items.AddRange(Packages);

        // ===== Giá tiền =====
        _lblPrice = new Label
        {
            Location = new Point(20, 145),
            AutoSize = true,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(192, 0, 0)
        };

        _cboPackages.SelectedIndexChanged += CboPackages_SelectedIndexChanged;
        _cboPackages.SelectedIndex = 0;

        // ===== Nút thanh toán =====
        _btnPay = new Button
        {
            Text = "Thanh toán qua VNPay",
            Location = new Point(20, 195),
            Size = new Size(250, 50),
            BackColor = Color.FromArgb(34, 139, 34),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _btnPay.Click += BtnPay_Click;

        // ===== Nút giả lập callback =====
        _btnSimulateCallback = new Button
        {
            Text = "Giả lập Callback thành công",
            Location = new Point(290, 195),
            Size = new Size(270, 50),
            BackColor = Color.Gray,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F),
            Visible = false
        };
        _btnSimulateCallback.Click += BtnSimulateCallback_Click;

        // ===== Trạng thái =====
        _lblStatus = new Label
        {
            Text = "Sẵn sàng",
            Location = new Point(20, 275),
            Size = new Size(560, 40),
            ForeColor = Color.DimGray
        };

        Controls.Add(lblTitle);
        Controls.Add(lblPackage);
        Controls.Add(_cboPackages);
        Controls.Add(_lblPrice);
        Controls.Add(_btnPay);
        Controls.Add(_btnSimulateCallback);
        Controls.Add(_lblStatus);
    }

    private PackageItem? SelectedPackage => _cboPackages.SelectedItem as PackageItem;

    private void CboPackages_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (SelectedPackage is { } package)
        {
            _lblPrice.Text = $"Giá: {package.Price:N0} đ";
        }
    }

    private async void BtnPay_Click(object? sender, EventArgs e)
    {
        var package = SelectedPackage;
        if (package is null)
        {
            return;
        }

        _btnPay.Enabled = false;
        _lblStatus.Text = "Đang kết nối VNPay...";

        try
        {
            await this.RunSafeAsync(async () =>
            {
                var result = await _paymentService.CreateMarketingPaymentUrlAsync(
                    package.Name, package.Price);

                if (!result.IsSuccess || string.IsNullOrEmpty(result.Data))
                {
                    _lblStatus.Text = result.ErrorMessage ?? "Không thể tạo URL thanh toán.";
                    MessageBox.Show(this, _lblStatus.Text, "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var url = result.Data;

                _btnSimulateCallback.Visible = true;
                _lblStatus.Text = "Đã tạo URL, vui lòng thanh toán trên trình duyệt.";

                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception)
                {
                    // Không mở được trình duyệt → vẫn cho user thấy URL để tự mở.
                    MessageBox.Show(this, url, "URL thanh toán",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
        }
        finally
        {
            _btnPay.Enabled = true;
        }
    }

    private async void BtnSimulateCallback_Click(object? sender, EventArgs e)
    {
        _btnSimulateCallback.Enabled = false;
        _lblStatus.Text = "Đang xử lý callback...";

        try
        {
            await this.RunSafeAsync(async () =>
            {
                var result = await _paymentService.ProcessCallbackAsync("vnp_ResponseCode=00&txn=999999");

                if (!result.IsSuccess)
                {
                    _lblStatus.Text = result.ErrorMessage ?? "Giao dịch thất bại.";
                    MessageBox.Show(this, _lblStatus.Text, "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _lblStatus.Text = "Giao dịch hoàn tất.";
                MessageBox.Show(this, "Thanh toán thành công!", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        }
        finally
        {
            _btnSimulateCallback.Enabled = true;
        }
    }

    /// <summary>Một gói Marketing hiển thị trong ComboBox.</summary>
    private sealed record PackageItem(string Name, decimal Price)
    {
        public override string ToString() => $"{Name} - {Price:N0}đ";
    }
}

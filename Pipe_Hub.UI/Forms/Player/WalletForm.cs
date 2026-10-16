using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Forms.Base;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Player;

public class WalletForm : BaseCrudForm
{
    private readonly IWalletService _walletService;

    private Label _lblBalanceTitle = null!;
    private Label _lblBalance = null!;
    private Panel _topPanel = null!;
    private List<TopUpRequestDto> _allRequests = new();

    public WalletForm(IWalletService walletService)
    {
        _walletService = walletService;

        Text = "Ví của tôi";
        _btnEdit.Visible = false;
        _btnDelete.Visible = false;
        _btnAdd.Text = "Nạp ví";
        _btnAdd.Visible = false;  // Thay bằng nút ở panel trên

        BuildTopPanel();

        Load += async (s, e) => await OnRefreshClickedAsync();
    }

    private void BuildTopPanel()
    {
        _topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            Padding = new Padding(20),
            BackColor = Color.FromArgb(245, 245, 245)
        };

        _lblBalanceTitle = new Label
        {
            Text = "Số dư hiện tại:",
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.Gray,
            Location = new Point(20, 20),
            AutoSize = true
        };

        _lblBalance = new Label
        {
            Text = "0 đ",
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            Location = new Point(20, 45),
            AutoSize = true
        };

        var btnTopUp = new Button
        {
            Text = "Nạp ví",
            Size = new Size(120, 40),
            Location = new Point(_topPanel.Width - 150, 30),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        btnTopUp.Click += BtnTopUp_Click;

        _topPanel.Controls.AddRange(new Control[] { _lblBalanceTitle, _lblBalance, btnTopUp });

        Controls.Add(_topPanel);
        _topPanel.BringToFront();
    }

    protected override void OnAddClicked()
    {
        // Không dùng — đã có nút Nạp ví riêng
    }

    protected override void OnEditClicked()
    {
        // Không dùng
    }

    protected override void OnDeleteClicked()
    {
        // Không dùng
    }

    protected override async Task OnRefreshClickedAsync()
    {
        await this.RunSafeAsync(async () =>
        {
            // Load balance
            var wallet = await _walletService.GetMyWalletAsync();
            _lblBalance.Text = $"{wallet.Balance:N0} đ";

            // Load lịch sử yêu cầu nạp
            var requests = await _walletService.GetMyTopUpRequestsAsync();
            _allRequests = requests.ToList();
            BindRequestsToGrid(_allRequests);
            SetCountLabel(_allRequests.Count);
        });
    }

    protected override void OnSearchChanged(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            BindRequestsToGrid(_allRequests);
            SetCountLabel(_allRequests.Count);
            return;
        }

        var filtered = _allRequests.Where(r =>
            (r.Status?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.PaymentMethod?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (r.RejectReason?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
        ).ToList();

        BindRequestsToGrid(filtered);
        SetCountLabel(filtered.Count);
    }

    private async void BtnTopUp_Click(object? sender, EventArgs e)
    {
        // Dialog nhập số tiền đơn giản
        using var dialog = new Form
        {
            Text = "Nạp ví",
            ClientSize = new Size(360, 220),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9F)
        };

        var errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        dialog.AutoValidate = AutoValidate.EnableAllowFocusChange;

        var lblAmount = new Label
        {
            Text = "Số tiền nạp:",
            Location = new Point(20, 20),
            Size = new Size(100, 25)
        };

        var txtAmount = new TextBox
        {
            Location = new Point(20, 50),
            Size = new Size(300, 25),
            Text = "100000"
        };
        txtAmount.Validating += (s, e) =>
        {
            var isValid = decimal.TryParse(txtAmount.Text, out var amt)
                && amt >= 10_000
                && amt <= 50_000_000;
            e.Cancel = !isValid;

            var msg = !decimal.TryParse(txtAmount.Text, out _)
                ? "Số tiền không hợp lệ."
                : amt < 10_000
                    ? "Số tiền nạp tối thiểu là 10,000."
                    : "Số tiền nạp tối đa là 50,000,000.";

            errorProvider.SetError(txtAmount, isValid ? "" : msg);
        };

        var lblNote = new Label
        {
            Text = "(tối thiểu 10,000 — tối đa 50,000,000)",
            Location = new Point(20, 80),
            Size = new Size(300, 20),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8F)
        };

        var lblMethod = new Label
        {
            Text = "Phương thức:",
            Location = new Point(20, 105),
            Size = new Size(100, 25)
        };

        var cboMethod = new ComboBox
        {
            Location = new Point(120, 105),
            Size = new Size(200, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboMethod.Items.AddRange(new object[] { "VNPay", "MoMo", "Bank Transfer" });
        cboMethod.SelectedIndex = 0;

        var btnOk = new Button
        {
            Text = "Xác nhận",
            Location = new Point(140, 145),
            Size = new Size(90, 30),
            DialogResult = DialogResult.OK
        };
        btnOk.Click += (s, e) =>
        {
            if (!decimal.TryParse(txtAmount.Text, out var amt)
                || amt < 10_000
                || amt > 50_000_000)
            {
                errorProvider.SetError(txtAmount, "Số tiền không hợp lệ (10,000 - 50,000,000).");
                dialog.DialogResult = DialogResult.None;
            }
        };

        var btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(240, 145),
            Size = new Size(80, 30),
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.AddRange(new Control[]
        {
            lblAmount, txtAmount, lblNote, lblMethod, cboMethod, btnOk, btnCancel
        });
        dialog.AcceptButton = btnOk;
        dialog.CancelButton = btnCancel;
        dialog.FormClosed += (s, e) => errorProvider.Dispose();

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        if (!decimal.TryParse(txtAmount.Text, out var amount))
        {
            MessageBox.Show("Số tiền không hợp lệ.", "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        await this.RunSafeAsync(async () =>
        {
            var method = cboMethod.SelectedItem?.ToString();
            var result = await _walletService.CreateTopUpRequestAsync(
                amount,
                method,
                paymentReference: null);

            if (!result.IsSuccess)
            {
                MessageBox.Show(result.ErrorMessage ?? "Không thể tạo yêu cầu nạp.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(
                $"Đã tạo yêu cầu nạp {amount:N0} đ.\nVui lòng chờ Admin duyệt.",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

            await OnRefreshClickedAsync();
        });
    }

    private void BindRequestsToGrid(IReadOnlyList<TopUpRequestDto> requests)
    {
        _grid.DataSource = null;
        _grid.Columns.Clear();
        _grid.AutoGenerateColumns = false;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Mã yêu cầu",
            DataPropertyName = "Id",
            FillWeight = 10
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Số tiền",
            DataPropertyName = "Amount",
            FillWeight = 15,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N0",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Trạng thái",
            DataPropertyName = "Status",
            FillWeight = 15
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Phương thức",
            DataPropertyName = "PaymentMethod",
            FillWeight = 15
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ngày tạo",
            DataPropertyName = "CreatedAt",
            FillWeight = 20,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Lý do từ chối",
            DataPropertyName = "RejectReason",
            FillWeight = 25
        });

        _grid.DataSource = requests;
    }
}

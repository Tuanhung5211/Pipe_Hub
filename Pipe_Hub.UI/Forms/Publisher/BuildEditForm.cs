using System.ComponentModel;
using Pipe_Hub.BLL.DTOs;
using Pipe_Hub.BLL.Interfaces;
using Pipe_Hub.UI.Helpers;

namespace Pipe_Hub.UI.Forms.Publisher;

/// <summary>
/// Dialog thêm mới / chỉnh sửa GameBuild của 1 game.
/// existingBuild == null → chế độ Thêm mới; existingBuild != null → chế độ Sửa.
/// </summary>
public class BuildEditForm : Form
{
    private readonly IBuildService _buildService;
    private readonly int _gameId;
    private readonly BuildDto? _existingBuild;
    private readonly bool _isEdit;

    private TextBox _txtVersion = null!;
    private ComboBox _cboChannel = null!;
    private TextBox _txtPatchNotes = null!;
    private TextBox _txtFilePath = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private ErrorProvider _errorProvider = null!;

    public BuildEditForm(IBuildService buildService, int gameId, BuildDto? existingBuild = null)
    {
        _buildService = buildService;
        _gameId = gameId;
        _existingBuild = existingBuild;
        _isEdit = existingBuild is not null;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoValidate = AutoValidate.EnableAllowFocusChange;

        Text = _isEdit ? "Pipe Hub — Sửa build" : "Pipe Hub — Thêm build";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Segoe UI", 9F);

        _errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        var lblTitle = new Label
        {
            Text = _isEdit ? "SỬA BUILD" : "THÊM BUILD",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 120, 215),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15),
            Size = new Size(460, 32)
        };

        var controls = new List<Control> { lblTitle };

        int y = 65;
        const int labelX = 30, inputX = 160, inputW = 270, rowH = 34;

        // Phiên bản
        controls.Add(new Label { Text = "Phiên bản:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtVersion = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            MaxLength = 50,
            Text = _existingBuild?.Version ?? string.Empty
        };
        _txtVersion.Validating += TxtVersion_Validating;
        controls.Add(_txtVersion);
        y += rowH;

        // Kênh phát hành
        controls.Add(new Label { Text = "Kênh:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _cboChannel = new ComboBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cboChannel.Items.AddRange(new object[] { "Stable", "Beta" });
        _cboChannel.SelectedIndex = 0;
        if (_isEdit && _existingBuild is not null)
        {
            var index = _cboChannel.Items.IndexOf(_existingBuild.Channel);
            if (index >= 0) _cboChannel.SelectedIndex = index;
        }
        controls.Add(_cboChannel);
        y += rowH;

        // Ghi chú bản cập nhật (multiline)
        controls.Add(new Label { Text = "Ghi chú:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtPatchNotes = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 90),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = _existingBuild?.PatchNotes ?? string.Empty
        };
        controls.Add(_txtPatchNotes);
        y += 90 + 8;

        // Đường dẫn file
        controls.Add(new Label { Text = "Đường dẫn file:", Location = new Point(labelX, y), Size = new Size(125, 25) });
        _txtFilePath = new TextBox
        {
            Location = new Point(inputX, y),
            Size = new Size(inputW, 25),
            MaxLength = 500,
            Text = _existingBuild?.FilePath ?? string.Empty
        };
        controls.Add(_txtFilePath);
        y += rowH + 8;

        // Buttons
        _btnSave = new Button
        {
            Text = "Lưu",
            Location = new Point(inputX, y),
            Size = new Size(120, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _btnSave.Click += BtnSave_Click;

        _btnCancel = new Button
        {
            Text = "Hủy",
            Location = new Point(inputX + 130, y),
            Size = new Size(120, 32)
        };
        _btnCancel.Click += (s, e) => Close();

        controls.Add(_btnSave);
        controls.Add(_btnCancel);

        AcceptButton = _btnSave;
        CancelButton = _btnCancel;

        ClientSize = new Size(460, y + 60);
        Controls.AddRange(controls.ToArray());
    }

    private void TxtVersion_Validating(object? sender, CancelEventArgs e)
    {
        var version = _txtVersion.Text.Trim();
        e.Cancel = !_errorProvider.Check(_txtVersion,
            !string.IsNullOrWhiteSpace(version),
            "Vui lòng nhập phiên bản.");
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateChildren()) return;

        var version = _txtVersion.Text.Trim();
        var channel = _cboChannel.SelectedItem?.ToString() ?? "Stable";
        var patchNotes = _txtPatchNotes.Text.Trim();
        var filePath = _txtFilePath.Text.Trim();

        await this.RunSafeAsync(async () =>
        {
            try
            {
                _btnSave.Enabled = false;
                _btnSave.Text = "Đang lưu...";

                var result = _isEdit
                    ? await _buildService.UpdateBuildAsync(_existingBuild!.Id, version, channel, patchNotes, filePath)
                    : await _buildService.CreateBuildAsync(_gameId, version, channel, patchNotes, filePath);

                if (!result.IsSuccess)
                {
                    MessageBox.Show(result.ErrorMessage ?? "Lưu build thất bại.", "Thông báo",
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
            _errorProvider?.Dispose();
        }
        base.Dispose(disposing);
    }
}

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CanXeApp.Controls;
using CanXeApp.Models;
using CanXeApp.Services;

namespace CanXeApp
{
    public class FormEditPhieu : Form
    {
        public PhieuCan PhieuCanData { get; private set; }
        public bool IsDeleted { get; private set; } = false;
        public bool IsPrintRequested { get; private set; } = false;
        public PhieuCanHistory? RecordedHistory { get; private set; } = null;
        public bool HasSavedChanges { get; private set; } = false;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string CurrentLanguage { get; set; } = "VI";

        private readonly DatabaseService _dbService;

        // Lưu bản snapshot giá trị gốc để kiểm tra người dùng đã thay đổi dữ liệu chưa
        private string _origSoXe = "";
        private string _origLoaiHang = "";
        private string _origKhachHang = "";
        private double _origKhoiLuongTong = 0;
        private double _origKhoiLuongXe = 0;
        private DateTime _origNgayInPhieu = DateTime.MinValue;
        private string _origSoChungTu = "";
        private string _origGhiChu = "";
        private string _origKieuCan = "";
        private bool _isSavedOrDeleted = false;

        private Label lblSoXe, lblLoaiHang, lblKhachHang;
        private Label lblKhoiLuongTong, lblKhoiLuongXe, lblKhoiLuongHang;
        private Label lblNgayInPhieu, lblSoChungTu, lblGhiChu, lblKieuCan, lblSTT;
        private Label lblLichSu;

        private TextBox txtSoXe, txtKhachHang;
        private ComboBox cbLoaiHang;
        private FormattedNumericTextBox numKhoiLuongTong, numKhoiLuongXe;
        private TextBox txtKhoiLuongHang;
        private VnDateTimePicker dtpNgayInPhieu;
        private TextBox txtSoChungTu, txtGhiChu;
        private ComboBox cbKieuCan;
        private TextBox txtSTT;

        private Button btnSave, btnPrintAndSave, btnPreview, btnDelete, btnCancel;
        private Button btnLichSu;

        private readonly string[] _dsLoaiHang = new string[]
        {
            "Mía", "Dưa", "Phế Liệu", "Keo", "Gỗ", "Lúa", "Đậu Đỏ", "Bắp", "Mì", "Bì Cao Su"
        };

        private float _zoom = 1.0f;

        public FormEditPhieu(PhieuCan phieuCan, string language = "VI", float zoom = 1.0f, DatabaseService? dbService = null)
        {
            PhieuCanData = phieuCan;
            CurrentLanguage = language;
            _zoom = zoom > 0 ? zoom : 1.0f;
            _dbService = dbService ?? new DatabaseService();
            InitializeComponentCustom();
            ApplyLanguage();
            LoadPhieuData();

            this.Shown += (s, e) => this.ActiveControl = null;
        }

        private void InitializeComponentCustom()
        {
            this.Text = $"CHỈNH SỬA PHIẾU CÂN - STT: Xe {PhieuCanData.STT}";
            int formW = (int)Math.Round(850 * _zoom);
            int formH = (int)Math.Round(475 * _zoom);
            this.ClientSize = new Size(formW, formH);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", Math.Max(8f, 11F * _zoom), FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 247, 250);

            this.Click += ClearFocus_Click;

            try
            {
                if (System.IO.File.Exists("app_icon.ico")) this.Icon = new Icon("app_icon.ico");
                else if (System.IO.File.Exists("app_icon.png")) this.Icon = Icon.FromHandle(((Bitmap)Image.FromFile("app_icon.png")).GetHicon());
                else this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            int y = (int)Math.Round(25 * _zoom);
            int yStep = (int)Math.Round(46 * _zoom);
            int lblX1 = (int)Math.Round(20 * _zoom);
            int txtX1 = (int)Math.Round(150 * _zoom);
            int txtWidth1 = (int)Math.Round(220 * _zoom);
            int lblX2 = (int)Math.Round(410 * _zoom);
            int txtX2 = (int)Math.Round(550 * _zoom);
            int txtWidth2 = (int)Math.Round(270 * _zoom);
            float fontBold = Math.Max(8f, 11.5F * _zoom);

            // STT & Số xe (Giới hạn 50 ký tự)
            lblSTT = CreateLabel("STT:", lblX1, y);
            txtSTT = new TextBox { Location = new Point(txtX1, y - (int)Math.Round(3 * _zoom)), Width = txtWidth1, ReadOnly = true, Enabled = false, Font = new Font("Segoe UI", fontBold, FontStyle.Bold) };

            lblSoXe = CreateLabelWithRedAsterisk("Số xe", lblX2, y);
            txtSoXe = new TextBox { Location = new Point(txtX2, y - (int)Math.Round(3 * _zoom)), Width = txtWidth2, Font = new Font("Segoe UI", fontBold, FontStyle.Bold), ForeColor = Color.DarkBlue, MaxLength = 50 };
            txtSoXe.Enter += Input_SelectAllOnEnter;
            txtSoXe.MouseClick += Input_SelectAllOnEnter;

            y += yStep;
            // Loại hàng & Khách hàng (Giới hạn 50 ký tự)
            lblLoaiHang = CreateLabel("Loại hàng:", lblX1, y);
            cbLoaiHang = new ComboBox { Location = new Point(txtX1, y - (int)Math.Round(3 * _zoom)), Width = txtWidth1, DropDownStyle = ComboBoxStyle.DropDown, Font = new Font("Segoe UI", fontBold, FontStyle.Bold), MaxLength = 50 };
            cbLoaiHang.Items.AddRange(_dsLoaiHang);
            cbLoaiHang.Leave += CbLoaiHang_Leave;
            cbLoaiHang.Enter += Input_SelectAllOnEnter;

            lblKhachHang = CreateLabel("Khách hàng:", lblX2, y);
            txtKhachHang = new TextBox { Location = new Point(txtX2, y - (int)Math.Round(3 * _zoom)), Width = txtWidth2, Font = new Font("Segoe UI", fontBold, FontStyle.Bold), MaxLength = 50 };
            txtKhachHang.Leave += TxtKhachHang_Leave;
            txtKhachHang.Enter += Input_SelectAllOnEnter;
            txtKhachHang.MouseClick += Input_SelectAllOnEnter;

            y += yStep;
            // KL Tong & Ngày in phiếu
            lblKhoiLuongTong = CreateLabel("KL Tổng (Kg):", lblX1, y);
            numKhoiLuongTong = new FormattedNumericTextBox { Location = new Point(txtX1, y - (int)Math.Round(3 * _zoom)), Width = txtWidth1, Font = new Font("Segoe UI", fontBold, FontStyle.Bold) };
            numKhoiLuongTong.TextChanged += CalculateKLHangRealtime;

            lblNgayInPhieu = CreateLabel("Ngày in phiếu:", lblX2, y);
            dtpNgayInPhieu = new VnDateTimePicker { Location = new Point(txtX2, y - (int)Math.Round(3 * _zoom)), Width = txtWidth2, Height = (int)Math.Round(34 * _zoom), Font = new Font("Segoe UI", fontBold, FontStyle.Regular), CustomFormat = "dd/MM/yyyy HH:mm", PreventFutureDates = true, Language = CurrentLanguage };

            y += yStep;
            // KL Xe & Kiểu cân
            lblKhoiLuongXe = CreateLabel("KL Xe (Kg):", lblX1, y);
            numKhoiLuongXe = new FormattedNumericTextBox { Location = new Point(txtX1, y - (int)Math.Round(3 * _zoom)), Width = txtWidth1, Font = new Font("Segoe UI", fontBold, FontStyle.Bold) };
            numKhoiLuongXe.TextChanged += CalculateKLHangRealtime;

            lblKieuCan = CreateLabel("Kiểu cân:", lblX2, y);
            cbKieuCan = new ComboBox { Location = new Point(txtX2, y - (int)Math.Round(3 * _zoom)), Width = txtWidth2, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", fontBold, FontStyle.Bold) };
            cbKieuCan.Items.AddRange(new object[] { "Cân dịch vụ", "Xuất hàng", "Nhập hàng" });

            y += yStep;
            // KL Hang & Số chứng từ (Giới hạn 50 ký tự)
            lblKhoiLuongHang = CreateLabel("KL Hàng (Kg):", lblX1, y);
            txtKhoiLuongHang = new TextBox { Location = new Point(txtX1, y - (int)Math.Round(3 * _zoom)), Width = txtWidth1, ReadOnly = true, Enabled = false, Font = new Font("Segoe UI", Math.Max(8f, 12F * _zoom), FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Red };

            lblSoChungTu = CreateLabel("Số chứng từ:", lblX2, y);
            txtSoChungTu = new TextBox { Location = new Point(txtX2, y - (int)Math.Round(3 * _zoom)), Width = txtWidth2, Font = new Font("Segoe UI", fontBold, FontStyle.Bold), MaxLength = 50 };
            txtSoChungTu.Enter += Input_SelectAllOnEnter;
            txtSoChungTu.MouseClick += Input_SelectAllOnEnter;

            y += yStep;
            // Ghi chú (Giới hạn 150 ký tự) & Lịch sử chỉnh sửa
            lblGhiChu = CreateLabel("Ghi chú:", lblX1, y);
            txtGhiChu = new TextBox { Location = new Point(txtX1, y - (int)Math.Round(3 * _zoom)), Width = txtWidth1, Font = new Font("Segoe UI", fontBold, FontStyle.Bold), MaxLength = 150 };
            txtGhiChu.Enter += Input_SelectAllOnEnter;
            txtGhiChu.MouseClick += Input_SelectAllOnEnter;

            lblLichSu = CreateLabel("Lịch sử:", lblX2, y);
            btnLichSu = CreateHistoryButton("📜 Xem Lịch Sử (0)", txtX2, y - (int)Math.Round(3 * _zoom), txtWidth2, (int)Math.Round(33 * _zoom));
            btnLichSu.Click += BtnLichSu_Click;

            y += (int)Math.Round(56 * _zoom);
            int btnW = (int)Math.Round(145 * _zoom);
            int btnWPrint = (int)Math.Round(165 * _zoom);
            int gap = (int)Math.Round(10 * _zoom);
            int totalBtnsW = (btnW * 4) + btnWPrint + (gap * 4);
            int startX = Math.Max(10, (formW - totalBtnsW) / 2);

            int x0 = startX;
            int x1 = x0 + btnW + gap;
            int x2 = x1 + btnWPrint + gap;
            int x3 = x2 + btnW + gap;
            int x4 = x3 + btnW + gap;

            btnSave = CreateButton("💾 Lưu Thay Đổi", x0, y, btnW, Color.FromArgb(0, 123, 255));
            btnPrintAndSave = CreateButton("🖨️ In Và Lưu Phiếu", x1, y, btnWPrint, Color.FromArgb(220, 53, 69));
            btnPreview = CreateButton("👁️ Xem Trước", x2, y, btnW, Color.FromArgb(23, 162, 184));
            btnDelete = CreateButton("🗑️ Xóa Phiếu", x3, y, btnW, Color.FromArgb(220, 53, 69));
            btnCancel = CreateButton("❌ Đóng", x4, y, btnW, Color.FromArgb(108, 117, 125));

            btnSave.Click += BtnSave_Click;
            btnPrintAndSave.Click += BtnPrintAndSave_Click;
            btnPreview.Click += BtnPreview_Click;
            btnDelete.Click += BtnDelete_Click;
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
                lblSTT, txtSTT, lblSoXe, txtSoXe,
                lblLoaiHang, cbLoaiHang, lblKhachHang, txtKhachHang,
                lblKhoiLuongTong, numKhoiLuongTong, lblNgayInPhieu, dtpNgayInPhieu,
                lblKhoiLuongXe, numKhoiLuongXe, lblKieuCan, cbKieuCan,
                lblKhoiLuongHang, txtKhoiLuongHang, lblSoChungTu, txtSoChungTu,
                lblGhiChu, txtGhiChu, lblLichSu, btnLichSu,
                btnSave, btnPrintAndSave, btnPreview, btnDelete, btnCancel
            });
        }

        public void ApplyLanguage()
        {
            dtpNgayInPhieu.Language = CurrentLanguage;
            if (CurrentLanguage == "EN")
            {
                this.Text = $"EDIT TICKET - NO: Xe {PhieuCanData.STT}";
                lblSTT.Text = "No:";
                UpdateSoXeLabelPosition("Plate No", (int)Math.Round(410 * _zoom), (int)Math.Round(25 * _zoom));
                lblLoaiHang.Text = "Goods Type:";
                lblKhachHang.Text = "Customer:";
                lblKhoiLuongTong.Text = "Gross Wt (Kg):";
                lblKhoiLuongXe.Text = "Tare Wt (Kg):";
                lblKhoiLuongHang.Text = "Net Wt (Kg):";
                lblNgayInPhieu.Text = "Ticket Date:";
                lblKieuCan.Text = "Weigh Type:";
                lblSoChungTu.Text = "Voucher No:";
                lblGhiChu.Text = "Note:";
                if (lblLichSu != null) lblLichSu.Text = "History:";

                btnSave.Text = "💾 Save Changes";
                btnPrintAndSave.Text = "🖨️ Print & Save";
                btnPreview.Text = "👁️ Preview";
                btnDelete.Text = "🗑️ Delete";
                btnCancel.Text = "❌ Close";

                int selKieuCan = cbKieuCan.SelectedIndex;
                cbKieuCan.Items.Clear();
                cbKieuCan.Items.AddRange(new object[] { "Service Weighing", "Export Goods", "Import Goods" });
                if (selKieuCan >= 0 && selKieuCan < cbKieuCan.Items.Count) cbKieuCan.SelectedIndex = selKieuCan;
            }
            else
            {
                this.Text = $"CHỈNH SỬA PHIẾU CÂN - STT: Xe {PhieuCanData.STT}";
                lblSTT.Text = "STT:";
                UpdateSoXeLabelPosition("Số xe", (int)Math.Round(410 * _zoom), (int)Math.Round(25 * _zoom));
                lblLoaiHang.Text = "Loại hàng:";
                lblKhachHang.Text = "Khách hàng:";
                lblKhoiLuongTong.Text = "KL Tổng (Kg):";
                lblKhoiLuongXe.Text = "KL Xe (Kg):";
                lblKhoiLuongHang.Text = "KL Hàng (Kg):";
                lblNgayInPhieu.Text = "Ngày in phiếu:";
                lblKieuCan.Text = "Kiểu cân:";
                lblSoChungTu.Text = "Số chứng từ:";
                lblGhiChu.Text = "Ghi chú:";
                if (lblLichSu != null) lblLichSu.Text = "Lịch sử:";

                btnSave.Text = "💾 Lưu Thay Đổi";
                btnPrintAndSave.Text = "🖨️ In Và Lưu Phiếu";
                btnPreview.Text = "👁️ Xem Trước";
                btnDelete.Text = "🗑️ Xóa Phiếu";
                btnCancel.Text = "❌ Đóng";

                int selKieuCan = cbKieuCan.SelectedIndex;
                cbKieuCan.Items.Clear();
                cbKieuCan.Items.AddRange(new object[] { "Cân dịch vụ", "Xuất hàng", "Nhập hàng" });
                if (selKieuCan >= 0 && selKieuCan < cbKieuCan.Items.Count) cbKieuCan.SelectedIndex = selKieuCan;
            }
            UpdateHistoryButtonUI();
        }

        private Label CreateLabel(string text, int x, int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 11F) };
            lbl.Click += ClearFocus_Click;
            return lbl;
        }

        private Label CreateLabelWithRedAsterisk(string text, int x, int y)
        {
            lblSoXe = new Label
            {
                Text = "",
                Location = new Point(x, y),
                Size = new Size(130, 26),
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 40, 60),
                BackColor = Color.Transparent
            };
            lblSoXe.Paint += LblSoXe_Paint;
            lblSoXe.Click += ClearFocus_Click;
            UpdateSoXeLabelPosition(text, x, y);
            return lblSoXe;
        }

        private void LblSoXe_Paint(object? sender, PaintEventArgs e)
        {
            if (lblSoXe == null) return;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            string prefix = "Số xe (";
            string star = "*";
            string suffix = "):";

            Font font = lblSoXe.Font;
            using var fontStar = new Font(font.FontFamily, font.SizeInPoints, FontStyle.Bold);

            Size szPrefix = TextRenderer.MeasureText(e.Graphics, prefix, font, Size.Empty, TextFormatFlags.NoPadding);
            Size szStar = TextRenderer.MeasureText(e.Graphics, star, fontStar, Size.Empty, TextFormatFlags.NoPadding);

            int curX = 0;
            int curY = 0;

            TextRenderer.DrawText(e.Graphics, prefix, font, new Point(curX, curY), lblSoXe.ForeColor, TextFormatFlags.NoPadding);
            curX += szPrefix.Width;

            TextRenderer.DrawText(e.Graphics, star, fontStar, new Point(curX, curY), Color.Red, TextFormatFlags.NoPadding);
            curX += szStar.Width;

            TextRenderer.DrawText(e.Graphics, suffix, font, new Point(curX, curY), lblSoXe.ForeColor, TextFormatFlags.NoPadding);
        }

        private void UpdateSoXeLabelPosition(string mainText, int x, int y)
        {
            if (lblSoXe == null) return;
            lblSoXe.Location = new Point(x, y);
            lblSoXe.Invalidate();
        }

        private Button CreateButton(string text, int x, int y, int width, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 42),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = Padding.Empty,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateHistoryButton(string text, int x, int y, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.FromArgb(235, 240, 248),
                ForeColor = Color.FromArgb(70, 80, 95),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", Math.Max(8f, 10F * _zoom), FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = Padding.Empty,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(195, 205, 220);
            return btn;
        }

        public void UpdateHistoryButtonUI()
        {
            if (btnLichSu == null) return;
            int count = 0;
            try
            {
                count = _dbService.GetHistoryCount(PhieuCanData.Id);
            }
            catch { }

            if (count == 0)
            {
                btnLichSu.Text = CurrentLanguage == "EN" ? "📜 View History (0)" : "📜 Xem Lịch Sử (0 lần)";
                btnLichSu.BackColor = Color.FromArgb(235, 240, 248);
                btnLichSu.ForeColor = Color.FromArgb(90, 100, 115);
                btnLichSu.FlatAppearance.BorderColor = Color.FromArgb(205, 215, 228);
            }
            else
            {
                btnLichSu.Text = CurrentLanguage == "EN" ? $"📜 View History ({count})" : $"📜 Xem Lịch Sử ({count} lần)";
                btnLichSu.BackColor = Color.FromArgb(111, 66, 193); // Tím hiện đại
                btnLichSu.ForeColor = Color.White;
                btnLichSu.FlatAppearance.BorderColor = Color.FromArgb(95, 50, 175);
            }
        }

        private void BtnLichSu_Click(object? sender, EventArgs e)
        {
            using var formHistory = new FormLichSuEdit(PhieuCanData.Id, PhieuCanData.STT, PhieuCanData.SoXe, _dbService, CurrentLanguage, _zoom);
            formHistory.ShowDialog(this);
        }

        private void ClearFocus_Click(object? sender, EventArgs e)
        {
            this.ActiveControl = null;
        }

        private void CbLoaiHang_Leave(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(cbLoaiHang.Text))
            {
                cbLoaiHang.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cbLoaiHang.Text.Trim().ToLower());
            }
        }

        private void TxtKhachHang_Leave(object? sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtKhachHang.Text))
            {
                txtKhachHang.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(txtKhachHang.Text.Trim().ToLower());
            }
        }

        private void Input_SelectAllOnEnter(object? sender, EventArgs e)
        {
            if (sender is TextBox txt)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    txt.SelectAll();
                });
            }
            else if (sender is ComboBox cb)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    cb.SelectAll();
                });
            }
        }

        private void CalculateKLHangRealtime(object? sender, EventArgs e)
        {
            double tong = numKhoiLuongTong != null ? numKhoiLuongTong.Value : 0;
            double xe = numKhoiLuongXe != null ? numKhoiLuongXe.Value : 0;

            if (xe > tong && tong > 0)
            {
                txtKhoiLuongHang.BackColor = Color.Red;
                txtKhoiLuongHang.ForeColor = Color.White;
                txtKhoiLuongHang.Text = CurrentLanguage == "EN" ? "TARE > GROSS!" : "KL XE > KL Tổng!";
            }
            else
            {
                txtKhoiLuongHang.BackColor = Color.Yellow;
                txtKhoiLuongHang.ForeColor = Color.DarkRed;
                txtKhoiLuongHang.Text = Math.Max(0, tong - xe).ToString("#,##0", new CultureInfo("de-DE"));
            }
        }

        private void LoadPhieuData()
        {
            txtSTT.Text = $"Xe {PhieuCanData.STT}";
            txtSoXe.Text = PhieuCanData.SoXe;
            cbLoaiHang.Text = PhieuCanData.LoaiHang;
            txtKhachHang.Text = PhieuCanData.KhachHang;
            numKhoiLuongTong.Value = PhieuCanData.KhoiLuongTong;
            numKhoiLuongXe.Value = PhieuCanData.KhoiLuongXe;
            dtpNgayInPhieu.Value = PhieuCanData.NgayInPhieu;
            txtSoChungTu.Text = PhieuCanData.SoChungTu;
            txtGhiChu.Text = PhieuCanData.GhiChu;
            cbKieuCan.SelectedItem = PhieuCanData.KieuCan;

            // Lưu snapshot giá trị ban đầu để phát hiện chỉnh sửa
            _origSoXe = PhieuCanData.SoXe ?? "";
            _origLoaiHang = PhieuCanData.LoaiHang ?? "";
            _origKhachHang = PhieuCanData.KhachHang ?? "";
            _origKhoiLuongTong = PhieuCanData.KhoiLuongTong;
            _origKhoiLuongXe = PhieuCanData.KhoiLuongXe;
            _origNgayInPhieu = PhieuCanData.NgayInPhieu;
            _origSoChungTu = PhieuCanData.SoChungTu ?? "";
            _origGhiChu = PhieuCanData.GhiChu ?? "";
            _origKieuCan = PhieuCanData.KieuCan ?? "";
            _isSavedOrDeleted = false;
            dtpNgayInPhieu.IsUserModified = false;
            UpdateHistoryButtonUI();

            CalculateKLHangRealtime(null, null);
        }

        public bool HasUnsavedChanges()
        {
            if (_isSavedOrDeleted) return false;

            if (txtSoXe.Text.Trim() != _origSoXe.Trim()) return true;
            if (cbLoaiHang.Text.Trim() != _origLoaiHang.Trim()) return true;
            if (txtKhachHang.Text.Trim() != _origKhachHang.Trim()) return true;
            if (Math.Abs(numKhoiLuongTong.Value - _origKhoiLuongTong) > 0.001) return true;
            if (Math.Abs(numKhoiLuongXe.Value - _origKhoiLuongXe) > 0.001) return true;
            if (Math.Abs((dtpNgayInPhieu.Value - _origNgayInPhieu).TotalMinutes) >= 1) return true;
            if (txtSoChungTu.Text.Trim() != _origSoChungTu.Trim()) return true;
            if (txtGhiChu.Text.Trim() != _origGhiChu.Trim()) return true;
            if ((cbKieuCan.SelectedItem?.ToString() ?? "") != _origKieuCan) return true;

            return false;
        }

        private void UpdateDataFromInputs()
        {
            PhieuCanData.SoXe = txtSoXe.Text.Trim();
            PhieuCanData.LoaiHang = cbLoaiHang.Text.Trim();
            PhieuCanData.KhachHang = txtKhachHang.Text.Trim();
            PhieuCanData.KhoiLuongTong = numKhoiLuongTong.Value;
            PhieuCanData.KhoiLuongXe = numKhoiLuongXe.Value;
            PhieuCanData.NgayInPhieu = dtpNgayInPhieu.Value;
            PhieuCanData.SoChungTu = txtSoChungTu.Text.Trim();
            PhieuCanData.GhiChu = txtGhiChu.Text.Trim();
            PhieuCanData.KieuCan = cbKieuCan.SelectedItem?.ToString() ?? "Cân dịch vụ";
        }

        private bool ValidateAndSaveInput()
        {
            if (string.IsNullOrWhiteSpace(txtSoXe.Text))
            {
                string msg = CurrentLanguage == "EN" ? "Please enter Plate Number!" : "Vui lòng nhập Biển số xe!";
                MessageBox.Show(msg, CurrentLanguage == "EN" ? "Notice" : "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool hasChanges = HasUnsavedChanges();

            UpdateDataFromInputs();

            if (hasChanges)
            {
                BuildRecordedHistory();
            }

            return true;
        }

        private void BuildRecordedHistory()
        {
            var deCulture = new CultureInfo("de-DE");
            var oldDict = new System.Collections.Generic.Dictionary<string, string>
            {
                ["SoXe"] = _origSoXe,
                ["LoaiHang"] = _origLoaiHang,
                ["KhachHang"] = _origKhachHang,
                ["KhoiLuongTong"] = _origKhoiLuongTong.ToString("#,##0", deCulture),
                ["KhoiLuongXe"] = _origKhoiLuongXe.ToString("#,##0", deCulture),
                ["KhoiLuongHang"] = Math.Max(0, _origKhoiLuongTong - _origKhoiLuongXe).ToString("#,##0", deCulture),
                ["NgayInPhieu"] = _origNgayInPhieu.ToString("dd/MM/yyyy HH:mm"),
                ["KieuCan"] = _origKieuCan,
                ["SoChungTu"] = _origSoChungTu,
                ["GhiChu"] = _origGhiChu
            };

            var newDict = new System.Collections.Generic.Dictionary<string, string>
            {
                ["SoXe"] = PhieuCanData.SoXe,
                ["LoaiHang"] = PhieuCanData.LoaiHang,
                ["KhachHang"] = PhieuCanData.KhachHang,
                ["KhoiLuongTong"] = PhieuCanData.KhoiLuongTong.ToString("#,##0", deCulture),
                ["KhoiLuongXe"] = PhieuCanData.KhoiLuongXe.ToString("#,##0", deCulture),
                ["KhoiLuongHang"] = PhieuCanData.KhoiLuongHang.ToString("#,##0", deCulture),
                ["NgayInPhieu"] = PhieuCanData.NgayInPhieu.ToString("dd/MM/yyyy HH:mm"),
                ["KieuCan"] = PhieuCanData.KieuCan,
                ["SoChungTu"] = PhieuCanData.SoChungTu,
                ["GhiChu"] = PhieuCanData.GhiChu
            };

            var changes = new System.Collections.Generic.List<string>();
            var fieldLabels = new System.Collections.Generic.Dictionary<string, (string Vi, string En)>
            {
                ["SoXe"] = ("Biển số xe", "Plate No"),
                ["LoaiHang"] = ("Loại hàng", "Goods Type"),
                ["KhachHang"] = ("Khách hàng", "Customer"),
                ["KhoiLuongTong"] = ("KL Tổng", "Gross Wt"),
                ["KhoiLuongXe"] = ("KL Xe", "Tare Wt"),
                ["KhoiLuongHang"] = ("KL Hàng", "Net Wt"),
                ["NgayInPhieu"] = ("Ngày in phiếu", "Ticket Date"),
                ["KieuCan"] = ("Kiểu cân", "Weigh Type"),
                ["SoChungTu"] = ("Số chứng từ", "Voucher No"),
                ["GhiChu"] = ("Ghi chú", "Note")
            };

            foreach (var kvp in fieldLabels)
            {
                string oldVal = oldDict.ContainsKey(kvp.Key) ? oldDict[kvp.Key] : "";
                string newVal = newDict.ContainsKey(kvp.Key) ? newDict[kvp.Key] : "";
                if (oldVal != newVal)
                {
                    string label = CurrentLanguage == "EN" ? kvp.Value.En : kvp.Value.Vi;
                    string unit = kvp.Key.StartsWith("KhoiLuong") ? " kg" : "";
                    changes.Add($"{label}: {oldVal}{unit} ➔ {newVal}{unit}");
                }
            }

            RecordedHistory = new PhieuCanHistory
            {
                PhieuCanId = PhieuCanData.Id,
                STT = PhieuCanData.STT,
                ThoiGianThayDoi = DateTime.Now,
                NguoiThayDoi = PhieuCanData.NguoiCan,
                HanhDong = CurrentLanguage == "EN" ? "Edit Ticket" : "Chỉnh sửa phiếu",
                NoiDungThayDoi = string.Join("\n", changes),
                DuLieuCuJson = System.Text.Json.JsonSerializer.Serialize(oldDict),
                DuLieuMoiJson = System.Text.Json.JsonSerializer.Serialize(newDict)
            };
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (ValidateAndSaveInput())
            {
                // Lưu phiếu cân vào cơ sở dữ liệu
                _dbService.SavePhieuCan(PhieuCanData);
                if (RecordedHistory != null)
                {
                    _dbService.AddPhieuCanHistory(RecordedHistory);
                    RecordedHistory = null;
                }

                HasSavedChanges = true;

                // Cập nhật snapshot giá trị ban đầu để phát hiện chỉnh sửa mới nếu có
                _origSoXe = PhieuCanData.SoXe ?? "";
                _origLoaiHang = PhieuCanData.LoaiHang ?? "";
                _origKhachHang = PhieuCanData.KhachHang ?? "";
                _origKhoiLuongTong = PhieuCanData.KhoiLuongTong;
                _origKhoiLuongXe = PhieuCanData.KhoiLuongXe;
                _origNgayInPhieu = PhieuCanData.NgayInPhieu;
                _origSoChungTu = PhieuCanData.SoChungTu ?? "";
                _origGhiChu = PhieuCanData.GhiChu ?? "";
                _origKieuCan = PhieuCanData.KieuCan ?? "";

                // Cập nhật lại số lần chỉnh sửa trên nút "Xem Lịch Sử"
                UpdateHistoryButtonUI();

                // Lưu ngầm không hiện popup thông báo theo yêu cầu, KHÔNG đóng modal để người dùng tiếp tục xem và chỉnh sửa
            }
        }

        private void BtnPrintAndSave_Click(object? sender, EventArgs e)
        {
            if (ValidateAndSaveInput())
            {
                // Lưu phiếu cân vào cơ sở dữ liệu
                _dbService.SavePhieuCan(PhieuCanData);
                if (RecordedHistory != null)
                {
                    _dbService.AddPhieuCanHistory(RecordedHistory);
                    RecordedHistory = null;
                }

                HasSavedChanges = true;
                IsPrintRequested = true;

                // Cập nhật snapshot giá trị ban đầu để phát hiện chỉnh sửa mới nếu có
                _origSoXe = PhieuCanData.SoXe ?? "";
                _origLoaiHang = PhieuCanData.LoaiHang ?? "";
                _origKhachHang = PhieuCanData.KhachHang ?? "";
                _origKhoiLuongTong = PhieuCanData.KhoiLuongTong;
                _origKhoiLuongXe = PhieuCanData.KhoiLuongXe;
                _origNgayInPhieu = PhieuCanData.NgayInPhieu;
                _origSoChungTu = PhieuCanData.SoChungTu ?? "";
                _origGhiChu = PhieuCanData.GhiChu ?? "";
                _origKieuCan = PhieuCanData.KieuCan ?? "";

                // Cập nhật lại số lần chỉnh sửa trên nút "Xem Lịch Sử"
                UpdateHistoryButtonUI();

                PrintService ps = new PrintService(PhieuCanData);
                ps.PrintDirect();

                // KHÔNG đóng modal để người dùng tiếp tục xem và thao tác
            }
        }

        private void BtnPreview_Click(object? sender, EventArgs e)
        {
            UpdateDataFromInputs();
            PrintService ps = new PrintService(PhieuCanData);
            ps.PrintPreview();
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            string msg = CurrentLanguage == "EN" ? $"Are you sure you want to delete Weighing Ticket No {PhieuCanData.STT}?" : $"Bạn có chắc chắn muốn xóa Phiếu cân STT {PhieuCanData.STT} không?";
            string title = CurrentLanguage == "EN" ? "Confirm Delete" : "Xác nhận xóa";
            var confirm = CustomMessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                _isSavedOrDeleted = true;
                IsDeleted = true;
                _dbService.DeletePhieuCan(PhieuCanData.Id);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isSavedOrDeleted && HasUnsavedChanges())
            {
                string confirmMsg = CurrentLanguage == "EN"
                    ? "You have unsaved changes. Are you sure you want to exit without saving?"
                    : "Bạn đang chỉnh sửa phiếu cân và chưa lưu lại. Bạn có chắc chắn muốn đóng mà không lưu không?";
                string confirmTitle = CurrentLanguage == "EN" ? "Unsaved Changes" : "Xác nhận thoát";

                var result = CustomMessageBox.Show(confirmMsg, confirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }
    }
}

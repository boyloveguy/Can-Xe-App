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
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string CurrentLanguage { get; set; } = "VI";

        private Label lblSoXe, lblLoaiHang, lblKhachHang;
        private Label lblKhoiLuongTong, lblKhoiLuongXe, lblKhoiLuongHang;
        private Label lblNgayInPhieu, lblSoChungTu, lblGhiChu, lblKieuCan, lblSTT;

        private TextBox txtSoXe, txtKhachHang;
        private ComboBox cbLoaiHang;
        private FormattedNumericTextBox numKhoiLuongTong, numKhoiLuongXe;
        private TextBox txtKhoiLuongHang;
        private VnDateTimePicker dtpNgayInPhieu;
        private TextBox txtSoChungTu, txtGhiChu;
        private ComboBox cbKieuCan;
        private TextBox txtSTT;

        private Button btnSave, btnPrintAndSave, btnPreview, btnDelete, btnCancel;

        private readonly string[] _dsLoaiHang = new string[]
        {
            "Mía", "Dưa", "Phế Liệu", "Keo", "Gỗ", "Lúa", "Đậu Đỏ", "Bắp", "Mì", "Bì Cao Su"
        };

        public FormEditPhieu(PhieuCan phieuCan, string language = "VI")
        {
            PhieuCanData = phieuCan;
            CurrentLanguage = language;
            InitializeComponentCustom();
            ApplyLanguage();
            LoadPhieuData();

            this.Shown += (s, e) => this.ActiveControl = null;
        }

        private void InitializeComponentCustom()
        {
            this.Text = $"CHỈNH SỬA PHIẾU CÂN - STT: {PhieuCanData.STT}";
            this.Size = new Size(720, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 247, 250);

            this.Click += ClearFocus_Click;

            try
            {
                if (System.IO.File.Exists("app_icon.ico")) this.Icon = new Icon("app_icon.ico");
                else if (System.IO.File.Exists("app_icon.png")) this.Icon = Icon.FromHandle(((Bitmap)Image.FromFile("app_icon.png")).GetHicon());
                else this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            int y = 20;
            int lblX1 = 20, txtX1 = 130, txtWidth1 = 180;
            int lblX2 = 345, txtX2 = 460, txtWidth2 = 220;

            // STT & Số xe (Giới hạn 50 ký tự)
            lblSTT = CreateLabel("STT:", lblX1, y);
            txtSTT = new TextBox { Location = new Point(txtX1, y - 3), Width = txtWidth1, ReadOnly = true, Enabled = false, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            lblSoXe = CreateLabelWithRedAsterisk("Số xe", lblX2, y);
            txtSoXe = new TextBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkBlue, MaxLength = 50 };
            txtSoXe.Enter += Input_SelectAllOnEnter;
            txtSoXe.MouseClick += Input_SelectAllOnEnter;

            y += 40;
            // Loại hàng & Khách hàng (Giới hạn 50 ký tự)
            lblLoaiHang = CreateLabel("Loại hàng:", lblX1, y);
            cbLoaiHang = new ComboBox { Location = new Point(txtX1, y - 3), Width = txtWidth1, DropDownStyle = ComboBoxStyle.DropDown, Font = new Font("Segoe UI", 10F, FontStyle.Bold), MaxLength = 50 };
            cbLoaiHang.Items.AddRange(_dsLoaiHang);
            cbLoaiHang.Leave += CbLoaiHang_Leave;
            cbLoaiHang.Enter += Input_SelectAllOnEnter;

            lblKhachHang = CreateLabel("Khách hàng:", lblX2, y);
            txtKhachHang = new TextBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, Font = new Font("Segoe UI", 10F, FontStyle.Bold), MaxLength = 50 };
            txtKhachHang.Leave += TxtKhachHang_Leave;
            txtKhachHang.Enter += Input_SelectAllOnEnter;
            txtKhachHang.MouseClick += Input_SelectAllOnEnter;

            y += 40;
            // KL Tong & Ngày in phiếu
            lblKhoiLuongTong = CreateLabel("KL Tổng (Kg):", lblX1, y);
            numKhoiLuongTong = new FormattedNumericTextBox { Location = new Point(txtX1, y - 3), Width = txtWidth1 };
            numKhoiLuongTong.TextChanged += CalculateKLHangRealtime;

            lblNgayInPhieu = CreateLabel("Ngày in phiếu:", lblX2, y);
            dtpNgayInPhieu = new VnDateTimePicker { Location = new Point(txtX2, y - 3), Width = txtWidth2, CustomFormat = "dd/MM/yyyy HH:mm", PreventFutureDates = true, Language = CurrentLanguage };

            y += 40;
            // KL Xe & Kiểu cân
            lblKhoiLuongXe = CreateLabel("KL Xe (Kg):", lblX1, y);
            numKhoiLuongXe = new FormattedNumericTextBox { Location = new Point(txtX1, y - 3), Width = txtWidth1 };
            numKhoiLuongXe.TextChanged += CalculateKLHangRealtime;

            lblKieuCan = CreateLabel("Kiểu cân:", lblX2, y);
            cbKieuCan = new ComboBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            cbKieuCan.Items.AddRange(new object[] { "Cân dịch vụ", "Xuất hàng", "Nhập hàng" });

            y += 40;
            // KL Hang & Số chứng từ (Giới hạn 50 ký tự)
            lblKhoiLuongHang = CreateLabel("KL Hàng (Kg):", lblX1, y);
            txtKhoiLuongHang = new TextBox { Location = new Point(txtX1, y - 3), Width = txtWidth1, ReadOnly = true, Enabled = false, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Red };

            lblSoChungTu = CreateLabel("Số chứng từ:", lblX2, y);
            txtSoChungTu = new TextBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, Font = new Font("Segoe UI", 10F, FontStyle.Bold), MaxLength = 50 };
            txtSoChungTu.Enter += Input_SelectAllOnEnter;
            txtSoChungTu.MouseClick += Input_SelectAllOnEnter;

            y += 40;
            // Ghi chú (Giới hạn 200 ký tự)
            lblGhiChu = CreateLabel("Ghi chú:", lblX1, y);
            txtGhiChu = new TextBox { Location = new Point(txtX1, y - 3), Width = txtWidth1, Font = new Font("Segoe UI", 10F, FontStyle.Bold), MaxLength = 200 };
            txtGhiChu.Enter += Input_SelectAllOnEnter;
            txtGhiChu.MouseClick += Input_SelectAllOnEnter;

            y += 60;
            btnSave = CreateButton("💾 Lưu Thay Đổi", 15, y, 130, Color.FromArgb(0, 123, 255));
            btnPrintAndSave = CreateButton("🖨️ In Và Lưu Phiếu", 150, y, 150, Color.FromArgb(220, 53, 69));
            btnPreview = CreateButton("👁️ Xem Trước", 305, y, 120, Color.FromArgb(23, 162, 184));
            btnDelete = CreateButton("🗑️ Xóa Phiếu", 430, y, 120, Color.FromArgb(220, 53, 69));
            btnCancel = CreateButton("❌ Đóng", 555, y, 110, Color.FromArgb(108, 117, 125));

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
                lblGhiChu, txtGhiChu,
                btnSave, btnPrintAndSave, btnPreview, btnDelete, btnCancel
            });
        }

        public void ApplyLanguage()
        {
            dtpNgayInPhieu.Language = CurrentLanguage;
            if (CurrentLanguage == "EN")
            {
                this.Text = $"EDIT WEIGHING TICKET - NO: {PhieuCanData.STT}";
                lblSTT.Text = "No:";
                UpdateSoXeLabelPosition("Plate No", 345, 20);
                lblLoaiHang.Text = "Goods Type:";
                lblKhachHang.Text = "Customer:";
                lblKhoiLuongTong.Text = "Gross Wt (Kg):";
                lblKhoiLuongXe.Text = "Tare Wt (Kg):";
                lblKhoiLuongHang.Text = "Net Wt (Kg):";
                lblNgayInPhieu.Text = "Ticket Date:";
                lblKieuCan.Text = "Weigh Type:";
                lblSoChungTu.Text = "Voucher No:";
                lblGhiChu.Text = "Note:";

                btnSave.Text = "💾 Save Changes";
                btnPrintAndSave.Text = "🖨️ Print & Save";
                btnPreview.Text = "👁️ Preview";
                btnDelete.Text = "🗑️ Delete Ticket";
                btnCancel.Text = "❌ Close";

                int selKieuCan = cbKieuCan.SelectedIndex;
                cbKieuCan.Items.Clear();
                cbKieuCan.Items.AddRange(new object[] { "Service Weighing", "Export Goods", "Import Goods" });
                if (selKieuCan >= 0 && selKieuCan < cbKieuCan.Items.Count) cbKieuCan.SelectedIndex = selKieuCan;
            }
            else
            {
                this.Text = $"CHỈNH SỬA PHIẾU CÂN - STT: {PhieuCanData.STT}";
                lblSTT.Text = "STT:";
                UpdateSoXeLabelPosition("Số xe", 345, 20);
                lblLoaiHang.Text = "Loại hàng:";
                lblKhachHang.Text = "Khách hàng:";
                lblKhoiLuongTong.Text = "KL Tổng (Kg):";
                lblKhoiLuongXe.Text = "KL Xe (Kg):";
                lblKhoiLuongHang.Text = "KL Hàng (Kg):";
                lblNgayInPhieu.Text = "Ngày in phiếu:";
                lblKieuCan.Text = "Kiểu cân:";
                lblSoChungTu.Text = "Số chứng từ:";
                lblGhiChu.Text = "Ghi chú:";

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
        }

        private Label CreateLabel(string text, int x, int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            lbl.Click += ClearFocus_Click;
            return lbl;
        }

        private Label lblSoXeAsterisk, lblSoXeClose;

        private Label CreateLabelWithRedAsterisk(string text, int x, int y)
        {
            lblSoXe = new Label { Text = text + " (", Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };
            lblSoXeAsterisk = new Label { Text = "*", Location = new Point(x + 40, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.Red };
            lblSoXeClose = new Label { Text = "):", Location = new Point(x + 48, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F) };

            lblSoXe.Click += ClearFocus_Click;
            lblSoXeAsterisk.Click += ClearFocus_Click;
            lblSoXeClose.Click += ClearFocus_Click;

            this.Controls.Add(lblSoXeAsterisk);
            this.Controls.Add(lblSoXeClose);
            UpdateSoXeLabelPosition(text, x, y);
            return lblSoXe;
        }

        private void UpdateSoXeLabelPosition(string mainText, int x, int y)
        {
            if (lblSoXe == null || lblSoXeAsterisk == null || lblSoXeClose == null) return;

            lblSoXe.Text = mainText + " (";
            using (Graphics g = this.CreateGraphics())
            {
                SizeF szMain = g.MeasureString(lblSoXe.Text, lblSoXe.Font);
                int mainWidth = (int)szMain.Width - 4;

                lblSoXeAsterisk.Location = new Point(x + mainWidth, y);
                SizeF szAst = g.MeasureString("*", lblSoXeAsterisk.Font);
                int astWidth = (int)szAst.Width - 6;

                lblSoXeClose.Location = new Point(x + mainWidth + astWidth, y);
            }
        }

        private Button CreateButton(string text, int x, int y, int width, Color backColor)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 40), BackColor = backColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
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
            txtSTT.Text = PhieuCanData.STT.ToString();
            txtSoXe.Text = PhieuCanData.SoXe;
            cbLoaiHang.Text = PhieuCanData.LoaiHang;
            txtKhachHang.Text = PhieuCanData.KhachHang;
            numKhoiLuongTong.Value = PhieuCanData.KhoiLuongTong;
            numKhoiLuongXe.Value = PhieuCanData.KhoiLuongXe;
            dtpNgayInPhieu.Value = PhieuCanData.NgayInPhieu;
            txtSoChungTu.Text = PhieuCanData.SoChungTu;
            txtGhiChu.Text = PhieuCanData.GhiChu;
            cbKieuCan.SelectedItem = PhieuCanData.KieuCan;

            CalculateKLHangRealtime(null, null);
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

            UpdateDataFromInputs();
            return true;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (ValidateAndSaveInput())
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void BtnPrintAndSave_Click(object? sender, EventArgs e)
        {
            if (ValidateAndSaveInput())
            {
                IsPrintRequested = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
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
                IsDeleted = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}

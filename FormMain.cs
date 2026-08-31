using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CanXeApp.Controls;
using CanXeApp.Models;
using CanXeApp.Services;

namespace CanXeApp
{
    public partial class FormMain : Form
    {
        private DatabaseService _dbService;
        private PhieuCan _currentPhieuCan = new PhieuCan();
        private string _currentLanguage = "VI"; // "VI" hoặc "EN"

        // UI Controls
        private Panel panelHeader;
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Button btnSwitchLang;

        private TableLayoutPanel tableMainLayout;
        private GroupBox gbInput;
        private Panel panelRight;

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

        private Button btnNew, btnSave, btnPrintPreview, btnPrint, btnExportExcel;
        private Panel panelSearch;
        private Label lblSearch;
        private TextBox txtSearch;
        private Button btnSearch;
        private ToolTip toolTipSearch;

        // Controls bộ lọc ngày
        private CheckBox chkFilterDate;
        private VnDateTimePicker dtpFilterFrom;
        private VnDateTimePicker dtpFilterTo;
        private Label lblFilterTo;

        private DataGridView dgvPhieuCan;

        // ContextMenuStrip cho danh sách phiếu cân
        private ContextMenuStrip contextMenuDgv;
        private ToolStripMenuItem menuItemEdit;
        private ToolStripMenuItem menuItemDelete;

        // Danh sách các loại hàng có sẵn
        private readonly string[] _dsLoaiHang = new string[]
        {
            "Mía", "Dưa", "Phế Liệu", "Keo", "Gỗ", "Lúa", "Đậu Đỏ", "Bắp", "Mì", "Bì Cao Su"
        };

        public FormMain()
        {
            var cultureInfo = new CultureInfo("vi-VN");
            cultureInfo.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            cultureInfo.DateTimeFormat.LongTimePattern = "HH:mm:ss";
            Application.CurrentCulture = cultureInfo;

            InitializeComponentCustom();
            _dbService = new DatabaseService();

            this.WindowState = FormWindowState.Maximized;

            LoadData();
            ResetForm();

            this.Shown += (s, e) => this.ActiveControl = null;
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Cân Xe - TRẠM CÂN THỊ TRẤN HAI RIÊNG";
            this.Size = new Size(1300, 750);
            this.MinimumSize = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 247, 250);

            this.Click += ClearFocus_Click;

            try
            {
                if (File.Exists("app_icon.ico")) this.Icon = new Icon("app_icon.ico");
                else if (File.Exists("app_icon.png")) this.Icon = Icon.FromHandle(((Bitmap)Image.FromFile("app_icon.png")).GetHicon());
                else this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            // 1. Header (Top Dock)
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.FromArgb(24, 119, 242)
            };
            lblHeaderTitle = new Label
            {
                Text = "TRẠM CÂN THỊ TRẤN HAI RIÊNG",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 12),
                AutoSize = true
            };
            lblHeaderSub = new Label
            {
                Text = "Đ. Hoàng Hoa Thám - TT. Hai Riêng - X. Sông Hinh - T. Đắk Lắk | SĐT: 0918.172.220",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(230, 240, 255),
                Location = new Point(22, 46),
                AutoSize = true
            };

            // Button Chuyển đổi ngôn ngữ ở góc trên bên phải Header
            btnSwitchLang = new Button
            {
                Text = "🌐 VI (Tiếng Việt)",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(panelHeader.Width - 180, 20),
                Size = new Size(160, 38),
                BackColor = Color.FromArgb(255, 255, 255),
                ForeColor = Color.FromArgb(24, 119, 242),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSwitchLang.FlatAppearance.BorderSize = 0;
            btnSwitchLang.Click += BtnSwitchLang_Click;

            panelHeader.Controls.Add(lblHeaderTitle);
            panelHeader.Controls.Add(lblHeaderSub);
            panelHeader.Controls.Add(btnSwitchLang);
            this.Controls.Add(panelHeader);

            // Responsive location cho btnSwitchLang khi resize header
            panelHeader.Resize += (s, e) =>
            {
                btnSwitchLang.Location = new Point(panelHeader.Width - 180, 20);
            };

            // 2. TableMainLayout (Responsive Dock Fill: 2 cột)
            tableMainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            tableMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 630F));
            tableMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 3. GroupBox Nhập liệu (Cột trái)
            gbInput = new GroupBox
            {
                Text = "THÔNG TIN PHIẾU CÂN XE MỚI",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(30, 40, 60),
                Margin = new Padding(5)
            };
            gbInput.Click += ClearFocus_Click;

            int y = 30;
            int lblX1 = 15, txtX1 = 130, txtWidth1 = 145;
            int lblX2 = 295, txtX2 = 410, txtWidth2 = 185;

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
            cbLoaiHang = new ComboBox
            {
                Location = new Point(txtX1, y - 3),
                Width = txtWidth1,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                MaxLength = 50
            };
            cbLoaiHang.Items.AddRange(_dsLoaiHang);
            cbLoaiHang.Leave += CbLoaiHang_Leave;
            cbLoaiHang.Enter += Input_SelectAllOnEnter;

            lblKhachHang = CreateLabel("Khách hàng:", lblX2, y);
            txtKhachHang = new TextBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, Font = new Font("Segoe UI", 10F, FontStyle.Bold), MaxLength = 50 };
            txtKhachHang.Leave += TxtKhachHang_Leave;
            txtKhachHang.Enter += Input_SelectAllOnEnter;
            txtKhachHang.MouseClick += Input_SelectAllOnEnter;

            y += 40;
            // Khối lượng tổng & Ngày in phiếu
            lblKhoiLuongTong = CreateLabel("KL Tổng (Kg):", lblX1, y);
            numKhoiLuongTong = new FormattedNumericTextBox
            {
                Location = new Point(txtX1, y - 3),
                Width = txtWidth1
            };
            numKhoiLuongTong.TextChanged += CalculateKLHangRealtime;

            lblNgayInPhieu = CreateLabel("Ngày in phiếu:", lblX2, y);
            dtpNgayInPhieu = new VnDateTimePicker { Location = new Point(txtX2, y - 3), Width = txtWidth2, CustomFormat = "dd/MM/yyyy HH:mm", PreventFutureDates = true, Language = _currentLanguage };

            y += 40;
            // Khối lượng xe & Kiểu cân
            lblKhoiLuongXe = CreateLabel("KL Xe (Kg):", lblX1, y);
            numKhoiLuongXe = new FormattedNumericTextBox
            {
                Location = new Point(txtX1, y - 3),
                Width = txtWidth1
            };
            numKhoiLuongXe.TextChanged += CalculateKLHangRealtime;

            lblKieuCan = CreateLabel("Kiểu cân:", lblX2, y);
            cbKieuCan = new ComboBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            cbKieuCan.Items.AddRange(new object[] { "Cân dịch vụ", "Xuất hàng", "Nhập hàng" });
            cbKieuCan.SelectedIndex = 0;

            y += 40;
            // Khối lượng hàng & Số chứng từ (Giới hạn 50 ký tự)
            lblKhoiLuongHang = CreateLabel("KL Hàng (Kg):", lblX1, y);
            txtKhoiLuongHang = new TextBox
            {
                Location = new Point(txtX1, y - 3),
                Width = txtWidth1,
                ReadOnly = true,
                Enabled = false,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Red
            };

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

            y += 50;
            // Buttons
            btnNew = CreateButton("➕ Tạo Mới", 15, y, 135, Color.FromArgb(40, 167, 69));
            btnSave = CreateButton("💾 Lưu Phiếu", 160, y, 135, Color.FromArgb(0, 123, 255));
            btnPrintPreview = CreateButton("👁️ Xem Trước", 305, y, 135, Color.FromArgb(23, 162, 184));
            btnPrint = CreateButton("🖨️ In Và Lưu Phiếu", 450, y, 155, Color.FromArgb(220, 53, 69));

            y += 45;
            btnExportExcel = CreateButton("📊 Xuất Danh Sách Excel", 15, y, 280, Color.FromArgb(40, 167, 69));

            gbInput.Controls.AddRange(new Control[] {
                lblSTT, txtSTT, lblSoXe, txtSoXe,
                lblLoaiHang, cbLoaiHang, lblKhachHang, txtKhachHang,
                lblKhoiLuongTong, numKhoiLuongTong, lblNgayInPhieu, dtpNgayInPhieu,
                lblKhoiLuongXe, numKhoiLuongXe, lblKieuCan, cbKieuCan,
                lblKhoiLuongHang, txtKhoiLuongHang, lblSoChungTu, txtSoChungTu,
                lblGhiChu, txtGhiChu,
                btnNew, btnSave, btnPrintPreview, btnPrint, btnExportExcel
            });

            // 4. Panel bên phải
            panelRight = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            panelRight.Click += ClearFocus_Click;

            panelSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45
            };
            panelSearch.Click += ClearFocus_Click;

            lblSearch = CreateLabel("Từ khóa:", 5, 10);
            txtSearch = new TextBox { Location = new Point(70, 7), Width = 180, Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), PlaceholderText = "Số xe, Loại hàng, Khách hàng..." };
            txtSearch.Enter += Input_SelectAllOnEnter;
            txtSearch.MouseClick += Input_SelectAllOnEnter;

            toolTipSearch = new ToolTip
            {
                ToolTipTitle = "Phạm vi tìm kiếm từ khóa:",
                IsBalloon = false,
                InitialDelay = 100,
                ReshowDelay = 100,
                ShowAlways = true
            };
            string searchHint = "Bạn có thể nhập từ khóa để tìm theo:\n• Số xe (Biển số)\n• Loại hàng (Mía, Keo...)\n• Tên khách hàng\n• Mã số STT phiếu cân";
            toolTipSearch.SetToolTip(txtSearch, searchHint);
            toolTipSearch.SetToolTip(lblSearch, searchHint);

            chkFilterDate = new CheckBox
            {
                Text = "Lọc theo ngày:",
                Location = new Point(265, 9),
                AutoSize = true,
                Checked = false
            };

            dtpFilterFrom = new VnDateTimePicker
            {
                Location = new Point(390, 6),
                Width = 120,
                CustomFormat = "dd/MM/yyyy",
                Enabled = false,
                PreventFutureDates = true,
                ShowTimePicker = false,
                Language = _currentLanguage
            };

            lblFilterTo = CreateLabel("đến", 518, 10);

            dtpFilterTo = new VnDateTimePicker
            {
                Location = new Point(548, 6),
                Width = 120,
                CustomFormat = "dd/MM/yyyy",
                Enabled = false,
                PreventFutureDates = true,
                ShowTimePicker = false,
                Language = _currentLanguage
            };

            btnSearch = CreateButton("🔍 Tìm kiếm", 678, 5, 110, Color.FromArgb(108, 117, 125));
            btnSearch.Height = 30;

            chkFilterDate.CheckedChanged += (s, e) =>
            {
                dtpFilterFrom.Enabled = chkFilterDate.Checked;
                dtpFilterTo.Enabled = chkFilterDate.Checked;
            };

            panelSearch.Controls.Add(lblSearch);
            panelSearch.Controls.Add(txtSearch);
            panelSearch.Controls.Add(chkFilterDate);
            panelSearch.Controls.Add(dtpFilterFrom);
            panelSearch.Controls.Add(lblFilterTo);
            panelSearch.Controls.Add(dtpFilterTo);
            panelSearch.Controls.Add(btnSearch);

            dgvPhieuCan = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 28 },
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(240, 245, 255) }
            };

            contextMenuDgv = new ContextMenuStrip();
            menuItemEdit = new ToolStripMenuItem("✏️ Chỉnh sửa phiếu này", null, (s, e) => OpenEditModal());
            menuItemDelete = new ToolStripMenuItem("🗑️ Xóa phiếu này", null, (s, e) => DeleteSelectedPhieu());
            contextMenuDgv.Items.Add(menuItemEdit);
            contextMenuDgv.Items.Add(menuItemDelete);
            dgvPhieuCan.ContextMenuStrip = contextMenuDgv;

            dgvPhieuCan.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenEditModal();
            };

            panelRight.Controls.Add(dgvPhieuCan);
            panelRight.Controls.Add(panelSearch);

            tableMainLayout.Controls.Add(gbInput, 0, 0);
            tableMainLayout.Controls.Add(panelRight, 1, 0);

            this.Controls.Add(tableMainLayout);
            tableMainLayout.BringToFront();

            // Events for buttons
            btnNew.Click += (s, e) => ResetForm();
            btnSave.Click += BtnSave_Click;
            btnPrintPreview.Click += BtnPrintPreview_Click;
            btnPrint.Click += BtnPrint_Click;
            btnExportExcel.Click += BtnExportExcel_Click;

            btnSearch.Click += (s, e) => LoadDataWithFilter();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadDataWithFilter(); };
        }

        private void BtnSwitchLang_Click(object? sender, EventArgs e)
        {
            _currentLanguage = _currentLanguage == "VI" ? "EN" : "VI";
            ApplyLanguageUI();
        }

        private void ApplyLanguageUI()
        {
            dtpNgayInPhieu.Language = _currentLanguage;
            dtpFilterFrom.Language = _currentLanguage;
            dtpFilterTo.Language = _currentLanguage;

            if (_currentLanguage == "EN")
            {
                btnSwitchLang.Text = "🌐 EN (English)";
                gbInput.Text = "NEW WEIGHING TICKET INFO";
                lblSTT.Text = "No:";
                UpdateSoXeLabelPosition("Plate No", 295, 30);
                lblLoaiHang.Text = "Goods Type:";
                lblKhachHang.Text = "Customer:";
                lblKhoiLuongTong.Text = "Gross Wt (Kg):";
                lblKhoiLuongXe.Text = "Tare Wt (Kg):";
                lblKhoiLuongHang.Text = "Net Wt (Kg):";
                lblNgayInPhieu.Text = "Ticket Date:";
                lblKieuCan.Text = "Weigh Type:";
                lblSoChungTu.Text = "Voucher No:";
                lblGhiChu.Text = "Note:";

                btnNew.Text = "➕ New";
                btnSave.Text = "💾 Save Ticket";
                btnPrintPreview.Text = "👁️ Preview";
                btnPrint.Text = "🖨️ Print & Save";
                btnExportExcel.Text = "📊 Export Excel List";

                lblSearch.Text = "Keyword:";
                txtSearch.PlaceholderText = "Plate No, Goods, Customer...";
                chkFilterDate.Text = "Filter by date:";
                lblFilterTo.Text = "to";
                btnSearch.Text = "🔍 Search";

                menuItemEdit.Text = "✏️ Edit this ticket";
                menuItemDelete.Text = "🗑️ Delete this ticket";

                int selKieuCan = cbKieuCan.SelectedIndex;
                cbKieuCan.Items.Clear();
                cbKieuCan.Items.AddRange(new object[] { "Service Weighing", "Export Goods", "Import Goods" });
                if (selKieuCan >= 0 && selKieuCan < cbKieuCan.Items.Count) cbKieuCan.SelectedIndex = selKieuCan;
            }
            else
            {
                btnSwitchLang.Text = "🌐 VI (Tiếng Việt)";
                gbInput.Text = "THÔNG TIN PHIẾU CÂN XE MỚI";
                lblSTT.Text = "STT:";
                UpdateSoXeLabelPosition("Số xe", 295, 30);
                lblLoaiHang.Text = "Loại hàng:";
                lblKhachHang.Text = "Khách hàng:";
                lblKhoiLuongTong.Text = "KL Tổng (Kg):";
                lblKhoiLuongXe.Text = "KL Xe (Kg):";
                lblKhoiLuongHang.Text = "KL Hàng (Kg):";
                lblNgayInPhieu.Text = "Ngày in phiếu:";
                lblKieuCan.Text = "Kiểu cân:";
                lblSoChungTu.Text = "Số chứng từ:";
                lblGhiChu.Text = "Ghi chú:";

                btnNew.Text = "➕ Tạo Mới";
                btnSave.Text = "💾 Lưu Phiếu";
                btnPrintPreview.Text = "👁️ Xem Trước";
                btnPrint.Text = "🖨️ In Và Lưu Phiếu";
                btnExportExcel.Text = "📊 Xuất Danh Sách Excel";

                lblSearch.Text = "Từ khóa:";
                txtSearch.PlaceholderText = "Số xe, Loại hàng, Khách hàng...";
                chkFilterDate.Text = "Lọc theo ngày:";
                lblFilterTo.Text = "đến";
                btnSearch.Text = "🔍 Tìm kiếm";

                menuItemEdit.Text = "✏️ Chỉnh sửa phiếu này";
                menuItemDelete.Text = "🗑️ Xóa phiếu này";

                int selKieuCan = cbKieuCan.SelectedIndex;
                cbKieuCan.Items.Clear();
                cbKieuCan.Items.AddRange(new object[] { "Cân dịch vụ", "Xuất hàng", "Nhập hàng" });
                if (selKieuCan >= 0 && selKieuCan < cbKieuCan.Items.Count) cbKieuCan.SelectedIndex = selKieuCan;
            }

            LoadDataWithFilter();
        }

        private Label CreateLabel(string text, int x, int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Regular) };
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

            gbInput.Controls.Add(lblSoXeAsterisk);
            gbInput.Controls.Add(lblSoXeClose);
            UpdateSoXeLabelPosition(text, x, y);
            return lblSoXe;
        }

        private void UpdateSoXeLabelPosition(string mainText, int x, int y)
        {
            if (lblSoXe == null || lblSoXeAsterisk == null || lblSoXeClose == null) return;

            lblSoXe.Text = mainText + " (";
            using (Graphics g = gbInput.CreateGraphics())
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
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 38), BackColor = backColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
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
                txtKhoiLuongHang.Text = _currentLanguage == "EN" ? "TARE > GROSS!" : "KL XE > KL Tổng!";
            }
            else
            {
                txtKhoiLuongHang.BackColor = Color.Yellow;
                txtKhoiLuongHang.ForeColor = Color.DarkRed;
                txtKhoiLuongHang.Text = Math.Max(0, tong - xe).ToString("#,##0", new CultureInfo("de-DE"));
            }
        }

        private void ResetForm()
        {
            _currentPhieuCan = new PhieuCan
            {
                Id = 0,
                STT = _dbService.GetNextSTT(),
                NgayInPhieu = DateTime.Now
            };

            txtSTT.Text = _currentPhieuCan.STT.ToString();
            txtSoXe.Text = "";
            cbLoaiHang.Text = "Bì Cao Su";
            txtKhachHang.Text = "";
            numKhoiLuongTong.Value = 0;
            numKhoiLuongXe.Value = 0;
            dtpNgayInPhieu.Value = DateTime.Now;
            txtSoChungTu.Text = "";
            txtGhiChu.Text = "";
            cbKieuCan.SelectedIndex = 0;

            CalculateKLHangRealtime(null, null);
            this.ActiveControl = null;
        }

        private void LoadDataWithFilter()
        {
            string keyword = txtSearch.Text.Trim();
            DateTime? fromDate = chkFilterDate.Checked ? dtpFilterFrom.Value.Date : null;
            DateTime? toDate = chkFilterDate.Checked ? dtpFilterTo.Value.Date : null;

            LoadData(keyword, fromDate, toDate);
        }

        private void LoadData(string keyword = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var list = _dbService.GetAllPhieuCan(keyword, fromDate, toDate);
            dgvPhieuCan.DataSource = list;

            if (dgvPhieuCan.Columns["Id"] != null) dgvPhieuCan.Columns["Id"].Visible = false;
            if (dgvPhieuCan.Columns["TramCanName"] != null) dgvPhieuCan.Columns["TramCanName"].Visible = false;
            if (dgvPhieuCan.Columns["TramCanAddress"] != null) dgvPhieuCan.Columns["TramCanAddress"].Visible = false;
            if (dgvPhieuCan.Columns["TramCanPhone"] != null) dgvPhieuCan.Columns["TramCanPhone"].Visible = false;

            if (dgvPhieuCan.Columns["SoChungTu"] != null) dgvPhieuCan.Columns["SoChungTu"].Visible = false;
            if (dgvPhieuCan.Columns["GhiChu"] != null) dgvPhieuCan.Columns["GhiChu"].Visible = false;
            if (dgvPhieuCan.Columns["KieuCan"] != null) dgvPhieuCan.Columns["KieuCan"].Visible = false;
            if (dgvPhieuCan.Columns["ChuHang"] != null) dgvPhieuCan.Columns["ChuHang"].Visible = false;
            if (dgvPhieuCan.Columns["TaiXe"] != null) dgvPhieuCan.Columns["TaiXe"].Visible = false;
            if (dgvPhieuCan.Columns["NguoiCan"] != null) dgvPhieuCan.Columns["NguoiCan"].Visible = false;

            foreach (DataGridViewColumn col in dgvPhieuCan.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Automatic;
            }

            if (dgvPhieuCan.Columns["STT"] != null)
            {
                dgvPhieuCan.Columns["STT"].HeaderText = _currentLanguage == "EN" ? "No" : "STT";
                dgvPhieuCan.Columns["STT"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvPhieuCan.Columns["STT"].Width = 60;
            }

            if (dgvPhieuCan.Columns["SoXe"] != null) dgvPhieuCan.Columns["SoXe"].HeaderText = _currentLanguage == "EN" ? "Plate No" : "Số xe";
            if (dgvPhieuCan.Columns["LoaiHang"] != null) dgvPhieuCan.Columns["LoaiHang"].HeaderText = _currentLanguage == "EN" ? "Goods Type" : "Loại hàng";
            if (dgvPhieuCan.Columns["KhachHang"] != null) dgvPhieuCan.Columns["KhachHang"].HeaderText = _currentLanguage == "EN" ? "Customer" : "Khách hàng";

            var cultureDot = new CultureInfo("de-DE"); // Dấu chấm phân cách hàng nghìn

            if (dgvPhieuCan.Columns["KhoiLuongTong"] != null)
            {
                dgvPhieuCan.Columns["KhoiLuongTong"].HeaderText = _currentLanguage == "EN" ? "Gross Wt" : "KL Tổng";
                dgvPhieuCan.Columns["KhoiLuongTong"].DefaultCellStyle.Format = "#,##0";
                dgvPhieuCan.Columns["KhoiLuongTong"].DefaultCellStyle.FormatProvider = cultureDot;
            }
            if (dgvPhieuCan.Columns["KhoiLuongXe"] != null)
            {
                dgvPhieuCan.Columns["KhoiLuongXe"].HeaderText = _currentLanguage == "EN" ? "Tare Wt" : "KL Xe";
                dgvPhieuCan.Columns["KhoiLuongXe"].DefaultCellStyle.Format = "#,##0";
                dgvPhieuCan.Columns["KhoiLuongXe"].DefaultCellStyle.FormatProvider = cultureDot;
            }
            if (dgvPhieuCan.Columns["KhoiLuongHang"] != null)
            {
                dgvPhieuCan.Columns["KhoiLuongHang"].HeaderText = _currentLanguage == "EN" ? "Net Wt (Kg)" : "KL Hàng (Kg)";
                dgvPhieuCan.Columns["KhoiLuongHang"].DefaultCellStyle.Format = "#,##0";
                dgvPhieuCan.Columns["KhoiLuongHang"].DefaultCellStyle.FormatProvider = cultureDot;
            }

            if (dgvPhieuCan.Columns["NgayInPhieu"] != null)
            {
                dgvPhieuCan.Columns["NgayInPhieu"].HeaderText = _currentLanguage == "EN" ? "Ticket Date" : "Ngày in phiếu";
                dgvPhieuCan.Columns["NgayInPhieu"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
        }

        private void GetInputData()
        {
            _currentPhieuCan.SoXe = txtSoXe.Text.Trim();
            _currentPhieuCan.LoaiHang = cbLoaiHang.Text.Trim();
            _currentPhieuCan.KhachHang = txtKhachHang.Text.Trim();
            _currentPhieuCan.KhoiLuongTong = numKhoiLuongTong.Value;
            _currentPhieuCan.KhoiLuongXe = numKhoiLuongXe.Value;
            _currentPhieuCan.NgayInPhieu = dtpNgayInPhieu.Value;
            _currentPhieuCan.SoChungTu = txtSoChungTu.Text.Trim();
            _currentPhieuCan.GhiChu = txtGhiChu.Text.Trim();
            _currentPhieuCan.KieuCan = cbKieuCan.SelectedItem?.ToString() ?? "Cân dịch vụ";
        }

        private bool SaveCurrentPhieu()
        {
            if (string.IsNullOrWhiteSpace(txtSoXe.Text))
            {
                string msg = _currentLanguage == "EN" ? "Please enter Plate Number!" : "Vui lòng nhập Biển số xe!";
                MessageBox.Show(msg, _currentLanguage == "EN" ? "Notice" : "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            GetInputData();
            _dbService.SavePhieuCan(_currentPhieuCan);
            return true;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (SaveCurrentPhieu())
            {
                LoadDataWithFilter();
                ResetForm();
            }
        }

        private void BtnPrint_Click(object? sender, EventArgs e)
        {
            if (SaveCurrentPhieu())
            {
                LoadDataWithFilter();
                PrintService ps = new PrintService(_currentPhieuCan);
                ps.PrintDirect();
                ResetForm();
            }
        }

        private void BtnPrintPreview_Click(object? sender, EventArgs e)
        {
            GetInputData();
            PrintService ps = new PrintService(_currentPhieuCan);
            ps.PrintPreview();
        }

        private void OpenEditModal()
        {
            if (dgvPhieuCan.CurrentRow != null && dgvPhieuCan.CurrentRow.DataBoundItem is PhieuCan item)
            {
                FormEditPhieu editForm = new FormEditPhieu(item, _currentLanguage);
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (editForm.IsDeleted)
                    {
                        _dbService.DeletePhieuCan(item.Id);
                    }
                    else
                    {
                        _dbService.SavePhieuCan(editForm.PhieuCanData);
                        if (editForm.IsPrintRequested)
                        {
                            PrintService ps = new PrintService(editForm.PhieuCanData);
                            ps.PrintDirect();
                        }
                    }
                    LoadDataWithFilter();
                }
            }
        }

        private void DeleteSelectedPhieu()
        {
            if (dgvPhieuCan.CurrentRow != null && dgvPhieuCan.CurrentRow.DataBoundItem is PhieuCan item)
            {
                string msg = _currentLanguage == "EN" ? $"Are you sure you want to delete Weighing Ticket No {item.STT}?" : $"Bạn có chắc chắn muốn xóa Phiếu cân STT {item.STT} không?";
                string title = _currentLanguage == "EN" ? "Confirm Delete" : "Xác nhận xóa";
                var confirm = CustomMessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    _dbService.DeletePhieuCan(item.Id);
                    LoadDataWithFilter();
                }
            }
        }

        private void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            DateTime? fromDate = chkFilterDate.Checked ? dtpFilterFrom.Value.Date : null;
            DateTime? toDate = chkFilterDate.Checked ? dtpFilterTo.Value.Date : null;

            var list = _dbService.GetAllPhieuCan(keyword, fromDate, toDate);
            if (list.Count == 0)
            {
                string msg = _currentLanguage == "EN" ? "No ticket data available to export to Excel!" : "Chưa có dữ liệu phiếu cân để xuất ra Excel!";
                MessageBox.Show(msg, _currentLanguage == "EN" ? "Notice" : "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel CSV File (*.csv)|*.csv",
                FileName = $"DanhSachPhieuCan_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = _currentLanguage == "EN" ? "Select Excel Save Location" : "Chọn nơi lưu file Excel"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("STT,Số Xe,Loại Hàng,Khách Hàng,KL Tổng (Kg),KL Xe (Kg),KL Hàng (Kg),Ngày In Phiếu,Số Chứng Từ,Ghi Chú,Kiểu Cân");

                    foreach (var item in list)
                    {
                        sb.AppendLine($"\"{item.STT}\",\"{item.SoXe}\",\"{item.LoaiHang}\",\"{item.KhachHang}\",\"{item.KhoiLuongTong}\",\"{item.KhoiLuongXe}\",\"{item.KhoiLuongHang}\",\"{item.NgayInPhieu:dd/MM/yyyy HH:mm}\",\"{item.SoChungTu}\",\"{item.GhiChu}\",\"{item.KieuCan}\"");
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    string successMsg = _currentLanguage == "EN" ? $"Excel file exported successfully!\nLocation:\n{sfd.FileName}" : $"Xuất file Excel thành công!\nNơi lưu file:\n{sfd.FileName}";
                    MessageBox.Show(successMsg, _currentLanguage == "EN" ? "Success" : "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất file Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        private Button btnNew, btnSave, btnPrintPreview, btnPrint;
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
        private Button btnPrintSelected;
        private readonly HashSet<long> _selectedPhieuIds = new HashSet<long>();
        private ToolTip? _actionToolTip;
        private int _lastHoveredRow = -1;
        private string _lastHoveredBtn = "";

        // Controls Biểu đồ thống kê
        private Panel panelLeft;
        private GroupBox gbChart;
        private Button btnChartTabMonths;
        private Button btnChartTabGoods;
        private ComboBox cbChartYear;
        private Label lblChartSummary;
        private DoubleBufferedPanel pnlChartCanvas;
        private int _currentChartTab = 0; // 0 = 12 tháng, 1 = Top mặt hàng

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
                Dock = DockStyle.Top,
                Height = 325,
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

            gbInput.Controls.AddRange(new Control[] {
                lblSTT, txtSTT, lblSoXe, txtSoXe,
                lblLoaiHang, cbLoaiHang, lblKhachHang, txtKhachHang,
                lblKhoiLuongTong, numKhoiLuongTong, lblNgayInPhieu, dtpNgayInPhieu,
                lblKhoiLuongXe, numKhoiLuongXe, lblKieuCan, cbKieuCan,
                lblKhoiLuongHang, txtKhoiLuongHang, lblSoChungTu, txtSoChungTu,
                lblGhiChu, txtGhiChu,
                btnNew, btnSave, btnPrintPreview, btnPrint
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

            lblSearch = CreateLabel("Từ khóa:", 8, 11);
            txtSearch = new TextBox { Location = new Point(72, 7), Width = 140, Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), PlaceholderText = "Số xe, hàng..." };
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
                Location = new Point(228, 9),
                AutoSize = true,
                Checked = false
            };

            dtpFilterFrom = new VnDateTimePicker
            {
                Location = new Point(350, 6),
                Width = 115,
                CustomFormat = "dd/MM/yyyy",
                Enabled = false,
                PreventFutureDates = true,
                ShowTimePicker = false,
                Language = _currentLanguage
            };

            lblFilterTo = CreateLabel("đến", 475, 11);

            dtpFilterTo = new VnDateTimePicker
            {
                Location = new Point(512, 6),
                Width = 115,
                CustomFormat = "dd/MM/yyyy",
                Enabled = false,
                PreventFutureDates = true,
                ShowTimePicker = false,
                Language = _currentLanguage
            };

            btnSearch = CreateButton("🔍 Tìm kiếm", 638, 5, 95, Color.FromArgb(108, 117, 125));
            btnSearch.Height = 31;

            btnPrintSelected = new Button
            {
                Location = new Point(panelSearch.Width - 195, 5),
                Size = new Size(185, 31),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Default,
                Enabled = false
            };
            btnPrintSelected.FlatAppearance.BorderSize = 0;
            btnPrintSelected.Click += BtnPrintSelected_Click;
            UpdatePrintSelectedButtonState();

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
            panelSearch.Controls.Add(btnPrintSelected);

            _actionToolTip = new ToolTip { InitialDelay = 100, ReshowDelay = 100 };

            dgvPhieuCan = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
                ColumnHeadersHeight = 34,
                RowTemplate = { Height = 32 },
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 238, 242)
            };

            // Bỏ focus row màu xanh: gán màu selection trùng với màu nền dòng bình thường
            dgvPhieuCan.DefaultCellStyle.SelectionBackColor = Color.White;
            dgvPhieuCan.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 60);
            dgvPhieuCan.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 254);
            dgvPhieuCan.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(246, 249, 254);
            dgvPhieuCan.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 60);
            dgvPhieuCan.RowHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            dgvPhieuCan.RowHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 40, 60);

            dgvPhieuCan.SelectionChanged += (s, e) => dgvPhieuCan.ClearSelection();
            dgvPhieuCan.DataBindingComplete += (s, e) => dgvPhieuCan.ClearSelection();
            dgvPhieuCan.CurrentCellDirtyStateChanged += DgvPhieuCan_CurrentCellDirtyStateChanged;
            dgvPhieuCan.CellValueChanged += DgvPhieuCan_CellValueChanged;
            dgvPhieuCan.CellPainting += DgvPhieuCan_CellPainting;
            dgvPhieuCan.CellMouseClick += DgvPhieuCan_CellMouseClick;
            dgvPhieuCan.CellMouseMove += DgvPhieuCan_CellMouseMove;
            dgvPhieuCan.CellMouseLeave += DgvPhieuCan_CellMouseLeave;
            dgvPhieuCan.ColumnHeaderMouseClick += DgvPhieuCan_ColumnHeaderMouseClick;
            dgvPhieuCan.CellContentClick += DgvPhieuCan_CellContentClick;
            dgvPhieuCan.CellMouseDown += DgvPhieuCan_CellMouseDown;

            contextMenuDgv = new ContextMenuStrip();
            menuItemEdit = new ToolStripMenuItem("✏️ Chỉnh sửa phiếu này", null, (s, e) => OpenEditModal());
            menuItemDelete = new ToolStripMenuItem("🗑️ Xóa phiếu này", null, (s, e) => DeleteSelectedPhieu());
            contextMenuDgv.Items.Add(menuItemEdit);
            contextMenuDgv.Items.Add(menuItemDelete);
            dgvPhieuCan.ContextMenuStrip = contextMenuDgv;

            dgvPhieuCan.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    string colName = dgvPhieuCan.Columns[e.ColumnIndex].Name;
                    if (colName != "colAction" && colName != "colSelect")
                    {
                        OpenEditModal();
                    }
                }
            };

            panelRight.Controls.Add(dgvPhieuCan);
            panelRight.Controls.Add(panelSearch);

            // 3. Khung bên trái: panelLeft chứa gbInput (phía trên) và gbChart (phía dưới)
            panelLeft = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            // GroupBox Biểu đồ thống kê
            gbChart = new GroupBox
            {
                Text = "📊 THỐNG KÊ HOẠT ĐỘNG CÂN",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(30, 40, 60),
                Margin = new Padding(5)
            };

            Panel panelChartHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.Transparent
            };

            btnChartTabMonths = new Button
            {
                Text = "📅 Số phiếu 12 tháng",
                Location = new Point(10, 5),
                Size = new Size(150, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChartTabMonths.FlatAppearance.BorderSize = 0;
            btnChartTabMonths.Click += (s, e) => SetChartTab(0);

            btnChartTabGoods = new Button
            {
                Text = "📦 Top mặt hàng",
                Location = new Point(165, 5),
                Size = new Size(135, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChartTabGoods.FlatAppearance.BorderSize = 0;
            btnChartTabGoods.Click += (s, e) => SetChartTab(1);

            Label lblChartYear = new Label
            {
                Text = "Năm:",
                Location = new Point(310, 9),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 80, 95)
            };

            cbChartYear = new ComboBox
            {
                Location = new Point(350, 6),
                Width = 75,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            cbChartYear.SelectedIndexChanged += (s, e) => {
                UpdateChartSummaryText();
                pnlChartCanvas.Invalidate();
            };

            lblChartSummary = new Label
            {
                Location = new Point(432, 9),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 119, 242)
            };

            panelChartHeader.Controls.Add(btnChartTabMonths);
            panelChartHeader.Controls.Add(btnChartTabGoods);
            panelChartHeader.Controls.Add(lblChartYear);
            panelChartHeader.Controls.Add(cbChartYear);
            panelChartHeader.Controls.Add(lblChartSummary);

            pnlChartCanvas = new DoubleBufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            pnlChartCanvas.Paint += PnlChartCanvas_Paint;

            gbChart.Controls.Add(pnlChartCanvas);
            gbChart.Controls.Add(panelChartHeader);

            panelLeft.Controls.Add(gbChart);
            panelLeft.Controls.Add(gbInput);
            gbChart.BringToFront();

            tableMainLayout.Controls.Add(panelLeft, 0, 0);
            tableMainLayout.Controls.Add(panelRight, 1, 0);

            this.Controls.Add(tableMainLayout);
            tableMainLayout.BringToFront();

            // Events for buttons
            btnNew.Click += (s, e) => ResetForm();
            btnSave.Click += BtnSave_Click;
            btnPrintPreview.Click += BtnPrintPreview_Click;
            btnPrint.Click += BtnPrint_Click;

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

            UpdatePrintSelectedButtonState();
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

            EnsureCustomColumns();

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
                if (col.Name != "colSelect")
                {
                    col.ReadOnly = true;
                    col.SortMode = DataGridViewColumnSortMode.Automatic;
                }
            }

            if (dgvPhieuCan.Columns["STT"] != null)
            {
                dgvPhieuCan.Columns["STT"].HeaderText = _currentLanguage == "EN" ? "No" : "STT";
                dgvPhieuCan.Columns["STT"].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dgvPhieuCan.Columns["STT"].Width = 55;
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

            // Đồng bộ trạng thái checkbox từ _selectedPhieuIds
            foreach (DataGridViewRow row in dgvPhieuCan.Rows)
            {
                if (row.DataBoundItem is PhieuCan item)
                {
                    row.Cells["colSelect"].Value = _selectedPhieuIds.Contains(item.Id);
                }
            }

            UpdatePrintSelectedButtonState();
            dgvPhieuCan.ClearSelection();
            UpdateChartData();
        }

        private void EnsureCustomColumns()
        {
            if (!dgvPhieuCan.Columns.Contains("colSelect"))
            {
                var colSelect = new DataGridViewCheckBoxColumn
                {
                    Name = "colSelect",
                    HeaderText = "",
                    Width = 36,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Resizable = DataGridViewTriState.False,
                    ReadOnly = false
                };
                dgvPhieuCan.Columns.Insert(0, colSelect);
            }
            dgvPhieuCan.Columns["colSelect"].DisplayIndex = 0;

            if (!dgvPhieuCan.Columns.Contains("colAction"))
            {
                var colAction = new DataGridViewTextBoxColumn
                {
                    Name = "colAction",
                    HeaderText = "",
                    Width = 115,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Resizable = DataGridViewTriState.False,
                    ReadOnly = true
                };
                dgvPhieuCan.Columns.Add(colAction);
            }
            dgvPhieuCan.Columns["colAction"].DisplayIndex = dgvPhieuCan.Columns.Count - 1;
            dgvPhieuCan.Columns["colAction"].HeaderText = "";
        }

        private void UpdatePrintSelectedButtonState()
        {
            if (btnPrintSelected == null) return;

            int count = _selectedPhieuIds.Count;
            if (count == 0)
            {
                btnPrintSelected.Enabled = false;
                btnPrintSelected.BackColor = Color.FromArgb(220, 224, 230);
                btnPrintSelected.ForeColor = Color.FromArgb(135, 140, 145);
                btnPrintSelected.Cursor = Cursors.Default;
                btnPrintSelected.Text = _currentLanguage == "EN" ? "🖨️ Selected Tickets (0)" : "🖨️ Phiếu đã chọn (0)";
            }
            else
            {
                btnPrintSelected.Enabled = true;
                btnPrintSelected.BackColor = Color.FromArgb(40, 167, 69);
                btnPrintSelected.ForeColor = Color.White;
                btnPrintSelected.Cursor = Cursors.Hand;
                btnPrintSelected.Text = _currentLanguage == "EN" ? $"🖨️ Selected Tickets ({count})" : $"🖨️ Phiếu đã chọn ({count})";
            }
        }

        private void BtnPrintSelected_Click(object? sender, EventArgs e)
        {
            if (_selectedPhieuIds.Count == 0) return;

            var allTickets = _dbService.GetAllPhieuCan();
            var selectedList = allTickets.Where(p => _selectedPhieuIds.Contains(p.Id)).OrderBy(p => p.STT).ToList();

            if (selectedList.Count == 0) return;

            string confirmMsg = _currentLanguage == "EN"
                ? $"Do you want to print {selectedList.Count} selected ticket(s)?"
                : $"Bạn có chắc chắn muốn in {selectedList.Count} phiếu cân đã chọn không?";
            string confirmTitle = _currentLanguage == "EN" ? "Print Confirmation" : "Xác nhận in phiếu";

            if (CustomMessageBox.Show(confirmMsg, confirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                using (PrintDialog printDialog = new PrintDialog())
                {
                    if (printDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        foreach (var ticket in selectedList)
                        {
                            PrintService ps = new PrintService(ticket);
                            ps.PrintToPrinter(printDialog.PrinterSettings);
                        }

                        string successMsg = _currentLanguage == "EN"
                            ? $"Sent {selectedList.Count} ticket(s) to printer successfully!"
                            : $"Đã gửi {selectedList.Count} phiếu cân đến máy in thành công!";
                        MessageBox.Show(successMsg, _currentLanguage == "EN" ? "Success" : "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void DgvPhieuCan_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvPhieuCan.IsCurrentCellDirty && dgvPhieuCan.CurrentCell is DataGridViewCheckBoxCell)
            {
                dgvPhieuCan.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvPhieuCan_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colSelect")
            {
                if (dgvPhieuCan.Rows[e.RowIndex].DataBoundItem is PhieuCan item)
                {
                    bool isChecked = Convert.ToBoolean(dgvPhieuCan.Rows[e.RowIndex].Cells["colSelect"].Value);
                    if (isChecked) _selectedPhieuIds.Add(item.Id);
                    else _selectedPhieuIds.Remove(item.Id);

                    dgvPhieuCan.InvalidateCell(dgvPhieuCan.Columns["colSelect"].HeaderCell);
                    UpdatePrintSelectedButtonState();
                }
            }
        }

        private void DgvPhieuCan_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colSelect")
            {
                dgvPhieuCan.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvPhieuCan_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colSelect")
            {
                dgvPhieuCan.EndEdit();
                dgvPhieuCan.CurrentCell = null; // Bỏ focus cell hiện tại để không bị khóa trạng thái

                bool allChecked = dgvPhieuCan.Rows.Count > 0 &&
                    dgvPhieuCan.Rows.Cast<DataGridViewRow>().All(r => Convert.ToBoolean(r.Cells["colSelect"].Value));

                bool targetState = !allChecked;

                foreach (DataGridViewRow row in dgvPhieuCan.Rows)
                {
                    row.Cells["colSelect"].Value = targetState;
                    if (row.DataBoundItem is PhieuCan item)
                    {
                        if (targetState) _selectedPhieuIds.Add(item.Id);
                        else _selectedPhieuIds.Remove(item.Id);
                    }
                }

                dgvPhieuCan.EndEdit();
                dgvPhieuCan.Refresh();
                UpdatePrintSelectedButtonState();
            }
        }

        private void DgvPhieuCan_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                dgvPhieuCan.CurrentCell = dgvPhieuCan.Rows[e.RowIndex].Cells[e.ColumnIndex >= 0 ? e.ColumnIndex : 1];
            }
        }

        private void DgvPhieuCan_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            // 1. Vẽ Checkbox Chọn tất cả ở Header
            if (e.RowIndex == -1 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colSelect")
            {
                e.PaintBackground(e.CellBounds, true);
                int cbSize = 14;
                int cbX = e.CellBounds.X + (e.CellBounds.Width - cbSize) / 2;
                int cbY = e.CellBounds.Y + (e.CellBounds.Height - cbSize) / 2;
                Rectangle cbRect = new Rectangle(cbX, cbY, cbSize, cbSize);

                bool allChecked = dgvPhieuCan.Rows.Count > 0 &&
                    dgvPhieuCan.Rows.Cast<DataGridViewRow>().All(r => Convert.ToBoolean(r.Cells["colSelect"].Value));

                ButtonState state = allChecked ? ButtonState.Checked : ButtonState.Normal;
                ControlPaint.DrawCheckBox(e.Graphics, cbRect, state);

                e.Handled = true;
                return;
            }

            // 2. Vẽ 3 nút icon (Xem trước, Sửa, In) ở cột Action
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colAction")
            {
                e.PaintBackground(e.CellBounds, true);

                int btnW = 28;
                int btnH = 24;
                int gap = 5;
                int startX = e.CellBounds.X + (e.CellBounds.Width - (btnW * 3 + gap * 2)) / 2;
                int startY = e.CellBounds.Y + (e.CellBounds.Height - btnH) / 2;

                Rectangle rcPreview = new Rectangle(startX, startY, btnW, btnH);
                Rectangle rcEdit = new Rectangle(startX + btnW + gap, startY, btnW, btnH);
                Rectangle rcPrint = new Rectangle(startX + (btnW + gap) * 2, startY, btnW, btnH);

                DrawActionBtn(e.Graphics, rcPreview, "preview", Color.FromArgb(232, 244, 253), Color.FromArgb(145, 202, 255));
                DrawActionBtn(e.Graphics, rcEdit, "edit", Color.FromArgb(254, 244, 230), Color.FromArgb(254, 215, 170));
                DrawActionBtn(e.Graphics, rcPrint, "print", Color.FromArgb(235, 247, 238), Color.FromArgb(163, 230, 185));

                e.Handled = true;
                return;
            }
        }

        private void DrawActionBtn(Graphics g, Rectangle rect, string actionType, Color bg, Color border)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using var brushBg = new SolidBrush(bg);
            using var penBorder = new Pen(border);
            g.FillRectangle(brushBg, rect);
            g.DrawRectangle(penBorder, rect);

            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;

            if (actionType == "preview") // Icon mắt (Xem trước) căn giữa tuyệt đối
            {
                using var penEye = new Pen(Color.FromArgb(24, 119, 242), 1.6f);
                using var brushPupil = new SolidBrush(Color.FromArgb(24, 119, 242));

                using var path = new GraphicsPath();
                path.AddArc(cx - 7.5f, cy - 6.5f, 15f, 11f, 25, 130);
                path.AddArc(cx - 7.5f, cy - 4.5f, 15f, 11f, 205, 130);
                path.CloseFigure();
                g.DrawPath(penEye, path);

                g.FillEllipse(brushPupil, cx - 2.2f, cy - 2.2f, 4.4f, 4.4f);
            }
            else if (actionType == "edit") // Icon bút chì (Chỉnh sửa)
            {
                using var penPencil = new Pen(Color.FromArgb(217, 119, 6), 1.6f);
                using var brushTip = new SolidBrush(Color.FromArgb(217, 119, 6));

                g.DrawLine(penPencil, cx - 4.5f, cy + 4.5f, cx + 4f, cy - 4f);
                g.DrawLine(penPencil, cx - 3f, cy + 6f, cx + 5.5f, cy - 2.5f);
                g.FillPolygon(brushTip, new PointF[] {
                    new PointF(cx - 6f, cy + 6f),
                    new PointF(cx - 5.5f, cy + 3f),
                    new PointF(cx - 3f, cy + 5.5f)
                });
            }
            else if (actionType == "print") // Icon máy in (In phiếu) căn giữa tuyệt đối
            {
                using var penPrint = new Pen(Color.FromArgb(22, 163, 74), 1.5f);
                using var brushPrint = new SolidBrush(Color.FromArgb(22, 163, 74));

                // Khổ giấy trên
                g.DrawRectangle(penPrint, cx - 4f, cy - 5.5f, 8f, 3.5f);
                // Thân máy in
                g.DrawRectangle(penPrint, cx - 6.5f, cy - 2f, 13f, 6.5f);
                // Khe nhả giấy
                g.DrawLine(penPrint, cx - 4f, cy + 1f, cx + 4f, cy + 1f);
                // Giấy in ra bên dưới
                g.FillRectangle(brushPrint, cx - 4f, cy + 2.5f, 8f, 3.5f);
            }
        }

        private void DgvPhieuCan_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colAction")
            {
                if (dgvPhieuCan.Rows[e.RowIndex].DataBoundItem is PhieuCan item)
                {
                    Rectangle cellBounds = dgvPhieuCan.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    int btnW = 28;
                    int btnH = 24;
                    int gap = 5;
                    int startX = (cellBounds.Width - (btnW * 3 + gap * 2)) / 2;
                    int startY = (cellBounds.Height - btnH) / 2;

                    Rectangle rcPreview = new Rectangle(startX, startY, btnW, btnH);
                    Rectangle rcEdit = new Rectangle(startX + btnW + gap, startY, btnW, btnH);
                    Rectangle rcPrint = new Rectangle(startX + (btnW + gap) * 2, startY, btnW, btnH);

                    if (rcPreview.Contains(e.X, e.Y))
                    {
                        PrintService ps = new PrintService(item);
                        ps.PrintPreview();
                    }
                    else if (rcEdit.Contains(e.X, e.Y))
                    {
                        OpenEditModal(item);
                    }
                    else if (rcPrint.Contains(e.X, e.Y))
                    {
                        PrintService ps = new PrintService(item);
                        ps.PrintDirect();
                    }
                }
            }
        }

        private void DgvPhieuCan_CellMouseMove(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colAction")
            {
                Rectangle cellBounds = dgvPhieuCan.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                int btnW = 28;
                int btnH = 24;
                int gap = 5;
                int startX = (cellBounds.Width - (btnW * 3 + gap * 2)) / 2;
                int startY = (cellBounds.Height - btnH) / 2;

                Rectangle rcPreview = new Rectangle(startX, startY, btnW, btnH);
                Rectangle rcEdit = new Rectangle(startX + btnW + gap, startY, btnW, btnH);
                Rectangle rcPrint = new Rectangle(startX + (btnW + gap) * 2, startY, btnW, btnH);

                string btn = "";
                string tip = "";
                if (rcPreview.Contains(e.X, e.Y))
                {
                    btn = "preview";
                    tip = _currentLanguage == "EN" ? "Preview Ticket" : "Xem trước phiếu";
                }
                else if (rcEdit.Contains(e.X, e.Y))
                {
                    btn = "edit";
                    tip = _currentLanguage == "EN" ? "Edit Ticket" : "Chỉnh sửa phiếu";
                }
                else if (rcPrint.Contains(e.X, e.Y))
                {
                    btn = "print";
                    tip = _currentLanguage == "EN" ? "Print Ticket" : "In phiếu cân";
                }

                if (!string.IsNullOrEmpty(btn))
                {
                    dgvPhieuCan.Cursor = Cursors.Hand;
                    if (_lastHoveredRow != e.RowIndex || _lastHoveredBtn != btn)
                    {
                        _lastHoveredRow = e.RowIndex;
                        _lastHoveredBtn = btn;
                        _actionToolTip?.Show(tip, dgvPhieuCan, cellBounds.Left + e.X, cellBounds.Top + e.Y - 22, 1500);
                    }
                }
                else
                {
                    dgvPhieuCan.Cursor = Cursors.Default;
                    _actionToolTip?.Hide(dgvPhieuCan);
                    _lastHoveredBtn = "";
                }
            }
            else
            {
                dgvPhieuCan.Cursor = Cursors.Default;
            }
        }

        private void DgvPhieuCan_CellMouseLeave(object? sender, DataGridViewCellEventArgs e)
        {
            _actionToolTip?.Hide(dgvPhieuCan);
            _lastHoveredRow = -1;
            _lastHoveredBtn = "";
            dgvPhieuCan.Cursor = Cursors.Default;
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

        private void OpenEditModal(PhieuCan? item = null)
        {
            if (item == null && dgvPhieuCan.CurrentRow != null && dgvPhieuCan.CurrentRow.DataBoundItem is PhieuCan curr)
            {
                item = curr;
            }

            if (item != null)
            {
                FormEditPhieu editForm = new FormEditPhieu(item, _currentLanguage);
                if (editForm.ShowDialog(this) == DialogResult.OK)
                {
                    if (editForm.IsDeleted)
                    {
                        _dbService.DeletePhieuCan(item.Id);
                        _selectedPhieuIds.Remove(item.Id);
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
                    _selectedPhieuIds.Remove(item.Id);
                    LoadDataWithFilter();
                }
            }
        }

        private void SetChartTab(int tabIndex)
        {
            _currentChartTab = tabIndex;
            UpdateChartTabButtonsUI();
            pnlChartCanvas?.Invalidate();
        }

        private void UpdateChartTabButtonsUI()
        {
            if (btnChartTabMonths == null || btnChartTabGoods == null) return;

            if (_currentChartTab == 0)
            {
                btnChartTabMonths.BackColor = Color.FromArgb(24, 119, 242);
                btnChartTabMonths.ForeColor = Color.White;
                btnChartTabGoods.BackColor = Color.FromArgb(235, 240, 248);
                btnChartTabGoods.ForeColor = Color.FromArgb(70, 80, 95);
            }
            else
            {
                btnChartTabMonths.BackColor = Color.FromArgb(235, 240, 248);
                btnChartTabMonths.ForeColor = Color.FromArgb(70, 80, 95);
                btnChartTabGoods.BackColor = Color.FromArgb(24, 119, 242);
                btnChartTabGoods.ForeColor = Color.White;
            }

            btnChartTabMonths.Text = _currentLanguage == "EN" ? "📅 Monthly Tickets" : "📅 Số phiếu 12 tháng";
            btnChartTabGoods.Text = _currentLanguage == "EN" ? "📦 Top Commodities" : "📦 Top mặt hàng";
        }

        private void UpdateChartData()
        {
            if (cbChartYear == null) return;

            int currentYear = DateTime.Now.Year;
            var allTickets = _dbService.GetAllPhieuCan();
            var years = allTickets.Select(p => p.NgayInPhieu.Year).Distinct().OrderByDescending(y => y).ToList();
            if (!years.Contains(currentYear)) years.Insert(0, currentYear);

            int curSelected = cbChartYear.SelectedItem != null && int.TryParse(cbChartYear.SelectedItem.ToString(), out int yr) ? yr : currentYear;

            cbChartYear.Items.Clear();
            foreach (var y in years) cbChartYear.Items.Add(y);
            cbChartYear.SelectedItem = years.Contains(curSelected) ? curSelected : currentYear;

            UpdateChartSummaryText();
            UpdateChartTabButtonsUI();
            pnlChartCanvas?.Invalidate();
        }

        private void UpdateChartSummaryText()
        {
            if (lblChartSummary == null || cbChartYear?.SelectedItem == null) return;
            if (!int.TryParse(cbChartYear.SelectedItem.ToString(), out int selectedYear)) return;

            var allTickets = _dbService.GetAllPhieuCan();
            int totalTickets = allTickets.Count(p => p.NgayInPhieu.Year == selectedYear);
            double totalKg = allTickets.Where(p => p.NgayInPhieu.Year == selectedYear).Sum(p => p.KhoiLuongHang);

            string sumText = _currentLanguage == "EN"
                ? $"Total: {totalTickets} tickets ({totalKg:N0} Kg)"
                : $"Tổng: {totalTickets} phiếu ({totalKg:N0} Kg)";
            lblChartSummary.Text = sumText;
        }

        private void PnlChartCanvas_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int width = pnlChartCanvas.Width;
            int height = pnlChartCanvas.Height;
            if (width < 50 || height < 50) return;

            int selectedYear = DateTime.Now.Year;
            if (cbChartYear?.SelectedItem != null && int.TryParse(cbChartYear.SelectedItem.ToString(), out int yr))
            {
                selectedYear = yr;
            }

            var allTickets = _dbService.GetAllPhieuCan();
            var yearTickets = allTickets.Where(p => p.NgayInPhieu.Year == selectedYear).ToList();

            if (_currentChartTab == 0)
            {
                DrawMonthlyChart(g, width, height, selectedYear, yearTickets);
            }
            else
            {
                DrawCommoditiesChart(g, width, height, selectedYear, yearTickets);
            }
        }

        private void DrawMonthlyChart(Graphics g, int width, int height, int selectedYear, List<PhieuCan> tickets)
        {
            int[] monthlyCounts = new int[12];
            foreach (var t in tickets)
            {
                int m = t.NgayInPhieu.Month;
                if (m >= 1 && m <= 12) monthlyCounts[m - 1]++;
            }

            int maxCount = 0;
            for (int i = 0; i < 12; i++) if (monthlyCounts[i] > maxCount) maxCount = monthlyCounts[i];
            if (maxCount < 5) maxCount = 5;

            int padLeft = 32;
            int padRight = 15;
            int padTop = 22;
            int padBottom = 26;

            int chartW = width - padLeft - padRight;
            int chartH = height - padTop - padBottom;
            if (chartW < 50 || chartH < 30) return;

            using var penGrid = new Pen(Color.FromArgb(235, 238, 242), 1f) { DashStyle = DashStyle.Dash };
            using var brushText = new SolidBrush(Color.FromArgb(120, 130, 140));
            using var fontSmall = new Font("Segoe UI", 7.5F);

            int gridSteps = 4;
            for (int s = 0; s <= gridSteps; s++)
            {
                int val = (maxCount * s) / gridSteps;
                float y = padTop + chartH - (float)s / gridSteps * chartH;
                g.DrawLine(penGrid, padLeft, y, width - padRight, y);
                g.DrawString(val.ToString(), fontSmall, brushText, padLeft - 25, y - 6);
            }

            float barSlot = (float)chartW / 12f;
            float barWidth = Math.Min(barSlot * 0.65f, 26f);

            using var fontBarVal = new Font("Segoe UI", 8F, FontStyle.Bold);
            using var fontMonth = new Font("Segoe UI", 8F, FontStyle.Regular);

            int currentMonth = DateTime.Now.Month;
            bool isCurrentYear = (selectedYear == DateTime.Now.Year);

            for (int i = 0; i < 12; i++)
            {
                int count = monthlyCounts[i];
                float barH = ((float)count / maxCount) * chartH;
                float x = padLeft + i * barSlot + (barSlot - barWidth) / 2f;
                float y = padTop + chartH - barH;

                bool isCurr = isCurrentYear && (i + 1 == currentMonth);

                Color fillTop = isCurr ? Color.FromArgb(24, 119, 242) : (count > 0 ? Color.FromArgb(96, 165, 250) : Color.FromArgb(226, 232, 240));
                Color fillBottom = isCurr ? Color.FromArgb(37, 99, 235) : (count > 0 ? Color.FromArgb(147, 197, 253) : Color.FromArgb(241, 245, 249));
                Color borderColor = isCurr ? Color.FromArgb(29, 78, 216) : (count > 0 ? Color.FromArgb(59, 130, 246) : Color.FromArgb(203, 213, 225));

                RectangleF rectBar = new RectangleF(x, y, barWidth, barH);
                if (barH > 0)
                {
                    using var brushBar = new LinearGradientBrush(new RectangleF(x, y, barWidth, Math.Max(barH, 1f)), fillTop, fillBottom, LinearGradientMode.Vertical);
                    using var penBar = new Pen(borderColor, 1f);
                    g.FillRectangle(brushBar, rectBar);
                    g.DrawRectangle(penBar, x, y, barWidth, barH);
                }

                string valStr = count.ToString();
                var szVal = g.MeasureString(valStr, fontBarVal);
                using var brushVal = new SolidBrush(isCurr ? Color.FromArgb(24, 119, 242) : (count > 0 ? Color.FromArgb(30, 41, 59) : Color.FromArgb(160, 174, 192)));
                g.DrawString(valStr, fontBarVal, brushVal, x + (barWidth - szVal.Width) / 2f, y - szVal.Height - 1);

                string mLabel = _currentLanguage == "EN" ? $"M{i + 1}" : $"T{i + 1}";
                var szM = g.MeasureString(mLabel, fontMonth);
                using var brushM = new SolidBrush(isCurr ? Color.FromArgb(24, 119, 242) : Color.FromArgb(71, 85, 105));
                g.DrawString(mLabel, isCurr ? fontBarVal : fontMonth, brushM, x + (barWidth - szM.Width) / 2f, padTop + chartH + 5);
            }
        }

        private void DrawCommoditiesChart(Graphics g, int width, int height, int selectedYear, List<PhieuCan> tickets)
        {
            var groups = tickets
                .Where(t => !string.IsNullOrWhiteSpace(t.LoaiHang))
                .GroupBy(t => t.LoaiHang.Trim())
                .Select(grp => new
                {
                    Name = grp.Key,
                    Count = grp.Count(),
                    TotalWeight = grp.Sum(x => x.KhoiLuongHang)
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.TotalWeight)
                .Take(5)
                .ToList();

            if (groups.Count == 0)
            {
                string emptyMsg = _currentLanguage == "EN" ? "No ticket data for this year." : "Chưa có dữ liệu phiếu cân trong năm này.";
                using var fontEmpty = new Font("Segoe UI", 10F, FontStyle.Italic);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(emptyMsg, fontEmpty, Brushes.Gray, new RectangleF(0, 0, width, height), sf);
                return;
            }

            int maxCount = groups[0].Count;
            if (maxCount <= 0) maxCount = 1;

            int padLeft = 15;
            int padRight = 15;
            int padTop = 15;
            int padBottom = 15;

            int availableH = height - padTop - padBottom;
            int rowCount = groups.Count;
            float rowH = (float)availableH / rowCount;

            Color[] barColors = new Color[]
            {
                Color.FromArgb(245, 158, 11), // 1st: Gold/Amber
                Color.FromArgb(16, 185, 129), // 2nd: Emerald green
                Color.FromArgb(59, 130, 246), // 3rd: Sky blue
                Color.FromArgb(139, 92, 246), // 4th: Purple
                Color.FromArgb(100, 116, 139) // 5th: Slate
            };

            using var fontName = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            using var fontVal = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var fontRank = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            for (int i = 0; i < rowCount; i++)
            {
                var item = groups[i];
                float y = padTop + i * rowH;
                float barMaxW = width - padLeft - padRight - 150;
                if (barMaxW < 50) barMaxW = 50;

                float barW = ((float)item.Count / maxCount) * barMaxW;
                if (barW < 8) barW = 8;

                string rankStr = i == 0 ? "🏆 1." : $"{i + 1}.";
                g.DrawString(rankStr, fontRank, i == 0 ? Brushes.Goldenrod : Brushes.DimGray, padLeft, y + 2);
                g.DrawString(item.Name, fontName, Brushes.Black, padLeft + 32, y + 2);

                float barY = y + 22;
                float barH = Math.Min(rowH - 26, 13f);
                if (barH < 6) barH = 6;

                using var brushSlot = new SolidBrush(Color.FromArgb(241, 245, 249));
                g.FillRectangle(brushSlot, padLeft + 32, barY, barMaxW, barH);

                using var brushFill = new SolidBrush(barColors[i % barColors.Length]);
                g.FillRectangle(brushFill, padLeft + 32, barY, barW, barH);

                string valText = $"{item.Count} phiếu | {item.TotalWeight:N0} Kg";
                using var brushValText = new SolidBrush(Color.FromArgb(71, 85, 105));
                g.DrawString(valText, fontVal, brushValText, padLeft + 38 + barMaxW, barY - 2);
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }
}

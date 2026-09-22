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
        private Button btnResetFilter;
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
        private Panel pnlChartWrapper;
        private Panel pnlChartTitleBar;
        private Label lblChartTitle;
        private Button btnToggleChart;
        private Panel pnlChartContent;
        private Panel panelChartHeader;
        private Button btnChartTabMonths;
        private Button btnChartTabGoods;
        private Label lblChartYear;
        private ComboBox cbChartYear;
        private Label lblChartSummary;
        private DoubleBufferedPanel pnlChartCanvas;
        private int _currentChartTab = 0; // 0 = 12 tháng, 1 = Top mặt hàng

        // Controls Toggle Đóng / Mở Bảng thống kê (Mặc định là đóng)
        private bool _isChartVisible = false;

        // Controls Zoom In / Zoom Out giao diện
        private float _currentZoom = 1.0f;
        private Panel pnlZoomControls;
        private Button btnZoomOut;
        private Button btnZoomReset;
        private Button btnZoomIn;
        private ToolTip? _zoomToolTip;

        private class ControlLayoutInfo
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public float FontSize;
            public FontStyle FontStyle;
            public string FontFamily = "Segoe UI";
        }
        private readonly Dictionary<Control, ControlLayoutInfo> _baseLayouts = new Dictionary<Control, ControlLayoutInfo>();

        // ContextMenuStrip cho danh sách phiếu cân
        private ContextMenuStrip contextMenuDgv;
        private ToolStripMenuItem menuItemEdit;
        private ToolStripMenuItem menuItemDelete;

        // Danh sách các loại hàng có sẵn
        private readonly string[] _dsLoaiHang = new string[]
        {
            "Mía", "Dưa", "Phế Liệu", "Keo", "Gỗ", "Lúa", "Đậu Đỏ", "Bắp", "Mì", "Bì Cao Su"
        };

        private System.Windows.Forms.Timer? _realtimeTimer;

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

            // Realtime timer cập nhật ngày in phiếu
            _realtimeTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000
            };
            _realtimeTimer.Tick += (s, e) =>
            {
                if (dtpNgayInPhieu != null && !dtpNgayInPhieu.IsUserModified && (_currentPhieuCan == null || _currentPhieuCan.Id == 0))
                {
                    dtpNgayInPhieu.Value = DateTime.Now;
                }
            };
            _realtimeTimer.Start();

            this.Shown += (s, e) => this.ActiveControl = null;
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Cân Xe - TRẠM CÂN THỊ TRẤN HAI RIÊNG";
            this.Size = new Size(1300, 750);
            this.MinimumSize = new Size(1100, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
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
                Height = 84,
                BackColor = Color.FromArgb(24, 119, 242)
            };
            lblHeaderTitle = new Label
            {
                Text = "TRẠM CÂN THỊ TRẤN HAI RIÊNG",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 12),
                AutoSize = true
            };
            lblHeaderSub = new Label
            {
                Text = "Đ. Hoàng Hoa Thám - TT. Hai Riêng - X. Sông Hinh - T. Đắk Lắk | SĐT: 0918.172.220",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(230, 240, 255),
                Location = new Point(22, 50),
                AutoSize = true
            };

            // Button Chuyển đổi ngôn ngữ ở góc trên bên phải Header
            btnSwitchLang = new Button
            {
                Text = "🌐 VI (Tiếng Việt)",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Size = new Size(145, 38),
                BackColor = Color.FromArgb(255, 255, 255),
                ForeColor = Color.FromArgb(24, 119, 242),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSwitchLang.FlatAppearance.BorderSize = 0;
            btnSwitchLang.Click += BtnSwitchLang_Click;

            // Toolbar Zoom In / Zoom Out / Reset 100%
            pnlZoomControls = new Panel
            {
                Size = new Size(155, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };

            btnZoomOut = new Button
            {
                Text = "",
                Location = new Point(0, 0),
                Size = new Size(38, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(24, 119, 242),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnZoomOut.FlatAppearance.BorderSize = 0;
            btnZoomOut.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float cx = btnZoomOut.Width / 2f;
                float cy = btnZoomOut.Height / 2f;
                float halfLen = 6.5f * _currentZoom;
                float penW = Math.Max(2f, 2.2f * _currentZoom);
                using var pen = new Pen(Color.FromArgb(24, 119, 242), penW) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                e.Graphics.DrawLine(pen, cx - halfLen, cy, cx + halfLen, cy);
            };
            btnZoomOut.Click += (s, e) => ZoomOut();

            btnZoomReset = new Button
            {
                Text = "100%",
                Location = new Point(42, 0),
                Size = new Size(70, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(24, 119, 242),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnZoomReset.FlatAppearance.BorderSize = 0;
            btnZoomReset.Click += (s, e) => ZoomReset();

            btnZoomIn = new Button
            {
                Text = "",
                Location = new Point(116, 0),
                Size = new Size(38, 38),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(24, 119, 242),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnZoomIn.FlatAppearance.BorderSize = 0;
            btnZoomIn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                float cx = btnZoomIn.Width / 2f;
                float cy = btnZoomIn.Height / 2f;
                float halfLen = 6.5f * _currentZoom;
                float penW = Math.Max(2f, 2.2f * _currentZoom);
                using var pen = new Pen(Color.FromArgb(24, 119, 242), penW) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                e.Graphics.DrawLine(pen, cx - halfLen, cy, cx + halfLen, cy);
                e.Graphics.DrawLine(pen, cx, cy - halfLen, cx, cy + halfLen);
            };
            btnZoomIn.Click += (s, e) => ZoomIn();

            pnlZoomControls.Controls.Add(btnZoomOut);
            pnlZoomControls.Controls.Add(btnZoomReset);
            pnlZoomControls.Controls.Add(btnZoomIn);

            _zoomToolTip = new ToolTip { InitialDelay = 200, ReshowDelay = 200 };
            _zoomToolTip.SetToolTip(btnZoomIn, "Phóng to giao diện (Ctrl + +)");
            _zoomToolTip.SetToolTip(btnZoomOut, "Thu nhỏ giao diện (Ctrl + -)");
            _zoomToolTip.SetToolTip(btnZoomReset, "Đặt lại kích thước chuẩn 100% (Ctrl + 0)");

            panelHeader.Controls.Add(lblHeaderTitle);
            panelHeader.Controls.Add(lblHeaderSub);
            panelHeader.Controls.Add(pnlZoomControls);
            panelHeader.Controls.Add(btnSwitchLang);
            this.Controls.Add(panelHeader);

            panelHeader.Resize += (s, e) => RepositionHeaderControls();
            RepositionHeaderControls();

            // 2. TableMainLayout (Responsive Dock Fill: 2 cột)
            tableMainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
                BackColor = Color.FromArgb(245, 247, 250)
            };
            tableMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 750F));
            tableMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 3. GroupBox Nhập liệu (Cột trái)
            gbInput = new GroupBox
            {
                Text = "THÔNG TIN PHIẾU CÂN XE MỚI",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 375,
                ForeColor = Color.FromArgb(30, 40, 60),
                Margin = new Padding(5)
            };
            gbInput.Click += ClearFocus_Click;

            int y = 32;
            int lblX1 = 15, txtX1 = 150, txtWidth1 = 190;
            int lblX2 = 370, txtX2 = 505, txtWidth2 = 225;

            // STT & Số xe (Giới hạn 50 ký tự)
            lblSTT = CreateLabel("STT:", lblX1, y);
            txtSTT = new TextBox { Location = new Point(txtX1, y - 3), Width = txtWidth1, ReadOnly = true, Enabled = false, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold) };

            lblSoXe = CreateLabelWithRedAsterisk("Số xe", lblX2, y);
            txtSoXe = new TextBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), ForeColor = Color.DarkBlue, MaxLength = 50 };
            txtSoXe.Enter += Input_SelectAllOnEnter;
            txtSoXe.MouseClick += Input_SelectAllOnEnter;

            y += 46;
            // Loại hàng & Khách hàng (Giới hạn 50 ký tự)
            lblLoaiHang = CreateLabel("Loại hàng:", lblX1, y);
            cbLoaiHang = new ComboBox
            {
                Location = new Point(txtX1, y - 3),
                Width = txtWidth1,
                DropDownStyle = ComboBoxStyle.DropDown,
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                MaxLength = 50
            };
            cbLoaiHang.Items.AddRange(_dsLoaiHang);
            cbLoaiHang.Leave += CbLoaiHang_Leave;
            cbLoaiHang.Enter += Input_SelectAllOnEnter;

            lblKhachHang = CreateLabel("Khách hàng:", lblX2, y);
            txtKhachHang = new TextBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), MaxLength = 50 };
            txtKhachHang.Leave += TxtKhachHang_Leave;
            txtKhachHang.Enter += Input_SelectAllOnEnter;
            txtKhachHang.MouseClick += Input_SelectAllOnEnter;

            y += 46;
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

            y += 46;
            // Khối lượng xe & Kiểu cân
            lblKhoiLuongXe = CreateLabel("KL Xe (Kg):", lblX1, y);
            numKhoiLuongXe = new FormattedNumericTextBox
            {
                Location = new Point(txtX1, y - 3),
                Width = txtWidth1
            };
            numKhoiLuongXe.TextChanged += CalculateKLHangRealtime;

            lblKieuCan = CreateLabel("Kiểu cân:", lblX2, y);
            cbKieuCan = new ComboBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold) };
            cbKieuCan.Items.AddRange(new object[] { "Cân dịch vụ", "Xuất hàng", "Nhập hàng" });
            cbKieuCan.SelectedIndex = 0;

            y += 46;
            // Khối lượng hàng & Số chứng từ (Giới hạn 50 ký tự)
            lblKhoiLuongHang = CreateLabel("KL Hàng (Kg):", lblX1, y);
            txtKhoiLuongHang = new TextBox
            {
                Location = new Point(txtX1, y - 3),
                Width = txtWidth1,
                ReadOnly = true,
                Enabled = false,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Red
            };

            lblSoChungTu = CreateLabel("Số chứng từ:", lblX2, y);
            txtSoChungTu = new TextBox { Location = new Point(txtX2, y - 3), Width = txtWidth2, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), MaxLength = 50 };
            txtSoChungTu.Enter += Input_SelectAllOnEnter;
            txtSoChungTu.MouseClick += Input_SelectAllOnEnter;

            y += 46;
            // Ghi chú (Giới hạn 150 ký tự)
            lblGhiChu = CreateLabel("Ghi chú:", lblX1, y);
            txtGhiChu = new TextBox { Location = new Point(txtX1, y - 3), Width = txtWidth1, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold), MaxLength = 150 };
            txtGhiChu.Enter += Input_SelectAllOnEnter;
            txtGhiChu.MouseClick += Input_SelectAllOnEnter;

            y += 56;
            // Buttons: 4 nút to rõ ràng, cân đối chiều ngang
            btnNew = CreateButton("➕ Tạo Mới", 15, y, 160, Color.FromArgb(40, 167, 69));
            btnSave = CreateButton("💾 Lưu Phiếu", 190, y, 165, Color.FromArgb(0, 123, 255));
            btnPrintPreview = CreateButton("👁️ Xem Trước", 370, y, 165, Color.FromArgb(23, 162, 184));
            btnPrint = CreateButton("🖨️ In Và Lưu Phiếu", 550, y, 180, Color.FromArgb(220, 53, 69));

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
                Height = 50
            };
            panelSearch.Click += ClearFocus_Click;

            lblSearch = CreateLabel("Từ khóa:", 8, 13);
            txtSearch = new TextBox { Location = new Point(80, 8), Width = 155, Font = new Font("Segoe UI", 11F, FontStyle.Regular), PlaceholderText = "Số xe, hàng..." };
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
                Location = new Point(248, 12),
                AutoSize = true,
                Checked = false,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular)
            };

            dtpFilterFrom = new VnDateTimePicker
            {
                Location = new Point(375, 8),
                Width = 135,
                CustomFormat = "dd/MM/yyyy",
                Enabled = false,
                PreventFutureDates = true,
                ShowTimePicker = false,
                Language = _currentLanguage
            };

            lblFilterTo = CreateLabel("đến", 518, 13);

            dtpFilterTo = new VnDateTimePicker
            {
                Location = new Point(560, 8),
                Width = 135,
                CustomFormat = "dd/MM/yyyy",
                Enabled = false,
                PreventFutureDates = true,
                ShowTimePicker = false,
                Language = _currentLanguage
            };

            btnSearch = CreateButton("🔍 Tìm", 705, 7, 85, Color.FromArgb(108, 117, 125));
            btnSearch.Height = 35;
            btnSearch.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);

            btnResetFilter = CreateButton("🔄 Bỏ lọc", 798, 7, 95, Color.FromArgb(235, 240, 248));
            btnResetFilter.Height = 35;
            btnResetFilter.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnResetFilter.ForeColor = Color.FromArgb(51, 65, 85);
            btnResetFilter.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnResetFilter.FlatAppearance.BorderSize = 1;

            btnPrintSelected = new Button
            {
                Location = new Point(panelSearch.Width - 215, 7),
                Size = new Size(205, 35),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
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

            btnResetFilter.Click += (s, e) => ResetSearchAndFilter();

            panelSearch.Controls.Add(lblSearch);
            panelSearch.Controls.Add(txtSearch);
            panelSearch.Controls.Add(chkFilterDate);
            panelSearch.Controls.Add(dtpFilterFrom);
            panelSearch.Controls.Add(lblFilterTo);
            panelSearch.Controls.Add(dtpFilterTo);
            panelSearch.Controls.Add(btnSearch);
            panelSearch.Controls.Add(btnResetFilter);
            panelSearch.Controls.Add(btnPrintSelected);

            panelSearch.Resize += (s, e) => RepositionSearchFilterControls();
            RepositionSearchFilterControls();

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
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 38 },
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.None,
                GridColor = Color.FromArgb(180, 190, 205),
                Font = new Font("Segoe UI", 11F, FontStyle.Regular)
            };
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null, dgvPhieuCan, new object[] { true });
            dgvPhieuCan.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            dgvPhieuCan.DefaultCellStyle.Font = new Font("Segoe UI", 11F, FontStyle.Regular);

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
            dgvPhieuCan.CellFormatting += DgvPhieuCan_CellFormatting;
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

            // Card Biểu đồ thống kê
            pnlChartWrapper = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                Margin = new Padding(5),
                BackColor = Color.FromArgb(245, 247, 250)
            };

            pnlChartTitleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.Transparent
            };

            lblChartTitle = new Label
            {
                Text = "📊 THỐNG KÊ HOẠT ĐỘNG CÂN",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 40, 60),
                Location = new Point(8, 8),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblChartTitle.Click += (s, e) => ToggleChartVisibility();

            btnToggleChart = new Button
            {
                Text = "▼ Mở",
                Size = new Size(85, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(24, 119, 242),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnToggleChart.FlatAppearance.BorderSize = 0;
            btnToggleChart.Location = new Point(lblChartTitle.Right + 12, 5);
            btnToggleChart.Click += (s, e) => ToggleChartVisibility();

            pnlChartTitleBar.Controls.Add(lblChartTitle);
            pnlChartTitleBar.Controls.Add(btnToggleChart);

            pnlChartContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            panelChartHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent
            };

            btnChartTabMonths = new Button
            {
                Text = "📅 Số phiếu 12 tháng",
                Location = new Point(10, 6),
                Size = new Size(175, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChartTabMonths.FlatAppearance.BorderSize = 0;
            btnChartTabMonths.Click += (s, e) => SetChartTab(0);

            btnChartTabGoods = new Button
            {
                Text = "📦 Top mặt hàng",
                Location = new Point(192, 6),
                Size = new Size(155, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnChartTabGoods.FlatAppearance.BorderSize = 0;
            btnChartTabGoods.Click += (s, e) => SetChartTab(1);

            lblChartYear = new Label
            {
                Text = "Năm:",
                Location = new Point(358, 11),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 80, 95)
            };

            cbChartYear = new ComboBox
            {
                Location = new Point(405, 8),
                Width = 85,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            cbChartYear.SelectedIndexChanged += (s, e) => {
                UpdateChartSummaryText();
                pnlChartCanvas.Invalidate();
            };

            lblChartSummary = new Label
            {
                Location = new Point(500, 11),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
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

            pnlChartContent.Controls.Add(pnlChartCanvas);
            pnlChartContent.Controls.Add(panelChartHeader);

            pnlChartWrapper.Controls.Add(pnlChartContent);
            pnlChartWrapper.Controls.Add(pnlChartTitleBar);

            panelLeft.Controls.Add(pnlChartWrapper);
            panelLeft.Controls.Add(gbInput);
            gbInput.SendToBack();

            // Mặc định là đóng bảng thống kê
            SetChartVisibility(false);

            tableMainLayout.Controls.Add(panelLeft, 0, 0);
            tableMainLayout.Controls.Add(panelRight, 1, 0);

            this.Controls.Add(tableMainLayout);
            tableMainLayout.BringToFront();

            // Events for buttons
            btnNew.Click += BtnNew_Click;
            btnSave.Click += BtnSave_Click;
            btnPrintPreview.Click += BtnPrintPreview_Click;
            btnPrint.Click += BtnPrint_Click;

            btnSearch.Click += (s, e) => LoadDataWithFilter();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadDataWithFilter(); };

            // Ghi nhận trạng thái gốc của các control để phóng to / thu nhỏ không bao giờ bị lệch
            CaptureBaseLayouts();
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
                UpdateSoXeLabelPosition("Plate No", 370, 32);
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
                if (btnResetFilter != null) btnResetFilter.Text = "🔄 Reset";

                menuItemEdit.Text = "✏️ Edit this ticket";
                menuItemDelete.Text = "🗑️ Delete this ticket";

                int selKieuCan = cbKieuCan.SelectedIndex;
                cbKieuCan.Items.Clear();
                cbKieuCan.Items.AddRange(new object[] { "Service Weighing", "Export Goods", "Import Goods" });
                if (selKieuCan >= 0 && selKieuCan < cbKieuCan.Items.Count) cbKieuCan.SelectedIndex = selKieuCan;

                if (lblChartTitle != null) lblChartTitle.Text = "📊 WEIGHING ACTIVITY STATISTICS";
                if (lblChartYear != null) lblChartYear.Text = "Year:";
            }
            else
            {
                btnSwitchLang.Text = "🌐 VI (Tiếng Việt)";
                gbInput.Text = "THÔNG TIN PHIẾU CÂN XE MỚI";
                lblSTT.Text = "STT:";
                UpdateSoXeLabelPosition("Số xe", 370, 32);
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
                btnSearch.Text = "🔍 Tìm";
                if (btnResetFilter != null) btnResetFilter.Text = "🔄 Bỏ lọc";

                menuItemEdit.Text = "✏️ Chỉnh sửa phiếu này";
                menuItemDelete.Text = "🗑️ Xóa phiếu này";

                int selKieuCan = cbKieuCan.SelectedIndex;
                cbKieuCan.Items.Clear();
                cbKieuCan.Items.AddRange(new object[] { "Cân dịch vụ", "Xuất hàng", "Nhập hàng" });
                if (selKieuCan >= 0 && selKieuCan < cbKieuCan.Items.Count) cbKieuCan.SelectedIndex = selKieuCan;

                if (lblChartTitle != null) lblChartTitle.Text = "📊 THỐNG KÊ HOẠT ĐỘNG CÂN";
                if (lblChartYear != null) lblChartYear.Text = "Năm:";
            }

            if (btnChartTabMonths != null) btnChartTabMonths.Text = _currentLanguage == "EN" ? "📅 Monthly Tickets" : "📅 Số phiếu 12 tháng";
            if (btnChartTabGoods != null) btnChartTabGoods.Text = _currentLanguage == "EN" ? "📦 Top Commodities" : "📦 Top mặt hàng";
            UpdateChartSummaryText();
            pnlChartCanvas?.Invalidate();

            SetChartVisibility(_isChartVisible);

            if (_zoomToolTip != null)
            {
                if (btnZoomIn != null) _zoomToolTip.SetToolTip(btnZoomIn, _currentLanguage == "EN" ? "Zoom In (Ctrl + +)" : "Phóng to giao diện (Ctrl + +)");
                if (btnZoomOut != null) _zoomToolTip.SetToolTip(btnZoomOut, _currentLanguage == "EN" ? "Zoom Out (Ctrl + -)" : "Thu nhỏ giao diện (Ctrl + -)");
                if (btnZoomReset != null) _zoomToolTip.SetToolTip(btnZoomReset, _currentLanguage == "EN" ? "Reset Zoom 100% (Ctrl + 0)" : "Đặt lại kích thước chuẩn 100% (Ctrl + 0)");
                if (btnToggleChart != null) _zoomToolTip.SetToolTip(btnToggleChart, _currentLanguage == "EN" ? "Open/Close Statistics Chart" : "Đóng / Mở bảng thống kê hoạt động cân");
            }

            UpdatePrintSelectedButtonState();
            RepositionSearchFilterControls();
            LoadDataWithFilter();
        }

        private Label CreateLabel(string text, int x, int y)
        {
            var lbl = new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Regular) };
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
            string prefix = (_currentLanguage == "EN" ? "Plate No" : "Số xe") + " (";
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
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 42), BackColor = backColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Cursor = Cursors.Hand };
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

        private bool HasUnsavedNewTicket()
        {
            if (_currentPhieuCan != null && _currentPhieuCan.Id != 0) return false;

            if (!string.IsNullOrWhiteSpace(txtSoXe.Text)) return true;
            if (!string.IsNullOrWhiteSpace(txtKhachHang.Text)) return true;
            if (numKhoiLuongTong.Value > 0) return true;
            if (numKhoiLuongXe.Value > 0) return true;
            if (!string.IsNullOrWhiteSpace(txtSoChungTu.Text)) return true;
            if (!string.IsNullOrWhiteSpace(txtGhiChu.Text)) return true;
            if (cbLoaiHang.Text.Trim() != "Bì Cao Su" && !string.IsNullOrWhiteSpace(cbLoaiHang.Text)) return true;
            if (cbKieuCan.SelectedIndex > 0) return true;
            if (dtpNgayInPhieu.IsUserModified) return true;

            return false;
        }

        private void BtnNew_Click(object? sender, EventArgs e)
        {
            if (HasUnsavedNewTicket())
            {
                string confirmMsg = _currentLanguage == "EN"
                    ? "You are creating a new ticket with unsaved information. Are you sure you want to discard it and create a new one?"
                    : "Bạn đang nhập thông tin phiếu cân mới chưa được lưu lại. Bạn có chắc chắn muốn bỏ qua để tạo mới không?";
                string confirmTitle = _currentLanguage == "EN" ? "Discard Unsaved Ticket" : "Xác nhận tạo mới";

                if (CustomMessageBox.Show(confirmMsg, confirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }

            ResetForm();
        }

        private void ResetForm()
        {
            _currentPhieuCan = new PhieuCan
            {
                Id = 0,
                STT = _dbService.GetNextSTT(),
                NgayInPhieu = DateTime.Now
            };

            txtSTT.Text = $"Xe {_currentPhieuCan.STT}";
            txtSoXe.Text = "";
            cbLoaiHang.Text = "Bì Cao Su";
            txtKhachHang.Text = "";
            numKhoiLuongTong.Value = 0;
            numKhoiLuongXe.Value = 0;
            dtpNgayInPhieu.IsUserModified = false;
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

        private void ResetSearchAndFilter()
        {
            txtSearch.Text = "";
            chkFilterDate.Checked = false;
            dtpFilterFrom.Value = DateTime.Now;
            dtpFilterTo.Value = DateTime.Now;
            dtpFilterFrom.Enabled = false;
            dtpFilterTo.Enabled = false;

            LoadData("", null, null);
            this.ActiveControl = null;
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
                dgvPhieuCan.Columns["STT"].Width = (int)Math.Round(72 * _currentZoom);
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

            // Đồng bộ trạng thái checkbox từ _selectedPhieuIds và cập nhật chiều cao dòng theo zoom
            int currentGridRowH = (int)Math.Round(38 * _currentZoom);
            foreach (DataGridViewRow row in dgvPhieuCan.Rows)
            {
                row.Height = currentGridRowH;
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
                    Width = (int)Math.Round(42 * _currentZoom),
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
                    Width = (int)Math.Round(135 * _currentZoom),
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

        private void DgvPhieuCan_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "STT" && e.Value != null)
            {
                e.Value = $"Xe {e.Value}";
                e.FormattingApplied = true;
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
            if (e.RowIndex < -1 || e.ColumnIndex < 0) return;

            Color gridLineColor = Color.FromArgb(180, 190, 205);

            // 1. Header Cells (RowIndex == -1)
            if (e.RowIndex == -1)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Focus & ~DataGridViewPaintParts.Border);

                if (dgvPhieuCan.Columns[e.ColumnIndex].Name == "colSelect")
                {
                    int cbSize = (int)Math.Round(16 * _currentZoom);
                    int cbX = e.CellBounds.X + (e.CellBounds.Width - cbSize) / 2;
                    int cbY = e.CellBounds.Y + (e.CellBounds.Height - cbSize) / 2;
                    Rectangle cbRect = new Rectangle(cbX, cbY, cbSize, cbSize);

                    bool allChecked = dgvPhieuCan.Rows.Count > 0 &&
                        dgvPhieuCan.Rows.Cast<DataGridViewRow>().All(r => Convert.ToBoolean(r.Cells["colSelect"].Value));

                    ButtonState state = allChecked ? ButtonState.Checked : ButtonState.Normal;
                    ControlPaint.DrawCheckBox(e.Graphics, cbRect, state);
                }

                // Đường viền Header: dưới và phải
                using (var pen = new Pen(gridLineColor, 1))
                {
                    e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    e.Graphics.DrawLine(pen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
                    if (e.ColumnIndex == 0)
                    {
                        e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Top, e.CellBounds.Left, e.CellBounds.Bottom - 1);
                    }
                }

                e.Handled = true;
                return;
            }

            // 2. Data Cells (RowIndex >= 0)
            if (e.RowIndex >= 0)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Focus & ~DataGridViewPaintParts.Border);

                // Cột Action: vẽ 3 nút icon (Xem trước, Sửa, In)
                if (dgvPhieuCan.Columns[e.ColumnIndex].Name == "colAction")
                {
                    int btnW = (int)Math.Round(34 * _currentZoom);
                    int btnH = (int)Math.Round(28 * _currentZoom);
                    int gap = (int)Math.Round(6 * _currentZoom);
                    int startX = e.CellBounds.X + (e.CellBounds.Width - (btnW * 3 + gap * 2)) / 2;
                    int startY = e.CellBounds.Y + (e.CellBounds.Height - btnH) / 2;

                    Rectangle rcPreview = new Rectangle(startX, startY, btnW, btnH);
                    Rectangle rcEdit = new Rectangle(startX + btnW + gap, startY, btnW, btnH);
                    Rectangle rcPrint = new Rectangle(startX + (btnW + gap) * 2, startY, btnW, btnH);

                    DrawActionBtn(e.Graphics, rcPreview, "preview", Color.FromArgb(232, 244, 253), Color.FromArgb(145, 202, 255));
                    DrawActionBtn(e.Graphics, rcEdit, "edit", Color.FromArgb(254, 244, 230), Color.FromArgb(254, 215, 170));
                    DrawActionBtn(e.Graphics, rcPrint, "print", Color.FromArgb(235, 247, 238), Color.FromArgb(163, 230, 185));
                }

                // Luôn vẽ đường kẻ dưới và phải rõ ràng cho TẤT CẢ các cell:
                // - Đảm bảo mọi hàng (đặc biệt dòng cuối cùng) luôn có viền ngang sắc nét, liền mạch
                // - Không bị mờ hay biến mất khi zoom
                using (var pen = new Pen(gridLineColor, 1))
                {
                    e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                    e.Graphics.DrawLine(pen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
                    if (e.ColumnIndex == 0)
                    {
                        e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Top, e.CellBounds.Left, e.CellBounds.Bottom - 1);
                    }
                }

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
            float z = _currentZoom;

            if (actionType == "preview") // Icon mắt (Xem trước) căn giữa tuyệt đối
            {
                using var penEye = new Pen(Color.FromArgb(24, 119, 242), 1.8f * z);
                using var brushPupil = new SolidBrush(Color.FromArgb(24, 119, 242));

                using var path = new GraphicsPath();
                path.AddArc(cx - 8.5f * z, cy - 7f * z, 17f * z, 12f * z, 25, 130);
                path.AddArc(cx - 8.5f * z, cy - 5f * z, 17f * z, 12f * z, 205, 130);
                path.CloseFigure();
                g.DrawPath(penEye, path);

                g.FillEllipse(brushPupil, cx - 2.5f * z, cy - 2.5f * z, 5f * z, 5f * z);
            }
            else if (actionType == "edit") // Icon bút chì (Chỉnh sửa) sắc nét, cân đối
            {
                using var penPencil = new Pen(Color.FromArgb(217, 119, 6), 1.7f * z);
                using var brushPencil = new SolidBrush(Color.FromArgb(217, 119, 6));

                PointF tip = new PointF(cx - 6f * z, cy + 6f * z);
                PointF side1 = new PointF(cx - 6f * z, cy + 2f * z);
                PointF side2 = new PointF(cx - 2f * z, cy + 6f * z);
                g.FillPolygon(brushPencil, new PointF[] { tip, side1, side2 });

                PointF top1 = new PointF(cx + 4f * z, cy - 8f * z);
                PointF top2 = new PointF(cx + 8f * z, cy - 4f * z);
                g.DrawLine(penPencil, side1, top1);
                g.DrawLine(penPencil, side2, top2);
                g.DrawLine(penPencil, top1, top2);

                PointF cap1 = new PointF(cx + 2f * z, cy - 6f * z);
                PointF cap2 = new PointF(cx + 6f * z, cy - 2f * z);
                g.DrawLine(penPencil, cap1, cap2);
            }
            else if (actionType == "print") // Icon máy in (In phiếu) căn giữa tuyệt đối
            {
                using var penPrint = new Pen(Color.FromArgb(22, 163, 74), 1.7f * z);
                using var brushPrint = new SolidBrush(Color.FromArgb(22, 163, 74));

                // Khổ giấy trên
                g.DrawRectangle(penPrint, cx - 4.5f * z, cy - 6.5f * z, 9f * z, 4f * z);
                // Thân máy in
                g.DrawRectangle(penPrint, cx - 7.5f * z, cy - 2.5f * z, 15f * z, 7.5f * z);
                // Khe nhả giấy
                g.DrawLine(penPrint, cx - 4.5f * z, cy + 1f * z, cx + 4.5f * z, cy + 1f * z);
                // Giấy in ra bên dưới
                g.FillRectangle(brushPrint, cx - 4.5f * z, cy + 2.5f * z, 9f * z, 4f * z);
            }
        }

        private void DgvPhieuCan_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvPhieuCan.Columns[e.ColumnIndex].Name == "colAction")
            {
                if (dgvPhieuCan.Rows[e.RowIndex].DataBoundItem is PhieuCan item)
                {
                    Rectangle cellBounds = dgvPhieuCan.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                    int btnW = (int)Math.Round(34 * _currentZoom);
                    int btnH = (int)Math.Round(28 * _currentZoom);
                    int gap = (int)Math.Round(6 * _currentZoom);
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
                int btnW = (int)Math.Round(34 * _currentZoom);
                int btnH = (int)Math.Round(28 * _currentZoom);
                int gap = (int)Math.Round(6 * _currentZoom);
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
                        _actionToolTip.Show(tip, dgvPhieuCan, cellBounds.Left + e.X + 12, cellBounds.Top + e.Y + 12, 1500);
                    }
                }
                else
                {
                    dgvPhieuCan.Cursor = Cursors.Default;
                    if (_lastHoveredRow != -1)
                    {
                        _lastHoveredRow = -1;
                        _lastHoveredBtn = "";
                        _actionToolTip.Hide(dgvPhieuCan);
                    }
                }
            }
            else
            {
                dgvPhieuCan.Cursor = Cursors.Default;
                if (_lastHoveredRow != -1)
                {
                    _lastHoveredRow = -1;
                    _lastHoveredBtn = "";
                    _actionToolTip.Hide(dgvPhieuCan);
                }
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
            if (!dtpNgayInPhieu.IsUserModified && (_currentPhieuCan == null || _currentPhieuCan.Id == 0))
            {
                dtpNgayInPhieu.Value = DateTime.Now;
            }

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
                // Sao chép đối tượng để nếu người dùng hủy/đóng popup thì không ảnh hưởng đến dữ liệu trong bảng
                var cloneItem = new PhieuCan
                {
                    Id = item.Id,
                    STT = item.STT,
                    TramCanName = item.TramCanName,
                    TramCanAddress = item.TramCanAddress,
                    TramCanPhone = item.TramCanPhone,
                    LoaiHang = item.LoaiHang,
                    KhachHang = item.KhachHang,
                    SoXe = item.SoXe,
                    KhoiLuongTong = item.KhoiLuongTong,
                    KhoiLuongXe = item.KhoiLuongXe,
                    NgayInPhieu = item.NgayInPhieu,
                    SoChungTu = item.SoChungTu,
                    GhiChu = item.GhiChu,
                    KieuCan = item.KieuCan,
                    ChuHang = item.ChuHang,
                    TaiXe = item.TaiXe,
                    NguoiCan = item.NguoiCan
                };

                FormEditPhieu editForm = new FormEditPhieu(cloneItem, _currentLanguage, _currentZoom, _dbService);
                var dialogResult = editForm.ShowDialog(this);
                if (dialogResult == DialogResult.OK || editForm.HasSavedChanges)
                {
                    if (editForm.IsDeleted)
                    {
                        _selectedPhieuIds.Remove(item.Id);
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

            string sumText = _currentLanguage == "EN"
                ? $"Total: {totalTickets} tickets"
                : $"Tổng: {totalTickets} phiếu";
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
            using var fontSmall = new Font("Segoe UI", Math.Max(7f, 9F * _currentZoom));

            int gridSteps = 4;
            for (int s = 0; s <= gridSteps; s++)
            {
                int val = (maxCount * s) / gridSteps;
                float y = padTop + chartH - (float)s / gridSteps * chartH;
                g.DrawLine(penGrid, padLeft, y, width - padRight, y);
                g.DrawString(val.ToString(), fontSmall, brushText, padLeft - (int)Math.Round(28 * _currentZoom), y - (int)Math.Round(7 * _currentZoom));
            }

            float barSlot = (float)chartW / 12f;
            float barWidth = Math.Min(barSlot * 0.65f, 28f * _currentZoom);

            using var fontBarVal = new Font("Segoe UI", Math.Max(7f, 9.5F * _currentZoom), FontStyle.Bold);
            using var fontMonth = new Font("Segoe UI", Math.Max(7f, 9.5F * _currentZoom), FontStyle.Regular);

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
                using var fontEmpty = new Font("Segoe UI", Math.Max(8f, 11F * _currentZoom), FontStyle.Italic);
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
            float maxRowH = 52f * _currentZoom;
            float rowH = Math.Min((float)availableH / Math.Max(rowCount, 1), maxRowH);

            Color[] barColors = new Color[]
            {
                Color.FromArgb(245, 158, 11), // 1st: Gold/Amber
                Color.FromArgb(16, 185, 129), // 2nd: Emerald green
                Color.FromArgb(59, 130, 246), // 3rd: Sky blue
                Color.FromArgb(139, 92, 246), // 4th: Purple
                Color.FromArgb(100, 116, 139) // 5th: Slate
            };

            using var fontName = new Font("Segoe UI", Math.Max(8f, 11F * _currentZoom), FontStyle.Bold);
            using var fontVal = new Font("Segoe UI", Math.Max(7f, 10F * _currentZoom), FontStyle.Regular);
            using var fontRank = new Font("Segoe UI", Math.Max(7f, 10F * _currentZoom), FontStyle.Bold);

            for (int i = 0; i < rowCount; i++)
            {
                var item = groups[i];
                float y = padTop + i * rowH;
                float barMaxW = width - padLeft - padRight - (int)Math.Round(170 * _currentZoom);
                if (barMaxW < 50) barMaxW = 50;

                float barW = ((float)item.Count / maxCount) * barMaxW;
                if (barW < 8) barW = 8;

                // Vẽ số thứ hạng
                string rankStr = $"{i + 1}.";
                using var brushRank = new SolidBrush(i == 0 ? Color.FromArgb(217, 119, 6) : Color.FromArgb(100, 116, 139));
                g.DrawString(rankStr, fontRank, brushRank, padLeft, y + 2);
                g.DrawString(item.Name, fontName, Brushes.Black, padLeft + (int)Math.Round(26 * _currentZoom), y + 2);

                float barY = y + (int)Math.Round(25 * _currentZoom);
                float barH = 12f * _currentZoom;

                using var brushSlot = new SolidBrush(Color.FromArgb(241, 245, 249));
                g.FillRectangle(brushSlot, padLeft + (int)Math.Round(26 * _currentZoom), barY, barMaxW, barH);

                using var brushFill = new SolidBrush(barColors[i % barColors.Length]);
                g.FillRectangle(brushFill, padLeft + (int)Math.Round(26 * _currentZoom), barY, barW, barH);

                string ticketWord = _currentLanguage == "EN" ? (item.Count > 1 ? "tickets" : "ticket") : "phiếu";
                string valText = $"{item.Count} {ticketWord} | {item.TotalWeight:N0} Kg";
                using var brushValText = new SolidBrush(Color.FromArgb(71, 85, 105));
                g.DrawString(valText, fontVal, brushValText, padLeft + (int)Math.Round(32 * _currentZoom) + barMaxW, barY - 2);
            }
        }

        private void CaptureBaseLayouts()
        {
            if (gbInput != null)
            {
                foreach (Control c in gbInput.Controls)
                {
                    _baseLayouts[c] = new ControlLayoutInfo
                    {
                        X = c.Left,
                        Y = c.Top,
                        Width = c.Width,
                        Height = c.Height,
                        FontSize = c.Font.SizeInPoints,
                        FontStyle = c.Font.Style,
                        FontFamily = c.Font.FontFamily.Name
                    };
                }
            }

            if (panelSearch != null)
            {
                foreach (Control c in panelSearch.Controls)
                {
                    _baseLayouts[c] = new ControlLayoutInfo
                    {
                        X = c.Left,
                        Y = c.Top,
                        Width = c.Width,
                        Height = c.Height,
                        FontSize = c.Font.SizeInPoints,
                        FontStyle = c.Font.Style,
                        FontFamily = c.Font.FontFamily.Name
                    };
                }
            }

            if (panelChartHeader != null)
            {
                foreach (Control c in panelChartHeader.Controls)
                {
                    _baseLayouts[c] = new ControlLayoutInfo
                    {
                        X = c.Left,
                        Y = c.Top,
                        Width = c.Width,
                        Height = c.Height,
                        FontSize = c.Font.SizeInPoints,
                        FontStyle = c.Font.Style,
                        FontFamily = c.Font.FontFamily.Name
                    };
                }
            }

            if (lblHeaderTitle != null)
            {
                _baseLayouts[lblHeaderTitle] = new ControlLayoutInfo
                {
                    X = lblHeaderTitle.Left,
                    Y = lblHeaderTitle.Top,
                    Width = lblHeaderTitle.Width,
                    Height = lblHeaderTitle.Height,
                    FontSize = lblHeaderTitle.Font.SizeInPoints,
                    FontStyle = lblHeaderTitle.Font.Style,
                    FontFamily = lblHeaderTitle.Font.FontFamily.Name
                };
            }

            if (lblHeaderSub != null)
            {
                _baseLayouts[lblHeaderSub] = new ControlLayoutInfo
                {
                    X = lblHeaderSub.Left,
                    Y = lblHeaderSub.Top,
                    Width = lblHeaderSub.Width,
                    Height = lblHeaderSub.Height,
                    FontSize = lblHeaderSub.Font.SizeInPoints,
                    FontStyle = lblHeaderSub.Font.Style,
                    FontFamily = lblHeaderSub.Font.FontFamily.Name
                };
            }
        }

        private void RepositionHeaderControls()
        {
            if (panelHeader == null || btnSwitchLang == null) return;

            int rightMargin = 16;
            int y = Math.Max(8, (panelHeader.Height - btnSwitchLang.Height) / 2);

            btnSwitchLang.Location = new Point(panelHeader.Width - btnSwitchLang.Width - rightMargin, y);
            if (pnlZoomControls != null)
            {
                pnlZoomControls.Location = new Point(btnSwitchLang.Left - pnlZoomControls.Width - 12, y);
            }
        }

        private void RepositionSearchFilterControls()
        {
            if (panelSearch == null || lblSearch == null || txtSearch == null || chkFilterDate == null ||
                dtpFilterFrom == null || lblFilterTo == null || dtpFilterTo == null ||
                btnSearch == null || btnResetFilter == null || btnPrintSelected == null)
            {
                return;
            }

            int pnlH = Math.Max(46, (int)Math.Round(50 * _currentZoom));
            panelSearch.Height = pnlH;
            int centerY = pnlH / 2;

            // Scale fonts
            float fontRegular = Math.Max(8f, 11F * _currentZoom);
            float fontBtn = Math.Max(8f, 10.5F * _currentZoom);

            lblSearch.Font = new Font("Segoe UI", fontRegular, FontStyle.Regular);
            txtSearch.Font = new Font("Segoe UI", fontRegular, FontStyle.Regular);
            chkFilterDate.Font = new Font("Segoe UI", fontRegular, FontStyle.Regular);
            dtpFilterFrom.Font = new Font("Segoe UI", fontRegular, FontStyle.Regular);
            lblFilterTo.Font = new Font("Segoe UI", fontRegular, FontStyle.Regular);
            dtpFilterTo.Font = new Font("Segoe UI", fontRegular, FontStyle.Regular);
            btnSearch.Font = new Font("Segoe UI", fontBtn, FontStyle.Bold);
            btnResetFilter.Font = new Font("Segoe UI", fontBtn, FontStyle.Bold);
            btnPrintSelected.Font = new Font("Segoe UI", fontBtn, FontStyle.Bold);

            // Scale control sizes
            int ctrlH = Math.Max(30, (int)Math.Round(35 * _currentZoom));
            txtSearch.Width = (int)Math.Round(145 * _currentZoom);

            dtpFilterFrom.Size = new Size((int)Math.Round(135 * _currentZoom), ctrlH);
            dtpFilterTo.Size = new Size((int)Math.Round(135 * _currentZoom), ctrlH);

            btnSearch.Size = new Size((int)Math.Round(85 * _currentZoom), ctrlH);
            btnResetFilter.Size = new Size((int)Math.Round(95 * _currentZoom), ctrlH);
            btnPrintSelected.Size = new Size((int)Math.Round(210 * _currentZoom), ctrlH);

            // Sequential horizontal positioning and exact vertical centering
            int curX = (int)Math.Round(8 * _currentZoom);

            lblSearch.Location = new Point(curX, centerY - (lblSearch.Height / 2));
            curX = lblSearch.Right + (int)Math.Round(5 * _currentZoom);

            txtSearch.Location = new Point(curX, centerY - (txtSearch.Height / 2));
            curX = txtSearch.Right + (int)Math.Round(12 * _currentZoom);

            chkFilterDate.Location = new Point(curX, centerY - (chkFilterDate.Height / 2));
            curX = chkFilterDate.Right + (int)Math.Round(5 * _currentZoom);

            dtpFilterFrom.Location = new Point(curX, centerY - (dtpFilterFrom.Height / 2));
            curX = dtpFilterFrom.Right + (int)Math.Round(6 * _currentZoom);

            lblFilterTo.Location = new Point(curX, centerY - (lblFilterTo.Height / 2));
            curX = lblFilterTo.Right + (int)Math.Round(6 * _currentZoom);

            dtpFilterTo.Location = new Point(curX, centerY - (dtpFilterTo.Height / 2));
            curX = dtpFilterTo.Right + (int)Math.Round(10 * _currentZoom);

            btnSearch.Location = new Point(curX, centerY - (btnSearch.Height / 2));
            curX = btnSearch.Right + (int)Math.Round(8 * _currentZoom);

            btnResetFilter.Location = new Point(curX, centerY - (btnResetFilter.Height / 2));

            // Position btnPrintSelected on the right
            int rightX = panelSearch.Width - btnPrintSelected.Width - (int)Math.Round(10 * _currentZoom);
            btnPrintSelected.Location = new Point(Math.Max(btnResetFilter.Right + (int)Math.Round(10 * _currentZoom), rightX), centerY - (btnPrintSelected.Height / 2));
        }

        private void ToggleChartVisibility()
        {
            SetChartVisibility(!_isChartVisible);
        }

        private void SetChartVisibility(bool visible)
        {
            _isChartVisible = visible;
            if (pnlChartContent != null) pnlChartContent.Visible = visible;

            if (pnlChartWrapper != null)
            {
                if (visible)
                {
                    pnlChartWrapper.Dock = DockStyle.Fill;
                    pnlChartWrapper.BringToFront();
                }
                else
                {
                    pnlChartWrapper.Dock = DockStyle.Top;
                    pnlChartWrapper.Height = (int)Math.Round(42 * _currentZoom);
                    pnlChartWrapper.BringToFront();
                }
            }

            if (btnToggleChart != null && lblChartTitle != null)
            {
                if (visible)
                {
                    btnToggleChart.Text = _currentLanguage == "EN" ? "▲ Close" : "▲ Đóng";
                    btnToggleChart.BackColor = Color.FromArgb(235, 240, 248);
                    btnToggleChart.ForeColor = Color.FromArgb(70, 85, 105);
                }
                else
                {
                    btnToggleChart.Text = _currentLanguage == "EN" ? "▼ Open" : "▼ Mở";
                    btnToggleChart.BackColor = Color.FromArgb(24, 119, 242);
                    btnToggleChart.ForeColor = Color.White;
                }
                btnToggleChart.Location = new Point(lblChartTitle.Right + (int)Math.Round(12 * _currentZoom), (int)Math.Round(5 * _currentZoom));
            }

            if (visible)
            {
                UpdateChartData();
                pnlChartCanvas?.Invalidate();
            }
        }

        private void ZoomIn()
        {
            float target = (float)Math.Round(_currentZoom + 0.10f, 2);
            if (target > 1.50f) target = 1.50f;
            ApplyZoom(target);
        }

        private void ZoomOut()
        {
            float target = (float)Math.Round(_currentZoom - 0.10f, 2);
            if (target < 0.80f) target = 0.80f;
            ApplyZoom(target);
        }

        private void ZoomReset()
        {
            ApplyZoom(1.0f);
        }

        private void ApplyZoom(float zoom)
        {
            _currentZoom = Math.Clamp(zoom, 0.80f, 1.50f);
            if (btnZoomReset != null)
            {
                btnZoomReset.Text = $"{(int)Math.Round(_currentZoom * 100)}%";
            }

            this.SuspendLayout();

            // 1. Scale Column 0 của tableMainLayout (base = 750F)
            if (tableMainLayout != null && tableMainLayout.ColumnStyles.Count > 0)
            {
                tableMainLayout.ColumnStyles[0].Width = (float)Math.Round(750F * _currentZoom);
            }

            // 2. Scale gbInput và các control con
            if (gbInput != null)
            {
                gbInput.Height = (int)Math.Round(375 * _currentZoom);
                gbInput.Font = new Font("Segoe UI", Math.Max(8f, 12F * _currentZoom), FontStyle.Bold);

                foreach (Control c in gbInput.Controls)
                {
                    if (_baseLayouts.TryGetValue(c, out var info))
                    {
                        int newX = (int)Math.Round(info.X * _currentZoom);
                        int newY = (int)Math.Round(info.Y * _currentZoom);
                        int newW = (int)Math.Round(info.Width * _currentZoom);
                        int newH = (int)Math.Round(info.Height * _currentZoom);

                        c.SetBounds(newX, newY, newW, newH);
                        float newFontSize = Math.Max(7f, info.FontSize * _currentZoom);
                        c.Font = new Font(info.FontFamily, newFontSize, info.FontStyle);
                    }
                }

                if (lblSoXe != null)
                {
                    lblSoXe.Size = new Size((int)Math.Round(140 * _currentZoom), (int)Math.Round(26 * _currentZoom));
                    lblSoXe.Font = new Font("Segoe UI", Math.Max(7f, 11F * _currentZoom), FontStyle.Regular);
                    UpdateSoXeLabelPosition(_currentLanguage == "EN" ? "Plate No" : "Số xe",
                        (int)Math.Round(370 * _currentZoom), (int)Math.Round(32 * _currentZoom));
                }
            }

            // 3. Scale panelSearch và các control tìm kiếm
            if (panelSearch != null)
            {
                RepositionSearchFilterControls();
            }

            // 4. Scale dgvPhieuCan
            if (dgvPhieuCan != null)
            {
                float gridFontSize = Math.Max(8f, 11F * _currentZoom);
                dgvPhieuCan.Font = new Font("Segoe UI", gridFontSize, FontStyle.Regular);
                dgvPhieuCan.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", gridFontSize, FontStyle.Bold);
                dgvPhieuCan.DefaultCellStyle.Font = new Font("Segoe UI", gridFontSize, FontStyle.Regular);
                dgvPhieuCan.AlternatingRowsDefaultCellStyle.Font = new Font("Segoe UI", gridFontSize, FontStyle.Regular);

                int rowH = (int)Math.Round(38 * _currentZoom);
                dgvPhieuCan.RowTemplate.Height = rowH;
                foreach (DataGridViewRow row in dgvPhieuCan.Rows)
                {
                    row.Height = rowH;
                }
                dgvPhieuCan.ColumnHeadersHeight = (int)Math.Round(40 * _currentZoom);

                var colSel = dgvPhieuCan.Columns["colSelect"];
                if (colSel != null) colSel.Width = (int)Math.Round(42 * _currentZoom);

                var colStt = dgvPhieuCan.Columns["STT"];
                if (colStt != null) colStt.Width = (int)Math.Round(72 * _currentZoom);

                var colAct = dgvPhieuCan.Columns["colAction"];
                if (colAct != null) colAct.Width = (int)Math.Round(135 * _currentZoom);
            }

            // 5. Scale panelHeader và các nút trong header
            if (panelHeader != null)
            {
                panelHeader.Height = (int)Math.Round(84 * _currentZoom);

                if (lblHeaderTitle != null && _baseLayouts.TryGetValue(lblHeaderTitle, out var titleInfo))
                {
                    lblHeaderTitle.Font = new Font(titleInfo.FontFamily, Math.Max(9f, titleInfo.FontSize * _currentZoom), titleInfo.FontStyle);
                    lblHeaderTitle.Location = new Point((int)Math.Round(titleInfo.X * _currentZoom), (int)Math.Round(titleInfo.Y * _currentZoom));
                }
                if (lblHeaderSub != null && _baseLayouts.TryGetValue(lblHeaderSub, out var subInfo))
                {
                    lblHeaderSub.Font = new Font(subInfo.FontFamily, Math.Max(7f, subInfo.FontSize * _currentZoom), subInfo.FontStyle);
                    lblHeaderSub.Location = new Point((int)Math.Round(subInfo.X * _currentZoom), (int)Math.Round(subInfo.Y * _currentZoom));
                }

                if (btnSwitchLang != null)
                {
                    btnSwitchLang.Width = (int)Math.Round(145 * _currentZoom);
                    btnSwitchLang.Height = (int)Math.Round(38 * _currentZoom);
                    btnSwitchLang.Font = new Font("Segoe UI", Math.Max(7f, 9.5F * _currentZoom), FontStyle.Bold);
                }

                if (pnlZoomControls != null && btnZoomOut != null && btnZoomReset != null && btnZoomIn != null)
                {
                    int zBtnH = (int)Math.Round(38 * _currentZoom);
                    btnZoomOut.Width = (int)Math.Round(38 * _currentZoom);
                    btnZoomOut.Height = zBtnH;
                    btnZoomOut.Invalidate();

                    btnZoomReset.Width = (int)Math.Round(70 * _currentZoom);
                    btnZoomReset.Height = zBtnH;
                    btnZoomReset.Font = new Font("Segoe UI", Math.Max(7f, 9.5F * _currentZoom), FontStyle.Bold);

                    btnZoomIn.Width = (int)Math.Round(38 * _currentZoom);
                    btnZoomIn.Height = zBtnH;
                    btnZoomIn.Invalidate();

                    btnZoomOut.Location = new Point(0, 0);
                    btnZoomReset.Location = new Point(btnZoomOut.Right + (int)Math.Round(4 * _currentZoom), 0);
                    btnZoomIn.Location = new Point(btnZoomReset.Right + (int)Math.Round(4 * _currentZoom), 0);
                    pnlZoomControls.Size = new Size(btnZoomIn.Right, zBtnH);
                }

                RepositionHeaderControls();
            }

            // 6. Scale Chart wrapper, Title Bar và header biểu đồ
            if (pnlChartTitleBar != null)
            {
                pnlChartTitleBar.Height = (int)Math.Round(40 * _currentZoom);
            }
            if (lblChartTitle != null)
            {
                lblChartTitle.Font = new Font("Segoe UI", Math.Max(8f, 11.5F * _currentZoom), FontStyle.Bold);
                lblChartTitle.Location = new Point((int)Math.Round(8 * _currentZoom), (int)Math.Round(8 * _currentZoom));
            }
            if (btnToggleChart != null && lblChartTitle != null)
            {
                btnToggleChart.Font = new Font("Segoe UI", Math.Max(7f, 9.5F * _currentZoom), FontStyle.Bold);
                btnToggleChart.Size = new Size((int)Math.Round(85 * _currentZoom), (int)Math.Round(28 * _currentZoom));
                btnToggleChart.Location = new Point(lblChartTitle.Right + (int)Math.Round(12 * _currentZoom), (int)Math.Round(5 * _currentZoom));
            }
            if (pnlChartWrapper != null && !_isChartVisible)
            {
                pnlChartWrapper.Height = (int)Math.Round(42 * _currentZoom);
            }
            if (panelChartHeader != null)
            {
                panelChartHeader.Height = (int)Math.Round(44 * _currentZoom);
                foreach (Control c in panelChartHeader.Controls)
                {
                    if (_baseLayouts.TryGetValue(c, out var info))
                    {
                        int newX = (int)Math.Round(info.X * _currentZoom);
                        int newY = (int)Math.Round(info.Y * _currentZoom);
                        int newW = (int)Math.Round(info.Width * _currentZoom);
                        int newH = (int)Math.Round(info.Height * _currentZoom);

                        c.SetBounds(newX, newY, newW, newH);
                        float newFontSize = Math.Max(7f, info.FontSize * _currentZoom);
                        c.Font = new Font(info.FontFamily, newFontSize, info.FontStyle);
                    }
                }
            }

            this.ResumeLayout(true);
            pnlChartCanvas?.Invalidate();
            if (dgvPhieuCan != null)
            {
                dgvPhieuCan.Invalidate(true);
                dgvPhieuCan.Update();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if ((keyData & Keys.Control) == Keys.Control)
            {
                Keys keyCode = keyData & Keys.KeyCode;
                if (keyCode == Keys.Add || keyCode == Keys.Oemplus)
                {
                    ZoomIn();
                    return true;
                }
                else if (keyCode == Keys.Subtract || keyCode == Keys.OemMinus)
                {
                    ZoomOut();
                    return true;
                }
                else if (keyCode == Keys.D0 || keyCode == Keys.NumPad0)
                {
                    ZoomReset();
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                if (e.Delta > 0)
                {
                    ZoomIn();
                }
                else if (e.Delta < 0)
                {
                    ZoomOut();
                }
                return;
            }

            base.OnMouseWheel(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (HasUnsavedNewTicket())
            {
                string confirmMsg = _currentLanguage == "EN"
                    ? "You are creating a new ticket with unsaved information. Are you sure you want to close the application?"
                    : "Bạn đang nhập thông tin phiếu cân mới chưa được lưu lại. Bạn có chắc chắn muốn đóng ứng dụng không?";
                string confirmTitle = _currentLanguage == "EN" ? "Exit Confirmation" : "Xác nhận đóng ứng dụng";

                if (CustomMessageBox.Show(confirmMsg, confirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using CanXeApp.Models;
using CanXeApp.Services;

namespace CanXeApp
{
    public class FormLichSuEdit : Form
    {
        private readonly int _phieuCanId;
        private readonly int _stt;
        private readonly string _soXe;
        private readonly DatabaseService _dbService;
        private readonly string _language;
        private readonly float _zoom;

        private List<PhieuCanHistory> _historyList = new();
        private DataGridView dgvHistory;
        private DataGridView dgvDetails;
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Label lblDetailTitle;
        private Label lblEmptyState;
        private Button btnClose;

        public FormLichSuEdit(int phieuCanId, int stt, string soXe, DatabaseService dbService, string language = "VI", float zoom = 1.0f)
        {
            _phieuCanId = phieuCanId;
            _stt = stt;
            _soXe = soXe ?? "";
            _dbService = dbService;
            _language = language;
            _zoom = zoom > 0 ? zoom : 1.0f;

            InitializeComponentCustom();
            LoadHistoryData();
        }

        private void InitializeComponentCustom()
        {
            int w = (int)Math.Round(1100 * _zoom);
            int h = (int)Math.Round(580 * _zoom);
            this.ClientSize = new Size(w, h);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Font = new Font("Segoe UI", Math.Max(8.5f, 10F * _zoom), FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.Text = _language == "EN" ? $"Ticket Edit History - STT: Xe {_stt}" : $"LỊCH SỬ CHỈNH SỬA PHIẾU CÂN - STT: Xe {_stt}";

            try
            {
                if (System.IO.File.Exists("app_icon.ico")) this.Icon = new Icon("app_icon.ico");
                else if (System.IO.File.Exists("app_icon.png")) this.Icon = Icon.FromHandle(((Bitmap)Image.FromFile("app_icon.png")).GetHicon());
            }
            catch { }

            // Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = (int)Math.Round(68 * _zoom),
                BackColor = Color.FromArgb(33, 43, 54),
                Padding = new Padding((int)Math.Round(16 * _zoom), (int)Math.Round(10 * _zoom), (int)Math.Round(16 * _zoom), (int)Math.Round(10 * _zoom))
            };

            lblHeaderTitle = new Label
            {
                Text = _language == "EN" ? $"📜 EDIT HISTORY - TICKET NO: Xe {_stt}" : $"📜 LỊCH SỬ CHỈNH SỬA - PHIẾU CÂN: Xe {_stt}",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", Math.Max(9.5f, 13F * _zoom), FontStyle.Bold),
                AutoSize = true,
                Location = new Point((int)Math.Round(16 * _zoom), (int)Math.Round(10 * _zoom))
            };

            lblHeaderSub = new Label
            {
                Text = _language == "EN" ? $"Plate Number: {_soXe} | Loading..." : $"Biển số xe: {_soXe} | Đang tải...",
                ForeColor = Color.FromArgb(200, 210, 225),
                Font = new Font("Segoe UI", Math.Max(8.5f, 10F * _zoom), FontStyle.Regular),
                AutoSize = true,
                Location = new Point((int)Math.Round(16 * _zoom), (int)Math.Round(38 * _zoom))
            };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);

            // Footer Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = (int)Math.Round(52 * _zoom),
                BackColor = Color.FromArgb(240, 243, 246)
            };

            btnClose = new Button
            {
                Text = _language == "EN" ? "❌ Close" : "❌ Đóng",
                Size = new Size((int)Math.Round(110 * _zoom), (int)Math.Round(36 * _zoom)),
                BackColor = Color.FromArgb(108, 117, 125),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", Math.Max(8.5f, 10F * _zoom), FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Location = new Point(w - btnClose.Width - (int)Math.Round(16 * _zoom), (pnlFooter.Height - btnClose.Height) / 2);
            btnClose.Click += (s, e) => this.Close();
            pnlFooter.Controls.Add(btnClose);

            // Body Container
            var pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding((int)Math.Round(16 * _zoom), (int)Math.Round(12 * _zoom), (int)Math.Round(16 * _zoom), (int)Math.Round(6 * _zoom)),
                BackColor = Color.FromArgb(248, 249, 250)
            };

            // Empty state label
            lblEmptyState = new Label
            {
                Text = _language == "EN" 
                    ? "ℹ️ This ticket has not been edited yet. The data is original." 
                    : "ℹ️ Phiếu cân này chưa từng bị chỉnh sửa. Toàn bộ dữ liệu hiện tại là dữ liệu gốc ban đầu.",
                ForeColor = Color.FromArgb(100, 110, 125),
                Font = new Font("Segoe UI", Math.Max(9.5f, 12F * _zoom), FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false
            };
            pnlBody.Controls.Add(lblEmptyState);

            // Table 1: Danh sách các lần sửa (History events)
            dgvHistory = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = (int)Math.Round(170 * _zoom),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", Math.Max(8.5f, 9.5F * _zoom))
            };
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 240, 248);
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 40, 60);
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", Math.Max(8.5f, 9.5F * _zoom), FontStyle.Bold);
            dgvHistory.ColumnHeadersHeight = (int)Math.Round(32 * _zoom);
            dgvHistory.RowTemplate.Height = (int)Math.Round(28 * _zoom);
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColLan",
                HeaderText = _language == "EN" ? "No." : "Lần",
                FillWeight = 8
            });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColThoiGian",
                HeaderText = _language == "EN" ? "Edit Time" : "Thời gian sửa",
                FillWeight = 18
            });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColNguoiSua",
                HeaderText = _language == "EN" ? "User / Operator" : "Người sửa",
                FillWeight = 14
            });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColNoiDung",
                HeaderText = _language == "EN" ? "Summary of Changes" : "Tóm tắt thay đổi",
                FillWeight = 60
            });

            dgvHistory.SelectionChanged += DgvHistory_SelectionChanged;

            // Detail Header
            lblDetailTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = (int)Math.Round(30 * _zoom),
                Text = _language == "EN" ? "🔍 Comparison of changed values (Before vs After):" : "🔍 So sánh chi tiết trước và sau khi sửa của lần chọn:",
                Font = new Font("Segoe UI", Math.Max(8.5f, 10F * _zoom), FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 40, 60),
                TextAlign = ContentAlignment.BottomLeft,
                Padding = new Padding(0, 0, 0, 4)
            };

            // Table 2: Chi tiết so sánh Before / After
            dgvDetails = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", Math.Max(8.5f, 9.5F * _zoom))
            };
            dgvDetails.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 240, 248);
            dgvDetails.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 40, 60);
            dgvDetails.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", Math.Max(8.5f, 9.5F * _zoom), FontStyle.Bold);
            dgvDetails.ColumnHeadersHeight = (int)Math.Round(32 * _zoom);
            dgvDetails.RowTemplate.Height = (int)Math.Round(28 * _zoom);
            dgvDetails.DefaultCellStyle.SelectionBackColor = Color.White;
            dgvDetails.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvDetails.SelectionChanged += (s, e) => dgvDetails.ClearSelection();

            dgvDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColTruong",
                HeaderText = _language == "EN" ? "Field" : "Thông tin / Trường",
                FillWeight = 22
            });
            dgvDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColCu",
                HeaderText = _language == "EN" ? "Before Edit (Old)" : "Trước khi sửa (Cũ)",
                FillWeight = 34
            });
            dgvDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColMoi",
                HeaderText = _language == "EN" ? "After Edit (New)" : "Sau khi sửa (Mới)",
                FillWeight = 34
            });
            dgvDetails.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColTrangThai",
                HeaderText = _language == "EN" ? "Status" : "Trạng thái",
                FillWeight = 16
            });

            // Add controls (order matters for DockStyle)
            pnlBody.Controls.Add(dgvDetails);
            pnlBody.Controls.Add(lblDetailTitle);
            pnlBody.Controls.Add(dgvHistory);

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlFooter);
            this.Controls.Add(pnlHeader);

            this.CancelButton = btnClose;
        }

        private void LoadHistoryData()
        {
            try
            {
                _historyList = _dbService.GetHistoryByPhieuCanId(_phieuCanId);
                // Sắp xếp các lần chỉnh sửa bắt đầu từ lần mới nhất lên đầu tiên
                _historyList.Sort((a, b) => b.Id.CompareTo(a.Id));
            }
            catch
            {
                _historyList = new List<PhieuCanHistory>();
            }

            int count = _historyList.Count;
            lblHeaderSub.Text = _language == "EN"
                ? $"Plate Number: {_soXe} | Total edits: {count}"
                : $"Biển số xe: {_soXe} | Tổng số lần chỉnh sửa: {count}";

            if (count == 0)
            {
                lblEmptyState.Visible = true;
                lblEmptyState.BringToFront();
                dgvHistory.Visible = false;
                lblDetailTitle.Visible = false;
                dgvDetails.Visible = false;
                return;
            }

            lblEmptyState.Visible = false;
            dgvHistory.Visible = true;
            lblDetailTitle.Visible = true;
            dgvDetails.Visible = true;

            dgvHistory.Rows.Clear();
            for (int i = 0; i < _historyList.Count; i++)
            {
                var h = _historyList[i];
                int lanNumber = count - i; // Lần mới nhất sẽ mang số thứ tự cao nhất
                string lanText = _language == "EN" ? $"#{lanNumber}" : $"Lần {lanNumber}";
                string userText = string.IsNullOrWhiteSpace(h.NguoiThayDoi) ? (_language == "EN" ? "Admin" : "Người cân") : h.NguoiThayDoi;
                string tomTat = (h.NoiDungThayDoi ?? "").Replace("\r\n", " • ").Replace("\n", " • ");

                dgvHistory.Rows.Add(lanText, h.ThoiGianThayDoi.ToString("dd/MM/yyyy HH:mm:ss"), userText, tomTat);
            }

            // Mặc định chọn dòng trên cùng (lần sửa mới nhất)
            if (dgvHistory.Rows.Count > 0)
            {
                dgvHistory.ClearSelection();
                dgvHistory.Rows[0].Selected = true;
                DisplayDetailComparison(_historyList[0]);
            }
        }

        private void DgvHistory_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvHistory.SelectedRows.Count == 0) return;
            int selectedIdx = dgvHistory.SelectedRows[0].Index;
            if (selectedIdx < 0 || selectedIdx >= _historyList.Count) return;

            var selectedHistory = _historyList[selectedIdx];
            DisplayDetailComparison(selectedHistory);
        }

        private void DisplayDetailComparison(PhieuCanHistory h)
        {
            dgvDetails.Rows.Clear();

            Dictionary<string, string>? dictCu = null;
            Dictionary<string, string>? dictMoi = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(h.DuLieuCuJson))
                    dictCu = JsonSerializer.Deserialize<Dictionary<string, string>>(h.DuLieuCuJson);
                if (!string.IsNullOrWhiteSpace(h.DuLieuMoiJson))
                    dictMoi = JsonSerializer.Deserialize<Dictionary<string, string>>(h.DuLieuMoiJson);
            }
            catch { }

            // Danh mục các trường cần hiển thị
            var fieldDefinitions = new List<(string Key, string NameVi, string NameEn)>
            {
                ("SoXe", "Biển số xe", "Plate Number"),
                ("LoaiHang", "Loại hàng", "Goods Type"),
                ("KhachHang", "Khách hàng", "Customer"),
                ("KhoiLuongTong", "KL Tổng (Kg)", "Gross Wt (Kg)"),
                ("KhoiLuongXe", "KL Xe (Kg)", "Tare Wt (Kg)"),
                ("KhoiLuongHang", "KL Hàng (Kg)", "Net Wt (Kg)"),
                ("NgayInPhieu", "Ngày in phiếu", "Ticket Date"),
                ("KieuCan", "Kiểu cân", "Weigh Type"),
                ("SoChungTu", "Số chứng từ", "Voucher No"),
                ("GhiChu", "Ghi chú", "Note")
            };

            // Tách thành danh sách trường bị thay đổi và trường giữ nguyên
            var changedRows = new List<(string Field, string OldVal, string NewVal)>();
            var unchangedRows = new List<(string Field, string OldVal, string NewVal)>();

            foreach (var f in fieldDefinitions)
            {
                string fieldName = _language == "EN" ? f.NameEn : f.NameVi;
                string oldVal = dictCu != null && dictCu.ContainsKey(f.Key) ? dictCu[f.Key] : "";
                string newVal = dictMoi != null && dictMoi.ContainsKey(f.Key) ? dictMoi[f.Key] : "";

                if (oldVal != newVal)
                {
                    changedRows.Add((fieldName, oldVal, newVal));
                }
                else
                {
                    unchangedRows.Add((fieldName, oldVal, newVal));
                }
            }

            // Thêm các trường ĐÃ THAY ĐỔI lên trên cùng với màu sắc nổi bật (không highlight xanh khi click)
            foreach (var item in changedRows)
            {
                int rowIdx = dgvDetails.Rows.Add(item.Field, item.OldVal, item.NewVal, _language == "EN" ? "CHANGED" : "ĐÃ THAY ĐỔI");
                var row = dgvDetails.Rows[rowIdx];
                Color bg = Color.FromArgb(254, 249, 231); // Vàng kem nhạt dịu mắt
                row.DefaultCellStyle.BackColor = bg;
                row.DefaultCellStyle.SelectionBackColor = bg;
                row.DefaultCellStyle.ForeColor = Color.Black;
                row.DefaultCellStyle.SelectionForeColor = Color.Black;
                row.DefaultCellStyle.Font = new Font(dgvDetails.Font, FontStyle.Bold);

                row.Cells["ColTrangThai"].Style.ForeColor = Color.FromArgb(217, 83, 79); // Đỏ cam
                row.Cells["ColTrangThai"].Style.SelectionForeColor = Color.FromArgb(217, 83, 79);
                row.Cells["ColTrangThai"].Style.SelectionBackColor = bg;
            }

            // Thêm các trường GIỮ NGUYÊN xuống dưới (không highlight xanh khi click)
            foreach (var item in unchangedRows)
            {
                int rowIdx = dgvDetails.Rows.Add(item.Field, item.OldVal, item.NewVal, _language == "EN" ? "Unchanged" : "Giữ nguyên");
                var row = dgvDetails.Rows[rowIdx];
                Color bg = Color.White;
                row.DefaultCellStyle.BackColor = bg;
                row.DefaultCellStyle.SelectionBackColor = bg;
                row.DefaultCellStyle.ForeColor = Color.Black;
                row.DefaultCellStyle.SelectionForeColor = Color.Black;

                row.Cells["ColTrangThai"].Style.ForeColor = Color.FromArgb(140, 150, 165);
                row.Cells["ColTrangThai"].Style.SelectionForeColor = Color.FromArgb(140, 150, 165);
                row.Cells["ColTrangThai"].Style.SelectionBackColor = bg;
            }

            dgvDetails.ClearSelection();
        }
    }
}

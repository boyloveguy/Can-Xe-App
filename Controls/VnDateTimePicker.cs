using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CanXeApp.Controls
{
    public class VnDateTimePicker : UserControl
    {
        private Panel _pnlContainer;
        private TextBox _txtDate;
        private Button _btnCalendar;
        private ToolStripDropDown _dropDown;
        private ToolStripControlHost _host;
        private CustomVnCalendarControl _calendar;

        private DateTime _value = DateTime.Now;
        private string _customFormat = "dd/MM/yyyy HH:mm";
        private bool _showTime = true;
        private bool _preventFutureDates = false;
        private string _language = "VI"; // "VI" hoặc "EN"

        public event EventHandler? ValueChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime Value
        {
            get => _value;
            set
            {
                if (_preventFutureDates && value > DateTime.Now)
                {
                    value = DateTime.Now;
                }

                _value = value;
                UpdateText();
                if (_calendar != null) _calendar.SelectedDate = _value;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CustomFormat
        {
            get => _customFormat;
            set
            {
                _customFormat = value;
                _showTime = _customFormat.Contains("HH") || _customFormat.Contains("mm");
                UpdateText();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowTimePicker
        {
            get => _showTime;
            set => _showTime = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool PreventFutureDates
        {
            get => _preventFutureDates;
            set
            {
                _preventFutureDates = value;
                if (_preventFutureDates && _value > DateTime.Now)
                {
                    Value = DateTime.Now;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                if (_calendar != null)
                {
                    _calendar.Language = _language;
                }
            }
        }

        public VnDateTimePicker()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Size = new Size(185, 28);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            _pnlContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(2, 3, 2, 1),
                BackColor = Color.White
            };

            _txtDate = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Multiline = false,
                BackColor = Color.White
            };
            _txtDate.KeyDown += TxtDate_KeyDown;
            _txtDate.Leave += TxtDate_Leave;

            _btnCalendar = new Button
            {
                Text = "📅",
                Dock = DockStyle.Right,
                Width = 26,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(240, 242, 245),
                Font = new Font("Segoe UI Emoji", 8.5F)
            };
            _btnCalendar.FlatAppearance.BorderSize = 0;
            _btnCalendar.Click += BtnCalendar_Click;

            _pnlContainer.Controls.Add(_txtDate);
            this.Controls.Add(_pnlContainer);
            this.Controls.Add(_btnCalendar);

            // Calendar Popup
            _calendar = new CustomVnCalendarControl();
            _calendar.DateSelected += Calendar_DateSelected;

            _host = new ToolStripControlHost(_calendar)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            _dropDown = new ToolStripDropDown
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _dropDown.Items.Add(_host);

            UpdateText();
        }

        private void UpdateText()
        {
            if (_txtDate != null)
            {
                _txtDate.Text = _value.ToString(_customFormat);
            }
        }

        private void BtnCalendar_Click(object? sender, EventArgs e)
        {
            if (_dropDown.Visible)
            {
                _dropDown.Close();
            }
            else
            {
                _calendar.Language = _language;
                _calendar.SelectedDate = _value;
                _calendar.PreventFutureDates = _preventFutureDates;
                _calendar.ShowTimePicker = _showTime;
                _host.Size = _calendar.Size;

                Point screenLocation = this.PointToScreen(new Point(0, this.Height));
                _dropDown.Show(screenLocation);
            }
        }

        private void Calendar_DateSelected(object? sender, DateTime e)
        {
            Value = e;
            _dropDown.Close();
        }

        private void TxtDate_Leave(object? sender, EventArgs e)
        {
            ParseInputText();
        }

        private void TxtDate_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ParseInputText();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ParseInputText()
        {
            if (DateTime.TryParseExact(_txtDate.Text.Trim(), _customFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) ||
                DateTime.TryParse(_txtDate.Text.Trim(), out dt))
            {
                Value = dt;
            }
            else
            {
                UpdateText();
            }
        }
    }

    public class CustomVnCalendarControl : UserControl
    {
        public event EventHandler<DateTime>? DateSelected;

        private DateTime _displayDate = DateTime.Now;
        private DateTime _selectedDate = DateTime.Now;
        private bool _showTimePicker = true;
        private string _language = "VI";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool PreventFutureDates { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ShowTimePicker
        {
            get => _showTimePicker;
            set
            {
                _showTimePicker = value;
                ApplyShowTimePicker();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                UpdateHeader();
                RenderDays();
                ApplyShowTimePicker();
            }
        }

        private Label lblHeader;
        private Button btnPrev, btnNext;
        private Panel panelDays;
        private Label lblTime;
        private NumericUpDown numHour, numMinute;
        private Button btnToday, btnOK;

        private readonly string[] _dayNamesVi = new string[] { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
        private readonly string[] _dayNamesEn = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private readonly string[] _monthNamesEn = new string[] { "", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                _displayDate = value;
                UpdateHeader();
                RenderDays();
                UpdateTimeControls();
            }
        }

        public CustomVnCalendarControl()
        {
            InitializeComponentCustom();
        }

        private void InitializeComponentCustom()
        {
            this.Size = new Size(285, 270);
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.BorderStyle = BorderStyle.FixedSingle;

            // 1. Header Month/Year
            Panel panelHeader = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = Color.FromArgb(24, 119, 242) };

            btnPrev = new Button { Text = "◄", Width = 30, Dock = DockStyle.Left, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.Click += (s, e) => { _displayDate = _displayDate.AddMonths(-1); UpdateHeader(); RenderDays(); };

            btnNext = new Button { Text = "►", Width = 30, Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += (s, e) => { _displayDate = _displayDate.AddMonths(1); UpdateHeader(); RenderDays(); };

            lblHeader = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White };

            panelHeader.Controls.Add(lblHeader);
            panelHeader.Controls.Add(btnPrev);
            panelHeader.Controls.Add(btnNext);

            // 2. Days Panel
            panelDays = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

            // 3. Footer (Giờ:Phút & Nút "Hôm nay" / "Chọn")
            Panel panelFooter = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(245, 247, 250) };

            lblTime = new Label { Text = "Giờ:", Location = new Point(4, 10), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            numHour = new NumericUpDown { Location = new Point(42, 7), Width = 38, Minimum = 0, Maximum = 23 };
            numMinute = new NumericUpDown { Location = new Point(83, 7), Width = 38, Minimum = 0, Maximum = 59 };

            btnToday = new Button { Text = "Hôm nay", Location = new Point(126, 6), Width = 80, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(108, 117, 125), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnToday.Click += (s, e) =>
            {
                SelectedDate = DateTime.Now;
                DateSelected?.Invoke(this, _selectedDate);
            };

            btnOK = new Button { Text = "Chọn", Location = new Point(211, 6), Width = 65, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 167, 69), ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            btnOK.Click += (s, e) =>
            {
                if (_showTimePicker)
                {
                    _selectedDate = new DateTime(_selectedDate.Year, _selectedDate.Month, _selectedDate.Day, (int)numHour.Value, (int)numMinute.Value, 0);
                }
                if (PreventFutureDates && _selectedDate > DateTime.Now) _selectedDate = DateTime.Now;
                DateSelected?.Invoke(this, _selectedDate);
            };

            panelFooter.Controls.AddRange(new Control[] { lblTime, numHour, numMinute, btnToday, btnOK });

            this.Controls.Add(panelDays);
            this.Controls.Add(panelHeader);
            this.Controls.Add(panelFooter);

            UpdateHeader();
            RenderDays();
            ApplyShowTimePicker();
        }

        private void ApplyShowTimePicker()
        {
            lblTime.Text = _language == "VI" ? "Giờ:" : "Time:";
            btnToday.Text = _language == "VI" ? "Hôm nay" : "Today";
            btnOK.Text = _language == "VI" ? "Chọn" : "OK";

            lblTime.Visible = _showTimePicker;
            numHour.Visible = _showTimePicker;
            numMinute.Visible = _showTimePicker;

            if (!_showTimePicker)
            {
                btnToday.Location = new Point(90, 6);
                btnToday.Width = 90;
                btnOK.Location = new Point(188, 6);
                btnOK.Width = 85;
            }
            else
            {
                btnToday.Location = new Point(126, 6);
                btnToday.Width = 80;
                btnOK.Location = new Point(211, 6);
                btnOK.Width = 65;
            }
        }

        private void UpdateHeader()
        {
            if (_language == "VI")
            {
                lblHeader.Text = $"Tháng {_displayDate.Month} năm {_displayDate.Year}";
            }
            else
            {
                lblHeader.Text = $"{_monthNamesEn[_displayDate.Month]} {_displayDate.Year}";
            }
        }

        private void UpdateTimeControls()
        {
            numHour.Value = _selectedDate.Hour;
            numMinute.Value = _selectedDate.Minute;
        }

        private void RenderDays()
        {
            panelDays.Controls.Clear();

            TableLayoutPanel table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 7,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            for (int i = 0; i < 7; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14.28F));
            }
            for (int i = 0; i < 7; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Percent, 14.28F));
            }

            string[] dayNames = _language == "VI" ? _dayNamesVi : _dayNamesEn;

            for (int i = 0; i < 7; i++)
            {
                Label lblDay = new Label
                {
                    Text = dayNames[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = (i == 0) ? Color.Red : Color.FromArgb(50, 50, 50)
                };
                table.Controls.Add(lblDay, i, 0);
            }

            DateTime firstDayOfMonth = new DateTime(_displayDate.Year, _displayDate.Month, 1);
            int startDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            int daysInMonth = DateTime.DaysInMonth(_displayDate.Year, _displayDate.Month);

            int currentCell = startDayOfWeek;
            int row = 1;

            for (int day = 1; day <= daysInMonth; day++)
            {
                int col = currentCell % 7;

                Button btnDay = new Button
                {
                    Text = day.ToString(),
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(1),
                    Font = new Font("Segoe UI", 8.5F),
                    Cursor = Cursors.Hand
                };
                btnDay.FlatAppearance.BorderSize = 0;

                DateTime cellDate = new DateTime(_displayDate.Year, _displayDate.Month, day);

                if (PreventFutureDates && cellDate.Date > DateTime.Now.Date)
                {
                    btnDay.Enabled = false;
                    btnDay.ForeColor = Color.LightGray;
                }

                if (cellDate.Date == _selectedDate.Date)
                {
                    btnDay.BackColor = Color.FromArgb(24, 119, 242);
                    btnDay.ForeColor = Color.White;
                    btnDay.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
                }
                else if (cellDate.Date == DateTime.Now.Date)
                {
                    btnDay.BackColor = Color.FromArgb(230, 240, 255);
                    btnDay.ForeColor = Color.DarkBlue;
                }

                int targetDay = day;
                btnDay.Click += (s, e) =>
                {
                    if (_showTimePicker)
                    {
                        _selectedDate = new DateTime(_displayDate.Year, _displayDate.Month, targetDay, (int)numHour.Value, (int)numMinute.Value, 0);
                    }
                    else
                    {
                        _selectedDate = new DateTime(_displayDate.Year, _displayDate.Month, targetDay);
                    }

                    if (PreventFutureDates && _selectedDate > DateTime.Now) _selectedDate = DateTime.Now;
                    DateSelected?.Invoke(this, _selectedDate);
                };

                table.Controls.Add(btnDay, col, row);

                currentCell++;
                if (col == 6) row++;
            }

            panelDays.Controls.Add(table);
        }
    }
}

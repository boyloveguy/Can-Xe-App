using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace CanXeApp.Controls
{
    public class FormattedNumericTextBox : TextBox
    {
        private bool _isFormatting = false;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double Value
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Text)) return 0;
                string cleanText = this.Text.Replace(".", "").Replace(",", "").Trim();
                return double.TryParse(cleanText, NumberStyles.Any, CultureInfo.InvariantCulture, out double result) ? result : 0;
            }
            set
            {
                if (value <= 0)
                {
                    this.Text = "0";
                }
                else
                {
                    this.Text = value.ToString("#,##0", new CultureInfo("de-DE"));
                }
            }
        }

        public FormattedNumericTextBox()
        {
            this.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            this.Text = "0";
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            // Chỉ cho phép gõ phím điều khiển (Backspace...) và phím số (0-9)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            if (_isFormatting) return;
            _isFormatting = true;

            try
            {
                int selectionStart = this.SelectionStart;
                string originalText = this.Text;

                // Đếm số lượng chữ số nằm trước vị trí con trỏ hiện tại
                int digitsBeforeCursor = 0;
                for (int i = 0; i < selectionStart && i < originalText.Length; i++)
                {
                    if (char.IsDigit(originalText[i])) digitsBeforeCursor++;
                }

                // Lọc lấy duy nhất các chữ số
                string rawDigits = new string(originalText.Where(char.IsDigit).ToArray());

                if (string.IsNullOrEmpty(rawDigits))
                {
                    this.Text = "";
                }
                else
                {
                    if (double.TryParse(rawDigits, out double val))
                    {
                        if (val > 999999999) val = 999999999;
                        string formatted = val.ToString("#,##0", new CultureInfo("de-DE"));
                        this.Text = formatted;

                        // Định vị lại con trỏ chính xác dựa theo số lượng chữ số trước con trỏ
                        int newSelectionStart = 0;
                        int digitCount = 0;
                        for (int i = 0; i < formatted.Length; i++)
                        {
                            if (char.IsDigit(formatted[i]))
                            {
                                digitCount++;
                            }
                            if (digitCount <= digitsBeforeCursor)
                            {
                                newSelectionStart = i + 1;
                            }
                        }
                        this.SelectionStart = Math.Min(newSelectionStart, formatted.Length);
                    }
                }
            }
            finally
            {
                _isFormatting = false;
                base.OnTextChanged(e);
            }
        }

        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            this.BeginInvoke((MethodInvoker)delegate
            {
                this.SelectAll();
            });
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (this.SelectionLength == 0)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    this.SelectAll();
                });
            }
        }
    }
}

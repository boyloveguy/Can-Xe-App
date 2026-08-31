using System;
using System.Drawing;
using System.Windows.Forms;

namespace CanXeApp
{
    public static class CustomMessageBox
    {
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (Form msgForm = new Form())
            {
                msgForm.Text = caption;
                msgForm.Size = new Size(420, 190);
                msgForm.StartPosition = FormStartPosition.CenterParent;
                msgForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                msgForm.MaximizeBox = false;
                msgForm.MinimizeBox = false;
                msgForm.ControlBox = true; // Cho phép hiển thị nút X góc trên bên phải
                msgForm.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                msgForm.BackColor = Color.White;

                PictureBox picIcon = new PictureBox
                {
                    Location = new Point(20, 20),
                    Size = new Size(40, 40),
                    SizeMode = PictureBoxSizeMode.CenterImage
                };

                if (icon == MessageBoxIcon.Question)
                    picIcon.Image = SystemIcons.Question.ToBitmap();
                else if (icon == MessageBoxIcon.Warning)
                    picIcon.Image = SystemIcons.Warning.ToBitmap();
                else if (icon == MessageBoxIcon.Error)
                    picIcon.Image = SystemIcons.Error.ToBitmap();
                else
                    picIcon.Image = SystemIcons.Information.ToBitmap();

                Label lblText = new Label
                {
                    Text = text,
                    Location = new Point(75, 20),
                    Size = new Size(310, 60),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 40, 60)
                };

                Panel panelBottom = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    BackColor = Color.FromArgb(245, 247, 250)
                };

                Button btnYes = new Button
                {
                    Text = "Có (Yes)",
                    DialogResult = DialogResult.Yes,
                    Location = new Point(190, 10),
                    Size = new Size(95, 32),
                    BackColor = Color.FromArgb(0, 123, 255),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                Button btnNo = new Button
                {
                    Text = "Không (No)",
                    DialogResult = DialogResult.No,
                    Location = new Point(295, 10),
                    Size = new Size(95, 32),
                    BackColor = Color.FromArgb(108, 117, 125),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };

                panelBottom.Controls.Add(btnYes);
                panelBottom.Controls.Add(btnNo);

                msgForm.Controls.Add(picIcon);
                msgForm.Controls.Add(lblText);
                msgForm.Controls.Add(panelBottom);

                msgForm.AcceptButton = btnYes;
                msgForm.CancelButton = btnNo; // Đóng bằng phím Esc hoặc nút X sẽ trả về DialogResult.No/Cancel

                return msgForm.ShowDialog();
            }
        }
    }
}

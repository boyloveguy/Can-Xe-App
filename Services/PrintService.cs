using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using CanXeApp.Models;

namespace CanXeApp.Services
{
    public class PrintService
    {
        private PhieuCan _phieuCan;

        public PrintService(PhieuCan phieuCan)
        {
            _phieuCan = phieuCan;
        }

        public void PrintPreview()
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += Pd_PrintPage;
            PrintPreviewDialog previewDialog = new PrintPreviewDialog
            {
                Document = pd,
                Width = 950,
                Height = 720,
                StartPosition = FormStartPosition.CenterScreen
            };
            if (previewDialog.PrintPreviewControl != null)
            {
                previewDialog.PrintPreviewControl.AutoZoom = false;
                previewDialog.PrintPreviewControl.Zoom = 1.0; // 100% zoom cho rõ nét
            }
            previewDialog.ShowDialog();
        }

        public void PrintDirect()
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += Pd_PrintPage;
            PrintDialog printDialog = new PrintDialog
            {
                Document = pd
            };
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                pd.Print();
            }
        }

        public void PrintToPrinter(PrinterSettings settings)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrinterSettings = settings;
            pd.PrintPage += Pd_PrintPage;
            pd.Print();
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Khung viền kép ngoài (Khổ A4/A5 chiều rộng 710px)
            int rectWidth = 710;
            int rectHeight = 490;

            // Căn giữa lề 2 bên trái/phải theo chiều rộng trang giấy
            float pageWidth = e.PageBounds.Width;
            float leftMargin = Math.Max(20f, (pageWidth - rectWidth) / 2f);
            float topMargin = 25f;  // Sát lề trên

            // Thiết lập Culture vi-VN với NumberGroupSeparator là dấu phẩy ","
            CultureInfo viCulture = new CultureInfo("vi-VN");
            viCulture.NumberFormat.NumberGroupSeparator = ",";

            Rectangle mainRect = new Rectangle((int)leftMargin, (int)topMargin, rectWidth, rectHeight);
            Rectangle innerRect = new Rectangle((int)leftMargin + 3, (int)topMargin + 3, rectWidth - 6, rectHeight - 6);
            g.DrawRectangle(Pens.Black, mainRect);
            g.DrawRectangle(Pens.Black, innerRect);

            float startX = leftMargin + 16;
            float currentY = topMargin + 15;

            // Font định dạng - Tăng font-weight rõ nét toàn bộ phiếu
            Font fontHeaderBold = new Font("Times New Roman", 14.5F, FontStyle.Bold);
            Font fontTitle = new Font("Times New Roman", 22F, FontStyle.Bold | FontStyle.Underline);
            Font fontLabelBold = new Font("Times New Roman", 12.5F, FontStyle.Bold); // Các nhãn đều in đậm rõ nét
            Font fontSubBold = new Font("Times New Roman", 12F, FontStyle.Bold); // Địa chỉ, SĐT đậm
            Font fontBold = new Font("Times New Roman", 12.5F, FontStyle.Bold);

            // Font to hơn cho Loại hàng, Khách hàng, Biển số xe và Khối lượng
            Font fontValueBold = new Font("Times New Roman", 14.5F, FontStyle.Bold); // Loại hàng & Khách hàng to rõ đậm
            Font fontSoXeBold = new Font("Times New Roman", 19F, FontStyle.Bold);
            Font fontKLBold = new Font("Times New Roman", 17F, FontStyle.Bold); // Khối lượng to nổi bật
            Font fontFooterSmallItalic = new Font("Times New Roman", 11F, FontStyle.Bold | FontStyle.Italic);

            // Header Trạm Cân & STT (Căn lề phải)
            g.DrawString(_phieuCan.TramCanName, fontHeaderBold, Brushes.Black, startX, currentY);

            string sttText = $"STT: Xe {_phieuCan.STT}";
            SizeF sttSize = g.MeasureString(sttText, fontHeaderBold);
            g.DrawString(sttText, fontHeaderBold, Brushes.Black, startX + (rectWidth - 32) - sttSize.Width, currentY);
            currentY += 24;

            g.DrawString(_phieuCan.TramCanAddress, fontSubBold, Brushes.Black, startX, currentY);
            currentY += 22;

            g.DrawString($"SĐT: {_phieuCan.TramCanPhone}", fontSubBold, Brushes.Black, startX, currentY);
            currentY += 24;

            float blockCenterX = startX + ((rectWidth - 32) / 2f);

            // Cột tâm số xe & khối lượng
            float col1X = startX + 12;
            float col2CenterX = startX + 230;
            float col3X = startX + 300;
            float col4X = startX + 375;

            // Cụm Tiêu đề, Loại hàng, Khách hàng căn giữa so với cột Biển số xe (col2CenterX)
            string titleText = "PHIẾU CÂN XE";
            SizeF titleSize = g.MeasureString(titleText, fontTitle);
            // Căn tiêu đề có tâm trùng với col2CenterX (hoặc lệch tâm xe hài hòa với nhãn)
            float titleBlockX = col2CenterX - (titleSize.Width / 2) + 20f;

            // 1. Tiêu đề PHIẾU CÂN XE
            g.DrawString(titleText, fontTitle, Brushes.Black, titleBlockX, currentY);
            currentY += 34;

            // 2. Loại hàng (Căn lề cùng cột với tiêu đề)
            g.DrawString("Loại hàng:", fontLabelBold, Brushes.Black, titleBlockX, currentY);
            SizeF szLblLoaiHang = g.MeasureString("Loại hàng:", fontLabelBold);
            g.DrawString(_phieuCan.LoaiHang, fontValueBold, Brushes.Black, titleBlockX + szLblLoaiHang.Width - 2, currentY - 1);
            currentY += 25;

            // 3. Khách hàng (Căn lề cùng cột với tiêu đề)
            g.DrawString("Khách hàng:", fontLabelBold, Brushes.Black, titleBlockX, currentY);
            SizeF szLblKhachHang = g.MeasureString("Khách hàng:", fontLabelBold);
            g.DrawString(_phieuCan.KhachHang, fontValueBold, Brushes.Black, titleBlockX + szLblKhachHang.Width - 2, currentY - 1);
            currentY += 30;

            // 4. Số xe
            g.DrawString("Số xe:", fontLabelBold, Brushes.Black, col1X, currentY);
            SizeF szSoXe = g.MeasureString(_phieuCan.SoXe, fontSoXeBold);
            g.DrawString(_phieuCan.SoXe, fontSoXeBold, Brushes.Black, col2CenterX - (szSoXe.Width / 2), currentY - 3);
            currentY += 28;

            // Xử lý giá trị hiển thị khối lượng với dấu chấm ngăn cách hàng nghìn (ví dụ: 5.533)
            var deCulture = new CultureInfo("de-DE");
            string strTong = _phieuCan.KhoiLuongTong > 0 ? _phieuCan.KhoiLuongTong.ToString("#,##0", deCulture) : "";
            string strXe = _phieuCan.KhoiLuongXe > 0 ? _phieuCan.KhoiLuongXe.ToString("#,##0", deCulture) : "";
            string strHang = "";
            if (_phieuCan.KhoiLuongHang > 0 && _phieuCan.KhoiLuongXe <= _phieuCan.KhoiLuongTong)
            {
                strHang = _phieuCan.KhoiLuongHang.ToString("#,##0", deCulture);
            }

            // Khối lượng tổng & Ngày in phiếu
            g.DrawString("Khối lượng tổng:", fontLabelBold, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strTong))
            {
                SizeF szTong = g.MeasureString(strTong, fontKLBold);
                g.DrawString(strTong, fontKLBold, Brushes.Black, col2CenterX - (szTong.Width / 2), currentY - 2);
            }
            g.DrawString("Kg", fontLabelBold, Brushes.Black, col3X, currentY);
            g.DrawString($"Ngày in phiếu:  {_phieuCan.NgayInPhieu:dd/MM/yyyy HH:mm}", fontLabelBold, Brushes.Black, col4X, currentY);
            currentY += 26;

            // Khối lượng xe
            g.DrawString("Khối lượng xe:", fontLabelBold, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strXe))
            {
                SizeF szXe = g.MeasureString(strXe, fontKLBold);
                g.DrawString(strXe, fontKLBold, Brushes.Black, col2CenterX - (szXe.Width / 2), currentY - 2);
            }
            g.DrawString("Kg", fontLabelBold, Brushes.Black, col3X, currentY);
            currentY += 26;

            // Khối lượng hàng
            g.DrawString("Khối lượng hàng:", fontLabelBold, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strHang))
            {
                SizeF szHang = g.MeasureString(strHang, fontKLBold);
                g.DrawString(strHang, fontKLBold, Brushes.Black, col2CenterX - (szHang.Width / 2), currentY - 2);
            }
            g.DrawString("Kg", fontLabelBold, Brushes.Black, col3X, currentY);
            currentY += 26;

            // Số chứng từ
            g.DrawString("Số chứng từ:", fontLabelBold, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.SoChungTu))
            {
                SizeF szSoChungTu = g.MeasureString(_phieuCan.SoChungTu, fontLabelBold);
                g.DrawString(_phieuCan.SoChungTu, fontLabelBold, Brushes.Black, col2CenterX - (szSoChungTu.Width / 2), currentY);
            }
            currentY += 25;

            // Ghi chú (Căn trái, tự động xuống dòng không bị tràn khung hay đè chữ)
            g.DrawString("Ghi chú:", fontLabelBold, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.GhiChu))
            {
                RectangleF rectGhiChu = new RectangleF(startX + 100, currentY, 575, 55);
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word
                };
                SizeF measuredSize = g.MeasureString(_phieuCan.GhiChu, fontLabelBold, (int)rectGhiChu.Width, sf);
                g.DrawString(_phieuCan.GhiChu, fontLabelBold, Brushes.Black, rectGhiChu, sf);
                currentY += (float)Math.Max(25, Math.Ceiling(measuredSize.Height));
            }
            else
            {
                currentY += 25;
            }

            // Kiểu cân
            g.DrawString("Kiểu cân:", fontLabelBold, Brushes.Black, col1X, currentY);
            SizeF szKieuCan = g.MeasureString(_phieuCan.KieuCan, fontLabelBold);
            g.DrawString(_phieuCan.KieuCan, fontLabelBold, Brushes.Black, col2CenterX - (szKieuCan.Width / 2), currentY);
            currentY += 32;

            // Chữ ký & Value
            float chuHangX = startX + 105;
            float taiXeX = startX + 330;
            float nguoiCanX = startX + 550;

            g.DrawString("Chủ hàng", fontBold, Brushes.Black, chuHangX - (g.MeasureString("Chủ hàng", fontBold).Width / 2), currentY);
            g.DrawString("Tài xế", fontBold, Brushes.Black, taiXeX - (g.MeasureString("Tài xế", fontBold).Width / 2), currentY);
            g.DrawString("Người cân", fontBold, Brushes.Black, nguoiCanX - (g.MeasureString("Người cân", fontBold).Width / 2), currentY);

            // Dòng text cảm ơn ở đáy phiếu cân
            string footerText = "* Cảm ơn quý khách đã sử dụng dịch vụ của chúng tôi *";
            SizeF footerSize = g.MeasureString(footerText, fontFooterSmallItalic);
            float footerX = blockCenterX - (footerSize.Width / 2);
            float bottomY = topMargin + rectHeight - 22;
            g.DrawString(footerText, fontFooterSmallItalic, Brushes.Black, footerX, bottomY);
        }
    }
}

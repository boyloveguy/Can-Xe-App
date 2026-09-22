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
            if (e.Graphics != null)
            {
                DrawTicket(e.Graphics, e.PageBounds.Width);
            }
        }

        public void DrawTicket(Graphics g, float pageWidth)
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Khung viền kép ngoài (Khổ A4/A5 chiều rộng 710px)
            int rectWidth = 710;
            int rectHeight = 490;

            // Căn giữa lề 2 bên trái/phải theo chiều rộng trang giấy
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

            // 1. Các mục ĐƯỢC IN ĐẬM (Bold) theo yêu cầu:
            // - Tên trạm cân
            // - Title "Phiếu Cân Xe"
            // - Tên loại hàng
            // - Số của các khối lượng
            // - Tên kiểu cân
            // 1. Các mục ĐƯỢC IN ĐẬM (Bold) theo yêu cầu:
            // - Tên trạm cân
            // - Title "Phiếu Cân Xe"
            // - Tên loại hàng
            // - Số của các khối lượng
            // - Tên kiểu cân
            Font fontTramCanBold = new Font("Times New Roman", 15F, FontStyle.Bold);
            Font fontTitle = new Font("Times New Roman", 22F, FontStyle.Bold | FontStyle.Underline);
            Font fontLoaiHangBold = new Font("Times New Roman", 14.5F, FontStyle.Bold);
            Font fontKLBold = new Font("Times New Roman", 16F, FontStyle.Bold);
            Font fontKieuCanBold = new Font("Times New Roman", 14F, FontStyle.Bold);

            // 2. Tất cả các phần còn lại KHÔNG IN ĐẬM (Regular):
            Font fontRegularHeader = new Font("Times New Roman", 12F, FontStyle.Regular); // Địa chỉ, SĐT, STT
            Font fontRegularLabel = new Font("Times New Roman", 13.5F, FontStyle.Regular); // Nhãn tên field: Loại hàng:, Khách hàng:, Số xe:, KL tổng:, KL xe:, KL hàng:, Kg, Ngày in phiếu:, Số chứng từ:, Ghi chú:, Kiểu cân:
            Font fontRegularValue = new Font("Times New Roman", 13.5F, FontStyle.Regular); // Giá trị Khách hàng, Số chứng từ, Ghi chú
            Font fontRegularSoXe = new Font("Times New Roman", 16F, FontStyle.Regular); // Biển số xe không in đậm
            Font fontRegularSign = new Font("Times New Roman", 13F, FontStyle.Regular); // Chủ hàng, Tài xế, Người cân
            Font fontFooterItalic = new Font("Times New Roman", 11F, FontStyle.Italic); // Dòng cảm ơn

            // Header Trạm Cân (IN ĐẬM) & STT (KHÔNG IN ĐẬM)
            float headerBaselineY = currentY + GetBaselineOffset(fontTramCanBold, g);
            DrawTextOnBaseline(g, _phieuCan.TramCanName, fontTramCanBold, Brushes.Black, startX, headerBaselineY);

            string sttText = $"STT: Xe {_phieuCan.STT}";
            SizeF sttSize = g.MeasureString(sttText, fontRegularHeader);
            DrawTextOnBaseline(g, sttText, fontRegularHeader, Brushes.Black, startX + (rectWidth - 32) - sttSize.Width, headerBaselineY);
            currentY += 24;

            // Địa chỉ & SĐT (KHÔNG IN ĐẬM)
            float addrBaselineY = currentY + GetBaselineOffset(fontRegularHeader, g);
            DrawTextOnBaseline(g, _phieuCan.TramCanAddress, fontRegularHeader, Brushes.Black, startX, addrBaselineY);
            currentY += 22;

            float phoneBaselineY = currentY + GetBaselineOffset(fontRegularHeader, g);
            DrawTextOnBaseline(g, $"SĐT: {_phieuCan.TramCanPhone}", fontRegularHeader, Brushes.Black, startX, phoneBaselineY);
            currentY += 24;

            float blockCenterX = startX + ((rectWidth - 32) / 2f);

            // Cột tâm số xe & khối lượng
            float col1X = startX + 12;
            float col2CenterX = startX + 230;
            float col3X = startX + 300;
            float col4X = startX + 365;

            // Cụm Tiêu đề, Loại hàng, Khách hàng căn giữa so với cột Biển số xe (col2CenterX)
            string titleText = "PHIẾU CÂN XE";
            SizeF titleSize = g.MeasureString(titleText, fontTitle);
            float titleBlockX = col2CenterX - (titleSize.Width / 2) + 20f;

            // 1. Tiêu đề PHIẾU CÂN XE (IN ĐẬM)
            float titleBaselineY = currentY + GetBaselineOffset(fontTitle, g);
            DrawTextOnBaseline(g, titleText, fontTitle, Brushes.Black, titleBlockX, titleBaselineY);
            currentY += 34;

            // 2. Loại hàng: nhãn thường, TÊN LOẠI HÀNG IN ĐẬM (ngang hàng trên cùng baseline)
            float loaiHangBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Loại hàng:", fontRegularLabel, Brushes.Black, titleBlockX, loaiHangBaselineY);
            SizeF szLblLoaiHang = g.MeasureString("Loại hàng:", fontRegularLabel);
            DrawTextOnBaseline(g, _phieuCan.LoaiHang, fontLoaiHangBold, Brushes.Black, titleBlockX + szLblLoaiHang.Width - 2, loaiHangBaselineY);
            currentY += 25;

            // 3. Khách hàng: nhãn thường, tên khách hàng thường (KHÔNG IN ĐẬM, ngang hàng)
            float khachHangBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Khách hàng:", fontRegularLabel, Brushes.Black, titleBlockX, khachHangBaselineY);
            if (!string.IsNullOrEmpty(_phieuCan.KhachHang))
            {
                SizeF szLblKhachHang = g.MeasureString("Khách hàng:", fontRegularLabel);
                DrawTextOnBaseline(g, _phieuCan.KhachHang, fontRegularValue, Brushes.Black, titleBlockX + szLblKhachHang.Width - 2, khachHangBaselineY);
            }
            currentY += 30;

            // 4. Số xe: nhãn thường, biển số xe thường (KHÔNG IN ĐẬM, ngang hàng)
            float soXeBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Số xe:", fontRegularLabel, Brushes.Black, col1X, soXeBaselineY);
            if (!string.IsNullOrEmpty(_phieuCan.SoXe))
            {
                SizeF szSoXe = g.MeasureString(_phieuCan.SoXe, fontRegularSoXe);
                DrawTextOnBaseline(g, _phieuCan.SoXe, fontRegularSoXe, Brushes.Black, col2CenterX - (szSoXe.Width / 2), soXeBaselineY);
            }
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

            // Khối lượng tổng (nhãn thường, SỐ IN ĐẬM, Kg thường, Ngày in thường - TẤT CẢ NẰM NGANG HÀNG TRÊN CÙNG BASELINE)
            float klTongBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Khối lượng tổng:", fontRegularLabel, Brushes.Black, col1X, klTongBaselineY);
            if (!string.IsNullOrEmpty(strTong))
            {
                SizeF szTong = g.MeasureString(strTong, fontKLBold);
                DrawTextOnBaseline(g, strTong, fontKLBold, Brushes.Black, col2CenterX - (szTong.Width / 2), klTongBaselineY);
            }
            DrawTextOnBaseline(g, "Kg", fontRegularLabel, Brushes.Black, col3X, klTongBaselineY);
            DrawTextOnBaseline(g, $"Ngày in phiếu:  {_phieuCan.NgayInPhieu:dd/MM/yyyy HH:mm}", fontRegularLabel, Brushes.Black, col4X, klTongBaselineY);
            currentY += 26;

            // Khối lượng xe (nhãn thường, SỐ IN ĐẬM, Kg thường - NGANG HÀNG)
            float klXeBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Khối lượng xe:", fontRegularLabel, Brushes.Black, col1X, klXeBaselineY);
            if (!string.IsNullOrEmpty(strXe))
            {
                SizeF szXe = g.MeasureString(strXe, fontKLBold);
                DrawTextOnBaseline(g, strXe, fontKLBold, Brushes.Black, col2CenterX - (szXe.Width / 2), klXeBaselineY);
            }
            DrawTextOnBaseline(g, "Kg", fontRegularLabel, Brushes.Black, col3X, klXeBaselineY);
            currentY += 26;

            // Khối lượng hàng (nhãn thường, SỐ IN ĐẬM, Kg thường - NGANG HÀNG)
            float klHangBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Khối lượng hàng:", fontRegularLabel, Brushes.Black, col1X, klHangBaselineY);
            if (!string.IsNullOrEmpty(strHang))
            {
                SizeF szHang = g.MeasureString(strHang, fontKLBold);
                DrawTextOnBaseline(g, strHang, fontKLBold, Brushes.Black, col2CenterX - (szHang.Width / 2), klHangBaselineY);
            }
            DrawTextOnBaseline(g, "Kg", fontRegularLabel, Brushes.Black, col3X, klHangBaselineY);
            currentY += 26;

            // Số chứng từ (nhãn thường, giá trị thường - NGANG HÀNG)
            float soChungTuBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Số chứng từ:", fontRegularLabel, Brushes.Black, col1X, soChungTuBaselineY);
            if (!string.IsNullOrEmpty(_phieuCan.SoChungTu))
            {
                SizeF szSoChungTu = g.MeasureString(_phieuCan.SoChungTu, fontRegularValue);
                DrawTextOnBaseline(g, _phieuCan.SoChungTu, fontRegularValue, Brushes.Black, col2CenterX - (szSoChungTu.Width / 2), soChungTuBaselineY);
            }
            currentY += 25;

            // Ghi chú (nhãn thường, giá trị thường)
            float ghiChuBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Ghi chú:", fontRegularLabel, Brushes.Black, col1X, ghiChuBaselineY);
            if (!string.IsNullOrEmpty(_phieuCan.GhiChu))
            {
                RectangleF rectGhiChu = new RectangleF(startX + 100, currentY, 575, 55);
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word
                };
                SizeF measuredSize = g.MeasureString(_phieuCan.GhiChu, fontRegularValue, (int)rectGhiChu.Width, sf);
                g.DrawString(_phieuCan.GhiChu, fontRegularValue, Brushes.Black, rectGhiChu, sf);
                currentY += (float)Math.Max(25, Math.Ceiling(measuredSize.Height));
            }
            else
            {
                currentY += 25;
            }

            // Kiểu cân: nhãn thường, TÊN KIỂU CÂN IN ĐẬM - NGANG HÀNG
            float kieuCanBaselineY = currentY + GetBaselineOffset(fontRegularLabel, g);
            DrawTextOnBaseline(g, "Kiểu cân:", fontRegularLabel, Brushes.Black, col1X, kieuCanBaselineY);
            if (!string.IsNullOrEmpty(_phieuCan.KieuCan))
            {
                SizeF szKieuCan = g.MeasureString(_phieuCan.KieuCan, fontKieuCanBold);
                DrawTextOnBaseline(g, _phieuCan.KieuCan, fontKieuCanBold, Brushes.Black, col2CenterX - (szKieuCan.Width / 2), kieuCanBaselineY);
            }
            currentY += 32;

            // Chữ ký (KHÔNG IN ĐẬM)
            float chuHangX = startX + 105;
            float taiXeX = startX + 330;
            float nguoiCanX = startX + 550;

            float signBaselineY = currentY + GetBaselineOffset(fontRegularSign, g);
            DrawTextOnBaseline(g, "Chủ hàng", fontRegularSign, Brushes.Black, chuHangX - (g.MeasureString("Chủ hàng", fontRegularSign).Width / 2), signBaselineY);
            DrawTextOnBaseline(g, "Tài xế", fontRegularSign, Brushes.Black, taiXeX - (g.MeasureString("Tài xế", fontRegularSign).Width / 2), signBaselineY);
            DrawTextOnBaseline(g, "Người cân", fontRegularSign, Brushes.Black, nguoiCanX - (g.MeasureString("Người cân", fontRegularSign).Width / 2), signBaselineY);

            // Dòng text cảm ơn ở đáy phiếu cân (Italic, KHÔNG IN ĐẬM)
            string footerText = "* Cảm ơn quý khách đã sử dụng dịch vụ của chúng tôi *";
            SizeF footerSize = g.MeasureString(footerText, fontFooterItalic);
            float footerX = blockCenterX - (footerSize.Width / 2);
            float bottomY = topMargin + rectHeight - 22;
            g.DrawString(footerText, fontFooterItalic, Brushes.Black, footerX, bottomY);
        }

        private static float GetBaselineOffset(Font font, Graphics g)
        {
            float emPixels = font.SizeInPoints * g.DpiY / 72.0f;
            return emPixels * font.FontFamily.GetCellAscent(font.Style) / font.FontFamily.GetEmHeight(font.Style);
        }

        private static void DrawTextOnBaseline(Graphics g, string text, Font font, Brush brush, float x, float baselineY)
        {
            float y = baselineY - GetBaselineOffset(font, g);
            g.DrawString(text, font, brush, x, y);
        }
    }
}

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
            // - Biển số xe (IN ĐẬM & tăng font size lên 1 tí)
            // - Số của các khối lượng (IN ĐẬM & tăng font size lên 1 tí)
            // - Tên kiểu cân
            Font fontTramCanBold = new Font("Times New Roman", 15F, FontStyle.Bold);
            Font fontTitle = new Font("Times New Roman", 22F, FontStyle.Bold | FontStyle.Underline);
            Font fontLoaiHangBold = new Font("Times New Roman", 13.5F, FontStyle.Bold); // Cùng size 13.5F với label Loại hàng:
            Font fontSoXeBold = new Font("Times New Roman", 18F, FontStyle.Bold); // Biển số xe IN ĐẬM & tăng cỡ
            Font fontKLBold = new Font("Times New Roman", 15.5F, FontStyle.Bold); // Khối lượng 15.5F Bold (tăng 1 tý)
            Font fontKieuCanBold = new Font("Times New Roman", 13.5F, FontStyle.Bold); // Kiểu cân 13.5F Bold

            // 2. Tất cả các phần còn lại KHÔNG IN ĐẬM (Regular):
            Font fontRegularHeader = new Font("Times New Roman", 12F, FontStyle.Regular); // Địa chỉ, SĐT, STT
            Font fontRegularLabel = new Font("Times New Roman", 13.5F, FontStyle.Regular); // Tên field 13.5F (Loại hàng:, Khách hàng:, Số xe:, KL tổng:, KL xe:, KL hàng:, Kg, Ngày in phiếu:, Số chứng từ:, Ghi chú:, Kiểu cân:)
            Font fontRegularValue = new Font("Times New Roman", 13.5F, FontStyle.Regular); // Giá trị Khách hàng, Số chứng từ, Ghi chú
            Font fontRegularSign = new Font("Times New Roman", 13F, FontStyle.Regular); // Chủ hàng, Tài xế, Người cân
            Font fontFooterItalic = new Font("Times New Roman", 11F, FontStyle.Italic); // Dòng cảm ơn

            // Tính toán bù lệch baseline giữa font nhãn (13.5F) và các font hiển thị giá trị
            // để chân chữ của giá trị luôn nằm ngang hàng hoàn toàn với chân chữ của nhãn tên field
            float pointScale = g.PageUnit == GraphicsUnit.Display ? (100.0f / 72.0f) : (g.DpiY / 72.0f);
            float GetAscent(Font font)
            {
                return font.SizeInPoints * font.FontFamily.GetCellAscent(font.Style)
                       / font.FontFamily.GetEmHeight(font.Style) * pointScale;
            }

            float ascLabel = GetAscent(fontRegularLabel);
            float loaiHangYOffset = GetAscent(fontLoaiHangBold) - ascLabel;
            float valueYOffset = GetAscent(fontRegularValue) - ascLabel;
            float soXeYOffset = GetAscent(fontSoXeBold) - ascLabel;
            float klYOffset = GetAscent(fontKLBold) - ascLabel;
            float kieuCanYOffset = GetAscent(fontKieuCanBold) - ascLabel;

            // Header Trạm Cân (IN ĐẬM) & STT (KHÔNG IN ĐẬM)
            g.DrawString(_phieuCan.TramCanName, fontTramCanBold, Brushes.Black, startX, currentY);

            string sttText = $"STT: Xe {_phieuCan.STT}";
            SizeF sttSize = g.MeasureString(sttText, fontRegularHeader);
            g.DrawString(sttText, fontRegularHeader, Brushes.Black, startX + (rectWidth - 32) - sttSize.Width, currentY);
            currentY += 24;

            // Địa chỉ & SĐT (KHÔNG IN ĐẬM)
            g.DrawString(_phieuCan.TramCanAddress, fontRegularHeader, Brushes.Black, startX, currentY);
            currentY += 22;

            g.DrawString($"SĐT: {_phieuCan.TramCanPhone}", fontRegularHeader, Brushes.Black, startX, currentY);
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
            g.DrawString(titleText, fontTitle, Brushes.Black, titleBlockX, currentY);
            currentY += 34;

            // 2. Loại hàng: nhãn thường, TÊN LOẠI HÀNG IN ĐẬM (cùng dòng ngang hàng chuẩn baseline)
            g.DrawString("Loại hàng:", fontRegularLabel, Brushes.Black, titleBlockX, currentY);
            SizeF szLblLoaiHang = g.MeasureString("Loại hàng:", fontRegularLabel);
            g.DrawString(_phieuCan.LoaiHang, fontLoaiHangBold, Brushes.Black, titleBlockX + szLblLoaiHang.Width + 2, currentY - loaiHangYOffset);
            currentY += 25;

            // 3. Khách hàng: nhãn thường, tên khách hàng thường (KHÔNG IN ĐẬM, cùng dòng ngang hàng chuẩn baseline)
            g.DrawString("Khách hàng:", fontRegularLabel, Brushes.Black, titleBlockX, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.KhachHang))
            {
                SizeF szLblKhachHang = g.MeasureString("Khách hàng:", fontRegularLabel);
                g.DrawString(_phieuCan.KhachHang, fontRegularValue, Brushes.Black, titleBlockX + szLblKhachHang.Width + 2, currentY - valueYOffset);
            }
            currentY += 30;

            // 4. Số xe: nhãn thường, BIỂN SỐ XE IN ĐẬM & TĂNG CỠ (căn ngang hàng chuẩn baseline với nhãn)
            g.DrawString("Số xe:", fontRegularLabel, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.SoXe))
            {
                SizeF szSoXe = g.MeasureString(_phieuCan.SoXe, fontSoXeBold);
                g.DrawString(_phieuCan.SoXe, fontSoXeBold, Brushes.Black, col2CenterX - (szSoXe.Width / 2), currentY - soXeYOffset);
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

            // Khối lượng tổng (nhãn thường, SỐ IN ĐẬM, Kg thường, Ngày in thường - CÙNG DÒNG NGANG HÀNG CHUẨN BASELINE)
            g.DrawString("Khối lượng tổng:", fontRegularLabel, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strTong))
            {
                SizeF szTong = g.MeasureString(strTong, fontKLBold);
                g.DrawString(strTong, fontKLBold, Brushes.Black, col2CenterX - (szTong.Width / 2), currentY - klYOffset);
            }
            g.DrawString("Kg", fontRegularLabel, Brushes.Black, col3X, currentY);
            g.DrawString($"Ngày in phiếu:  {_phieuCan.NgayInPhieu:dd/MM/yyyy HH:mm}", fontRegularLabel, Brushes.Black, col4X, currentY);
            currentY += 26;

            // Khối lượng xe (nhãn thường, SỐ IN ĐẬM, Kg thường - CÙNG DÒNG NGANG HÀNG CHUẨN BASELINE)
            g.DrawString("Khối lượng xe:", fontRegularLabel, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strXe))
            {
                SizeF szXe = g.MeasureString(strXe, fontKLBold);
                g.DrawString(strXe, fontKLBold, Brushes.Black, col2CenterX - (szXe.Width / 2), currentY - klYOffset);
            }
            g.DrawString("Kg", fontRegularLabel, Brushes.Black, col3X, currentY);
            currentY += 26;

            // Khối lượng hàng (nhãn thường, SỐ IN ĐẬM, Kg thường - CÙNG DÒNG NGANG HÀNG CHUẨN BASELINE)
            g.DrawString("Khối lượng hàng:", fontRegularLabel, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strHang))
            {
                SizeF szHang = g.MeasureString(strHang, fontKLBold);
                g.DrawString(strHang, fontKLBold, Brushes.Black, col2CenterX - (szHang.Width / 2), currentY - klYOffset);
            }
            g.DrawString("Kg", fontRegularLabel, Brushes.Black, col3X, currentY);
            currentY += 26;

            // Số chứng từ (nhãn thường, giá trị thường - CÙNG DÒNG NGANG HÀNG CHUẨN BASELINE)
            g.DrawString("Số chứng từ:", fontRegularLabel, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.SoChungTu))
            {
                SizeF szSoChungTu = g.MeasureString(_phieuCan.SoChungTu, fontRegularValue);
                g.DrawString(_phieuCan.SoChungTu, fontRegularValue, Brushes.Black, col2CenterX - (szSoChungTu.Width / 2), currentY - valueYOffset);
            }
            currentY += 25;

            // Ghi chú (nhãn thường, giá trị thường)
            g.DrawString("Ghi chú:", fontRegularLabel, Brushes.Black, col1X, currentY);
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

            // Kiểu cân: nhãn thường, TÊN KIỂU CÂN IN ĐẬM - CÙNG DÒNG NGANG HÀNG CHUẨN BASELINE)
            g.DrawString("Kiểu cân:", fontRegularLabel, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.KieuCan))
            {
                SizeF szKieuCan = g.MeasureString(_phieuCan.KieuCan, fontKieuCanBold);
                g.DrawString(_phieuCan.KieuCan, fontKieuCanBold, Brushes.Black, col2CenterX - (szKieuCan.Width / 2), currentY - kieuCanYOffset);
            }
            currentY += 32;

            // Chữ ký (KHÔNG IN ĐẬM)
            float chuHangX = startX + 105;
            float taiXeX = startX + 330;
            float nguoiCanX = startX + 550;

            g.DrawString("Chủ hàng", fontRegularSign, Brushes.Black, chuHangX - (g.MeasureString("Chủ hàng", fontRegularSign).Width / 2), currentY);
            g.DrawString("Tài xế", fontRegularSign, Brushes.Black, taiXeX - (g.MeasureString("Tài xế", fontRegularSign).Width / 2), currentY);
            g.DrawString("Người cân", fontRegularSign, Brushes.Black, nguoiCanX - (g.MeasureString("Người cân", fontRegularSign).Width / 2), currentY);

            // Dòng text cảm ơn ở đáy phiếu cân (Italic, KHÔNG IN ĐẬM)
            string footerText = "* Cảm ơn quý khách đã sử dụng dịch vụ của chúng tôi *";
            SizeF footerSize = g.MeasureString(footerText, fontFooterItalic);
            float footerX = blockCenterX - (footerSize.Width / 2);
            float bottomY = topMargin + rectHeight - 22;
            g.DrawString(footerText, fontFooterItalic, Brushes.Black, footerX, bottomY);
        }
    }
}

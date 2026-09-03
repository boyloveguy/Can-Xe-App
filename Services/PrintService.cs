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
                Width = 850,
                Height = 650,
                StartPosition = FormStartPosition.CenterScreen
            };
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
            float leftMargin = e.MarginBounds.Left;
            float topMargin = e.MarginBounds.Top;

            // Thiết lập Culture vi-VN với NumberGroupSeparator là dấu phẩy ","
            CultureInfo viCulture = new CultureInfo("vi-VN");
            viCulture.NumberFormat.NumberGroupSeparator = ",";

            // Khung viền kép ngoài
            int rectWidth = 650;
            int rectHeight = 440;
            Rectangle mainRect = new Rectangle((int)leftMargin, (int)topMargin, rectWidth, rectHeight);
            Rectangle innerRect = new Rectangle((int)leftMargin + 3, (int)topMargin + 3, rectWidth - 6, rectHeight - 6);
            g.DrawRectangle(Pens.Black, mainRect);
            g.DrawRectangle(Pens.Black, innerRect);

            float startX = leftMargin + 15;
            float currentY = topMargin + 15;

            // Font định dạng
            Font fontHeaderBold = new Font("Times New Roman", 13, FontStyle.Bold);
            Font fontTitle = new Font("Times New Roman", 18, FontStyle.Bold | FontStyle.Underline);
            Font fontRegular = new Font("Times New Roman", 11, FontStyle.Regular);
            Font fontBold = new Font("Times New Roman", 11, FontStyle.Bold);

            Font fontSoXeBold = new Font("Times New Roman", 15, FontStyle.Bold);
            Font fontKLBold = new Font("Times New Roman", 12, FontStyle.Bold);
            Font fontFooterSmallItalic = new Font("Times New Roman", 8.5F, FontStyle.Bold | FontStyle.Italic);

            // Header Trạm Cân & STT (Căn lề phải)
            g.DrawString(_phieuCan.TramCanName, fontHeaderBold, Brushes.Black, startX, currentY);

            string sttText = $"STT: {_phieuCan.STT}";
            SizeF sttSize = g.MeasureString(sttText, fontRegular);
            g.DrawString(sttText, fontRegular, Brushes.Black, startX + 620 - sttSize.Width, currentY);
            currentY += 22;

            g.DrawString(_phieuCan.TramCanAddress, fontRegular, Brushes.Black, startX, currentY);
            currentY += 20;

            g.DrawString($"SĐT: {_phieuCan.TramCanPhone}", fontRegular, Brushes.Black, startX, currentY);
            currentY += 25;

            float blockCenterX = startX + 325;

            // 1. Tiêu đề PHIẾU CÂN XE
            float titleX = startX + 130;
            g.DrawString("PHIẾU CÂN XE", fontTitle, Brushes.Black, titleX, currentY);
            currentY += 32;

            // 2. Loại hàng
            g.DrawString("Loại hàng:  ", fontRegular, Brushes.Black, titleX, currentY);
            SizeF szLabelLoaiHang = g.MeasureString("Loại hàng:  ", fontRegular);
            g.DrawString(_phieuCan.LoaiHang, fontBold, Brushes.Black, titleX + szLabelLoaiHang.Width - 5, currentY);
            currentY += 22;

            // 3. Khách hàng
            g.DrawString("Khách hàng:  ", fontRegular, Brushes.Black, titleX, currentY);
            SizeF szLabelKhachHang = g.MeasureString("Khách hàng:  ", fontRegular);
            g.DrawString(_phieuCan.KhachHang, fontBold, Brushes.Black, titleX + szLabelKhachHang.Width - 5, currentY);
            currentY += 28;

            // Các cột thông tin khối lượng
            float col1X = startX + 10;
            float col2CenterX = startX + 220;
            float col3X = startX + 280;
            float col4X = startX + 350;

            // Số xe
            g.DrawString("Số xe:", fontRegular, Brushes.Black, col1X, currentY);
            SizeF szSoXe = g.MeasureString(_phieuCan.SoXe, fontSoXeBold);
            g.DrawString(_phieuCan.SoXe, fontSoXeBold, Brushes.Black, col2CenterX - (szSoXe.Width / 2), currentY - 3);
            currentY += 25;

            // Xử lý giá trị hiển thị khối lượng với dấu chấm ngăn cách hàng nghìn (ví dụ: 5.533)
            // Nếu giá trị = 0 thì hiển thị ô trống ""
            var deCulture = new CultureInfo("de-DE");
            string strTong = _phieuCan.KhoiLuongTong > 0 ? _phieuCan.KhoiLuongTong.ToString("#,##0", deCulture) : "";
            string strXe = _phieuCan.KhoiLuongXe > 0 ? _phieuCan.KhoiLuongXe.ToString("#,##0", deCulture) : "";
            string strHang = "";
            if (_phieuCan.KhoiLuongHang > 0 && _phieuCan.KhoiLuongXe <= _phieuCan.KhoiLuongTong)
            {
                strHang = _phieuCan.KhoiLuongHang.ToString("#,##0", deCulture);
            }

            // Khối lượng tổng & Ngày in phiếu
            g.DrawString("Khối lượng tổng:", fontRegular, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strTong))
            {
                SizeF szTong = g.MeasureString(strTong, fontKLBold);
                g.DrawString(strTong, fontKLBold, Brushes.Black, col2CenterX - (szTong.Width / 2), currentY);
            }
            g.DrawString("Kg", fontRegular, Brushes.Black, col3X, currentY);
            g.DrawString($"Ngày in phiếu:  {_phieuCan.NgayInPhieu:dd/MM/yyyy HH:mm}", fontRegular, Brushes.Black, col4X, currentY);
            currentY += 22;

            // Khối lượng xe
            g.DrawString("Khối lượng xe:", fontRegular, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strXe))
            {
                SizeF szXe = g.MeasureString(strXe, fontKLBold);
                g.DrawString(strXe, fontKLBold, Brushes.Black, col2CenterX - (szXe.Width / 2), currentY);
            }
            g.DrawString("Kg", fontRegular, Brushes.Black, col3X, currentY);
            currentY += 22;

            // Khối lượng hàng
            g.DrawString("Khối lượng hàng:", fontRegular, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(strHang))
            {
                SizeF szHang = g.MeasureString(strHang, fontKLBold);
                g.DrawString(strHang, fontKLBold, Brushes.Black, col2CenterX - (szHang.Width / 2), currentY);
            }
            g.DrawString("Kg", fontRegular, Brushes.Black, col3X, currentY);
            currentY += 22;

            // Số chứng từ
            g.DrawString("Số chứng từ:", fontRegular, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.SoChungTu))
            {
                SizeF szSoChungTu = g.MeasureString(_phieuCan.SoChungTu, fontRegular);
                g.DrawString(_phieuCan.SoChungTu, fontRegular, Brushes.Black, col2CenterX - (szSoChungTu.Width / 2), currentY);
            }
            currentY += 22;

            // Ghi chú (Căn trái, tự động xuống dòng không bị tràn khung hay đè chữ)
            g.DrawString("Ghi chú:", fontRegular, Brushes.Black, col1X, currentY);
            if (!string.IsNullOrEmpty(_phieuCan.GhiChu))
            {
                RectangleF rectGhiChu = new RectangleF(startX + 90, currentY, 520, 60);
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near, // Căn trái theo yêu cầu
                    LineAlignment = StringAlignment.Near,
                    Trimming = StringTrimming.Word
                };
                SizeF measuredSize = g.MeasureString(_phieuCan.GhiChu, fontRegular, (int)rectGhiChu.Width, sf);
                g.DrawString(_phieuCan.GhiChu, fontRegular, Brushes.Black, rectGhiChu, sf);
                currentY += (float)Math.Max(22, Math.Ceiling(measuredSize.Height));
            }
            else
            {
                currentY += 22;
            }

            // Kiểu cân
            g.DrawString("Kiểu cân:", fontRegular, Brushes.Black, col1X, currentY);
            SizeF szKieuCan = g.MeasureString(_phieuCan.KieuCan, fontBold);
            g.DrawString(_phieuCan.KieuCan, fontBold, Brushes.Black, col2CenterX - (szKieuCan.Width / 2), currentY);
            currentY += 30;

            // Chữ ký & Value (Đã bỏ các ô nhập Chủ hàng, Tài xế, Người cân trên form nên chỉ in nhãn để ký)
            float chuHangX = startX + 90;
            float taiXeX = startX + 285;
            float nguoiCanX = startX + 480;

            g.DrawString("Chủ hàng", fontBold, Brushes.Black, chuHangX - (g.MeasureString("Chủ hàng", fontBold).Width / 2), currentY);
            g.DrawString("Tài xế", fontBold, Brushes.Black, taiXeX - (g.MeasureString("Tài xế", fontBold).Width / 2), currentY);
            g.DrawString("Người cân", fontBold, Brushes.Black, nguoiCanX - (g.MeasureString("Người cân", fontBold).Width / 2), currentY);

            // Dòng text cảm ơn ở đáy phiếu cân
            string footerText = "* Cảm ơn quý khách đã sử dụng dịch vụ của chúng tôi *";
            SizeF footerSize = g.MeasureString(footerText, fontFooterSmallItalic);
            float footerX = blockCenterX - (footerSize.Width / 2);
            float bottomY = topMargin + rectHeight - 20;
            g.DrawString(footerText, fontFooterSmallItalic, Brushes.Black, footerX, bottomY);
        }
    }
}

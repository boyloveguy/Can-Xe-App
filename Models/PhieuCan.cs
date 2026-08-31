using System;

namespace CanXeApp.Models
{
    public class PhieuCan
    {
        public int Id { get; set; }
        public int STT { get; set; }
        public string TramCanName { get; set; } = "TRẠM CÂN THỊ TRẤN HAI RIÊNG";
        public string TramCanAddress { get; set; } = "Đ. Hoàng Hoa Thám - TT. Hai Riêng - X. Sông Hinh - T. Đắk Lắk";
        public string TramCanPhone { get; set; } = "0918.172.220";
        public string LoaiHang { get; set; } = string.Empty;
        public string KhachHang { get; set; } = string.Empty;
        public string SoXe { get; set; } = string.Empty;
        public double KhoiLuongTong { get; set; }
        public double KhoiLuongXe { get; set; }
        public double KhoiLuongHang => Math.Max(0, KhoiLuongTong - KhoiLuongXe);
        public DateTime NgayInPhieu { get; set; } = DateTime.Now; // Thay Ngày vào & Ngày ra bằng Ngày in phiếu
        public string SoChungTu { get; set; } = string.Empty;
        public string GhiChu { get; set; } = string.Empty;
        public string KieuCan { get; set; } = "Cân dịch vụ";
        public string ChuHang { get; set; } = string.Empty;
        public string TaiXe { get; set; } = string.Empty;
        public string NguoiCan { get; set; } = string.Empty;
    }
}

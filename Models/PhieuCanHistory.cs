using System;

namespace CanXeApp.Models
{
    public class PhieuCanHistory
    {
        public int Id { get; set; }
        public int PhieuCanId { get; set; }
        public int STT { get; set; }
        public DateTime ThoiGianThayDoi { get; set; } = DateTime.Now;
        public string NguoiThayDoi { get; set; } = string.Empty;
        public string HanhDong { get; set; } = "Chỉnh sửa phiếu";
        public string NoiDungThayDoi { get; set; } = string.Empty;
        public string DuLieuCuJson { get; set; } = string.Empty;
        public string DuLieuMoiJson { get; set; } = string.Empty;
    }
}

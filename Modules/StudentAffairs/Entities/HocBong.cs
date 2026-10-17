using StudentManagement.Common;

namespace StudentManagement.Modules.StudentAffairs.Entities;

/// <summary>Học bổng.</summary>
public class HocBong : BaseEntity
{
    public int SinhVienId { get; set; } // -> Students.SinhVien (khác module: chỉ giữ Id)
    public int HocKyId { get; set; } // -> Academic.HocKy (khác module: chỉ giữ Id)
    public string Ten { get; set; } = string.Empty;
    public decimal SoTien { get; set; }

    /// <summary>Mức học bổng: Xuất sắc / Giỏi / Khá.</summary>
    public string? MucHocBong { get; set; }

    /// <summary>Điểm trung bình học kỳ (thang 4) lúc xét.</summary>
    public decimal? DiemTrungBinh { get; set; }

    /// <summary>Điểm rèn luyện lúc xét.</summary>
    public int? DiemRenLuyen { get; set; }
}

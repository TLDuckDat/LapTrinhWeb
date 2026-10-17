using StudentManagement.Common;

namespace StudentManagement.Modules.StudentAffairs.Entities;

/// <summary>Học phí phải nộp theo học kỳ (cho phép nộp nhiều đợt).</summary>
public class HocPhi : BaseEntity
{
    public int SinhVienId { get; set; } // -> Students.SinhVien (khác module: chỉ giữ Id)
    public int HocKyId { get; set; } // -> Academic.HocKy (khác module: chỉ giữ Id)

    /// <summary>Tổng số tiền phải nộp của học kỳ (đã trừ miễn giảm).</summary>
    public decimal SoTien { get; set; }

    /// <summary>Tổng số tiền sinh viên đã nộp (cộng dồn qua các đợt).</summary>
    public decimal SoTienDaNop { get; set; }

    /// <summary>true khi SoTienDaNop &gt;= SoTien.</summary>
    public bool DaThanhToan { get; set; }
}

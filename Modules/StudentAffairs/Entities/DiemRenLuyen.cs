using StudentManagement.Common;

namespace StudentManagement.Modules.StudentAffairs.Entities;

/// <summary>Điểm rèn luyện theo học kỳ.</summary>
public class DiemRenLuyen : BaseEntity
{
    public int SinhVienId { get; set; } // -> Students.SinhVien (khác module: chỉ giữ Id)
    public int HocKyId { get; set; } // -> Academic.HocKy (khác module: chỉ giữ Id)
    public int Diem { get; set; }
    public string? XepLoai { get; set; }

    /// <summary>true khi Phòng CTSV đã duyệt và khóa sổ, không được sửa nữa (trừ khi Admin mở khóa).</summary>
    public bool DaKhoa { get; set; }
}

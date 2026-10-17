using StudentManagement.Common;

namespace StudentManagement.Modules.StudentAffairs.Entities;

/// <summary>Quyết định khen thưởng hoặc kỷ luật.</summary>
public class KhenThuongKyLuat : BaseEntity
{
    public int SinhVienId { get; set; } // -> Students.SinhVien (khác module: chỉ giữ Id)
    public LoaiKhenThuongKyLuat Loai { get; set; } = LoaiKhenThuongKyLuat.KhenThuong;
    public string? SoQuyetDinh { get; set; }
    public DateOnly NgayQuyetDinh { get; set; }
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>Học kỳ áp dụng quyết định. Để null được vì dữ liệu cũ chưa có cột này.</summary>
    public int? HocKyId { get; set; } // -> Academic.HocKy (khác module: chỉ giữ Id)

    /// <summary>Hình thức kỷ luật (chỉ dùng khi Loai = KyLuat).</summary>
    public HinhThucKyLuat? HinhThuc { get; set; }
}

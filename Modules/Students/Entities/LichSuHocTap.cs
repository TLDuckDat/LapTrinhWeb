using StudentManagement.Common;

namespace StudentManagement.Modules.Students.Entities;

/// <summary>Lịch sử thay đổi trạng thái học tập (bảo lưu, thôi học, chuyển trạng thái).</summary>
public class LichSuHocTap : BaseEntity
{
    public int SinhVienId { get; set; }
    public TrangThaiSinhVien TrangThaiCu { get; set; }
    public TrangThaiSinhVien TrangThaiMoi { get; set; }
    public string? SoQuyetDinh { get; set; }
    public DateOnly? NgayHieuLuc { get; set; }
    public string? LyDo { get; set; }
    public string? GhiChu { get; set; }
}

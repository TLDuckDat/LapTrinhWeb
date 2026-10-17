using StudentManagement.Common;

namespace StudentManagement.Modules.StudentAffairs.Entities;

/// <summary>Lưu vết mỗi lần sửa điểm rèn luyện (ai sửa, sửa khi nào, vì sao).</summary>
public class LichSuSuaDiemRenLuyen : BaseEntity
{
    public int DiemRenLuyenId { get; set; } // -> DiemRenLuyen
    public int DiemCu { get; set; }
    public int DiemMoi { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public int? NguoiSuaId { get; set; } // -> Auth.TaiKhoan (lấy từ token, chỉ giữ Id)
    public string? TenNguoiSua { get; set; }
    public DateTime NgaySua { get; set; }
}

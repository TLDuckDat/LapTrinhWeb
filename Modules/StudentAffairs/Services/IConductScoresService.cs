using StudentManagement.Modules.StudentAffairs.Dtos;

namespace StudentManagement.Modules.StudentAffairs.Services;

/// <summary>SA-01: Quản lý điểm rèn luyện.</summary>
public interface IConductScoresService
{
    /// <param name="laAdmin">Admin được nhập ngoài thời hạn mở cổng.</param>
    Task<DiemRenLuyenResponse> NhapDiemRenLuyenAsync(DiemRenLuyenRequest request, bool laAdmin);

    Task<List<DiemRenLuyenResponse>> GetDiemRenLuyenAsync(int? sinhVienId, int? hocKyId);

    /// <summary>Sửa điểm (bắt buộc lý do), tự động lưu vết người sửa / ngày sửa.</summary>
    Task<DiemRenLuyenResponse> SuaDiemRenLuyenAsync(
        int id, SuaDiemRenLuyenRequest request, int? nguoiSuaId, string? tenNguoiSua, bool laAdmin);

    Task<List<LichSuSuaDiemRenLuyenResponse>> GetLichSuSuaAsync(int diemRenLuyenId);

    /// <summary>Khóa sổ điểm rèn luyện cả học kỳ (sau khi Phòng CTSV duyệt).</summary>
    Task<KhoaSoDiemRenLuyenResponse> KhoaSoAsync(int hocKyId);

    /// <summary>Mở khóa sổ (chỉ Admin).</summary>
    Task<KhoaSoDiemRenLuyenResponse> MoKhoaSoAsync(int hocKyId);
}

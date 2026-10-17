using StudentManagement.Modules.StudentAffairs.Dtos;

namespace StudentManagement.Modules.StudentAffairs.Services;

/// <summary>SA-02, SA-03: Tính học phí, thanh toán và công nợ sinh viên.</summary>
public interface ITuitionsService
{
    // ----- Tính học phí -----
    Task<TinhHocPhiResponse> TinhHocPhiAsync(int hocKyId);
    Task<TinhHocPhiResponse> TinhHocPhiSinhVienAsync(int hocKyId, int sinhVienId);
    Task<List<HocPhiResponse>> GetHocPhiAsync(int? sinhVienId, int? hocKyId);

    // ----- Thanh toán -----
    Task<ThanhToanResponse> ThanhToanAsync(int id, ThanhToanRequest request);
    Task<List<GiaoDichHocPhiResponse>> GetGiaoDichAsync(int hocPhiId);

    // ----- Công nợ -----
    Task<KiemTraNoHocPhiResponse> KiemTraNoHocPhiAsync(int sinhVienId);

    /// <summary>
    /// Dùng cho module Registration: true nếu sinh viên còn nợ học phí đã quá hạn
    /// -> không cho đăng ký học phần kỳ mới.
    /// </summary>
    Task<bool> CoNoQuaHanAsync(int sinhVienId);

    /// <summary>
    /// Dùng cho module Graduation: true nếu sinh viên đã đóng đủ 100% học phí tích lũy
    /// -> mới được đưa vào danh sách xét tốt nghiệp.
    /// </summary>
    Task<bool> DaHoanThanhHocPhiAsync(int sinhVienId);
}

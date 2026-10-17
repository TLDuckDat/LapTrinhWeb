using StudentManagement.Modules.StudentAffairs.Dtos;

namespace StudentManagement.Modules.StudentAffairs.Services;

/// <summary>SA-05: Xét học bổng.</summary>
public interface IScholarshipsService
{
    Task<XetHocBongResponse> XetHocBongAsync(int hocKyId);

    /// <summary>Tra cứu danh sách đã nhận học bổng (lọc theo học kỳ / sinh viên).</summary>
    Task<List<HocBongResponse>> GetHocBongAsync(int? hocKyId, int? sinhVienId);
}

using StudentManagement.Modules.StudentAffairs.Dtos;

namespace StudentManagement.Modules.StudentAffairs.Services;

/// <summary>SA-04: Quản lý miễn giảm học phí.</summary>
public interface ITuitionDiscountsService
{
    Task<MienGiamResponse> CreateMienGiamAsync(MienGiamRequest request);
    Task<List<MienGiamResponse>> GetMienGiamAsync(int? sinhVienId, int? hocKyId);
    Task<MienGiamResponse> SuaMienGiamAsync(int id, SuaMienGiamRequest request);
    Task<MienGiamResponse> XoaMienGiamAsync(int id);
}

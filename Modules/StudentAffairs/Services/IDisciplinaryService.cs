using StudentManagement.Modules.StudentAffairs.Dtos;

namespace StudentManagement.Modules.StudentAffairs.Services;

/// <summary>SA-06: Khen thưởng / kỷ luật.</summary>
public interface IDisciplinaryService
{
    Task<KhenThuongKyLuatResponse> CreateKhenThuongKyLuatAsync(KhenThuongKyLuatRequest request);
}

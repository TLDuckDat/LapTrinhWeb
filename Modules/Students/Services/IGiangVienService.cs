using StudentManagement.Common;
using StudentManagement.Modules.Students.Dtos;

namespace StudentManagement.Modules.Students.Services;

/// <summary>Giao diện nghiệp vụ quản lý Giảng viên (STU-05: Quản lý cố vấn và giảng viên).</summary>
public interface IGiangVienService
{
    Task<PagedResult<GiangVienDto>> GetPagedAsync(GiangVienFilterQuery query);
    Task<GiangVienDto?> GetByIdAsync(int id);
    Task<GiangVienDto> CreateAsync(GiangVienRequest request);
    Task<GiangVienDto?> UpdateAsync(int id, GiangVienRequest request);
    Task<bool> DeleteAsync(int id);
}

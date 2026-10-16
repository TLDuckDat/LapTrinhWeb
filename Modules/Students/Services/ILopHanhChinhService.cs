using StudentManagement.Common;
using StudentManagement.Modules.Students.Dtos;

namespace StudentManagement.Modules.Students.Services;

/// <summary>Giao diện nghiệp vụ quản lý Lớp hành chính (STU-04, STU-05).</summary>
public interface ILopHanhChinhService
{
    // STU-04: Lấy danh sách lớp hành chính phân trang và lọc
    Task<PagedResult<LopHanhChinhDto>> GetPagedAsync(LopHanhChinhFilterQuery query);

    // STU-04: Chi tiết lớp hành chính kèm danh sách sinh viên
    Task<LopHanhChinhDetailDto?> GetByIdAsync(int id);

    // STU-04: Thêm mới lớp hành chính
    Task<LopHanhChinhDto> CreateAsync(LopHanhChinhRequest request);

    // STU-04: Cập nhật thông tin lớp hành chính
    Task<LopHanhChinhDto?> UpdateAsync(int id, LopHanhChinhRequest request);

    // STU-04: Xóa lớp hành chính (kiểm tra ràng buộc Restrict)
    Task<bool> DeleteAsync(int id);

    // STU-05: Phân công giảng viên làm Cố vấn học tập cho lớp
    Task<LopHanhChinhDto?> PhanCoVanAsync(int id, PhanCoVanRequest request);

    // STU-04: Phân sinh viên hàng loạt vào lớp (.ExecuteUpdateAsync)
    Task<int> PhanLopAsync(int id, PhanLopRequest request);
}

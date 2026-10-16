using StudentManagement.Common;
using StudentManagement.Modules.Students.Dtos;

namespace StudentManagement.Modules.Students.Services;

/// <summary>Giao diện nghiệp vụ quản lý sinh viên (Epic STU: STU-01, 02, 03, 04, 05, 07).</summary>
public interface ISinhVienService
{
    // STU-01, STU-03: Danh sách phân trang, tìm kiếm và lọc đa tiêu chí
    Task<PagedResult<SinhVienDto>> GetPagedAsync(SinhVienFilterQuery query);

    // STU-02: Xem chi tiết hồ sơ sinh viên đầy đủ thông tin
    Task<SinhVienDetailDto?> GetByIdAsync(int id);

    // STU-01: Thêm mới hồ sơ sinh viên
    Task<SinhVienDetailDto> CreateAsync(CreateSinhVienRequest request);

    // STU-01: Cập nhật thông tin sinh viên
    Task<SinhVienDetailDto?> UpdateAsync(int id, UpdateSinhVienRequest request);

    // STU-01: Xóa sinh viên (có kiểm tra ràng buộc khóa ngoại Restrict)
    Task<bool> DeleteAsync(int id);

    // STU-04: Chuyển lớp hành chính cho một sinh viên
    Task<SinhVienDetailDto?> ChuyenLopAsync(int id, ChuyenLopHanhChinhRequest request);

    // STU-06: Đổi trạng thái học tập cơ bản
    Task<SinhVienDetailDto?> ChangeStatusAsync(int id, ChangeStatusRequest request);

    // STU-07: Tiếp nhận đơn và xử lý bảo lưu
    Task<SinhVienDetailDto?> BaoLuuAsync(int id, BaoLuuRequest request);

    // STU-07: Ra quyết định và xử lý thôi học
    Task<SinhVienDetailDto?> ThoiHocAsync(int id, ThoiHocRequest request);

    // STU-07: Phục hồi trạng thái quay lại học sau bảo lưu
    Task<SinhVienDetailDto?> QuayLaiHocAsync(int id, QuayLaiHocRequest request);

    // STU-07: Xem lịch sử biến động trạng thái học tập của sinh viên
    Task<List<LichSuHocTapDto>> GetLichSuHocTapAsync(int sinhVienId);
}

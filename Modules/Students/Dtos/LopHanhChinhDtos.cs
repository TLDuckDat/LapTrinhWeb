using System.ComponentModel.DataAnnotations;
using StudentManagement.Common;

namespace StudentManagement.Modules.Students.Dtos;

/// <summary>DTO danh sách lớp hành chính kèm sĩ số.</summary>
public record LopHanhChinhDto(
    int Id,
    string Ma,
    string Ten,
    int NganhId,
    string? TenNganh,
    int KhoaTuyen,
    int? CoVanId,
    string? TenCoVan,
    int SiSo,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>DTO chi tiết lớp hành chính kèm danh sách sinh viên.</summary>
public record LopHanhChinhDetailDto(
    int Id,
    string Ma,
    string Ten,
    int NganhId,
    string? TenNganh,
    string? TenKhoa,
    int KhoaTuyen,
    int? CoVanId,
    string? TenCoVan,
    int SiSo,
    List<SinhVienDto> DanhSachSinhVien,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Request tạo/cập nhật lớp hành chính.</summary>
public record LopHanhChinhRequest(
    [Required(ErrorMessage = "Mã lớp là bắt buộc.")]
    [MaxLength(20, ErrorMessage = "Mã lớp tối đa 20 ký tự.")]
    string Ma,

    [Required(ErrorMessage = "Tên lớp là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Tên lớp tối đa 100 ký tự.")]
    string Ten,

    [Required(ErrorMessage = "Ngành học là bắt buộc.")]
    int NganhId,

    [Range(2000, 2100, ErrorMessage = "Khóa tuyển phải trong khoảng 2000 - 2100.")]
    int KhoaTuyen,

    int? CoVanId
);

/// <summary>Request phân lớp hàng loạt cho sinh viên.</summary>
public record PhanLopRequest(
    [Required(ErrorMessage = "Danh sách ID sinh viên không được rỗng.")]
    List<int> SinhVienIds
);

/// <summary>Request phân công cố vấn học tập.</summary>
public record PhanCoVanRequest(
    int? CoVanId
);

/// <summary>Bộ lọc lớp hành chính.</summary>
public class LopHanhChinhFilterQuery : PagedQuery
{
    public int? NganhId { get; set; }
    public int? KhoaTuyen { get; set; }
    public int? CoVanId { get; set; }
}

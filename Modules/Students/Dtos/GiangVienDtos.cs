using System.ComponentModel.DataAnnotations;
using StudentManagement.Common;

namespace StudentManagement.Modules.Students.Dtos;

/// <summary>DTO trả về cho giảng viên.</summary>
public record GiangVienDto(
    int Id,
    string Ma,
    string HoTen,
    string? HocVi,
    string? Email,
    string? SoDienThoai,
    int BoMonId,
    string? TenBoMon,
    string? TenKhoa,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Request tạo/cập nhật thông tin giảng viên.</summary>
public record GiangVienRequest(
    [Required(ErrorMessage = "Mã giảng viên là bắt buộc.")]
    [MaxLength(20, ErrorMessage = "Mã giảng viên tối đa 20 ký tự.")]
    string Ma,

    [Required(ErrorMessage = "Họ tên giảng viên là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    string HoTen,

    [MaxLength(50, ErrorMessage = "Học vị tối đa 50 ký tự.")]
    string? HocVi,

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    string? Email,

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    string? SoDienThoai,

    [Required(ErrorMessage = "Bộ môn là bắt buộc.")]
    int BoMonId
);

/// <summary>Bộ lọc cho giảng viên.</summary>
public class GiangVienFilterQuery : PagedQuery
{
    public int? BoMonId { get; set; }
    public int? KhoaId { get; set; }
}

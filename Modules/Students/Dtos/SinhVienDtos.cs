using System.ComponentModel.DataAnnotations;
using StudentManagement.Common;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.Students.Dtos;

/// <summary>DTO trả về cho danh sách sinh viên phân trang.</summary>
public record SinhVienDto(
    int Id,
    string MSSV,
    string HoTen,
    DateOnly NgaySinh,
    string? GioiTinh,
    string? CCCD,
    string? Email,
    string? SoDienThoai,
    TrangThaiSinhVien TrangThai,
    int NganhId,
    string? TenNganh,
    int? LopHanhChinhId,
    string? TenLopHanhChinh,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>DTO xem chi tiết hồ sơ sinh viên đầy đủ thông tin (Khoa, Ngành, Chuyên ngành, Lớp, Cố vấn).</summary>
public record SinhVienDetailDto(
    int Id,
    string MSSV,
    string HoTen,
    DateOnly NgaySinh,
    string? GioiTinh,
    string? CCCD,
    string? Email,
    string? SoDienThoai,
    string? DiaChi,
    TrangThaiSinhVien TrangThai,
    int NganhId,
    string? TenNganh,
    string? TenKhoa,
    int? ChuyenNganhId,
    string? TenChuyenNganh,
    int? LopHanhChinhId,
    string? TenLopHanhChinh,
    string? TenCoVan,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>Request tạo mới hồ sơ sinh viên.</summary>
public record CreateSinhVienRequest(
    [Required(ErrorMessage = "Mã số sinh viên là bắt buộc.")]
    [MaxLength(20, ErrorMessage = "MSSV tối đa 20 ký tự.")]
    string MSSV,

    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    string HoTen,

    [Required(ErrorMessage = "Ngày sinh là bắt buộc.")]
    DateOnly NgaySinh,

    [MaxLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự.")]
    string? GioiTinh,

    [MaxLength(20, ErrorMessage = "CCCD tối đa 20 ký tự.")]
    string? CCCD,

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    string? Email,

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    string? SoDienThoai,

    [MaxLength(250, ErrorMessage = "Địa chỉ tối đa 250 ký tự.")]
    string? DiaChi,

    [Required(ErrorMessage = "Ngành học là bắt buộc.")]
    int NganhId,

    int? ChuyenNganhId,
    int? LopHanhChinhId
);

/// <summary>Request cập nhật thông tin hồ sơ sinh viên.</summary>
public record UpdateSinhVienRequest(
    [Required(ErrorMessage = "Họ tên là bắt buộc.")]
    [MaxLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    string HoTen,

    [Required(ErrorMessage = "Ngày sinh là bắt buộc.")]
    DateOnly NgaySinh,

    [MaxLength(10, ErrorMessage = "Giới tính tối đa 10 ký tự.")]
    string? GioiTinh,

    [MaxLength(20, ErrorMessage = "CCCD tối đa 20 ký tự.")]
    string? CCCD,

    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [MaxLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    string? Email,

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
    string? SoDienThoai,

    [MaxLength(250, ErrorMessage = "Địa chỉ tối đa 250 ký tự.")]
    string? DiaChi,

    [Required(ErrorMessage = "Ngành học là bắt buộc.")]
    int NganhId,

    int? ChuyenNganhId,
    int? LopHanhChinhId
);

/// <summary>Bộ lọc đa tiêu chí cho danh sách sinh viên.</summary>
public class SinhVienFilterQuery : PagedQuery
{
    public int? KhoaId { get; set; }
    public int? NganhId { get; set; }
    public int? LopHanhChinhId { get; set; }
    public TrangThaiSinhVien? TrangThai { get; set; }
}

/// <summary>Request đổi trạng thái học tập.</summary>
public record ChangeStatusRequest(
    // Kiểu nullable để [Required] có tác dụng (enum thường thiếu trường sẽ tự thành DangHoc = 0);
    // [EnumDataType] chặn giá trị ngoài enum, ví dụ 99.
    [Required(ErrorMessage = "Trạng thái mới là bắt buộc.")]
    [EnumDataType(typeof(TrangThaiSinhVien), ErrorMessage = "Trạng thái không hợp lệ (0: DangHoc, 1: BaoLuu, 2: ThoiHoc, 3: DaTotNghiep).")]
    TrangThaiSinhVien? TrangThai,
    [MaxLength(250, ErrorMessage = "Lý do tối đa 250 ký tự.")]
    string? LyDo,
    [MaxLength(50, ErrorMessage = "Số quyết định tối đa 50 ký tự.")]
    string? SoQuyetDinh,
    DateOnly? NgayHieuLuc,
    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? GhiChu
);

/// <summary>Request tiếp nhận đơn bảo lưu.</summary>
public record BaoLuuRequest(
    [Required(ErrorMessage = "Lý do bảo lưu là bắt buộc.")]
    [MaxLength(250, ErrorMessage = "Lý do tối đa 250 ký tự.")]
    string LyDo,
    [MaxLength(50, ErrorMessage = "Số quyết định tối đa 50 ký tự.")]
    string? SoQuyetDinh,
    DateOnly? NgayHieuLuc,
    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? GhiChu
);

/// <summary>Request quyết định thôi học.</summary>
public record ThoiHocRequest(
    [Required(ErrorMessage = "Lý do thôi học là bắt buộc.")]
    [MaxLength(250, ErrorMessage = "Lý do tối đa 250 ký tự.")]
    string LyDo,
    [MaxLength(50, ErrorMessage = "Số quyết định tối đa 50 ký tự.")]
    string? SoQuyetDinh,
    DateOnly? NgayHieuLuc,
    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? GhiChu
);

/// <summary>Request tiếp nhận quay lại học sau bảo lưu.</summary>
public record QuayLaiHocRequest(
    [MaxLength(50, ErrorMessage = "Số quyết định tối đa 50 ký tự.")]
    string? SoQuyetDinh,
    DateOnly? NgayHieuLuc,
    [MaxLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
    string? GhiChu
);

/// <summary>Request chuyển lớp hành chính cho sinh viên (đặt tên riêng để không trùng ChuyenLopRequest của module Grades trên Swagger).</summary>
public record ChuyenLopHanhChinhRequest(
    [Required(ErrorMessage = "Lớp hành chính mới là bắt buộc.")]
    int LopHanhChinhId
);

/// <summary>DTO lịch sử thay đổi trạng thái học tập.</summary>
public record LichSuHocTapDto(
    int Id,
    int SinhVienId,
    TrangThaiSinhVien TrangThaiCu,
    TrangThaiSinhVien TrangThaiMoi,
    string? SoQuyetDinh,
    DateOnly? NgayHieuLuc,
    string? LyDo,
    string? GhiChu,
    DateTime CreatedAt
);

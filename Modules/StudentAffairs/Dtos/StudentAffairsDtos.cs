
using StudentManagement.Modules.StudentAffairs.Entities;
using System.ComponentModel.DataAnnotations;

namespace StudentManagement.Modules.StudentAffairs.Dtos;

// #####################################################################
// 1. ĐIỂM RÈN LUYỆN
// #####################################################################

// SA-01: Dữ liệu nhập điểm rèn luyện.
public class DiemRenLuyenRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "SinhVienId phải lớn hơn 0.")]
    public int SinhVienId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "HocKyId phải lớn hơn 0.")]
    public int HocKyId { get; set; }

    [Range(0, 100, ErrorMessage = "Điểm rèn luyện phải từ 0 đến 100.")]
    public int Diem { get; set; }
}

// Dữ liệu sửa điểm rèn luyện (bắt buộc có lý do sửa).
public class SuaDiemRenLuyenRequest
{
    [Range(0, 100, ErrorMessage = "Điểm rèn luyện phải từ 0 đến 100.")]
    public int Diem { get; set; }

    [Required(ErrorMessage = "Phải nhập lý do sửa điểm.")]
    [MaxLength(500, ErrorMessage = "Lý do sửa không được vượt quá 500 ký tự.")]
    public string LyDoSua { get; set; } = string.Empty;
}

public class DiemRenLuyenResponse
{
    public int Id { get; set; }
    public int SinhVienId { get; set; }
    public string MSSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int HocKyId { get; set; }
    public string TenHocKy { get; set; } = string.Empty;
    public int Diem { get; set; }
    public string? XepLoai { get; set; }
    public bool DaKhoa { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LichSuSuaDiemRenLuyenResponse
{
    public int Id { get; set; }
    public int DiemRenLuyenId { get; set; }
    public int DiemCu { get; set; }
    public int DiemMoi { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public int? NguoiSuaId { get; set; }
    public string? TenNguoiSua { get; set; }
    public DateTime NgaySua { get; set; }
}

public class KhoaSoDiemRenLuyenResponse
{
    public int HocKyId { get; set; }
    public bool DaKhoa { get; set; }
    public int SoBanGhi { get; set; }
}

// #####################################################################
// 2. HỌC PHÍ & THANH TOÁN
// #####################################################################

/// <summary>Kết quả tính học phí của 1 sinh viên.</summary>
public class TinhHocPhiItem
{
    public int SinhVienId { get; set; }
    public int TongTinChi { get; set; }
    public decimal DonGiaTinChi { get; set; }
    public decimal TongTienGoc { get; set; }
    public decimal TyLeMienGiam { get; set; }
    public decimal SoTienHocPhi { get; set; }
    public decimal SoTienDaNop { get; set; }
    public decimal SoTienConNo { get; set; }

    /// <summary>Đã nộp nhiều hơn học phí mới (do rút môn / thêm miễn giảm) -> cần hoàn lại cho sinh viên.</summary>
    public decimal SoTienThua { get; set; }

    public bool DaThanhToan { get; set; }
}

/// <summary>Kết quả tính học phí cả học kỳ (hoặc 1 sinh viên).</summary>
public class TinhHocPhiResponse
{
    public int HocKyId { get; set; }
    public string TenHocKy { get; set; } = string.Empty;
    public decimal DonGiaTinChi { get; set; }
    public int SoSinhVien { get; set; }
    public string? ThongBao { get; set; }
    public List<TinhHocPhiItem> DanhSach { get; set; } = new();
}

/// <summary>Một dòng học phí khi xem danh sách.</summary>
public class HocPhiResponse
{
    public int Id { get; set; }
    public int SinhVienId { get; set; }
    public string MSSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int HocKyId { get; set; }
    public string TenHocKy { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public decimal SoTienDaNop { get; set; }
    public decimal SoTienConNo { get; set; }
    public bool DaThanhToan { get; set; }

    /// <summary>Ngày bắt đầu học kỳ + SoNgayHanNopHocPhi (null nếu không đặt hạn).</summary>
    public DateOnly? HanNop { get; set; }

    /// <summary>Đã qua hạn nộp mà vẫn còn nợ.</summary>
    public bool DaQuaHan { get; set; }
}

/// <summary>Dữ liệu nộp học phí (1 đợt).</summary>
public class ThanhToanRequest
{
    /// <summary>Số tiền nộp đợt này (phải &gt; 0 và không vượt quá số còn nợ).</summary>
    public decimal SoTien { get; set; }

    /// <summary>TienMat hoặc ChuyenKhoan.</summary>
    public PhuongThucThanhToan PhuongThuc { get; set; }

    /// <summary>Mã giao dịch / số biên lai. Bắt buộc khi chuyển khoản.</summary>
    public string? MaGiaoDich { get; set; }

    /// <summary>Ngày nộp. Bỏ trống thì lấy thời điểm hiện tại.</summary>
    public DateTime? NgayThanhToan { get; set; }
}

/// <summary>Kết quả nộp học phí.</summary>
public class ThanhToanResponse
{
    public int Id { get; set; }
    public int SinhVienId { get; set; }
    public int HocKyId { get; set; }
    public decimal SoTien { get; set; }
    public decimal SoTienVuaNop { get; set; }
    public decimal SoTienDaNop { get; set; }
    public decimal SoTienConNo { get; set; }
    public bool DaThanhToan { get; set; }
    public int GiaoDichId { get; set; }
    public string ThongBao { get; set; } = string.Empty;
}

public class GiaoDichHocPhiResponse
{
    public int Id { get; set; }
    public int HocPhiId { get; set; }
    public decimal SoTien { get; set; }
    public DateTime NgayThanhToan { get; set; }
    public string PhuongThuc { get; set; } = string.Empty;
    public string? MaGiaoDich { get; set; }
    public string? GhiChu { get; set; }
}

/// <summary>Tình trạng nợ học phí của 1 sinh viên.</summary>
public class KiemTraNoHocPhiResponse
{
    public int SinhVienId { get; set; }
    public decimal TongConNo { get; set; }
    public decimal TongNoQuaHan { get; set; }
    public bool CoNoQuaHan { get; set; }

    /// <summary>false nếu còn nợ quá hạn -> không được đăng ký học phần kỳ mới.</summary>
    public bool DuocDangKyHocPhan { get; set; }

    /// <summary>false nếu chưa đóng đủ 100% học phí -> bị loại khỏi xét tốt nghiệp.</summary>
    public bool DuDieuKienTotNghiepVeHocPhi { get; set; }
}

// #####################################################################
// 3. MIỄN GIẢM
// #####################################################################

// SA-04: Dữ liệu tạo miễn giảm học phí.
public record MienGiamRequest(
    int SinhVienId,
    int HocKyId,
    decimal TyLe,
    string? LyDo
);

// Dữ liệu sửa tỷ lệ / lý do miễn giảm.
public class SuaMienGiamRequest
{
    public decimal TyLe { get; set; }
    public string? LyDo { get; set; }
}

public class MienGiamResponse
{
    public int Id { get; set; }
    public int SinhVienId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public int HocKyId { get; set; }
    public string TenHocKy { get; set; } = string.Empty;
    public decimal TyLe { get; set; }
    public string? LyDo { get; set; }

    /// <summary>Tổng tỷ lệ miễn giảm của sinh viên trong kỳ (sau thao tác).</summary>
    public decimal TongTyLeTrongKy { get; set; }

    /// <summary>Số tiền học phí mới sau khi tự động tính lại (null nếu kỳ chưa tính học phí).</summary>
    public decimal? HocPhiSauCapNhat { get; set; }
}

// #####################################################################
// 4. HỌC BỔNG
// #####################################################################

/// <summary>Một sinh viên được cấp học bổng.</summary>
public class HocBongItem
{
    public int SinhVienId { get; set; }
    public int NganhId { get; set; }
    public int Diem { get; set; }
    public decimal DiemTrungBinh { get; set; }
    public string MucHocBong { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
}

/// <summary>Một sinh viên không được cấp và lý do.</summary>
public class KhongDatHocBongItem
{
    public int SinhVienId { get; set; }
    public string LyDo { get; set; } = string.Empty;
}

/// <summary>Kết quả xét học bổng cả học kỳ.</summary>
public class XetHocBongResponse
{
    public int HocKyId { get; set; }
    public string TenHocKy { get; set; } = string.Empty;
    public decimal SoTienHocBong { get; set; }
    public int SoLuongHocBong { get; set; }
    public decimal TongTienDaCap { get; set; }
    public List<HocBongItem> DanhSach { get; set; } = new();
    public List<KhongDatHocBongItem> DanhSachKhongDat { get; set; } = new();
}

/// <summary>Một dòng khi tra cứu danh sách học bổng.</summary>
public class HocBongResponse
{
    public int Id { get; set; }
    public int SinhVienId { get; set; }
    public string MSSV { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int HocKyId { get; set; }
    public string TenHocKy { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string? MucHocBong { get; set; }
    public decimal? DiemTrungBinh { get; set; }
    public int? DiemRenLuyen { get; set; }
    public decimal SoTien { get; set; }
}

// #####################################################################
// 5. KHEN THƯỞNG / KỶ LUẬT
// #####################################################################

// SA-06: Dữ liệu tạo quyết định khen thưởng / kỷ luật.
public record KhenThuongKyLuatRequest(
    int SinhVienId,
    LoaiKhenThuongKyLuat Loai,
    string? SoQuyetDinh,
    DateOnly NgayQuyetDinh,
    string NoiDung,
    int HocKyId,                    // Học kỳ áp dụng quyết định (bắt buộc)
    HinhThucKyLuat? HinhThuc        // Bắt buộc khi Loai = KyLuat
);

public class KhenThuongKyLuatResponse
{
    public int Id { get; set; }
    public int SinhVienId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Loai { get; set; } = string.Empty;
    public string? HinhThuc { get; set; }
    public string? SoQuyetDinh { get; set; }
    public DateOnly NgayQuyetDinh { get; set; }
    public int? HocKyId { get; set; }
    public string NoiDung { get; set; } = string.Empty;

    /// <summary>Trạng thái sinh viên sau khi xử lý quyết định.</summary>
    public string TrangThaiSinhVien { get; set; } = string.Empty;
}

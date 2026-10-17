namespace StudentManagement.Modules.StudentAffairs;

/// <summary>
/// Thông số nghiệp vụ của module StudentAffairs.
/// Giá trị mặc định nằm ngay bên dưới. Muốn đổi thông số nào thì thêm đúng thông số đó
/// vào mục "StudentAffairs" trong appsettings.json, ví dụ: "StudentAffairs": { "DonGiaTinChi": 700000 }.
/// </summary>
public class StudentAffairsSettings
{
    // ===================== HỌC PHÍ =====================

    /// <summary>Đơn giá 1 tín chỉ (VNĐ).</summary>
    public decimal DonGiaTinChi { get; set; } = 600_000m;

    /// <summary>
    /// Hạn nộp học phí = ngày bắt đầu học kỳ + N ngày. Quá hạn mà còn nợ thì bị coi là "nợ quá hạn".
    /// Đặt 0 để không có hạn.
    /// </summary>
    public int SoNgayHanNopHocPhi { get; set; } = 30;

    // ===================== ĐIỂM RÈN LUYỆN =====================

    /// <summary>
    /// Số ngày mở cổng nhập/sửa điểm rèn luyện, tính từ ngày kết thúc học kỳ.
    /// Đặt 0 để không giới hạn thời gian.
    /// </summary>
    public int SoNgayMoCongNhapDiemRenLuyen { get; set; } = 30;

    // ===================== HỌC BỔNG =====================

    /// <summary>Số tiền học bổng ứng với 100% (mức Xuất sắc).</summary>
    public decimal SoTienHocBong { get; set; } = 1_000_000m;

    /// <summary>Tên học bổng được cấp.</summary>
    public string TenHocBong { get; set; } = "Học bổng khuyến khích";

    /// <summary>Số tín chỉ tối thiểu phải đăng ký và hoàn thành trong kỳ.</summary>
    public int TinChiToiThieuXetHocBong { get; set; } = 14;

    /// <summary>Điểm tổng kết (thang 10) tối thiểu của mọi học phần trong kỳ (điểm D = 4.0).</summary>
    public decimal DiemHocPhanToiThieu { get; set; } = 4.0m;

    // Mức Xuất sắc
    public decimal XuatSacGpaToiThieu { get; set; } = 3.6m;
    public int XuatSacDrlToiThieu { get; set; } = 90;
    public decimal XuatSacPhanTramTien { get; set; } = 100m;

    // Mức Giỏi
    public decimal GioiGpaToiThieu { get; set; } = 3.2m;
    public int GioiDrlToiThieu { get; set; } = 80;
    public decimal GioiPhanTramTien { get; set; } = 80m;

    // Mức Khá
    public decimal KhaGpaToiThieu { get; set; } = 2.8m;
    public int KhaDrlToiThieu { get; set; } = 70;
    public decimal KhaPhanTramTien { get; set; } = 60m;

    /// <summary>Số suất tối đa mỗi ngành, tính theo % số sinh viên đang học của ngành.</summary>
    public decimal PhanTramSuatToiDaMoiNganh { get; set; } = 5m;
}

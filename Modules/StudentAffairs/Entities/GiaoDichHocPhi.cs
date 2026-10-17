using StudentManagement.Common;

namespace StudentManagement.Modules.StudentAffairs.Entities;

/// <summary>Một lần nộp tiền của một bản ghi học phí (phục vụ đối soát).</summary>
public class GiaoDichHocPhi : BaseEntity
{
    public int HocPhiId { get; set; } // -> HocPhi

    /// <summary>Số tiền nộp đợt này.</summary>
    public decimal SoTien { get; set; }

    public DateTime NgayThanhToan { get; set; }
    public PhuongThucThanhToan PhuongThuc { get; set; }

    /// <summary>Mã giao dịch ngân hàng / số biên lai (bắt buộc khi chuyển khoản).</summary>
    public string? MaGiaoDich { get; set; }

    public string? GhiChu { get; set; }
}

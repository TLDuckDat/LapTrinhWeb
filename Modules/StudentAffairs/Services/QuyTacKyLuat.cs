using StudentManagement.Modules.StudentAffairs.Entities;

namespace StudentManagement.Modules.StudentAffairs.Services;

/// <summary>Quy tắc kỷ luật dùng chung cho nhập điểm rèn luyện và xét học bổng (hàm thuần, không truy vấn CSDL).</summary>
public static class QuyTacKyLuat
{
    /// <summary>
    /// Điểm rèn luyện tối đa được phép theo hình thức kỷ luật.
    /// Trả về 100 nếu không bị giới hạn.
    /// </summary>
    public static int DiemRenLuyenToiDa(HinhThucKyLuat? hinhThuc)
    {
        switch (hinhThuc)
        {
            case HinhThucKyLuat.KhienTrach:
                return 79;  // không quá loại Khá (< 80)
            case HinhThucKyLuat.CanhCao:
                return 64;  // không quá loại Trung bình (< 65)
            case HinhThucKyLuat.DinhChiHocTap:
            case HinhThucKyLuat.BuocThoiHoc:
                return 49;  // loại Yếu/Kém (< 50)
            default:
                return 100;
        }
    }
}

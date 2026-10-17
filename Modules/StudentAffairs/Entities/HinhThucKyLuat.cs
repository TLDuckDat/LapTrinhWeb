namespace StudentManagement.Modules.StudentAffairs.Entities;

/// <summary>Hình thức kỷ luật (từ nhẹ đến nặng).</summary>
public enum HinhThucKyLuat
{
    KhienTrach,     // ĐRL tối đa < 80 (không quá Khá)
    CanhCao,        // ĐRL tối đa < 65 (không quá Trung bình)
    DinhChiHocTap,  // ĐRL < 50 (Yếu/Kém), sinh viên chuyển sang BaoLuu
    BuocThoiHoc     // ĐRL < 50, sinh viên chuyển sang ThoiHoc
}

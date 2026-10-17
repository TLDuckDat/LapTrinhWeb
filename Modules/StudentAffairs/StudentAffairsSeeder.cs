using Microsoft.EntityFrameworkCore;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.Courses.Entities;
using StudentManagement.Modules.Grades.Entities;
using StudentManagement.Modules.Registration.Entities;
using StudentManagement.Modules.StudentAffairs.Entities;
using StudentManagement.Modules.StudentAffairs.Services;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.StudentAffairs;

/// <summary>
/// Dữ liệu mẫu để test module StudentAffairs trên Swagger (chỉ chạy ở Development, sau DbSeeder).
/// Đã có dữ liệu test (môn TST01) thì bỏ qua, nên chạy lại app nhiều lần không bị nhân đôi.
///
/// Học kỳ 1 có sẵn kết quả học tập để test xét học bổng:
///   Bình (95 ĐRL, GPA 4.0) Xuất sắc · Cường (85, 3.4) Giỏi · An (78, 4.0) Khá · Dung (75, 3.0) Khá
///   Hà: có môn học lại · Khoa: chỉ 6 tín chỉ · Linh: có điểm F · Minh: thiếu điểm 1 môn · Nam: ĐRL 60
/// "HK test - cổng ĐRL mở" vừa kết thúc 5 ngày trước -> cổng nhập điểm rèn luyện đang mở (test với Oanh).
/// </summary>
public static class StudentAffairsSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Set<MonHoc>().AnyAsync(x => x.Ma == "TST01")) return;

        var khoaKT = await db.Set<Khoa>().FirstOrDefaultAsync(x => x.Ma == "KT");
        var nganhCNTT = await db.Set<Nganh>().FirstOrDefaultAsync(x => x.Ma == "7480201");
        var hk1 = await db.Set<HocKy>().OrderBy(x => x.Id).FirstOrDefaultAsync(x => x.SoThuTu == 1);
        if (khoaKT == null || nganhCNTT == null || hk1 == null) return; // thiếu dữ liệu gốc của DbSeeder

        var homNay = DateOnly.FromDateTime(DateTime.Now);

        // 1. Ngành thứ 2 (để test chỉ tiêu học bổng theo ngành) và học kỳ đang mở cổng nhập ĐRL
        var nganhKT = new Nganh { Ma = "TEST-KT", Ten = "Kế toán (test)", KhoaId = khoaKT.Id };
        var hkMoCong = new HocKy
        {
            Ten = "HK test - cổng ĐRL mở", SoThuTu = 2, NamHocId = hk1.NamHocId,
            NgayBatDau = homNay.AddDays(-130), NgayKetThuc = homNay.AddDays(-5)
        };
        db.AddRange(nganhKT, hkMoCong);
        await db.SaveChangesAsync();

        // 2. Sinh viên test (2 SV gốc 2025000001 Bình, 2025000002 Cường đã có từ DbSeeder)
        string[] tenSinhVien =
        [
            "Phạm Văn An", "Đỗ Thị Dung", "Vũ Thị Hà", "Ngô Văn Khoa",
            "Bùi Thị Linh", "Hoàng Văn Minh", "Lý Văn Nam", "Trịnh Thị Oanh"
        ];
        for (int i = 0; i < tenSinhVien.Length; i++)
        {
            db.Add(new SinhVien
            {
                MSSV = $"TEST00{i + 1}",
                HoTen = tenSinhVien[i],
                NgaySinh = new DateOnly(2007, 1, i + 1),
                NganhId = i == 0 ? nganhCNTT.Id : nganhKT.Id // An thuộc CNTT, còn lại thuộc Kế toán
            });
        }

        // 3. Môn học (mỗi môn 3 tín chỉ) và lớp học phần Học kỳ 1
        var lopTheoMa = new Dictionary<string, LopHocPhan>();
        for (int i = 1; i <= 5; i++)
        {
            var mon = new MonHoc { Ma = $"TST0{i}", Ten = $"Môn test {i}", SoTinChi = 3 };
            db.Add(mon);
            await db.SaveChangesAsync();

            var lop = new LopHocPhan
            {
                Ma = $"HK1-TST0{i}", HocKyId = hk1.Id, MonHocId = mon.Id,
                SiSoToiDa = 60, TrangThai = TrangThaiLopHocPhan.DaKetThuc
            };
            db.Add(lop);
            lopTheoMa[mon.Ma] = lop;
        }
        await db.SaveChangesAsync();

        var sinhVienTheoMssv = await db.Set<SinhVien>().ToDictionaryAsync(x => x.MSSV);

        // 4. Đăng ký học phần + điểm tổng kết (thang 10). null = chưa có điểm.
        var ketQuaHocTap = new (string Mssv, decimal?[] Diem, string? MonHocLai)[]
        {
            ("2025000001", [9.0m, 9.0m, 9.0m, 9.0m, 9.0m], null),   // Bình: GPA 4.0
            ("2025000002", [8.0m, 8.0m, 8.0m, 8.0m, 7.0m], null),   // Cường: GPA 3.4
            ("TEST001",    [9.0m, 9.0m, 9.0m, 9.0m, 9.0m], null),   // An
            ("TEST002",    [7.0m, 7.0m, 7.0m, 7.0m, 7.0m], null),   // Dung: GPA 3.0
            ("TEST003",    [8.5m, 8.5m, 8.5m, 8.5m, 8.5m], "TST05"), // Hà: có môn học lại
            ("TEST004",    [9.0m, 9.0m], null),                      // Khoa: chỉ 6 tín chỉ
            ("TEST005",    [9.0m, 9.0m, 9.0m, 9.0m, 3.5m], null),   // Linh: điểm F
            ("TEST006",    [9.0m, 9.0m, 9.0m, 9.0m, null], null),   // Minh: thiếu điểm
            ("TEST007",    [9.5m, 9.5m, 9.5m, 9.5m, 9.5m], null),   // Nam: GPA 4.0
            ("TEST008",    [8.0m, 8.0m, 8.0m], null),                // Oanh: 9 tín chỉ
        };

        var diemCanThem = new List<(DangKyHocPhan DangKy, decimal Diem)>();
        foreach (var (mssv, danhSachDiem, monHocLai) in ketQuaHocTap)
        {
            for (int i = 0; i < danhSachDiem.Length; i++)
            {
                string maMon = $"TST0{i + 1}";
                var dangKy = new DangKyHocPhan
                {
                    SinhVienId = sinhVienTheoMssv[mssv].Id,
                    LopHocPhanId = lopTheoMa[maMon].Id,
                    LoaiDangKy = maMon == monHocLai ? LoaiDangKy.HocLai : LoaiDangKy.HocMoi
                };
                db.Add(dangKy);

                if (danhSachDiem[i] is decimal diem)
                    diemCanThem.Add((dangKy, diem));
            }
        }
        await db.SaveChangesAsync();

        foreach (var (dangKy, diem) in diemCanThem)
            db.Add(new DiemHocPhan { DangKyHocPhanId = dangKy.Id, DiemTongKet = diem, DiemChu = DiemChu(diem), DaKhoa = true });

        // 5. Điểm rèn luyện Học kỳ 1 (nhập thẳng vì cổng nhập của HK1 đã đóng)
        var diemRenLuyen = new Dictionary<string, int>
        {
            ["2025000001"] = 95, ["2025000002"] = 85, ["TEST001"] = 78, ["TEST002"] = 75, ["TEST003"] = 90,
            ["TEST004"] = 90, ["TEST005"] = 90, ["TEST006"] = 90, ["TEST007"] = 60
        };
        foreach (var (mssv, diem) in diemRenLuyen)
        {
            db.Add(new DiemRenLuyen
            {
                SinhVienId = sinhVienTheoMssv[mssv].Id,
                HocKyId = hk1.Id,
                Diem = diem,
                XepLoai = ConductScoresService.XepLoai(diem)
            });
        }
        await db.SaveChangesAsync();
    }

    private static string DiemChu(decimal diem) => diem switch
    {
        >= 8.5m => "A",
        >= 8.0m => "B+",
        >= 7.0m => "B",
        >= 6.5m => "C+",
        >= 5.5m => "C",
        >= 5.0m => "D+",
        >= 4.0m => "D",
        _ => "F"
    };
}

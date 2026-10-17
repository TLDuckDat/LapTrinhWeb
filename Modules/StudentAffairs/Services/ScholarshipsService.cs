using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.Courses.Entities;
using StudentManagement.Modules.Grades.Entities;
using StudentManagement.Modules.Registration.Entities;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Entities;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.StudentAffairs.Services;

public class ScholarshipsService(AppDbContext db, IOptions<StudentAffairsSettings> settings) : IScholarshipsService
{
    private readonly AppDbContext _db = db;
    private readonly StudentAffairsSettings _settings = settings.Value;

    /// <summary>Thông tin tạm của 1 sinh viên đủ điều kiện, dùng để xếp hạng.</summary>
    private class UngVien
    {
        public int SinhVienId { get; set; }
        public int NganhId { get; set; }
        public int DiemRenLuyen { get; set; }
        public decimal DiemTrungBinh { get; set; }
        public string MucHocBong { get; set; } = string.Empty;
        public decimal SoTien { get; set; }
    }

    /// <summary>Một học phần sinh viên học trong kỳ (đã kèm số tín chỉ và điểm).</summary>
    private class HocPhanTrongKy
    {
        public int SoTinChi { get; set; }
        public LoaiDangKy LoaiDangKy { get; set; }
        public decimal? DiemTongKet { get; set; }
    }

    // =================================================================
    // SA-05: XÉT HỌC BỔNG
    // =================================================================
    public async Task<XetHocBongResponse> XetHocBongAsync(int hocKyId)
    {
        // ---------- BƯỚC 0: Kiểm tra học kỳ ----------
        var hocKy = await _db.Set<HocKy>()
            .FirstOrDefaultAsync(x => x.Id == hocKyId);

        if (hocKy == null)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        var ketQua = new XetHocBongResponse
        {
            HocKyId = hocKyId,
            TenHocKy = hocKy.Ten,
            SoTienHocBong = _settings.SoTienHocBong
        };

        // ---------- BƯỚC 1: Lấy toàn bộ dữ liệu cần thiết về RAM ----------

        // 1a. Điểm rèn luyện trong kỳ
        var danhSachDrl = await _db.Set<DiemRenLuyen>()
            .Where(x => x.HocKyId == hocKyId)
            .ToListAsync();

        var drlDict = new Dictionary<int, int>();
        foreach (var d in danhSachDrl)
            drlDict[d.SinhVienId] = d.Diem;

        // 1b. Sinh viên đang học (để lấy ngành và đếm sĩ số ngành)
        var sinhVienDangHoc = await _db.Set<SinhVien>()
            .Where(x => x.TrangThai == TrangThaiSinhVien.DangHoc)
            .Select(x => new { x.Id, x.NganhId })
            .ToListAsync();

        var nganhCuaSinhVien = new Dictionary<int, int>();   // SinhVienId -> NganhId
        var siSoNganh = new Dictionary<int, int>();          // NganhId -> số SV đang học
        foreach (var sv in sinhVienDangHoc)
        {
            nganhCuaSinhVien[sv.Id] = sv.NganhId;
            siSoNganh[sv.NganhId] = siSoNganh.GetValueOrDefault(sv.NganhId, 0) + 1;
        }

        // 1c. Các học phần sinh viên đăng ký hợp lệ trong kỳ
        var danhSachDangKy = await (
            from dk in _db.Set<DangKyHocPhan>()
            join lhp in _db.Set<LopHocPhan>() on dk.LopHocPhanId equals lhp.Id
            join mh in _db.Set<MonHoc>() on lhp.MonHocId equals mh.Id
            where lhp.HocKyId == hocKyId
                  && dk.TrangThai == TrangThaiDangKy.DaDangKy
            select new
            {
                DangKyId = dk.Id,
                dk.SinhVienId,
                dk.LoaiDangKy,
                mh.SoTinChi
            }
        ).ToListAsync();

        // 1d. Điểm học phần của các lượt đăng ký đó
        var dangKyIds = danhSachDangKy.Select(x => x.DangKyId).ToList();
        var danhSachDiem = await _db.Set<DiemHocPhan>()
            .Where(x => dangKyIds.Contains(x.DangKyHocPhanId))
            .ToListAsync();

        var diemTheoDangKy = new Dictionary<int, decimal?>();
        foreach (var d in danhSachDiem)
            diemTheoDangKy[d.DangKyHocPhanId] = d.DiemTongKet;

        // Gom học phần theo sinh viên
        var hocPhanTheoSinhVien = new Dictionary<int, List<HocPhanTrongKy>>();
        foreach (var dk in danhSachDangKy)
        {
            if (!hocPhanTheoSinhVien.ContainsKey(dk.SinhVienId))
                hocPhanTheoSinhVien[dk.SinhVienId] = new List<HocPhanTrongKy>();

            hocPhanTheoSinhVien[dk.SinhVienId].Add(new HocPhanTrongKy
            {
                SoTinChi = dk.SoTinChi,
                LoaiDangKy = dk.LoaiDangKy,
                DiemTongKet = diemTheoDangKy.GetValueOrDefault(dk.DangKyId)
            });
        }

        // 1e. Sinh viên bị kỷ luật trong kỳ này
        var biKyLuatTrongKy = (await _db.Set<KhenThuongKyLuat>()
            .Where(x => x.HocKyId == hocKyId && x.Loai == LoaiKhenThuongKyLuat.KyLuat)
            .Select(x => x.SinhVienId)
            .ToListAsync()).ToHashSet();

        // 1f. Học bổng đã cấp trong kỳ (chạy lại không cấp trùng, vẫn tính vào chỉ tiêu)
        var hocBongDaCap = await _db.Set<HocBong>()
            .Where(x => x.HocKyId == hocKyId)
            .ToListAsync();

        var daCoHocBong = new HashSet<int>();
        var soSuatDaDungTheoNganh = new Dictionary<int, int>();
        decimal tongTienDaCap = 0m;
        foreach (var hb in hocBongDaCap)
        {
            daCoHocBong.Add(hb.SinhVienId);
            tongTienDaCap += hb.SoTien;

            if (nganhCuaSinhVien.TryGetValue(hb.SinhVienId, out int nganhId))
                soSuatDaDungTheoNganh[nganhId] = soSuatDaDungTheoNganh.GetValueOrDefault(nganhId, 0) + 1;
        }

        // ---------- BƯỚC 2: Lọc từng sinh viên có điểm rèn luyện trong kỳ ----------
        var danhSachUngVien = new List<UngVien>();

        foreach (var cap in drlDict)
        {
            int sinhVienId = cap.Key;
            int diemRenLuyen = cap.Value;

            if (daCoHocBong.Contains(sinhVienId))
                continue; // đã cấp ở lần chạy trước

            if (!nganhCuaSinhVien.ContainsKey(sinhVienId))
            {
                ThemKhongDat(ketQua, sinhVienId, "Sinh viên không ở trạng thái Đang học.");
                continue;
            }

            if (biKyLuatTrongKy.Contains(sinhVienId))
            {
                ThemKhongDat(ketQua, sinhVienId, "Có quyết định kỷ luật trong học kỳ.");
                continue;
            }

            var hocPhans = hocPhanTheoSinhVien.GetValueOrDefault(sinhVienId) ?? new List<HocPhanTrongKy>();

            // Không được có môn học lại / học cải thiện
            bool coHocLai = false;
            foreach (var hp in hocPhans)
            {
                if (hp.LoaiDangKy != LoaiDangKy.HocMoi)
                    coHocLai = true;
            }
            if (coHocLai)
            {
                ThemKhongDat(ketQua, sinhVienId, "Có học phần học lại / học cải thiện trong kỳ.");
                continue;
            }

            // Mọi học phần phải có điểm và đạt từ điểm D trở lên
            bool chuaCoDiem = false;
            bool coDiemF = false;
            int tongTinChi = 0;
            decimal tongDiemNhanTinChi = 0m;

            foreach (var hp in hocPhans)
            {
                if (hp.DiemTongKet == null)
                {
                    chuaCoDiem = true;
                    continue;
                }
                if (hp.DiemTongKet.Value < _settings.DiemHocPhanToiThieu)
                    coDiemF = true;

                tongTinChi += hp.SoTinChi;
                tongDiemNhanTinChi += DoiSangThang4(hp.DiemTongKet.Value) * hp.SoTinChi;
            }

            if (chuaCoDiem)
            {
                ThemKhongDat(ketQua, sinhVienId, "Có học phần chưa có điểm tổng kết (chưa hoàn thành).");
                continue;
            }
            if (coDiemF)
            {
                ThemKhongDat(ketQua, sinhVienId, "Có học phần dưới điểm D (nợ môn).");
                continue;
            }

            // Số tín chỉ hoàn thành tối thiểu
            if (tongTinChi < _settings.TinChiToiThieuXetHocBong)
            {
                ThemKhongDat(ketQua, sinhVienId,
                    $"Chỉ hoàn thành {tongTinChi} tín chỉ (cần tối thiểu {_settings.TinChiToiThieuXetHocBong}).");
                continue;
            }

            // Điểm trung bình học kỳ thang 4 (trung bình có trọng số theo tín chỉ)
            decimal gpa = Math.Round(tongDiemNhanTinChi / tongTinChi, 2);

            // Xếp mức học bổng (xét từ cao xuống thấp)
            string? muc = null;
            decimal phanTram = 0m;

            if (gpa >= _settings.XuatSacGpaToiThieu && diemRenLuyen >= _settings.XuatSacDrlToiThieu)
            {
                muc = "Xuất sắc";
                phanTram = _settings.XuatSacPhanTramTien;
            }
            else if (gpa >= _settings.GioiGpaToiThieu && diemRenLuyen >= _settings.GioiDrlToiThieu)
            {
                muc = "Giỏi";
                phanTram = _settings.GioiPhanTramTien;
            }
            else if (gpa >= _settings.KhaGpaToiThieu && diemRenLuyen >= _settings.KhaDrlToiThieu)
            {
                muc = "Khá";
                phanTram = _settings.KhaPhanTramTien;
            }

            if (muc == null)
            {
                ThemKhongDat(ketQua, sinhVienId,
                    $"GPA {gpa} / ĐRL {diemRenLuyen} chưa đạt mức Khá trở lên.");
                continue;
            }

            danhSachUngVien.Add(new UngVien
            {
                SinhVienId = sinhVienId,
                NganhId = nganhCuaSinhVien[sinhVienId],
                DiemRenLuyen = diemRenLuyen,
                DiemTrungBinh = gpa,
                MucHocBong = muc,
                SoTien = Math.Round(_settings.SoTienHocBong * phanTram / 100m, 0)
            });
        }

        // ---------- BƯỚC 3: Xếp hạng từ cao xuống thấp ----------
        var danhSachXepHang = danhSachUngVien
            .OrderByDescending(x => x.DiemTrungBinh)
            .ThenByDescending(x => x.DiemRenLuyen)
            .ToList();

        // ---------- BƯỚC 4: Cấp học bổng đến khi hết suất của ngành ----------
        foreach (var uv in danhSachXepHang)
        {
            // 4a. Chỉ tiêu theo ngành: tối đa X% số sinh viên đang học của ngành (ít nhất 1 suất)
            int siSo = siSoNganh.GetValueOrDefault(uv.NganhId, 0);
            int soSuatToiDa = (int)Math.Floor(siSo * _settings.PhanTramSuatToiDaMoiNganh / 100m);
            if (soSuatToiDa < 1)
                soSuatToiDa = 1;

            int soSuatDaDung = soSuatDaDungTheoNganh.GetValueOrDefault(uv.NganhId, 0);
            if (soSuatDaDung >= soSuatToiDa)
            {
                ThemKhongDat(ketQua, uv.SinhVienId, $"Ngành đã hết chỉ tiêu ({soSuatToiDa} suất).");
                continue;
            }

            // 4b. Cấp học bổng
            var hocBong = new HocBong
            {
                SinhVienId = uv.SinhVienId,
                HocKyId = hocKyId,
                Ten = _settings.TenHocBong + " - " + uv.MucHocBong,
                SoTien = uv.SoTien,
                MucHocBong = uv.MucHocBong,
                DiemTrungBinh = uv.DiemTrungBinh,
                DiemRenLuyen = uv.DiemRenLuyen
            };
            _db.Set<HocBong>().Add(hocBong);

            soSuatDaDungTheoNganh[uv.NganhId] = soSuatDaDung + 1;
            tongTienDaCap += uv.SoTien;

            ketQua.DanhSach.Add(new HocBongItem
            {
                SinhVienId = uv.SinhVienId,
                NganhId = uv.NganhId,
                Diem = uv.DiemRenLuyen,
                DiemTrungBinh = uv.DiemTrungBinh,
                MucHocBong = uv.MucHocBong,
                Ten = hocBong.Ten,
                SoTien = hocBong.SoTien
            });
        }

        await _db.SaveChangesAsync();

        ketQua.SoLuongHocBong = ketQua.DanhSach.Count;
        ketQua.TongTienDaCap = tongTienDaCap;
        return ketQua;
    }

    /// <summary>Đổi điểm tổng kết thang 10 sang thang 4 (theo quy chế tín chỉ).</summary>
    private static decimal DoiSangThang4(decimal diemThang10)
    {
        if (diemThang10 >= 8.5m) return 4.0m; // A
        if (diemThang10 >= 8.0m) return 3.5m; // B+
        if (diemThang10 >= 7.0m) return 3.0m; // B
        if (diemThang10 >= 6.5m) return 2.5m; // C+
        if (diemThang10 >= 5.5m) return 2.0m; // C
        if (diemThang10 >= 5.0m) return 1.5m; // D+
        if (diemThang10 >= 4.0m) return 1.0m; // D
        return 0m;                            // F
    }

    private static void ThemKhongDat(XetHocBongResponse ketQua, int sinhVienId, string lyDo)
    {
        ketQua.DanhSachKhongDat.Add(new KhongDatHocBongItem { SinhVienId = sinhVienId, LyDo = lyDo });
    }

    // =================================================================
    // TRA CỨU DANH SÁCH ĐÃ NHẬN HỌC BỔNG
    // =================================================================
    public async Task<List<HocBongResponse>> GetHocBongAsync(int? hocKyId, int? sinhVienId)
    {
        var query =
            from hb in _db.Set<HocBong>().AsNoTracking()
            join sv in _db.Set<SinhVien>().AsNoTracking() on hb.SinhVienId equals sv.Id
            join hk in _db.Set<HocKy>().AsNoTracking() on hb.HocKyId equals hk.Id
            select new HocBongResponse
            {
                Id = hb.Id,
                SinhVienId = hb.SinhVienId,
                MSSV = sv.MSSV,
                HoTen = sv.HoTen,
                HocKyId = hb.HocKyId,
                TenHocKy = hk.Ten,
                Ten = hb.Ten,
                MucHocBong = hb.MucHocBong,
                DiemTrungBinh = hb.DiemTrungBinh,
                DiemRenLuyen = hb.DiemRenLuyen,
                SoTien = hb.SoTien
            };

        if (hocKyId.HasValue)
            query = query.Where(x => x.HocKyId == hocKyId.Value);

        if (sinhVienId.HasValue)
            query = query.Where(x => x.SinhVienId == sinhVienId.Value);

        return await query
            .OrderBy(x => x.HocKyId)
            .ThenByDescending(x => x.SoTien)
            .ThenBy(x => x.MSSV)
            .ToListAsync();
    }
}

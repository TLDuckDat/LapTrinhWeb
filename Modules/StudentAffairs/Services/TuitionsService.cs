using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.Courses.Entities;
using StudentManagement.Modules.Registration.Entities;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Entities;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.StudentAffairs.Services;

public class TuitionsService(AppDbContext db, IOptions<StudentAffairsSettings> settings) : ITuitionsService
{
    private readonly AppDbContext _db = db;
    private readonly StudentAffairsSettings _settings = settings.Value;

    // =================================================================
    // SA-02: TÍNH HỌC PHÍ CẢ HỌC KỲ
    // =================================================================
    public async Task<TinhHocPhiResponse> TinhHocPhiAsync(int hocKyId)
    {
        var hocKy = await _db.Set<HocKy>()
            .FirstOrDefaultAsync(x => x.Id == hocKyId);

        if (hocKy == null)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        // null = tính cho tất cả sinh viên trong kỳ
        return await TinhHocPhiNoiBoAsync(hocKy, null);
    }

    // =================================================================
    // TÍNH LẠI HỌC PHÍ CHO RIÊNG 1 SINH VIÊN
    // (khi sinh viên đổi môn hoặc được duyệt miễn giảm bổ sung)
    // =================================================================
    public async Task<TinhHocPhiResponse> TinhHocPhiSinhVienAsync(int hocKyId, int sinhVienId)
    {
        var hocKy = await _db.Set<HocKy>()
            .FirstOrDefaultAsync(x => x.Id == hocKyId);

        if (hocKy == null)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        var sinhVienTonTai = await _db.Set<SinhVien>()
            .AnyAsync(x => x.Id == sinhVienId);

        if (!sinhVienTonTai)
            throw new KeyNotFoundException("Không tìm thấy sinh viên.");

        return await TinhHocPhiNoiBoAsync(hocKy, sinhVienId);
    }

    /// <summary>
    /// Phần tính học phí dùng chung cho "cả kỳ" và "1 sinh viên".
    /// Toàn bộ dữ liệu được lấy về trước (không truy vấn CSDL trong vòng lặp).
    /// </summary>
    private async Task<TinhHocPhiResponse> TinhHocPhiNoiBoAsync(HocKy hocKy, int? chiSinhVienId)
    {
        int hocKyId = hocKy.Id;
        decimal donGiaTinChi = _settings.DonGiaTinChi;

        // ---------- BƯỚC 1: Lấy dữ liệu về RAM ----------

        // 1a. Miễn giảm trong kỳ -> cộng tổng tỷ lệ của từng sinh viên (tối đa 100%)
        var mienGiamQuery = _db.Set<MienGiam>().Where(x => x.HocKyId == hocKyId);
        if (chiSinhVienId.HasValue)
            mienGiamQuery = mienGiamQuery.Where(x => x.SinhVienId == chiSinhVienId.Value);

        var tyLeMienGiamDict = new Dictionary<int, decimal>();
        foreach (var mg in await mienGiamQuery.ToListAsync())
        {
            decimal tongCu = tyLeMienGiamDict.GetValueOrDefault(mg.SinhVienId, 0m);
            tyLeMienGiamDict[mg.SinhVienId] = Math.Min(100m, tongCu + mg.TyLe);
        }

        // 1b. Học phí đã có trong kỳ -> Dictionary theo SinhVienId
        var hocPhiQuery = _db.Set<HocPhi>().Where(x => x.HocKyId == hocKyId);
        if (chiSinhVienId.HasValue)
            hocPhiQuery = hocPhiQuery.Where(x => x.SinhVienId == chiSinhVienId.Value);

        var hocPhiDict = await hocPhiQuery.ToDictionaryAsync(x => x.SinhVienId);

        // 1c. Số tín chỉ của các lớp học phần đăng ký hợp lệ (TrangThai == DaDangKy)
        var tinChiQuery =
            from dk in _db.Set<DangKyHocPhan>()
            join lhp in _db.Set<LopHocPhan>() on dk.LopHocPhanId equals lhp.Id
            join mh in _db.Set<MonHoc>() on lhp.MonHocId equals mh.Id
            where lhp.HocKyId == hocKyId
                  && dk.TrangThai == TrangThaiDangKy.DaDangKy
            select new { dk.SinhVienId, mh.SoTinChi };

        if (chiSinhVienId.HasValue)
            tinChiQuery = tinChiQuery.Where(x => x.SinhVienId == chiSinhVienId.Value);

        var tongTinChiDict = new Dictionary<int, int>();
        foreach (var dong in await tinChiQuery.ToListAsync())
            tongTinChiDict[dong.SinhVienId] = tongTinChiDict.GetValueOrDefault(dong.SinhVienId, 0) + dong.SoTinChi;

        // 1d. Danh sách sinh viên cần xử lý = có đăng ký môn HOẶC đã có học phí cũ
        //     (sinh viên rút hết môn vẫn cần xử lý để đưa học phí về 0đ)
        var dsSinhVienId = new SortedSet<int>(tongTinChiDict.Keys);
        dsSinhVienId.UnionWith(hocPhiDict.Keys);

        var ketQua = new TinhHocPhiResponse
        {
            HocKyId = hocKyId,
            TenHocKy = hocKy.Ten,
            DonGiaTinChi = donGiaTinChi
        };

        if (dsSinhVienId.Count == 0)
        {
            ketQua.ThongBao = chiSinhVienId.HasValue
                ? "Sinh viên chưa đăng ký học phần nào trong học kỳ này."
                : "Chưa có sinh viên đăng ký học phần trong học kỳ này.";
            return ketQua;
        }

        // ---------- BƯỚC 2: Tính lại số tiền phải nộp ----------
        foreach (var sinhVienId in dsSinhVienId)
        {
            int tongTinChi = tongTinChiDict.GetValueOrDefault(sinhVienId, 0);
            decimal tongTienGoc = tongTinChi * donGiaTinChi;
            decimal tyLeGiam = tyLeMienGiamDict.GetValueOrDefault(sinhVienId, 0m);

            // Công thức: TienGoc * (1 - TyLe/100)
            decimal soTienPhaiNop = Math.Round(tongTienGoc * (1m - (tyLeGiam / 100m)), 2);

            if (!hocPhiDict.TryGetValue(sinhVienId, out var hocPhi))
            {
                // Lần đầu tính học phí cho sinh viên trong kỳ
                hocPhi = new HocPhi { SinhVienId = sinhVienId, HocKyId = hocKyId };
                _db.Set<HocPhi>().Add(hocPhi);
            }

            hocPhi.SoTien = soTienPhaiNop;
            hocPhi.DaThanhToan = hocPhi.SoTienDaNop >= hocPhi.SoTien;

            ketQua.DanhSach.Add(new TinhHocPhiItem
            {
                SinhVienId = sinhVienId,
                TongTinChi = tongTinChi,
                DonGiaTinChi = donGiaTinChi,
                TongTienGoc = tongTienGoc,
                TyLeMienGiam = tyLeGiam,
                SoTienHocPhi = soTienPhaiNop,
                SoTienDaNop = hocPhi.SoTienDaNop,
                SoTienConNo = SoTienConNo(hocPhi),
                // Đã nộp nhiều hơn số phải nộp mới (rút bớt môn / thêm miễn giảm) -> phòng tài vụ hoàn lại
                SoTienThua = Math.Max(0m, hocPhi.SoTienDaNop - hocPhi.SoTien),
                DaThanhToan = hocPhi.DaThanhToan
            });
        }

        await _db.SaveChangesAsync();

        ketQua.SoSinhVien = ketQua.DanhSach.Count;
        return ketQua;
    }

    // =================================================================
    // XEM DANH SÁCH HỌC PHÍ (lọc theo sinh viên / học kỳ)
    // =================================================================
    public async Task<List<HocPhiResponse>> GetHocPhiAsync(int? sinhVienId, int? hocKyId)
    {
        var query =
            from hp in _db.Set<HocPhi>().AsNoTracking()
            join sv in _db.Set<SinhVien>().AsNoTracking() on hp.SinhVienId equals sv.Id
            join hk in _db.Set<HocKy>().AsNoTracking() on hp.HocKyId equals hk.Id
            select new { HocPhi = hp, sv.MSSV, sv.HoTen, HocKy = hk };

        if (sinhVienId.HasValue)
            query = query.Where(x => x.HocPhi.SinhVienId == sinhVienId.Value);

        if (hocKyId.HasValue)
            query = query.Where(x => x.HocPhi.HocKyId == hocKyId.Value);

        var danhSach = await query
            .OrderBy(x => x.MSSV)
            .ThenBy(x => x.HocPhi.HocKyId)
            .ToListAsync();

        // Hạn nộp và quá hạn được tính khi trả về, không lưu trong CSDL
        return danhSach.Select(x => new HocPhiResponse
        {
            Id = x.HocPhi.Id,
            SinhVienId = x.HocPhi.SinhVienId,
            MSSV = x.MSSV,
            HoTen = x.HoTen,
            HocKyId = x.HocPhi.HocKyId,
            TenHocKy = x.HocKy.Ten,
            SoTien = x.HocPhi.SoTien,
            SoTienDaNop = x.HocPhi.SoTienDaNop,
            SoTienConNo = SoTienConNo(x.HocPhi),
            DaThanhToan = x.HocPhi.DaThanhToan,
            HanNop = HanNop(x.HocKy),
            DaQuaHan = DaQuaHan(x.HocPhi, x.HocKy)
        }).ToList();
    }

    // =================================================================
    // SA-03: NỘP HỌC PHÍ (cho phép nộp từng đợt)
    // =================================================================
    public async Task<ThanhToanResponse> ThanhToanAsync(int id, ThanhToanRequest request)
    {
        if (request == null)
            throw new ArgumentException("Thiếu thông tin thanh toán.");

        // 1. Kiểm tra bản ghi học phí có tồn tại không
        var hocPhi = await _db.Set<HocPhi>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (hocPhi == null)
            throw new KeyNotFoundException("Không tìm thấy bản ghi học phí.");

        // 2. Chặn thanh toán hóa đơn 0đ (do được miễn 100% hoặc không có tín chỉ)
        if (hocPhi.SoTien <= 0)
            throw new InvalidOperationException(
                "Học phí bằng 0đ (được miễn 100% hoặc không có tín chỉ), không cần thực hiện thanh toán.");

        // 3. Chặn thanh toán khi đã đóng đủ
        decimal conNo = SoTienConNo(hocPhi);
        if (conNo <= 0)
            throw new InvalidOperationException("Học phí này đã được thanh toán đủ.");

        // 4. Kiểm tra số tiền nộp
        if (request.SoTien <= 0)
            throw new ArgumentException("Số tiền nộp phải lớn hơn 0.");

        if (request.SoTien > conNo)
            throw new ArgumentException(
                $"Số tiền nộp ({request.SoTien:N0}đ) vượt quá số tiền còn nợ ({conNo:N0}đ).");

        // 5. Kiểm tra phương thức
        if (!Enum.IsDefined(request.PhuongThuc))
            throw new ArgumentException("Phương thức thanh toán chỉ được là TienMat hoặc ChuyenKhoan.");

        // 6. Kiểm tra mã giao dịch
        string? maGiaoDich = string.IsNullOrWhiteSpace(request.MaGiaoDich)
            ? null
            : request.MaGiaoDich.Trim();

        if (request.PhuongThuc == PhuongThucThanhToan.ChuyenKhoan && maGiaoDich == null)
            throw new ArgumentException("Chuyển khoản bắt buộc phải có mã giao dịch.");

        if (maGiaoDich != null && maGiaoDich.Length > 50)
            throw new ArgumentException("Mã giao dịch không được vượt quá 50 ký tự.");

        if (maGiaoDich != null)
        {
            var maDaTonTai = await _db.Set<GiaoDichHocPhi>()
                .AnyAsync(x => x.MaGiaoDich == maGiaoDich);

            if (maDaTonTai)
                throw new InvalidOperationException("Mã giao dịch đã tồn tại, có thể giao dịch này đã được ghi nhận.");
        }

        // 7. Kiểm tra ngày thanh toán (không được ở tương lai)
        DateTime ngayThanhToan = request.NgayThanhToan ?? DateTime.Now;
        if (ngayThanhToan.Date > DateTime.Now.Date)
            throw new ArgumentException("Ngày thanh toán không được sau ngày hôm nay.");

        // 8. Ghi nhận tiền và lưu giao dịch
        hocPhi.SoTienDaNop += request.SoTien;
        hocPhi.DaThanhToan = hocPhi.SoTienDaNop >= hocPhi.SoTien;

        var giaoDich = new GiaoDichHocPhi
        {
            HocPhiId = hocPhi.Id,
            SoTien = request.SoTien,
            NgayThanhToan = ngayThanhToan,
            PhuongThuc = request.PhuongThuc,
            MaGiaoDich = maGiaoDich
        };
        _db.Set<GiaoDichHocPhi>().Add(giaoDich);

        await _db.SaveChangesAsync();

        decimal conNoMoi = SoTienConNo(hocPhi);
        return new ThanhToanResponse
        {
            Id = hocPhi.Id,
            SinhVienId = hocPhi.SinhVienId,
            HocKyId = hocPhi.HocKyId,
            SoTien = hocPhi.SoTien,
            SoTienVuaNop = request.SoTien,
            SoTienDaNop = hocPhi.SoTienDaNop,
            SoTienConNo = conNoMoi,
            DaThanhToan = hocPhi.DaThanhToan,
            GiaoDichId = giaoDich.Id,
            ThongBao = hocPhi.DaThanhToan
                ? "Đã thanh toán đủ học phí."
                : $"Đã ghi nhận nộp {request.SoTien:N0}đ, còn nợ {conNoMoi:N0}đ."
        };
    }

    // Xem lịch sử giao dịch của 1 bản ghi học phí
    public async Task<List<GiaoDichHocPhiResponse>> GetGiaoDichAsync(int hocPhiId)
    {
        var hocPhiTonTai = await _db.Set<HocPhi>()
            .AnyAsync(x => x.Id == hocPhiId);

        if (!hocPhiTonTai)
            throw new KeyNotFoundException("Không tìm thấy bản ghi học phí.");

        var danhSach = await _db.Set<GiaoDichHocPhi>()
            .AsNoTracking()
            .Where(x => x.HocPhiId == hocPhiId)
            .OrderBy(x => x.NgayThanhToan)
            .ToListAsync();

        return danhSach.Select(gd => new GiaoDichHocPhiResponse
        {
            Id = gd.Id,
            HocPhiId = gd.HocPhiId,
            SoTien = gd.SoTien,
            NgayThanhToan = gd.NgayThanhToan,
            PhuongThuc = gd.PhuongThuc.ToString(),
            MaGiaoDich = gd.MaGiaoDich,
            GhiChu = gd.GhiChu
        }).ToList();
    }

    // =================================================================
    // KIỂM TRA CÔNG NỢ CỦA 1 SINH VIÊN
    // =================================================================
    public async Task<KiemTraNoHocPhiResponse> KiemTraNoHocPhiAsync(int sinhVienId)
    {
        var sinhVienTonTai = await _db.Set<SinhVien>()
            .AnyAsync(x => x.Id == sinhVienId);

        if (!sinhVienTonTai)
            throw new KeyNotFoundException("Không tìm thấy sinh viên.");

        decimal tongConNo = 0m;
        decimal tongNoQuaHan = 0m;
        foreach (var (hocPhi, hocKy) in await LayHocPhiConNoAsync(sinhVienId))
        {
            tongConNo += SoTienConNo(hocPhi);
            if (DaQuaHan(hocPhi, hocKy))
                tongNoQuaHan += SoTienConNo(hocPhi);
        }

        return new KiemTraNoHocPhiResponse
        {
            SinhVienId = sinhVienId,
            TongConNo = tongConNo,
            TongNoQuaHan = tongNoQuaHan,
            CoNoQuaHan = tongNoQuaHan > 0,
            DuocDangKyHocPhan = tongNoQuaHan <= 0,
            DuDieuKienTotNghiepVeHocPhi = tongConNo <= 0
        };
    }

    // Dùng cho module Registration
    public async Task<bool> CoNoQuaHanAsync(int sinhVienId)
    {
        var danhSach = await LayHocPhiConNoAsync(sinhVienId);
        return danhSach.Any(x => DaQuaHan(x.HocPhi, x.HocKy));
    }

    // Dùng cho module Graduation
    public async Task<bool> DaHoanThanhHocPhiAsync(int sinhVienId)
    {
        return !await _db.Set<HocPhi>()
            .AnyAsync(x => x.SinhVienId == sinhVienId && x.SoTienDaNop < x.SoTien);
    }

    // =================================================================
    // CÁC HÀM PHỤ
    // =================================================================

    private async Task<List<(HocPhi HocPhi, HocKy HocKy)>> LayHocPhiConNoAsync(int sinhVienId)
    {
        var danhSach = await (
            from hp in _db.Set<HocPhi>().AsNoTracking()
            join hk in _db.Set<HocKy>().AsNoTracking() on hp.HocKyId equals hk.Id
            where hp.SinhVienId == sinhVienId && hp.SoTienDaNop < hp.SoTien
            select new { hp, hk }
        ).ToListAsync();

        return danhSach.Select(x => (x.hp, x.hk)).ToList();
    }

    private static decimal SoTienConNo(HocPhi hocPhi) => Math.Max(0m, hocPhi.SoTien - hocPhi.SoTienDaNop);

    /// <summary>Hạn nộp = ngày bắt đầu học kỳ + SoNgayHanNopHocPhi (null nếu không đặt hạn).</summary>
    private DateOnly? HanNop(HocKy hocKy) =>
        _settings.SoNgayHanNopHocPhi > 0 ? hocKy.NgayBatDau.AddDays(_settings.SoNgayHanNopHocPhi) : null;

    private bool DaQuaHan(HocPhi hocPhi, HocKy hocKy)
    {
        var hanNop = HanNop(hocKy);
        return hanNop.HasValue
            && DateOnly.FromDateTime(DateTime.Now) > hanNop.Value
            && SoTienConNo(hocPhi) > 0;
    }
}

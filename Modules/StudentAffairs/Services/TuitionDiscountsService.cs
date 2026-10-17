using Microsoft.EntityFrameworkCore;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Entities;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.StudentAffairs.Services;

public class TuitionDiscountsService(AppDbContext db, ITuitionsService tuitionsService) : ITuitionDiscountsService
{
    private readonly AppDbContext _db = db;
    private readonly ITuitionsService _tuitionsService = tuitionsService;

    // =================================================================
    // SA-04: TẠO MIỄN GIẢM HỌC PHÍ
    // =================================================================
    public async Task<MienGiamResponse> CreateMienGiamAsync(MienGiamRequest request)
    {
        // 1. Kiểm tra ID hợp lệ (> 0)
        if (request.SinhVienId <= 0 || request.HocKyId <= 0)
            throw new ArgumentException("SinhVienId và HocKyId phải lớn hơn 0.");

        // 2. Kiểm tra tỷ lệ và lý do
        KiemTraTyLeVaLyDo(request.TyLe, request.LyDo);

        // 3. Kiểm tra sinh viên và học kỳ có tồn tại không
        var sinhVien = await _db.Set<SinhVien>()
            .FirstOrDefaultAsync(x => x.Id == request.SinhVienId);

        if (sinhVien == null)
            throw new KeyNotFoundException("Không tìm thấy sinh viên.");

        var hocKy = await _db.Set<HocKy>()
            .FirstOrDefaultAsync(x => x.Id == request.HocKyId);

        if (hocKy == null)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        // 4. Tổng các mức miễn giảm của sinh viên trong kỳ không vượt quá 100%
        decimal tongTyLeCu = await TinhTongTyLeAsync(request.SinhVienId, request.HocKyId, boQuaId: null);
        if (tongTyLeCu + request.TyLe > 100)
            throw new InvalidOperationException(
                $"Tổng mức miễn giảm vượt quá 100% (đang có {tongTyLeCu}%, thêm {request.TyLe}%).");

        // 5. Lưu hồ sơ miễn giảm
        var mienGiam = new MienGiam
        {
            SinhVienId = request.SinhVienId,
            HocKyId = request.HocKyId,
            TyLe = request.TyLe,
            LyDo = request.LyDo
        };

        _db.Set<MienGiam>().Add(mienGiam);
        await _db.SaveChangesAsync();

        // 6. Tự động tính lại hóa đơn học phí của sinh viên (nếu kỳ này đã tính học phí)
        decimal? hocPhiMoi = await TinhLaiHocPhiNeuDaCoAsync(mienGiam.SinhVienId, mienGiam.HocKyId);

        return TaoResponse(mienGiam, sinhVien.HoTen, hocKy.Ten, tongTyLeCu + mienGiam.TyLe, hocPhiMoi);
    }

    // =================================================================
    // XEM DANH SÁCH MIỄN GIẢM (lọc theo sinh viên / học kỳ)
    // =================================================================
    public async Task<List<MienGiamResponse>> GetMienGiamAsync(int? sinhVienId, int? hocKyId)
    {
        var query =
            from mg in _db.Set<MienGiam>().AsNoTracking()
            join sv in _db.Set<SinhVien>().AsNoTracking() on mg.SinhVienId equals sv.Id
            join hk in _db.Set<HocKy>().AsNoTracking() on mg.HocKyId equals hk.Id
            select new MienGiamResponse
            {
                Id = mg.Id,
                SinhVienId = mg.SinhVienId,
                HoTen = sv.HoTen,
                HocKyId = mg.HocKyId,
                TenHocKy = hk.Ten,
                TyLe = mg.TyLe,
                LyDo = mg.LyDo
            };

        if (sinhVienId.HasValue)
            query = query.Where(x => x.SinhVienId == sinhVienId.Value);

        if (hocKyId.HasValue)
            query = query.Where(x => x.HocKyId == hocKyId.Value);

        var danhSach = await query
            .OrderBy(x => x.HocKyId)
            .ThenBy(x => x.SinhVienId)
            .ToListAsync();

        // Điền tổng tỷ lệ trong kỳ của từng sinh viên (tính trên RAM, không truy vấn thêm)
        var tongTheoSinhVienVaKy = new Dictionary<string, decimal>();
        foreach (var mg in danhSach)
        {
            string khoa = mg.SinhVienId + "_" + mg.HocKyId;
            tongTheoSinhVienVaKy[khoa] = tongTheoSinhVienVaKy.GetValueOrDefault(khoa, 0m) + mg.TyLe;
        }
        foreach (var mg in danhSach)
            mg.TongTyLeTrongKy = tongTheoSinhVienVaKy[mg.SinhVienId + "_" + mg.HocKyId];

        return danhSach;
    }

    // =================================================================
    // SỬA TỶ LỆ / LÝ DO MIỄN GIẢM
    // =================================================================
    public async Task<MienGiamResponse> SuaMienGiamAsync(int id, SuaMienGiamRequest request)
    {
        if (request == null)
            throw new ArgumentException("Thiếu dữ liệu sửa miễn giảm.");

        // 1. Kiểm tra hồ sơ có tồn tại không
        var mienGiam = await _db.Set<MienGiam>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (mienGiam == null)
            throw new KeyNotFoundException("Không tìm thấy hồ sơ miễn giảm.");

        // 2. Kiểm tra tỷ lệ và lý do
        KiemTraTyLeVaLyDo(request.TyLe, request.LyDo);

        // 3. Tổng các hồ sơ khác + tỷ lệ mới không vượt quá 100%
        decimal tongTyLeKhac = await TinhTongTyLeAsync(mienGiam.SinhVienId, mienGiam.HocKyId, boQuaId: mienGiam.Id);
        if (tongTyLeKhac + request.TyLe > 100)
            throw new InvalidOperationException(
                $"Tổng mức miễn giảm vượt quá 100% (các hồ sơ khác đang có {tongTyLeKhac}%).");

        // 4. Lưu thay đổi
        mienGiam.TyLe = request.TyLe;
        mienGiam.LyDo = request.LyDo;
        await _db.SaveChangesAsync();

        // 5. Tự động tính lại hóa đơn học phí
        decimal? hocPhiMoi = await TinhLaiHocPhiNeuDaCoAsync(mienGiam.SinhVienId, mienGiam.HocKyId);

        var (hoTen, tenHocKy) = await LayTenSinhVienVaHocKyAsync(mienGiam.SinhVienId, mienGiam.HocKyId);
        return TaoResponse(mienGiam, hoTen, tenHocKy, tongTyLeKhac + mienGiam.TyLe, hocPhiMoi);
    }

    // =================================================================
    // HỦY HỒ SƠ MIỄN GIẢM
    // =================================================================
    public async Task<MienGiamResponse> XoaMienGiamAsync(int id)
    {
        // 1. Kiểm tra hồ sơ có tồn tại không
        var mienGiam = await _db.Set<MienGiam>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (mienGiam == null)
            throw new KeyNotFoundException("Không tìm thấy hồ sơ miễn giảm.");

        // 2. Xóa hồ sơ
        _db.Set<MienGiam>().Remove(mienGiam);
        await _db.SaveChangesAsync();

        // 3. Tự động tính lại hóa đơn học phí
        decimal? hocPhiMoi = await TinhLaiHocPhiNeuDaCoAsync(mienGiam.SinhVienId, mienGiam.HocKyId);

        decimal tongConLai = await TinhTongTyLeAsync(mienGiam.SinhVienId, mienGiam.HocKyId, boQuaId: null);
        var (hoTen, tenHocKy) = await LayTenSinhVienVaHocKyAsync(mienGiam.SinhVienId, mienGiam.HocKyId);
        return TaoResponse(mienGiam, hoTen, tenHocKy, tongConLai, hocPhiMoi);
    }

    // =================================================================
    // CÁC HÀM PHỤ
    // =================================================================

    /// <summary>Tỷ lệ phải trong [0..100], lý do tối đa 500 ký tự.</summary>
    private static void KiemTraTyLeVaLyDo(decimal tyLe, string? lyDo)
    {
        if (tyLe < 0 || tyLe > 100)
            throw new ArgumentException("Tỷ lệ miễn giảm phải từ 0 đến 100.");

        if (lyDo != null && lyDo.Length > 500)
            throw new ArgumentException("Lý do miễn giảm không được vượt quá 500 ký tự.");
    }

    /// <summary>Tổng tỷ lệ miễn giảm của sinh viên trong kỳ (có thể bỏ qua 1 hồ sơ đang sửa).</summary>
    private async Task<decimal> TinhTongTyLeAsync(int sinhVienId, int hocKyId, int? boQuaId)
    {
        var danhSach = await _db.Set<MienGiam>()
            .Where(x => x.SinhVienId == sinhVienId && x.HocKyId == hocKyId)
            .ToListAsync();

        decimal tong = 0m;
        foreach (var mg in danhSach)
        {
            if (boQuaId.HasValue && mg.Id == boQuaId.Value)
                continue;
            tong += mg.TyLe;
        }
        return tong;
    }

    /// <summary>
    /// Nếu sinh viên đã có hóa đơn học phí trong kỳ thì tính lại ngay và trả về số tiền mới.
    /// Nếu kỳ chưa tính học phí thì không làm gì (trả về null).
    /// </summary>
    private async Task<decimal?> TinhLaiHocPhiNeuDaCoAsync(int sinhVienId, int hocKyId)
    {
        var daCoHocPhi = await _db.Set<HocPhi>()
            .AnyAsync(x => x.SinhVienId == sinhVienId && x.HocKyId == hocKyId);

        if (!daCoHocPhi)
            return null;

        var ketQua = await _tuitionsService.TinhHocPhiSinhVienAsync(hocKyId, sinhVienId);

        foreach (var item in ketQua.DanhSach)
        {
            if (item.SinhVienId == sinhVienId)
                return item.SoTienHocPhi;
        }
        return null;
    }

    private async Task<(string HoTen, string TenHocKy)> LayTenSinhVienVaHocKyAsync(int sinhVienId, int hocKyId)
    {
        var sinhVien = await _db.Set<SinhVien>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == sinhVienId);
        var hocKy = await _db.Set<HocKy>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == hocKyId);
        return (sinhVien?.HoTen ?? string.Empty, hocKy?.Ten ?? string.Empty);
    }

    private static MienGiamResponse TaoResponse(
        MienGiam mienGiam, string hoTen, string tenHocKy, decimal tongTyLe, decimal? hocPhiMoi)
    {
        return new MienGiamResponse
        {
            Id = mienGiam.Id,
            SinhVienId = mienGiam.SinhVienId,
            HoTen = hoTen,
            HocKyId = mienGiam.HocKyId,
            TenHocKy = tenHocKy,
            TyLe = mienGiam.TyLe,
            LyDo = mienGiam.LyDo,
            TongTyLeTrongKy = tongTyLe,
            HocPhiSauCapNhat = hocPhiMoi
        };
    }
}

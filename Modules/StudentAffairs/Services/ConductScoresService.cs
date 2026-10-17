using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Entities;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.StudentAffairs.Services;

public class ConductScoresService(AppDbContext db, IOptions<StudentAffairsSettings> settings) : IConductScoresService
{
    private readonly AppDbContext _db = db;
    private readonly StudentAffairsSettings _settings = settings.Value;

    // =================================================================
    // SA-01: NHẬP ĐIỂM RÈN LUYỆN
    // =================================================================
    public async Task<DiemRenLuyenResponse> NhapDiemRenLuyenAsync(DiemRenLuyenRequest request, bool laAdmin)
    {
        // 1. Kiểm tra sinh viên có tồn tại trong hệ thống không
        var sinhVien = await _db.Set<SinhVien>()
            .FirstOrDefaultAsync(x => x.Id == request.SinhVienId);

        if (sinhVien == null)
            throw new KeyNotFoundException("Không tìm thấy sinh viên.");

        // 2. Chặn sinh viên không còn học (thôi học, bảo lưu, tốt nghiệp) nhập điểm rèn luyện
        if (sinhVien.TrangThai != TrangThaiSinhVien.DangHoc)
            throw new InvalidOperationException(
                $"Sinh viên đang ở trạng thái '{sinhVien.TrangThai}', không thể nhập điểm rèn luyện.");

        // 3. Kiểm tra học kỳ có tồn tại không
        var hocKy = await _db.Set<HocKy>()
            .FirstOrDefaultAsync(x => x.Id == request.HocKyId);

        if (hocKy == null)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        // 4. Kiểm tra điểm hợp lệ trong khoảng 0 đến 100
        if (request.Diem < 0 || request.Diem > 100)
            throw new ArgumentException("Điểm rèn luyện phải từ 0 đến 100.");

        // 5. Chỉ được nhập trong thời hạn mở cổng
        KiemTraCongNhapDiem(hocKy, laAdmin);

        // 6. Sổ điểm của học kỳ đã khóa thì không được nhập thêm
        var soDaKhoa = await _db.Set<DiemRenLuyen>()
            .AnyAsync(x => x.HocKyId == request.HocKyId && x.DaKhoa);

        if (soDaKhoa)
            throw new InvalidOperationException("Sổ điểm rèn luyện của học kỳ này đã khóa.");

        // 7. Chặn nhập trùng: Một sinh viên chỉ có duy nhất 1 điểm rèn luyện trong 1 học kỳ
        var daTonTai = await _db.Set<DiemRenLuyen>()
            .AnyAsync(x =>
                x.SinhVienId == request.SinhVienId &&
                x.HocKyId == request.HocKyId);

        if (daTonTai)
            throw new InvalidOperationException("Sinh viên đã có điểm trong học kỳ này.");

        // 8. Sinh viên bị kỷ luật trong kỳ thì điểm không được vượt khung cho phép
        await KiemTraKhungDiemTheoKyLuatAsync(request.SinhVienId, hocKy, request.Diem);

        // 9. Tạo bản ghi điểm mới và lưu vào CSDL
        var diem = new DiemRenLuyen
        {
            SinhVienId = request.SinhVienId,
            HocKyId = request.HocKyId,
            Diem = request.Diem,
            XepLoai = XepLoai(request.Diem)
        };

        _db.Set<DiemRenLuyen>().Add(diem);
        await _db.SaveChangesAsync();

        return TaoResponse(diem, sinhVien, hocKy);
    }

    // =================================================================
    // XEM DANH SÁCH ĐIỂM (lọc theo sinh viên / học kỳ)
    // =================================================================
    public async Task<List<DiemRenLuyenResponse>> GetDiemRenLuyenAsync(int? sinhVienId, int? hocKyId)
    {
        var query =
            from d in _db.Set<DiemRenLuyen>().AsNoTracking()
            join sv in _db.Set<SinhVien>().AsNoTracking() on d.SinhVienId equals sv.Id
            join hk in _db.Set<HocKy>().AsNoTracking() on d.HocKyId equals hk.Id
            select new DiemRenLuyenResponse
            {
                Id = d.Id,
                SinhVienId = d.SinhVienId,
                MSSV = sv.MSSV,
                HoTen = sv.HoTen,
                HocKyId = d.HocKyId,
                TenHocKy = hk.Ten,
                Diem = d.Diem,
                XepLoai = d.XepLoai,
                DaKhoa = d.DaKhoa,
                CreatedAt = d.CreatedAt
            };

        if (sinhVienId.HasValue)
            query = query.Where(x => x.SinhVienId == sinhVienId.Value);

        if (hocKyId.HasValue)
            query = query.Where(x => x.HocKyId == hocKyId.Value);

        return await query
            .OrderBy(x => x.MSSV)
            .ToListAsync();
    }

    // =================================================================
    // SỬA ĐIỂM RÈN LUYỆN (có lưu vết)
    // =================================================================
    public async Task<DiemRenLuyenResponse> SuaDiemRenLuyenAsync(
        int id, SuaDiemRenLuyenRequest request, int? nguoiSuaId, string? tenNguoiSua, bool laAdmin)
    {
        if (request == null)
            throw new ArgumentException("Thiếu dữ liệu sửa điểm.");

        // 1. Kiểm tra bản ghi điểm có tồn tại không
        var diem = await _db.Set<DiemRenLuyen>()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (diem == null)
            throw new KeyNotFoundException("Không tìm thấy điểm rèn luyện.");

        // 2. Kiểm tra dữ liệu sửa
        if (request.Diem < 0 || request.Diem > 100)
            throw new ArgumentException("Điểm rèn luyện phải từ 0 đến 100.");

        if (string.IsNullOrWhiteSpace(request.LyDoSua))
            throw new ArgumentException("Phải nhập lý do sửa điểm.");

        if (request.LyDoSua.Length > 500)
            throw new ArgumentException("Lý do sửa không được vượt quá 500 ký tự.");

        if (request.Diem == diem.Diem)
            throw new ArgumentException("Điểm mới trùng với điểm hiện tại, không cần sửa.");

        // 3. Sổ điểm đã khóa thì không ai được sửa (Admin phải mở khóa trước)
        if (diem.DaKhoa)
            throw new InvalidOperationException("Điểm rèn luyện đã bị khóa sổ, cần Admin mở khóa trước khi sửa.");

        // 4. Chỉ được sửa trong thời hạn mở cổng
        var hocKy = await _db.Set<HocKy>()
            .FirstOrDefaultAsync(x => x.Id == diem.HocKyId);

        if (hocKy == null)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        KiemTraCongNhapDiem(hocKy, laAdmin);

        // 5. Điểm mới không được vượt khung theo kỷ luật
        await KiemTraKhungDiemTheoKyLuatAsync(diem.SinhVienId, hocKy, request.Diem);

        // 6. Lưu vết lần sửa
        _db.Set<LichSuSuaDiemRenLuyen>().Add(new LichSuSuaDiemRenLuyen
        {
            DiemRenLuyenId = diem.Id,
            DiemCu = diem.Diem,
            DiemMoi = request.Diem,
            LyDo = request.LyDoSua.Trim(),
            NguoiSuaId = nguoiSuaId,
            TenNguoiSua = tenNguoiSua,
            NgaySua = DateTime.Now
        });

        // 7. Cập nhật điểm và xếp loại
        diem.Diem = request.Diem;
        diem.XepLoai = XepLoai(request.Diem);

        await _db.SaveChangesAsync();

        var sinhVien = await _db.Set<SinhVien>()
            .FirstOrDefaultAsync(x => x.Id == diem.SinhVienId);

        return TaoResponse(diem, sinhVien, hocKy);
    }

    // Xem lịch sử sửa của 1 bản ghi điểm
    public async Task<List<LichSuSuaDiemRenLuyenResponse>> GetLichSuSuaAsync(int diemRenLuyenId)
    {
        var tonTai = await _db.Set<DiemRenLuyen>()
            .AnyAsync(x => x.Id == diemRenLuyenId);

        if (!tonTai)
            throw new KeyNotFoundException("Không tìm thấy điểm rèn luyện.");

        return await _db.Set<LichSuSuaDiemRenLuyen>()
            .AsNoTracking()
            .Where(x => x.DiemRenLuyenId == diemRenLuyenId)
            .OrderBy(x => x.NgaySua)
            .Select(x => new LichSuSuaDiemRenLuyenResponse
            {
                Id = x.Id,
                DiemRenLuyenId = x.DiemRenLuyenId,
                DiemCu = x.DiemCu,
                DiemMoi = x.DiemMoi,
                LyDo = x.LyDo,
                NguoiSuaId = x.NguoiSuaId,
                TenNguoiSua = x.TenNguoiSua,
                NgaySua = x.NgaySua
            })
            .ToListAsync();
    }

    // =================================================================
    // KHÓA SỔ / MỞ KHÓA SỔ ĐIỂM RÈN LUYỆN CẢ HỌC KỲ
    // =================================================================
    public async Task<KhoaSoDiemRenLuyenResponse> KhoaSoAsync(int hocKyId)
    {
        return await DatTrangThaiKhoaAsync(hocKyId, daKhoa: true);
    }

    public async Task<KhoaSoDiemRenLuyenResponse> MoKhoaSoAsync(int hocKyId)
    {
        return await DatTrangThaiKhoaAsync(hocKyId, daKhoa: false);
    }

    private async Task<KhoaSoDiemRenLuyenResponse> DatTrangThaiKhoaAsync(int hocKyId, bool daKhoa)
    {
        var hocKyTonTai = await _db.Set<HocKy>()
            .AnyAsync(x => x.Id == hocKyId);

        if (!hocKyTonTai)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        var danhSach = await _db.Set<DiemRenLuyen>()
            .Where(x => x.HocKyId == hocKyId)
            .ToListAsync();

        if (danhSach.Count == 0)
            throw new InvalidOperationException("Học kỳ này chưa có điểm rèn luyện nào.");

        foreach (var d in danhSach)
            d.DaKhoa = daKhoa;

        await _db.SaveChangesAsync();

        return new KhoaSoDiemRenLuyenResponse
        {
            HocKyId = hocKyId,
            DaKhoa = daKhoa,
            SoBanGhi = danhSach.Count
        };
    }

    // =================================================================
    // CÁC HÀM PHỤ
    // =================================================================

    /// <summary>Quy đổi điểm số sang xếp loại rèn luyện.</summary>
    internal static string XepLoai(int diem)
    {
        if (diem >= 90) return "Xuất sắc";
        if (diem >= 80) return "Tốt";
        if (diem >= 65) return "Khá";
        if (diem >= 50) return "Trung bình";
        return "Yếu";
    }

    /// <summary>
    /// Cổng nhập/sửa điểm chỉ mở từ ngày kết thúc học kỳ đến N ngày sau đó (N lấy từ cấu hình).
    /// Admin được bỏ qua. N = 0 nghĩa là không giới hạn.
    /// </summary>
    private void KiemTraCongNhapDiem(HocKy hocKy, bool laAdmin)
    {
        if (laAdmin || _settings.SoNgayMoCongNhapDiemRenLuyen <= 0)
            return;

        var homNay = DateOnly.FromDateTime(DateTime.Now);
        var ngayMo = hocKy.NgayKetThuc;
        var ngayDong = hocKy.NgayKetThuc.AddDays(_settings.SoNgayMoCongNhapDiemRenLuyen);

        if (homNay < ngayMo || homNay > ngayDong)
            throw new InvalidOperationException(
                $"Cổng nhập/sửa điểm rèn luyện của học kỳ chỉ mở từ {ngayMo:dd/MM/yyyy} đến {ngayDong:dd/MM/yyyy}.");
    }

    /// <summary>
    /// Nếu sinh viên bị kỷ luật trong kỳ thì điểm không được vượt khung:
    /// Khiển trách &lt; 80, Cảnh cáo &lt; 65, Đình chỉ / Buộc thôi học &lt; 50.
    /// </summary>
    private async Task KiemTraKhungDiemTheoKyLuatAsync(int sinhVienId, HocKy hocKy, int diem)
    {
        var danhSachKyLuat = await _db.Set<KhenThuongKyLuat>()
            .Where(x => x.SinhVienId == sinhVienId
                        && x.HocKyId == hocKy.Id
                        && x.Loai == LoaiKhenThuongKyLuat.KyLuat)
            .ToListAsync();

        int diemToiDa = 100;
        string? hinhThucNangNhat = null;

        foreach (var kl in danhSachKyLuat)
        {
            int toiDa = QuyTacKyLuat.DiemRenLuyenToiDa(kl.HinhThuc);
            if (toiDa < diemToiDa)
            {
                diemToiDa = toiDa;
                hinhThucNangNhat = kl.HinhThuc?.ToString();
            }
        }

        if (diem > diemToiDa)
            throw new ArgumentException(
                $"Sinh viên bị kỷ luật ({hinhThucNangNhat}) trong kỳ nên điểm rèn luyện tối đa là {diemToiDa}.");
    }

    private static DiemRenLuyenResponse TaoResponse(DiemRenLuyen diem, SinhVien? sinhVien, HocKy hocKy)
    {
        return new DiemRenLuyenResponse
        {
            Id = diem.Id,
            SinhVienId = diem.SinhVienId,
            MSSV = sinhVien?.MSSV ?? string.Empty,
            HoTen = sinhVien?.HoTen ?? string.Empty,
            HocKyId = diem.HocKyId,
            TenHocKy = hocKy.Ten,
            Diem = diem.Diem,
            XepLoai = diem.XepLoai,
            DaKhoa = diem.DaKhoa,
            CreatedAt = diem.CreatedAt
        };
    }
}

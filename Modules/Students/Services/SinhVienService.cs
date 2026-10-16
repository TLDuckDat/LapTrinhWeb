using Microsoft.EntityFrameworkCore;
using StudentManagement.Common;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.Students.Dtos;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.Students.Services;

/// <summary>Thực thi nghiệp vụ quản lý sinh viên (STU-01, 02, 03, 04, 06, 07).</summary>
public class SinhVienService(AppDbContext db) : ISinhVienService
{
    private readonly AppDbContext _db = db;

    // STU-01 & STU-03: Lấy danh sách sinh viên có phân trang và lọc đa tiêu chí
    public async Task<PagedResult<SinhVienDto>> GetPagedAsync(SinhVienFilterQuery query)
    {
        // Câu truy vấn gốc kết hợp LEFT JOIN với Nganh và LopHanhChinh
        var baseQuery = from sv in _db.Set<SinhVien>().AsNoTracking()
                        join ng in _db.Set<Nganh>().AsNoTracking() on sv.NganhId equals ng.Id into ngGroup
                        from ng in ngGroup.DefaultIfEmpty()
                        join lop in _db.Set<LopHanhChinh>().AsNoTracking() on sv.LopHanhChinhId equals lop.Id into lopGroup
                        from lop in lopGroup.DefaultIfEmpty()
                        select new
                        {
                            sv.Id,
                            sv.MSSV,
                            sv.HoTen,
                            sv.NgaySinh,
                            sv.GioiTinh,
                            sv.CCCD,
                            sv.Email,
                            sv.SoDienThoai,
                            sv.TrangThai,
                            sv.NganhId,
                            TenNganh = ng != null ? ng.Ten : null,
                            KhoaId = ng != null ? (int?)ng.KhoaId : null,
                            sv.LopHanhChinhId,
                            TenLopHanhChinh = lop != null ? lop.Ten : null,
                            sv.CreatedAt,
                            sv.UpdatedAt
                        };

        // Lọc theo Khoa
        if (query.KhoaId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.KhoaId == query.KhoaId.Value);
        }

        // Lọc theo Ngành
        if (query.NganhId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.NganhId == query.NganhId.Value);
        }

        // Lọc theo Lớp hành chính
        if (query.LopHanhChinhId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.LopHanhChinhId == query.LopHanhChinhId.Value);
        }

        // Lọc theo Trạng thái học tập
        if (query.TrangThai.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.TrangThai == query.TrangThai.Value);
        }

        // Tìm kiếm theo từ khóa (MSSV hoặc Họ tên)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            baseQuery = baseQuery.Where(x => x.MSSV.Contains(search) || x.HoTen.Contains(search));
        }

        var total = await baseQuery.CountAsync();

        var items = await baseQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new SinhVienDto(
                x.Id,
                x.MSSV,
                x.HoTen,
                x.NgaySinh,
                x.GioiTinh,
                x.CCCD,
                x.Email,
                x.SoDienThoai,
                x.TrangThai,
                x.NganhId,
                x.TenNganh,
                x.LopHanhChinhId,
                x.TenLopHanhChinh,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync();

        return new PagedResult<SinhVienDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    // STU-02: Xem chi tiết hồ sơ sinh viên (LINQ Projection + LEFT JOIN)
    public async Task<SinhVienDetailDto?> GetByIdAsync(int id)
    {
        return await QueryDetailSingleAsync(id);
    }

    // STU-01: Tạo mới hồ sơ sinh viên
    public async Task<SinhVienDetailDto> CreateAsync(CreateSinhVienRequest request)
    {
        // 1. Kiểm tra ngày sinh hợp lệ (không vượt quá ngày hiện tại)
        if (request.NgaySinh >= DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Ngày sinh của sinh viên phải trước ngày hiện tại.");

        // Chuẩn hóa dữ liệu trước khi kiểm tra trùng (tránh " 2025000001 " lọt qua kiểm tra rồi lỗi 500 ở unique index)
        var mssv = request.MSSV.Trim();
        var cccd = ChuanHoaCccd(request.CCCD);

        // 2. Kiểm tra trùng MSSV
        if (await _db.Set<SinhVien>().AnyAsync(x => x.MSSV == mssv))
            throw new InvalidOperationException($"Mã số sinh viên '{mssv}' đã tồn tại trên hệ thống.");

        // 3. Kiểm tra ngành học có tồn tại
        if (!await _db.Set<Nganh>().AnyAsync(x => x.Id == request.NganhId))
            throw new KeyNotFoundException($"Không tìm thấy ngành học với ID {request.NganhId}.");

        // 4. Nếu có chuyên ngành, kiểm tra tồn tại và phải thuộc đúng ngành
        if (request.ChuyenNganhId.HasValue)
        {
            var cn = await _db.Set<ChuyenNganh>().FirstOrDefaultAsync(x => x.Id == request.ChuyenNganhId.Value);
            if (cn is null)
                throw new KeyNotFoundException($"Không tìm thấy chuyên ngành với ID {request.ChuyenNganhId.Value}.");
            if (cn.NganhId != request.NganhId)
                throw new ArgumentException($"Chuyên ngành '{cn.Ten}' không thuộc ngành đào tạo của sinh viên.");
        }

        // 5. Nếu có lớp hành chính, kiểm tra tồn tại và phải thuộc đúng ngành
        if (request.LopHanhChinhId.HasValue)
        {
            var lop = await _db.Set<LopHanhChinh>().FirstOrDefaultAsync(x => x.Id == request.LopHanhChinhId.Value);
            if (lop is null)
                throw new KeyNotFoundException($"Không tìm thấy lớp hành chính với ID {request.LopHanhChinhId.Value}.");
            if (lop.NganhId != request.NganhId)
                throw new ArgumentException($"Lớp hành chính '{lop.Ten}' không thuộc ngành đào tạo của sinh viên.");
        }

        // 6. Kiểm tra trùng CCCD nếu có
        if (cccd is not null && await _db.Set<SinhVien>().AnyAsync(x => x.CCCD == cccd))
        {
            throw new InvalidOperationException($"Số CCCD '{cccd}' đã được sử dụng bởi sinh viên khác.");
        }

        var entity = new SinhVien
        {
            MSSV = mssv,
            HoTen = request.HoTen.Trim(),
            NgaySinh = request.NgaySinh,
            GioiTinh = request.GioiTinh?.Trim(),
            CCCD = cccd,
            Email = request.Email?.Trim(),
            SoDienThoai = request.SoDienThoai?.Trim(),
            DiaChi = request.DiaChi?.Trim(),
            NganhId = request.NganhId,
            ChuyenNganhId = request.ChuyenNganhId,
            LopHanhChinhId = request.LopHanhChinhId,
            TrangThai = TrangThaiSinhVien.DangHoc
        };

        _db.Add(entity);
        await _db.SaveChangesAsync();

        return (await QueryDetailSingleAsync(entity.Id))!;
    }

    // STU-01: Cập nhật thông tin hồ sơ sinh viên
    public async Task<SinhVienDetailDto?> UpdateAsync(int id, UpdateSinhVienRequest request)
    {
        var entity = await _db.Set<SinhVien>().FindAsync(id);
        if (entity is null) return null;

        if (request.NgaySinh >= DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Ngày sinh của sinh viên phải trước ngày hiện tại.");

        if (!await _db.Set<Nganh>().AnyAsync(x => x.Id == request.NganhId))
            throw new KeyNotFoundException($"Không tìm thấy ngành học với ID {request.NganhId}.");

        if (request.ChuyenNganhId.HasValue)
        {
            var cn = await _db.Set<ChuyenNganh>().FirstOrDefaultAsync(x => x.Id == request.ChuyenNganhId.Value);
            if (cn is null)
                throw new KeyNotFoundException($"Không tìm thấy chuyên ngành với ID {request.ChuyenNganhId.Value}.");
            if (cn.NganhId != request.NganhId)
                throw new ArgumentException($"Chuyên ngành '{cn.Ten}' không thuộc ngành đào tạo của sinh viên.");
        }

        if (request.LopHanhChinhId.HasValue)
        {
            var lop = await _db.Set<LopHanhChinh>().FirstOrDefaultAsync(x => x.Id == request.LopHanhChinhId.Value);
            if (lop is null)
                throw new KeyNotFoundException($"Không tìm thấy lớp hành chính với ID {request.LopHanhChinhId.Value}.");
            if (lop.NganhId != request.NganhId)
                throw new ArgumentException($"Lớp hành chính '{lop.Ten}' không thuộc ngành đào tạo của sinh viên.");
        }

        var cccd = ChuanHoaCccd(request.CCCD);
        if (cccd is not null && await _db.Set<SinhVien>().AnyAsync(x => x.CCCD == cccd && x.Id != id))
        {
            throw new InvalidOperationException($"Số CCCD '{cccd}' đã được sử dụng bởi sinh viên khác.");
        }

        entity.HoTen = request.HoTen.Trim();
        entity.NgaySinh = request.NgaySinh;
        entity.GioiTinh = request.GioiTinh?.Trim();
        entity.CCCD = cccd;
        entity.Email = request.Email?.Trim();
        entity.SoDienThoai = request.SoDienThoai?.Trim();
        entity.DiaChi = request.DiaChi?.Trim();
        entity.NganhId = request.NganhId;
        entity.ChuyenNganhId = request.ChuyenNganhId;
        entity.LopHanhChinhId = request.LopHanhChinhId;

        await _db.SaveChangesAsync();

        return await QueryDetailSingleAsync(id);
    }

    // STU-01: Xóa sinh viên
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Set<SinhVien>().FindAsync(id);
        if (entity is null) return false;

        _db.Remove(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Không thể xóa sinh viên do có dữ liệu liên quan (điểm thi, đăng ký môn, học phí...).");
        }

        return true;
    }

    // STU-04: Chuyển lớp hành chính cho 1 sinh viên
    public async Task<SinhVienDetailDto?> ChuyenLopAsync(int id, ChuyenLopHanhChinhRequest request)
    {
        var sv = await _db.Set<SinhVien>().FindAsync(id);
        if (sv is null) return null;

        // Sinh viên đã thôi học / tốt nghiệp không còn sinh hoạt lớp, không cho chuyển lớp
        if (sv.TrangThai is TrangThaiSinhVien.ThoiHoc or TrangThaiSinhVien.DaTotNghiep)
            throw new InvalidOperationException("Sinh viên đã thôi học hoặc đã tốt nghiệp, không thể chuyển lớp hành chính.");

        var lopMoi = await _db.Set<LopHanhChinh>().FirstOrDefaultAsync(x => x.Id == request.LopHanhChinhId);
        if (lopMoi is null)
            throw new KeyNotFoundException($"Không tìm thấy lớp hành chính với ID {request.LopHanhChinhId}.");

        if (lopMoi.NganhId != sv.NganhId)
            throw new ArgumentException($"Lớp '{lopMoi.Ten}' thuộc ngành khác, không thể chuyển sinh viên sang lớp này.");

        sv.LopHanhChinhId = request.LopHanhChinhId;
        await _db.SaveChangesAsync();

        return await QueryDetailSingleAsync(id);
    }

    // STU-06: Đổi trạng thái học tập
    public async Task<SinhVienDetailDto?> ChangeStatusAsync(int id, ChangeStatusRequest request)
    {
        // TrangThai đã được [Required] + [EnumDataType] kiểm tra ở DTO nên chắc chắn có giá trị hợp lệ
        var trangThaiMoi = request.TrangThai!.Value;

        return await ApplyStatusChangeAsync(
            sinhVienId: id,
            newStatus: trangThaiMoi,
            lyDo: request.LyDo ?? $"Chuyển trạng thái sang {trangThaiMoi}",
            soQuyetDinh: request.SoQuyetDinh,
            ngayHieuLuc: request.NgayHieuLuc,
            ghiChu: request.GhiChu
        );
    }

    // STU-07: Tiếp nhận đơn bảo lưu
    public async Task<SinhVienDetailDto?> BaoLuuAsync(int id, BaoLuuRequest request)
    {
        var sv = await _db.Set<SinhVien>().FindAsync(id);
        if (sv is null) return null;

        if (sv.TrangThai != TrangThaiSinhVien.DangHoc)
            throw new InvalidOperationException("Chỉ sinh viên đang ở trạng thái 'Đang học' mới được phép bảo lưu.");

        return await ApplyStatusChangeAsync(
            sinhVienId: id,
            newStatus: TrangThaiSinhVien.BaoLuu,
            lyDo: request.LyDo,
            soQuyetDinh: request.SoQuyetDinh,
            ngayHieuLuc: request.NgayHieuLuc,
            ghiChu: request.GhiChu
        );
    }

    // STU-07: Tiếp nhận quyết định thôi học
    public async Task<SinhVienDetailDto?> ThoiHocAsync(int id, ThoiHocRequest request)
    {
        var sv = await _db.Set<SinhVien>().FindAsync(id);
        if (sv is null) return null;

        if (sv.TrangThai == TrangThaiSinhVien.ThoiHoc)
            throw new InvalidOperationException("Sinh viên này đã ở trạng thái 'Thôi học' từ trước.");

        if (sv.TrangThai == TrangThaiSinhVien.DaTotNghiep)
            throw new InvalidOperationException("Sinh viên đã 'Tốt nghiệp', không thể áp dụng quyết định thôi học.");

        return await ApplyStatusChangeAsync(
            sinhVienId: id,
            newStatus: TrangThaiSinhVien.ThoiHoc,
            lyDo: request.LyDo,
            soQuyetDinh: request.SoQuyetDinh,
            ngayHieuLuc: request.NgayHieuLuc,
            ghiChu: request.GhiChu
        );
    }

    // STU-07: Tiếp nhận sinh viên quay lại học
    public async Task<SinhVienDetailDto?> QuayLaiHocAsync(int id, QuayLaiHocRequest request)
    {
        var sv = await _db.Set<SinhVien>().FindAsync(id);
        if (sv is null) return null;

        // Chỉ cho phép khôi phục khi đang Bảo lưu
        if (sv.TrangThai != TrangThaiSinhVien.BaoLuu)
            throw new InvalidOperationException("Chỉ sinh viên đang trong diện 'Bảo lưu' mới được phép tiếp nhận quay lại học.");

        return await ApplyStatusChangeAsync(
            sinhVienId: id,
            newStatus: TrangThaiSinhVien.DangHoc,
            lyDo: "Quay lại học sau thời gian bảo lưu",
            soQuyetDinh: request.SoQuyetDinh,
            ngayHieuLuc: request.NgayHieuLuc,
            ghiChu: request.GhiChu
        );
    }

    // STU-07: Xem lịch sử biến động trạng thái học tập
    public async Task<List<LichSuHocTapDto>> GetLichSuHocTapAsync(int sinhVienId)
    {
        if (!await _db.Set<SinhVien>().AnyAsync(x => x.Id == sinhVienId))
            throw new KeyNotFoundException($"Không tìm thấy sinh viên với ID {sinhVienId}.");

        return await _db.Set<LichSuHocTap>().AsNoTracking()
            .Where(x => x.SinhVienId == sinhVienId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new LichSuHocTapDto(
                x.Id,
                x.SinhVienId,
                x.TrangThaiCu,
                x.TrangThaiMoi,
                x.SoQuyetDinh,
                x.NgayHieuLuc,
                x.LyDo,
                x.GhiChu,
                x.CreatedAt
            ))
            .ToListAsync();
    }

    // Hàm private dùng chung: Cập nhật trạng thái và ghi bản ghi LichSuHocTap
    private async Task<SinhVienDetailDto?> ApplyStatusChangeAsync(
        int sinhVienId,
        TrangThaiSinhVien newStatus,
        string? lyDo,
        string? soQuyetDinh,
        DateOnly? ngayHieuLuc,
        string? ghiChu)
    {
        var sv = await _db.Set<SinhVien>().FindAsync(sinhVienId);
        if (sv is null) return null;

        if (sv.TrangThai == newStatus)
            throw new InvalidOperationException($"Sinh viên hiện tại đã ở trạng thái '{newStatus}'.");

        // Bảng chuyển trạng thái hợp lệ:
        //   DangHoc -> BaoLuu, ThoiHoc, DaTotNghiep
        //   BaoLuu  -> DangHoc (quay lại học), ThoiHoc
        //   ThoiHoc, DaTotNghiep: trạng thái cuối, không đổi được nữa
        if (sv.TrangThai == TrangThaiSinhVien.ThoiHoc)
            throw new InvalidOperationException("Sinh viên đã thôi học, không thể đổi trạng thái học tập.");

        if (sv.TrangThai == TrangThaiSinhVien.DaTotNghiep)
            throw new InvalidOperationException("Sinh viên đã tốt nghiệp, không thể đổi trạng thái học tập.");

        if (sv.TrangThai == TrangThaiSinhVien.BaoLuu && newStatus == TrangThaiSinhVien.DaTotNghiep)
            throw new InvalidOperationException("Sinh viên đang bảo lưu, phải quay lại học trước khi xét tốt nghiệp.");

        var oldStatus = sv.TrangThai;
        sv.TrangThai = newStatus;

        var history = new LichSuHocTap
        {
            SinhVienId = sinhVienId,
            TrangThaiCu = oldStatus,
            TrangThaiMoi = newStatus,
            SoQuyetDinh = soQuyetDinh,
            NgayHieuLuc = ngayHieuLuc ?? DateOnly.FromDateTime(DateTime.Today),
            LyDo = lyDo,
            GhiChu = ghiChu
        };

        _db.Add(history);
        await _db.SaveChangesAsync();

        return await QueryDetailSingleAsync(sinhVienId);
    }

    // Chuẩn hóa CCCD: bỏ khoảng trắng, chuỗi rỗng coi như không nhập (null) để không đụng unique index
    private static string? ChuanHoaCccd(string? cccd)
        => string.IsNullOrWhiteSpace(cccd) ? null : cccd.Trim();

    // Hàm private dùng chung: Gom toàn bộ thông tin chi tiết bằng 1 câu LINQ Projection + LEFT JOIN
    private async Task<SinhVienDetailDto?> QueryDetailSingleAsync(int id)
    {
        var query = from sv in _db.Set<SinhVien>().AsNoTracking()
                    where sv.Id == id
                    join ng in _db.Set<Nganh>().AsNoTracking() on sv.NganhId equals ng.Id into ngGroup
                    from ng in ngGroup.DefaultIfEmpty()
                    join kh in _db.Set<Khoa>().AsNoTracking() on (ng != null ? ng.KhoaId : 0) equals kh.Id into khGroup
                    from kh in khGroup.DefaultIfEmpty()
                    join cn in _db.Set<ChuyenNganh>().AsNoTracking() on sv.ChuyenNganhId equals cn.Id into cnGroup
                    from cn in cnGroup.DefaultIfEmpty()
                    join lop in _db.Set<LopHanhChinh>().AsNoTracking() on sv.LopHanhChinhId equals lop.Id into lopGroup
                    from lop in lopGroup.DefaultIfEmpty()
                    join gv in _db.Set<GiangVien>().AsNoTracking() on (lop != null ? lop.CoVanId : 0) equals gv.Id into gvGroup
                    from gv in gvGroup.DefaultIfEmpty()
                    select new SinhVienDetailDto(
                        sv.Id,
                        sv.MSSV,
                        sv.HoTen,
                        sv.NgaySinh,
                        sv.GioiTinh,
                        sv.CCCD,
                        sv.Email,
                        sv.SoDienThoai,
                        sv.DiaChi,
                        sv.TrangThai,
                        sv.NganhId,
                        ng != null ? ng.Ten : null,
                        kh != null ? kh.Ten : null,
                        sv.ChuyenNganhId,
                        cn != null ? cn.Ten : null,
                        sv.LopHanhChinhId,
                        lop != null ? lop.Ten : null,
                        lop != null && gv != null ? gv.HoTen : null,
                        sv.CreatedAt,
                        sv.UpdatedAt
                    );

        return await query.FirstOrDefaultAsync();
    }
}

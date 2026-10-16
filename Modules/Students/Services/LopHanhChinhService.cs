using Microsoft.EntityFrameworkCore;
using StudentManagement.Common;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.Students.Dtos;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.Students.Services;

/// <summary>Thực thi nghiệp vụ quản lý Lớp hành chính (STU-04, STU-05).</summary>
public class LopHanhChinhService(AppDbContext db) : ILopHanhChinhService
{
    private readonly AppDbContext _db = db;

    // STU-04: Lấy danh sách lớp phân trang, lọc và tính sĩ số
    public async Task<PagedResult<LopHanhChinhDto>> GetPagedAsync(LopHanhChinhFilterQuery query)
    {
        var baseQuery = from lop in _db.Set<LopHanhChinh>().AsNoTracking()
                        join ng in _db.Set<Nganh>().AsNoTracking() on lop.NganhId equals ng.Id into ngGroup
                        from ng in ngGroup.DefaultIfEmpty()
                        join gv in _db.Set<GiangVien>().AsNoTracking() on lop.CoVanId equals gv.Id into gvGroup
                        from gv in gvGroup.DefaultIfEmpty()
                        select new
                        {
                            lop.Id,
                            lop.Ma,
                            lop.Ten,
                            lop.NganhId,
                            TenNganh = ng != null ? ng.Ten : null,
                            lop.KhoaTuyen,
                            lop.CoVanId,
                            TenCoVan = gv != null ? gv.HoTen : null,
                            SiSo = _db.Set<SinhVien>().Count(sv => sv.LopHanhChinhId == lop.Id),
                            lop.CreatedAt,
                            lop.UpdatedAt
                        };

        if (query.NganhId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.NganhId == query.NganhId.Value);
        }

        if (query.KhoaTuyen.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.KhoaTuyen == query.KhoaTuyen.Value);
        }

        if (query.CoVanId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.CoVanId == query.CoVanId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            baseQuery = baseQuery.Where(x => x.Ma.Contains(search) || x.Ten.Contains(search));
        }

        var total = await baseQuery.CountAsync();

        var items = await baseQuery
            .OrderBy(x => x.Ma)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new LopHanhChinhDto(
                x.Id,
                x.Ma,
                x.Ten,
                x.NganhId,
                x.TenNganh,
                x.KhoaTuyen,
                x.CoVanId,
                x.TenCoVan,
                x.SiSo,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync();

        return new PagedResult<LopHanhChinhDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    // STU-04: Xem chi tiết lớp hành chính kèm danh sách sinh viên
    public async Task<LopHanhChinhDetailDto?> GetByIdAsync(int id)
    {
        var info = await (from lop in _db.Set<LopHanhChinh>().AsNoTracking()
                          where lop.Id == id
                          join ng in _db.Set<Nganh>().AsNoTracking() on lop.NganhId equals ng.Id into ngGroup
                          from ng in ngGroup.DefaultIfEmpty()
                          join kh in _db.Set<Khoa>().AsNoTracking() on (ng != null ? ng.KhoaId : 0) equals kh.Id into khGroup
                          from kh in khGroup.DefaultIfEmpty()
                          join gv in _db.Set<GiangVien>().AsNoTracking() on lop.CoVanId equals gv.Id into gvGroup
                          from gv in gvGroup.DefaultIfEmpty()
                          select new
                          {
                              lop.Id,
                              lop.Ma,
                              lop.Ten,
                              lop.NganhId,
                              TenNganh = ng != null ? ng.Ten : null,
                              TenKhoa = kh != null ? kh.Ten : null,
                              lop.KhoaTuyen,
                              lop.CoVanId,
                              TenCoVan = gv != null ? gv.HoTen : null,
                              lop.CreatedAt,
                              lop.UpdatedAt
                          }).FirstOrDefaultAsync();

        if (info is null) return null;

        var danhSachSinhVien = await _db.Set<SinhVien>().AsNoTracking()
            .Where(x => x.LopHanhChinhId == id)
            .OrderBy(x => x.MSSV)
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
                info.TenNganh,
                x.LopHanhChinhId,
                info.Ten,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync();

        return new LopHanhChinhDetailDto(
            info.Id,
            info.Ma,
            info.Ten,
            info.NganhId,
            info.TenNganh,
            info.TenKhoa,
            info.KhoaTuyen,
            info.CoVanId,
            info.TenCoVan,
            danhSachSinhVien.Count,
            danhSachSinhVien,
            info.CreatedAt,
            info.UpdatedAt
        );
    }

    // STU-04: Tạo mới lớp hành chính
    public async Task<LopHanhChinhDto> CreateAsync(LopHanhChinhRequest request)
    {
        var ma = request.Ma.Trim();
        if (await _db.Set<LopHanhChinh>().AnyAsync(x => x.Ma == ma))
            throw new InvalidOperationException($"Mã lớp hành chính '{ma}' đã tồn tại trên hệ thống.");

        if (!await _db.Set<Nganh>().AnyAsync(x => x.Id == request.NganhId))
            throw new KeyNotFoundException($"Không tìm thấy ngành học với ID {request.NganhId}.");

        if (request.CoVanId.HasValue && !await _db.Set<GiangVien>().AnyAsync(x => x.Id == request.CoVanId.Value))
            throw new KeyNotFoundException($"Không tìm thấy giảng viên cố vấn với ID {request.CoVanId.Value}.");

        var entity = new LopHanhChinh
        {
            Ma = ma,
            Ten = request.Ten.Trim(),
            NganhId = request.NganhId,
            KhoaTuyen = request.KhoaTuyen,
            CoVanId = request.CoVanId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync();

        return (await GetDtoByIdAsync(entity.Id))!;
    }

    // STU-04: Cập nhật lớp hành chính
    public async Task<LopHanhChinhDto?> UpdateAsync(int id, LopHanhChinhRequest request)
    {
        var entity = await _db.Set<LopHanhChinh>().FindAsync(id);
        if (entity is null) return null;

        var ma = request.Ma.Trim();
        if (await _db.Set<LopHanhChinh>().AnyAsync(x => x.Ma == ma && x.Id != id))
            throw new InvalidOperationException($"Mã lớp hành chính '{ma}' đã tồn tại trên hệ thống.");

        if (!await _db.Set<Nganh>().AnyAsync(x => x.Id == request.NganhId))
            throw new KeyNotFoundException($"Không tìm thấy ngành học với ID {request.NganhId}.");

        // Không cho đổi ngành khi lớp còn sinh viên: tránh sinh viên ngành A nằm trong lớp của ngành B
        if (entity.NganhId != request.NganhId &&
            await _db.Set<SinhVien>().AnyAsync(x => x.LopHanhChinhId == id))
            throw new InvalidOperationException("Không thể đổi ngành của lớp hành chính đang có sinh viên. Hãy chuyển sinh viên sang lớp khác trước.");

        if (request.CoVanId.HasValue && !await _db.Set<GiangVien>().AnyAsync(x => x.Id == request.CoVanId.Value))
            throw new KeyNotFoundException($"Không tìm thấy giảng viên cố vấn với ID {request.CoVanId.Value}.");

        entity.Ma = ma;
        entity.Ten = request.Ten.Trim();
        entity.NganhId = request.NganhId;
        entity.KhoaTuyen = request.KhoaTuyen;
        entity.CoVanId = request.CoVanId;

        await _db.SaveChangesAsync();

        return await GetDtoByIdAsync(id);
    }

    // STU-04: Xóa lớp hành chính
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Set<LopHanhChinh>().FindAsync(id);
        if (entity is null) return false;

        _db.Remove(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Không thể xóa lớp hành chính đang có sinh viên trực thuộc.");
        }

        return true;
    }

    // STU-05: Phân công Cố vấn học tập
    public async Task<LopHanhChinhDto?> PhanCoVanAsync(int id, PhanCoVanRequest request)
    {
        var lop = await _db.Set<LopHanhChinh>().FindAsync(id);
        if (lop is null) return null;

        if (request.CoVanId.HasValue && !await _db.Set<GiangVien>().AnyAsync(x => x.Id == request.CoVanId.Value))
            throw new KeyNotFoundException($"Không tìm thấy giảng viên với ID {request.CoVanId.Value}.");

        lop.CoVanId = request.CoVanId;
        await _db.SaveChangesAsync();

        return await GetDtoByIdAsync(id);
    }

    // STU-04: Phân lớp hàng loạt bằng .ExecuteUpdateAsync() của EF Core
    public async Task<int> PhanLopAsync(int id, PhanLopRequest request)
    {
        if (request.SinhVienIds == null || request.SinhVienIds.Count == 0)
            throw new ArgumentException("Danh sách sinh viên để phân lớp không được để trống.");

        var lop = await _db.Set<LopHanhChinh>().FindAsync(id);
        if (lop is null)
            throw new KeyNotFoundException($"Không tìm thấy lớp hành chính với ID {id}.");

        var distinctIds = request.SinhVienIds.Distinct().ToList();

        // 1. Kiểm tra toàn bộ sinh viên có tồn tại trong hệ thống
        var existingStudents = await _db.Set<SinhVien>().AsNoTracking()
            .Where(x => distinctIds.Contains(x.Id))
            .Select(x => new { x.Id, x.NganhId, x.MSSV, x.TrangThai })
            .ToListAsync();

        if (existingStudents.Count != distinctIds.Count)
            throw new KeyNotFoundException("Một hoặc nhiều sinh viên trong danh sách không tồn tại trên hệ thống.");

        // 2. Chốt chặn nghiệp vụ: Kiểm tra tất cả sinh viên có CÙNG NGÀNH với lớp hành chính không
        var differentMajorStudent = existingStudents.FirstOrDefault(x => x.NganhId != lop.NganhId);
        if (differentMajorStudent != null)
            throw new ArgumentException($"Sinh viên '{differentMajorStudent.MSSV}' thuộc ngành khác, không thể phân vào lớp '{lop.Ten}' (Ngành ID: {lop.NganhId}).");

        // 3. Không phân lớp cho sinh viên đã thôi học / tốt nghiệp
        var inactiveStudent = existingStudents.FirstOrDefault(x =>
            x.TrangThai is TrangThaiSinhVien.ThoiHoc or TrangThaiSinhVien.DaTotNghiep);
        if (inactiveStudent != null)
            throw new InvalidOperationException($"Sinh viên '{inactiveStudent.MSSV}' đã thôi học hoặc đã tốt nghiệp, không thể phân lớp.");

        // 3. Thực thi cập nhật hàng loạt bằng 1 câu lệnh SQL duy nhất trực tiếp dưới database
        var updatedRows = await _db.Set<SinhVien>()
            .Where(x => distinctIds.Contains(x.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LopHanhChinhId, id)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
            );

        return updatedRows;
    }

    private async Task<LopHanhChinhDto?> GetDtoByIdAsync(int id)
    {
        var query = from lop in _db.Set<LopHanhChinh>().AsNoTracking()
                    where lop.Id == id
                    join ng in _db.Set<Nganh>().AsNoTracking() on lop.NganhId equals ng.Id into ngGroup
                    from ng in ngGroup.DefaultIfEmpty()
                    join gv in _db.Set<GiangVien>().AsNoTracking() on lop.CoVanId equals gv.Id into gvGroup
                    from gv in gvGroup.DefaultIfEmpty()
                    select new LopHanhChinhDto(
                        lop.Id,
                        lop.Ma,
                        lop.Ten,
                        lop.NganhId,
                        ng != null ? ng.Ten : null,
                        lop.KhoaTuyen,
                        lop.CoVanId,
                        gv != null ? gv.HoTen : null,
                        _db.Set<SinhVien>().Count(sv => sv.LopHanhChinhId == lop.Id),
                        lop.CreatedAt,
                        lop.UpdatedAt
                    );

        return await query.FirstOrDefaultAsync();
    }
}

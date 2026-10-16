using Microsoft.EntityFrameworkCore;
using StudentManagement.Common;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.Students.Dtos;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.Students.Services;

/// <summary>Thực thi nghiệp vụ quản lý Giảng viên (STU-05).</summary>
public class GiangVienService(AppDbContext db) : IGiangVienService
{
    private readonly AppDbContext _db = db;

    public async Task<PagedResult<GiangVienDto>> GetPagedAsync(GiangVienFilterQuery query)
    {
        var baseQuery = from gv in _db.Set<GiangVien>().AsNoTracking()
                        join bm in _db.Set<BoMon>().AsNoTracking() on gv.BoMonId equals bm.Id into bmGroup
                        from bm in bmGroup.DefaultIfEmpty()
                        join kh in _db.Set<Khoa>().AsNoTracking() on (bm != null ? bm.KhoaId : 0) equals kh.Id into khGroup
                        from kh in khGroup.DefaultIfEmpty()
                        select new
                        {
                            gv.Id,
                            gv.Ma,
                            gv.HoTen,
                            gv.HocVi,
                            gv.Email,
                            gv.SoDienThoai,
                            gv.BoMonId,
                            TenBoMon = bm != null ? bm.Ten : null,
                            KhoaId = bm != null ? (int?)bm.KhoaId : null,
                            TenKhoa = kh != null ? kh.Ten : null,
                            gv.CreatedAt,
                            gv.UpdatedAt
                        };

        if (query.BoMonId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.BoMonId == query.BoMonId.Value);
        }

        if (query.KhoaId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.KhoaId == query.KhoaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            baseQuery = baseQuery.Where(x => x.Ma.Contains(search) || x.HoTen.Contains(search));
        }

        var total = await baseQuery.CountAsync();

        var items = await baseQuery
            .OrderBy(x => x.Ma)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new GiangVienDto(
                x.Id,
                x.Ma,
                x.HoTen,
                x.HocVi,
                x.Email,
                x.SoDienThoai,
                x.BoMonId,
                x.TenBoMon,
                x.TenKhoa,
                x.CreatedAt,
                x.UpdatedAt
            ))
            .ToListAsync();

        return new PagedResult<GiangVienDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<GiangVienDto?> GetByIdAsync(int id)
    {
        var query = from gv in _db.Set<GiangVien>().AsNoTracking()
                    where gv.Id == id
                    join bm in _db.Set<BoMon>().AsNoTracking() on gv.BoMonId equals bm.Id into bmGroup
                    from bm in bmGroup.DefaultIfEmpty()
                    join kh in _db.Set<Khoa>().AsNoTracking() on (bm != null ? bm.KhoaId : 0) equals kh.Id into khGroup
                    from kh in khGroup.DefaultIfEmpty()
                    select new GiangVienDto(
                        gv.Id,
                        gv.Ma,
                        gv.HoTen,
                        gv.HocVi,
                        gv.Email,
                        gv.SoDienThoai,
                        gv.BoMonId,
                        bm != null ? bm.Ten : null,
                        kh != null ? kh.Ten : null,
                        gv.CreatedAt,
                        gv.UpdatedAt
                    );

        return await query.FirstOrDefaultAsync();
    }

    public async Task<GiangVienDto> CreateAsync(GiangVienRequest request)
    {
        var ma = request.Ma.Trim();
        if (await _db.Set<GiangVien>().AnyAsync(x => x.Ma == ma))
            throw new InvalidOperationException($"Mã giảng viên '{ma}' đã tồn tại trên hệ thống.");

        if (!await _db.Set<BoMon>().AnyAsync(x => x.Id == request.BoMonId))
            throw new KeyNotFoundException($"Không tìm thấy bộ môn với ID {request.BoMonId}.");

        var entity = new GiangVien
        {
            Ma = ma,
            HoTen = request.HoTen.Trim(),
            HocVi = request.HocVi?.Trim(),
            Email = request.Email?.Trim(),
            SoDienThoai = request.SoDienThoai?.Trim(),
            BoMonId = request.BoMonId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(entity.Id))!;
    }

    public async Task<GiangVienDto?> UpdateAsync(int id, GiangVienRequest request)
    {
        var entity = await _db.Set<GiangVien>().FindAsync(id);
        if (entity is null) return null;

        var ma = request.Ma.Trim();
        if (await _db.Set<GiangVien>().AnyAsync(x => x.Ma == ma && x.Id != id))
            throw new InvalidOperationException($"Mã giảng viên '{ma}' đã tồn tại trên hệ thống.");

        if (!await _db.Set<BoMon>().AnyAsync(x => x.Id == request.BoMonId))
            throw new KeyNotFoundException($"Không tìm thấy bộ môn với ID {request.BoMonId}.");

        entity.Ma = ma;
        entity.HoTen = request.HoTen.Trim();
        entity.HocVi = request.HocVi?.Trim();
        entity.Email = request.Email?.Trim();
        entity.SoDienThoai = request.SoDienThoai?.Trim();
        entity.BoMonId = request.BoMonId;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Set<GiangVien>().FindAsync(id);
        if (entity is null) return false;

        _db.Remove(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Không thể xóa giảng viên đang phụ trách cố vấn học tập hoặc có dữ liệu liên quan.");
        }

        return true;
    }
}

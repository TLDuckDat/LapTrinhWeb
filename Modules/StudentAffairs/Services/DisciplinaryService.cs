using Microsoft.EntityFrameworkCore;
using StudentManagement.Data;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.StudentAffairs.Dtos;
using StudentManagement.Modules.StudentAffairs.Entities;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.StudentAffairs.Services;

public class DisciplinaryService(AppDbContext db) : IDisciplinaryService
{
    private readonly AppDbContext _db = db;

    // =================================================================
    // SA-06: KHEN THƯỞNG / KỶ LUẬT
    // =================================================================
    public async Task<KhenThuongKyLuatResponse> CreateKhenThuongKyLuatAsync(KhenThuongKyLuatRequest request)
    {
        // 1. Kiểm tra ID sinh viên > 0
        if (request.SinhVienId <= 0)
            throw new ArgumentException("SinhVienId phải lớn hơn 0.");

        // 2. Kiểm tra nội dung không được để trống
        if (string.IsNullOrWhiteSpace(request.NoiDung))
            throw new ArgumentException("Nội dung không được để trống.");

        // 3. Giới hạn độ dài chuỗi theo đúng cấu hình CSDL để chống crash lỗi 500 (String truncation)
        if (request.NoiDung.Length > 1000)
            throw new ArgumentException("Nội dung quyết định không được vượt quá 1000 ký tự.");

        string? soQuyetDinh = string.IsNullOrWhiteSpace(request.SoQuyetDinh) ? null : request.SoQuyetDinh.Trim();

        if (soQuyetDinh != null && soQuyetDinh.Length > 50)
            throw new ArgumentException("Số quyết định không được vượt quá 50 ký tự.");

        // 4. Kiểm tra loại quyết định phải thuộc Enum (KhenThuong hoặc KyLuat)
        if (!Enum.IsDefined(request.Loai))
            throw new ArgumentException("Loại quyết định không hợp lệ.");

        // 5. Kỷ luật bắt buộc có hình thức; khen thưởng thì không có hình thức
        if (request.Loai == LoaiKhenThuongKyLuat.KyLuat)
        {
            if (request.HinhThuc == null || !Enum.IsDefined(request.HinhThuc.Value))
                throw new ArgumentException(
                    "Quyết định kỷ luật phải có hình thức: KhienTrach, CanhCao, DinhChiHocTap hoặc BuocThoiHoc.");
        }
        else if (request.HinhThuc != null)
        {
            throw new ArgumentException("Quyết định khen thưởng không có hình thức kỷ luật.");
        }

        // 6. Quyết định phải gắn với một học kỳ cụ thể
        if (request.HocKyId <= 0)
            throw new ArgumentException("HocKyId phải lớn hơn 0.");

        var hocKyTonTai = await _db.Set<HocKy>()
            .AnyAsync(x => x.Id == request.HocKyId);

        if (!hocKyTonTai)
            throw new KeyNotFoundException("Không tìm thấy học kỳ.");

        // 7. Số quyết định không được trùng
        if (soQuyetDinh != null)
        {
            var trungSo = await _db.Set<KhenThuongKyLuat>()
                .AnyAsync(x => x.SoQuyetDinh == soQuyetDinh);

            if (trungSo)
                throw new InvalidOperationException($"Số quyết định '{soQuyetDinh}' đã tồn tại.");
        }

        // 8. Kiểm tra sinh viên có tồn tại trong hệ thống không
        var sinhVien = await _db.Set<SinhVien>()
            .FirstOrDefaultAsync(x => x.Id == request.SinhVienId);

        if (sinhVien == null)
            throw new KeyNotFoundException("Không tìm thấy sinh viên.");

        // 9. Tạo quyết định
        var quyetDinh = new KhenThuongKyLuat
        {
            SinhVienId = request.SinhVienId,
            Loai = request.Loai,
            SoQuyetDinh = soQuyetDinh,
            NgayQuyetDinh = request.NgayQuyetDinh,
            NoiDung = request.NoiDung.Trim(),
            HocKyId = request.HocKyId,
            HinhThuc = request.HinhThuc
        };
        _db.Set<KhenThuongKyLuat>().Add(quyetDinh);

        // 10. Đồng bộ trạng thái sinh viên khi bị xử lý nặng
        if (quyetDinh.HinhThuc == HinhThucKyLuat.DinhChiHocTap)
        {
            // Đình chỉ học tập -> chuyển sang Bảo lưu (tạm dừng học)
            if (sinhVien.TrangThai == TrangThaiSinhVien.DangHoc)
                sinhVien.TrangThai = TrangThaiSinhVien.BaoLuu;
        }
        else if (quyetDinh.HinhThuc == HinhThucKyLuat.BuocThoiHoc)
        {
            // Buộc thôi học -> chuyển sang Thôi học
            if (sinhVien.TrangThai != TrangThaiSinhVien.DaTotNghiep)
                sinhVien.TrangThai = TrangThaiSinhVien.ThoiHoc;
        }

        await _db.SaveChangesAsync();

        return new KhenThuongKyLuatResponse
        {
            Id = quyetDinh.Id,
            SinhVienId = quyetDinh.SinhVienId,
            HoTen = sinhVien.HoTen,
            Loai = quyetDinh.Loai.ToString(),
            HinhThuc = quyetDinh.HinhThuc?.ToString(),
            SoQuyetDinh = quyetDinh.SoQuyetDinh,
            NgayQuyetDinh = quyetDinh.NgayQuyetDinh,
            HocKyId = quyetDinh.HocKyId,
            NoiDung = quyetDinh.NoiDung,
            TrangThaiSinhVien = sinhVien.TrangThai.ToString()
        };
    }
}

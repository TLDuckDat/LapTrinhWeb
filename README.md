# Quản lý Sinh viên – StudentManagement

ASP.NET Core Web API (.NET 10) + SQL Server, kiến trúc **Modular Monolith đơn giản**: một project, mỗi module là một thư mục trong `Modules/`.

## 1. Chạy dự án

Yêu cầu: .NET SDK 10, SQL Server (Express/LocalDB đều được), công cụ EF:

```bash
dotnet tool install --global dotnet-ef   # chỉ cần một lần
```

Sửa `appsettings.json` → `ConnectionStrings:DefaultConnection` cho đúng SQL Server trên máy mình
(ví dụ `Server=.\\SQLEXPRESS;...` hoặc `Server=(localdb)\\MSSQLLocalDB;...`), rồi chạy tại thư mục gốc (nơi có `StudentManagement.csproj`):

```bash
dotnet run --launch-profile http
```

Mở Swagger: http://localhost:5235/swagger

Ở môi trường **Development**, khi khởi động ứng dụng sẽ tự chạy migration và seed dữ liệu mẫu (`Data/DbSeeder.cs`).

Đăng nhập: `POST /api/auth/login` với `{ "tenDangNhap": "admin", "matKhau": "Admin@123" }`, copy `data.token`,
bấm nút **Authorize** trên Swagger và dán token vào. Mọi API khác đều yêu cầu token.

> Nếu khởi động báo lỗi migration kiểu `Cannot find the object "HocPhi"`: CSDL trên máy đã được migrate bằng migration cũ
> không còn trong code. Xóa CSDL dev và chạy lại: `dotnet ef database drop -f` rồi `dotnet run --launch-profile http`.

## 2. Migration

```bash
# Tạo migration mới sau khi thêm/sửa Entity hoặc Configuration
dotnet ef migrations add <TenMigration> -o Data/Migrations

# Áp dụng vào CSDL (hoặc chỉ cần chạy app ở Development)
dotnet ef database update

# Xóa migration cuối nếu chưa áp dụng
dotnet ef migrations remove
```

> Đặt tên migration theo task, ví dụ `REG03_ThemCotGhiChu`. Trước khi tạo migration hãy `git pull` để tránh hai người cùng sửa model snapshot.

## 3. Module → Epic Jira → Thực thể

| Module | Epic | Route chính | Thực thể |
|---|---|---|---|
| Auth | AUTH | `/api/auth` | TaiKhoan, enum VaiTro |
| Academic | ACA | `/api/khoa`, `/api/bo-mon`, ... | Khoa (**mẫu CRUD**), BoMon, Nganh, ChuyenNganh, NamHoc, HocKy, ChuongTrinhDaoTao |
| Students | STU | `/api/sinh-vien`, `/api/giang-vien`, `/api/lop-hanh-chinh` | SinhVien, GiangVien, LopHanhChinh, enum TrangThaiSinhVien |
| Courses | CRS | `/api/mon-hoc`, `/api/phong-hoc`, `/api/lop-hoc-phan` | MonHoc, MonHocTienQuyet, PhongHoc, LopHocPhan, LichHoc, enum TrangThaiLopHocPhan |
| Registration | REG | `/api/dang-ky-hoc-phan`, `/api/dot-dang-ky` | DotDangKy, DangKyHocPhan, enum TrangThaiDotDangKy, LoaiDangKy, TrangThaiDangKy |
| Grades | GRD | `/api/diem`, `/api/phuc-khao` | DiemHocPhan, PhucKhao, enum TrangThaiPhucKhao |
| StudentAffairs | SA | `/api/cong-tac-sinh-vien` | DiemRenLuyen, LichSuSuaDiemRenLuyen, HocPhi, GiaoDichHocPhi, MienGiam, HocBong, KhenThuongKyLuat, enum LoaiKhenThuongKyLuat, HinhThucKyLuat, PhuongThucThanhToan |
| Graduation | GRAD | `/api/tot-nghiep` | ThucTap, DeTaiKhoaLuan, HoiDongBaoVe, XetTotNghiep, enum KetQuaXetTotNghiep |
| Reports | RPT | `/api/bao-cao`, `/api/thong-bao` | ThongBao (báo cáo không có bảng riêng) |

Các phương thức chưa làm đều `throw new NotImplementedException("TODO")` kèm comment mã task, ví dụ `// REG-03 Đăng ký học phần`.
Tìm toàn bộ việc còn lại: tìm kiếm chuỗi `NotImplementedException` trong solution.

## 4. Cấu trúc một module

```
Modules/<Module>/
├── Entities/         Lớp thực thể (kế thừa BaseEntity) và enum
├── Configurations/   IEntityTypeConfiguration<T> – bảng, độ dài, index, khóa ngoại
├── Dtos/             Request/Response record
├── Services/         IXxxService + XxxService (dùng AppDbContext trực tiếp)
├── Controllers/      Chỉ gọi Service, trả về ApiResponse<T>
└── <Module>Module.cs AddXxxModule(this IServiceCollection) – đăng ký DI
```

### Quy ước
- Controller **chỉ** gọi Service; Service dùng `AppDbContext` trực tiếp (`_db.Set<T>()`), không có Repository.
- Route: `/api/<tai-nguyen>` (chữ thường, gạch nối).
- Enum lưu dạng chuỗi: `.HasConversion<string>().HasMaxLength(30)`.
- Thực thể đặt tên tiếng Việt không dấu; lớp kỹ thuật tiếng Anh (`KhoaService`, `KhoaDto`...).
- Tham chiếu thực thể **module khác chỉ bằng Id**, không có navigation property. Khóa ngoại vẫn tạo trong CSDL:
  `builder.HasOne<SinhVien>().WithMany().HasForeignKey(x => x.SinhVienId).OnDelete(DeleteBehavior.Restrict);`
- Mọi phản hồi bọc trong `ApiResponse<T>` (`Success`, `Message`, `Data`); danh sách dùng `PagedQuery` + `PagedResult<T>`.

## 5. Thêm một chức năng mới theo mẫu Khoa

Ví dụ làm CRUD cho **BoMon** (task ACA-02):

1. **Entity** – đã có trong `Modules/Academic/Entities/BoMon.cs`. Nếu thêm cột: sửa entity + configuration.
2. **DTO** – tạo `Dtos/BoMonDtos.cs` theo `Dtos/KhoaDtos.cs`: `BoMonDto` (trả về) và `BoMonRequest` (tạo/sửa, có `[Required]`, `[MaxLength]`).
3. **Service** – tạo `IBoMonService` + `BoMonService` theo `KhoaService`:
   - `GetPagedAsync(PagedQuery)` → `AsNoTracking`, lọc `Search`, `Skip/Take`, `Select` sang DTO.
   - `GetByIdAsync`, `CreateAsync` (kiểm tra trùng mã), `UpdateAsync`, `DeleteAsync` (bắt `DbUpdateException` khi bị Restrict).
4. **Controller** – tạo `BoMonController` với `[Route("api/bo-mon")]` theo `KhoaController`: 404 khi không thấy, 409 khi trùng/ràng buộc, 201 khi tạo.
5. **Đăng ký DI** – thêm `services.AddScoped<IBoMonService, BoMonService>();` vào `AcademicModule.cs`. **Không sửa Program.cs.**
6. Xóa phương thức stub tương ứng trong `AcademicService` / `AcademicController` (nếu đã thay thế).
7. Nếu đổi model: `dotnet ef migrations add ACA02_...`, chạy app, test trên Swagger.

## 6. Quy tắc làm nhóm

- Mỗi người làm trong **thư mục module của mình** (`Modules/<Module>/`). Không sửa file module khác; cần thay đổi chung (`Common/`, `Data/`, `Program.cs`) thì báo nhóm trước.
- Cần dữ liệu module khác: truy vấn qua `_db.Set<T>()` theo Id, không thêm navigation sang module khác.
- Branch đặt theo mã Jira: `feature/REG-03`, sửa lỗi: `bugfix/GRD-02`.
- Commit message bắt đầu bằng mã task: `REG-03 Kiểm tra trùng lịch khi đăng ký`.
- Tạo Pull Request vào `develop`, cần ít nhất 1 người review; `dotnet build` phải thành công.
- Migration: mỗi PR tối đa một migration, rebase lên `develop` mới nhất trước khi tạo.

## 7. Module StudentAffairs (Công tác sinh viên)

6 chức năng: điểm rèn luyện, tính học phí, nộp học phí, miễn giảm, xét học bổng, khen thưởng / kỷ luật.

### 7.1. Dữ liệu test

Ở Development, `StudentAffairsSeeder` tự thêm dữ liệu test sau `DbSeeder` (8 sinh viên TEST001..TEST008, 5 môn TST01..TST05,
đăng ký + điểm Học kỳ 1, điểm rèn luyện, và một học kỳ "HK test - cổng ĐRL mở" đang mở cổng nhập điểm).
Không cần chạy SQL tay. Muốn làm lại từ đầu: `dotnet ef database drop -f` rồi chạy app.

### 7.2. Thông số nghiệp vụ

Giá trị mặc định nằm trong `StudentAffairsSettings.cs` (đơn giá tín chỉ, số ngày hạn nộp học phí, số ngày mở cổng nhập
điểm rèn luyện, điều kiện và mức tiền học bổng, % chỉ tiêu mỗi ngành). Muốn đổi thông số nào thì thêm đúng thông số đó
vào `appsettings.json`, ví dụ `"StudentAffairs": { "DonGiaTinChi": 700000 }`. Đặt số ngày = 0 nghĩa là không giới hạn.

Hạn nộp học phí = ngày bắt đầu học kỳ + `SoNgayHanNopHocPhi` (mặc định 30). Quá hạn mà còn nợ thì `DaQuaHan = true`.

### 7.3. Danh sách API (`/api/cong-tac-sinh-vien/...`)

Các API ghi dữ liệu yêu cầu vai trò Admin hoặc PhongDaoTao (nhập / sửa điểm rèn luyện thêm GiangVien).

| Nhóm | Phương thức & đường dẫn | Ghi chú |
|---|---|---|
| Điểm rèn luyện | `GET diem-ren-luyen?sinhVienId=&hocKyId=` | |
| | `POST diem-ren-luyen` | Chỉ trong thời hạn mở cổng (Admin được bỏ qua) |
| | `PUT diem-ren-luyen/{id}` | Body `{ diem, lyDoSua }`, tự lưu người sửa + ngày sửa |
| | `GET diem-ren-luyen/{id}/lich-su-sua` | Lịch sử sửa điểm |
| | `POST diem-ren-luyen/khoa-so/{hocKyId}` | Khóa sổ cả kỳ |
| | `POST diem-ren-luyen/mo-khoa-so/{hocKyId}` | Mở khóa (chỉ Admin) |
| Học phí | `POST hoc-phi/tinh/{hocKyId}` | Tính cả kỳ. Ai đã nộp nhiều hơn học phí mới thì có `soTienThua` |
| | `POST hoc-phi/tinh/{hocKyId}/sinh-vien/{sinhVienId}` | Tính lại cho 1 sinh viên |
| | `GET hoc-phi?sinhVienId=&hocKyId=` | Có SoTienDaNop, SoTienConNo, HanNop, DaQuaHan |
| | `PUT hoc-phi/{id}/thanh-toan` | Body `{ soTien, phuongThuc (0=TienMat, 1=ChuyenKhoan), maGiaoDich, ngayThanhToan }` — nộp từng đợt |
| | `GET hoc-phi/{id}/giao-dich` | Lịch sử giao dịch |
| | `GET hoc-phi/kiem-tra-no/{sinhVienId}` | Có nợ quá hạn? Được đăng ký học phần? Đủ điều kiện tốt nghiệp về học phí? |
| Miễn giảm | `GET / POST mien-giam`, `PUT / DELETE mien-giam/{id}` | Tổng ≤ 100%, tự tính lại hóa đơn nếu kỳ đã tính học phí |
| Học bổng | `POST hoc-bong/xet/{hocKyId}` | Trả về cả danh sách được cấp và danh sách không đạt (kèm lý do) |
| | `GET hoc-bong?hocKyId=&sinhVienId=` | Tra cứu người đã nhận học bổng |
| Khen thưởng / kỷ luật | `POST khen-thuong-ky-luat` | Bắt buộc `hocKyId`; kỷ luật phải có `hinhThuc` (0=KhienTrach, 1=CanhCao, 2=DinhChiHocTap, 3=BuocThoiHoc). Đình chỉ → sinh viên Bảo lưu, buộc thôi học → Thôi học |

Kỷ luật trong học kỳ nào thì giới hạn điểm rèn luyện và loại khỏi xét học bổng của học kỳ đó.

### 7.4. Dùng cho module khác (Registration, Graduation)

`ITuitionsService` có sẵn 2 hàm kiểm tra, chỉ cần inject và gọi:

```csharp
// REG-03 Đăng ký học phần: chặn sinh viên còn nợ học phí quá hạn
if (await _tuitionsService.CoNoQuaHanAsync(request.SinhVienId))
    throw new InvalidOperationException("Sinh viên còn nợ học phí quá hạn, không được đăng ký học phần.");

// GRAD Xét tốt nghiệp: loại sinh viên chưa đóng đủ 100% học phí tích lũy
if (!await _tuitionsService.DaHoanThanhHocPhiAsync(sinhVienId))
    // loại khỏi danh sách xét tốt nghiệp
```

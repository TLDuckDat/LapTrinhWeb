using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentManagement.Modules.Academic.Entities;
using StudentManagement.Modules.StudentAffairs.Entities;
using StudentManagement.Modules.Students.Entities;

namespace StudentManagement.Modules.StudentAffairs.Configurations;

/// <summary>Cấu hình EF cho KhenThuongKyLuat.</summary>
public class KhenThuongKyLuatConfiguration : IEntityTypeConfiguration<KhenThuongKyLuat>
{
    public void Configure(EntityTypeBuilder<KhenThuongKyLuat> builder)
    {
        builder.ToTable("KhenThuongKyLuat");
        builder.Property(x => x.Loai).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.HinhThuc).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.SoQuyetDinh).HasMaxLength(50);
        builder.Property(x => x.NoiDung).HasMaxLength(1000).IsRequired();
        // Số quyết định không được trùng (bỏ qua các dòng để trống)
        builder.HasIndex(x => x.SoQuyetDinh).IsUnique().HasFilter("[SoQuyetDinh] IS NOT NULL");
        builder.HasOne<SinhVien>().WithMany().HasForeignKey(x => x.SinhVienId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<HocKy>().WithMany().HasForeignKey(x => x.HocKyId).OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentManagement.Modules.StudentAffairs.Entities;

namespace StudentManagement.Modules.StudentAffairs.Configurations;

/// <summary>Cấu hình EF cho GiaoDichHocPhi.</summary>
public class GiaoDichHocPhiConfiguration : IEntityTypeConfiguration<GiaoDichHocPhi>
{
    public void Configure(EntityTypeBuilder<GiaoDichHocPhi> builder)
    {
        builder.ToTable("GiaoDichHocPhi");
        builder.Property(x => x.SoTien).HasPrecision(18, 2);
        builder.Property(x => x.PhuongThuc).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.MaGiaoDich).HasMaxLength(50);
        builder.Property(x => x.GhiChu).HasMaxLength(500);
        // Mã giao dịch không được trùng (bỏ qua các dòng không có mã)
        builder.HasIndex(x => x.MaGiaoDich).IsUnique().HasFilter("[MaGiaoDich] IS NOT NULL");
        builder.HasOne<HocPhi>().WithMany().HasForeignKey(x => x.HocPhiId).OnDelete(DeleteBehavior.Restrict);
    }
}

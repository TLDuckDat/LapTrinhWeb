using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentManagement.Modules.StudentAffairs.Entities;

namespace StudentManagement.Modules.StudentAffairs.Configurations;

/// <summary>Cấu hình EF cho LichSuSuaDiemRenLuyen.</summary>
public class LichSuSuaDiemRenLuyenConfiguration : IEntityTypeConfiguration<LichSuSuaDiemRenLuyen>
{
    public void Configure(EntityTypeBuilder<LichSuSuaDiemRenLuyen> builder)
    {
        builder.ToTable("LichSuSuaDiemRenLuyen");
        builder.Property(x => x.LyDo).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TenNguoiSua).HasMaxLength(200);
        builder.HasOne<DiemRenLuyen>().WithMany().HasForeignKey(x => x.DiemRenLuyenId).OnDelete(DeleteBehavior.Restrict);
    }
}

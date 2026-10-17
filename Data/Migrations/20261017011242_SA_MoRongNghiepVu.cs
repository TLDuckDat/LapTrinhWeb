using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class SA_MoRongNghiepVu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HinhThuc",
                table: "KhenThuongKyLuat",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HocKyId",
                table: "KhenThuongKyLuat",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SoTienDaNop",
                table: "HocPhi",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiemRenLuyen",
                table: "HocBong",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiemTrungBinh",
                table: "HocBong",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MucHocBong",
                table: "HocBong",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DaKhoa",
                table: "DiemRenLuyen",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GiaoDichHocPhi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HocPhiId = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NgayThanhToan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhuongThuc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MaGiaoDich = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaoDichHocPhi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GiaoDichHocPhi_HocPhi_HocPhiId",
                        column: x => x.HocPhiId,
                        principalTable: "HocPhi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuSuaDiemRenLuyen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiemRenLuyenId = table.Column<int>(type: "int", nullable: false),
                    DiemCu = table.Column<int>(type: "int", nullable: false),
                    DiemMoi = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NguoiSuaId = table.Column<int>(type: "int", nullable: true),
                    TenNguoiSua = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgaySua = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuSuaDiemRenLuyen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuSuaDiemRenLuyen_DiemRenLuyen_DiemRenLuyenId",
                        column: x => x.DiemRenLuyenId,
                        principalTable: "DiemRenLuyen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KhenThuongKyLuat_HocKyId",
                table: "KhenThuongKyLuat",
                column: "HocKyId");

            migrationBuilder.CreateIndex(
                name: "IX_KhenThuongKyLuat_SoQuyetDinh",
                table: "KhenThuongKyLuat",
                column: "SoQuyetDinh",
                unique: true,
                filter: "[SoQuyetDinh] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichHocPhi_HocPhiId",
                table: "GiaoDichHocPhi",
                column: "HocPhiId");

            migrationBuilder.CreateIndex(
                name: "IX_GiaoDichHocPhi_MaGiaoDich",
                table: "GiaoDichHocPhi",
                column: "MaGiaoDich",
                unique: true,
                filter: "[MaGiaoDich] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuSuaDiemRenLuyen_DiemRenLuyenId",
                table: "LichSuSuaDiemRenLuyen",
                column: "DiemRenLuyenId");

            migrationBuilder.AddForeignKey(
                name: "FK_KhenThuongKyLuat_HocKy_HocKyId",
                table: "KhenThuongKyLuat",
                column: "HocKyId",
                principalTable: "HocKy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KhenThuongKyLuat_HocKy_HocKyId",
                table: "KhenThuongKyLuat");

            migrationBuilder.DropTable(
                name: "GiaoDichHocPhi");

            migrationBuilder.DropTable(
                name: "LichSuSuaDiemRenLuyen");

            migrationBuilder.DropIndex(
                name: "IX_KhenThuongKyLuat_HocKyId",
                table: "KhenThuongKyLuat");

            migrationBuilder.DropIndex(
                name: "IX_KhenThuongKyLuat_SoQuyetDinh",
                table: "KhenThuongKyLuat");

            migrationBuilder.DropColumn(
                name: "HinhThuc",
                table: "KhenThuongKyLuat");

            migrationBuilder.DropColumn(
                name: "HocKyId",
                table: "KhenThuongKyLuat");

            migrationBuilder.DropColumn(
                name: "SoTienDaNop",
                table: "HocPhi");

            migrationBuilder.DropColumn(
                name: "DiemRenLuyen",
                table: "HocBong");

            migrationBuilder.DropColumn(
                name: "DiemTrungBinh",
                table: "HocBong");

            migrationBuilder.DropColumn(
                name: "MucHocBong",
                table: "HocBong");

            migrationBuilder.DropColumn(
                name: "DaKhoa",
                table: "DiemRenLuyen");
        }
    }
}

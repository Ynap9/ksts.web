using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ThemLoKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoKy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NguoiTaoId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenDuAn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Thumbprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaiToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    TongSo = table.Column<int>(type: "int", nullable: false),
                    DaXong = table.Column<int>(type: "int", nullable: false),
                    SoLoi = table.Column<int>(type: "int", nullable: false),
                    LoiChung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThoiDiemBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiDiemXong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TienToKho = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DaBaoDuAnKyXong = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoKy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoKyFile",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoKyId = table.Column<int>(type: "int", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    TenFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ObjectKeyNguon = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ObjectKeyDaKy = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    LyDoLoi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThoiGianKy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DauThoiGian = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoKyFile", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoKyFile_LoKyId_TrangThai_ThuTu",
                table: "LoKyFile",
                columns: new[] { "LoKyId", "TrangThai", "ThuTu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoKy");

            migrationBuilder.DropTable(
                name: "LoKyFile");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoKy",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUser = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Thumbprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    TongSo = table.Column<int>(type: "int", nullable: false),
                    DaXong = table.Column<int>(type: "int", nullable: false),
                    SoLoi = table.Column<int>(type: "int", nullable: false),
                    LoiChung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThoiDiemBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiDiemXong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoKy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoKyFile",
                schema: "core",
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
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoKyFile", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoKyFile_LoKy_LoKyId",
                        column: x => x.LoKyId,
                        principalSchema: "core",
                        principalTable: "LoKy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoKy_IdUser_Deleted",
                schema: "core",
                table: "LoKy",
                columns: new[] { "IdUser", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LoKyFile_LoKyId_TenFile",
                schema: "core",
                table: "LoKyFile",
                columns: new[] { "LoKyId", "TenFile" });

            migrationBuilder.CreateIndex(
                name: "IX_LoKyFile_LoKyId_TrangThai_ThuTu",
                schema: "core",
                table: "LoKyFile",
                columns: new[] { "LoKyId", "TrangThai", "ThuTu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoKyFile",
                schema: "core");

            migrationBuilder.DropTable(
                name: "LoKy",
                schema: "core");
        }
    }
}

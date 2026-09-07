using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Template",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTemplate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Thumbprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenChungThu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LyDoKy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NoiKy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnhDauDoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnhDauDoObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnhChuKyTuoiUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnhChuKyTuoiObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HienThiChuKySo = table.Column<bool>(type: "bit", nullable: false),
                    NhoiChuKySoVaoAnh = table.Column<bool>(type: "bit", nullable: false),
                    KyDe = table.Column<bool>(type: "bit", nullable: false),
                    DoDamDauDo = table.Column<int>(type: "int", nullable: false, defaultValue: 140),
                    DoDamChuKyTuoi = table.Column<int>(type: "int", nullable: false, defaultValue: 140),
                    DoDayNetChuKyTuoi = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    MauChuKySo = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false, defaultValue: "#000000"),
                    MauChuKyTuoi = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
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
                    table.PrimaryKey("PK_Template", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplatePosition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    XRatio = table.Column<double>(type: "float", nullable: false),
                    YRatio = table.Column<double>(type: "float", nullable: false),
                    WidthRatio = table.Column<double>(type: "float", nullable: false),
                    HeightRatio = table.Column<double>(type: "float", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatePosition", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Template");

            migrationBuilder.DropTable(
                name: "TemplatePosition");
        }
    }
}

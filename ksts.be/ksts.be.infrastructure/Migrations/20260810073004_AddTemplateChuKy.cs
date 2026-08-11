using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTemplateChuKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Template",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUser = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TenTemplate = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Thumbprint = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenChungThu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LyDoKy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NoiKy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AnhDauDoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnhDauDoObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnhChuKyTuoiUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnhChuKyTuoiObjectKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                schema: "core",
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
                    HeightRatio = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplatePosition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplatePosition_Template_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "core",
                        principalTable: "Template",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Template_IdUser_Deleted",
                schema: "core",
                table: "Template",
                columns: new[] { "IdUser", "Deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Template_IdUser_TenTemplate",
                schema: "core",
                table: "Template",
                columns: new[] { "IdUser", "TenTemplate" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplatePosition_TemplateId",
                schema: "core",
                table: "TemplatePosition",
                column: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplatePosition",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Template",
                schema: "core");
        }
    }
}

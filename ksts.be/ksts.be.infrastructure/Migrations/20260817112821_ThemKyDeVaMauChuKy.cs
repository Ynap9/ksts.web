using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ThemKyDeVaMauChuKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "KyDe",
                schema: "core",
                table: "Template",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MauChuKySo",
                schema: "core",
                table: "Template",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#000000");

            migrationBuilder.AddColumn<string>(
                name: "MauChuKyTuoi",
                schema: "core",
                table: "Template",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#000000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KyDe",
                schema: "core",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "MauChuKySo",
                schema: "core",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "MauChuKyTuoi",
                schema: "core",
                table: "Template");
        }
    }
}

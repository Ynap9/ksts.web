using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoDamAnhTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoDamChuKyTuoi",
                schema: "core",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.AddColumn<int>(
                name: "DoDamDauDo",
                schema: "core",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoDamChuKyTuoi",
                schema: "core",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "DoDamDauDo",
                schema: "core",
                table: "Template");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTuyChonChuKyTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HienThiChuKySo",
                schema: "core",
                table: "Template",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NhoiChuKySoVaoAnh",
                schema: "core",
                table: "Template",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HienThiChuKySo",
                schema: "core",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "NhoiChuKySoVaoAnh",
                schema: "core",
                table: "Template");
        }
    }
}

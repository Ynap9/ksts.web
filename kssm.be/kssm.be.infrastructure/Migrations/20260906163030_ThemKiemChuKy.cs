using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ThemKiemChuKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ChuKyHopLe",
                table: "LoKyFile",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoChuKy",
                table: "LoKyFile",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChuKyHopLe",
                table: "LoKyFile");

            migrationBuilder.DropColumn(
                name: "LyDoChuKy",
                table: "LoKyFile");
        }
    }
}

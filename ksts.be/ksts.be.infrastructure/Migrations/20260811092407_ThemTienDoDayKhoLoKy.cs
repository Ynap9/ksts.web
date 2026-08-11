using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ThemTienDoDayKhoLoKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DaDayLenKho",
                schema: "core",
                table: "LoKy",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DangDayLenKho",
                schema: "core",
                table: "LoKy",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HoanTatDayLenKho",
                schema: "core",
                table: "LoKy",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LoiDayLenKho",
                schema: "core",
                table: "LoKy",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoLoiDayLenKho",
                schema: "core",
                table: "LoKy",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TienToKho",
                schema: "core",
                table: "LoKy",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaDayLenKho",
                schema: "core",
                table: "LoKy");

            migrationBuilder.DropColumn(
                name: "DangDayLenKho",
                schema: "core",
                table: "LoKy");

            migrationBuilder.DropColumn(
                name: "HoanTatDayLenKho",
                schema: "core",
                table: "LoKy");

            migrationBuilder.DropColumn(
                name: "LoiDayLenKho",
                schema: "core",
                table: "LoKy");

            migrationBuilder.DropColumn(
                name: "SoLoiDayLenKho",
                schema: "core",
                table: "LoKy");

            migrationBuilder.DropColumn(
                name: "TienToKho",
                schema: "core",
                table: "LoKy");
        }
    }
}

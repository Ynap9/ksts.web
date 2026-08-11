using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DoiMacDinhDoDamAnh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DoDamDauDo",
                schema: "core",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 140,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);

            migrationBuilder.AlterColumn<int>(
                name: "DoDamChuKyTuoi",
                schema: "core",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 140,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DoDamDauDo",
                schema: "core",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 140);

            migrationBuilder.AlterColumn<int>(
                name: "DoDamChuKyTuoi",
                schema: "core",
                table: "Template",
                type: "int",
                nullable: false,
                defaultValue: 100,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 140);
        }
    }
}

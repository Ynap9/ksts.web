using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Dong_Goi_Config_8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PackageDone",
                table: "PackageSession",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PackageFailReason",
                table: "PackageSession",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageStatus",
                table: "PackageSession",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PackageTotal",
                table: "PackageSession",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageDone",
                table: "PackageSession");

            migrationBuilder.DropColumn(
                name: "PackageFailReason",
                table: "PackageSession");

            migrationBuilder.DropColumn(
                name: "PackageStatus",
                table: "PackageSession");

            migrationBuilder.DropColumn(
                name: "PackageTotal",
                table: "PackageSession");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Dong_Goi_Config_6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ObjectKeyDaKy",
                table: "LoKyFile");

            migrationBuilder.DropColumn(
                name: "TaiToken",
                table: "LoKy");

            migrationBuilder.DropColumn(
                name: "TienToKho",
                table: "LoKy");

            migrationBuilder.AddColumn<string>(
                name: "DriveFileId",
                table: "LoKyFile",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveFolderId",
                table: "LoKy",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThuMucDaKy",
                table: "LoKy",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriveFileId",
                table: "LoKyFile");

            migrationBuilder.DropColumn(
                name: "DriveFolderId",
                table: "LoKy");

            migrationBuilder.DropColumn(
                name: "ThuMucDaKy",
                table: "LoKy");

            migrationBuilder.AddColumn<string>(
                name: "ObjectKeyDaKy",
                table: "LoKyFile",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaiToken",
                table: "LoKy",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TienToKho",
                table: "LoKy",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}

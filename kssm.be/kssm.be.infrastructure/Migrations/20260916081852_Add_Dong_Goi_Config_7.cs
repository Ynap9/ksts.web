using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Dong_Goi_Config_7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageObjectKey",
                table: "PackageDossier");

            migrationBuilder.AddColumn<string>(
                name: "DriveFolderId",
                table: "PackageSession",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriveFolderName",
                table: "PackageSession",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PackagedDate",
                table: "PackageSession",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageDriveFileId",
                table: "PackageDossier",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriveFolderId",
                table: "PackageSession");

            migrationBuilder.DropColumn(
                name: "DriveFolderName",
                table: "PackageSession");

            migrationBuilder.DropColumn(
                name: "PackagedDate",
                table: "PackageSession");

            migrationBuilder.DropColumn(
                name: "PackageDriveFileId",
                table: "PackageDossier");

            migrationBuilder.AddColumn<string>(
                name: "PackageObjectKey",
                table: "PackageDossier",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}

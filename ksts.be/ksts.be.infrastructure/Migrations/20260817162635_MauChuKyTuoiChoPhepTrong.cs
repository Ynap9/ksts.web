using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <summary>
    /// Lets the wet-ink colour be empty, which now means "no colour picked, keep the original ink". Black
    /// used to carry that meaning, so rows already storing black are moved to NULL: they were saved when
    /// black meant "leave the image alone", and under the new rule black would dye the signature black.
    /// </summary>
    public partial class MauChuKyTuoiChoPhepTrong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MauChuKyTuoi",
                schema: "core",
                table: "Template",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldDefaultValue: "#000000");

            migrationBuilder.Sql(
                "UPDATE [core].[Template] SET [MauChuKyTuoi] = NULL WHERE [MauChuKyTuoi] = '#000000';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [core].[Template] SET [MauChuKyTuoi] = '#000000' WHERE [MauChuKyTuoi] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "MauChuKyTuoi",
                schema: "core",
                table: "Template",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#000000",
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldNullable: true);
        }
    }
}

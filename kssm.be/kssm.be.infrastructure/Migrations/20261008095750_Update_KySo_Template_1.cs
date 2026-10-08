using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_KySo_Template_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CoChuKySo",
                table: "Template",
                type: "float",
                nullable: false,
                defaultValue: 10.0);

            migrationBuilder.AddColumn<string>(
                name: "FontChuKySo",
                table: "Template",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Times New Roman");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoChuKySo",
                table: "Template");

            migrationBuilder.DropColumn(
                name: "FontChuKySo",
                table: "Template");
        }
    }
}

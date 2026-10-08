using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Update_KySo_Template : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Template",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Template");
        }
    }
}

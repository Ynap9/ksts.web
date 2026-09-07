using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_document_id : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentId",
                table: "LoKyFile",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "LoKyFile");
        }
    }
}

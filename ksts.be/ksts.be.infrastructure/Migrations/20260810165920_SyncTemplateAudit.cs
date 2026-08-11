using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ksts.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncTemplateAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "TemplatePosition",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                schema: "core",
                table: "TemplatePosition",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "getdate()");

            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                schema: "core",
                table: "TemplatePosition",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "core",
                table: "TemplatePosition",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                schema: "core",
                table: "TemplatePosition",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "core",
                table: "TemplatePosition",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                schema: "core",
                table: "TemplatePosition",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "core",
                table: "TemplatePosition");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                schema: "core",
                table: "TemplatePosition");

            migrationBuilder.DropColumn(
                name: "Deleted",
                schema: "core",
                table: "TemplatePosition");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "core",
                table: "TemplatePosition");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                schema: "core",
                table: "TemplatePosition");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "core",
                table: "TemplatePosition");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                schema: "core",
                table: "TemplatePosition");
        }
    }
}

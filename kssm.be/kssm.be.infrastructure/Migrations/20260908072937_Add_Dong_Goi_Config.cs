using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace kssm.be.infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Add_Dong_Goi_Config : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeValue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetadataField",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    FieldLength = table.Column<int>(type: "int", nullable: true),
                    CodeGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataField", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetadataFieldAlias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetadataFieldId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataFieldAlias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageFieldConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackageVariantId = table.Column<int>(type: "int", nullable: false),
                    MetadataFieldId = table.Column<int>(type: "int", nullable: false),
                    EadElement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Requirement = table.Column<int>(type: "int", nullable: false),
                    ConditionField = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConditionValues = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequiredByOrg = table.Column<bool>(type: "bit", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DescriptionOverride = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DataTypeOverride = table.Column<int>(type: "int", nullable: true),
                    FieldLengthOverride = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageFieldConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageVariant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariantKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PackageType = table.Column<int>(type: "int", nullable: false),
                    ObjectType = table.Column<int>(type: "int", nullable: false),
                    SchemaFile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "getdate()"),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageVariant", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeValue_CodeGroup_Code",
                table: "CodeValue",
                columns: new[] { "CodeGroup", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataField_FieldKey",
                table: "MetadataField",
                column: "FieldKey");

            migrationBuilder.CreateIndex(
                name: "IX_MetadataFieldAlias_MetadataFieldId",
                table: "MetadataFieldAlias",
                column: "MetadataFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageFieldConfig_PackageVariantId_MetadataFieldId",
                table: "PackageFieldConfig",
                columns: new[] { "PackageVariantId", "MetadataFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_PackageVariant_VariantKey",
                table: "PackageVariant",
                column: "VariantKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeValue");

            migrationBuilder.DropTable(
                name: "MetadataField");

            migrationBuilder.DropTable(
                name: "MetadataFieldAlias");

            migrationBuilder.DropTable(
                name: "PackageFieldConfig");

            migrationBuilder.DropTable(
                name: "PackageVariant");
        }
    }
}

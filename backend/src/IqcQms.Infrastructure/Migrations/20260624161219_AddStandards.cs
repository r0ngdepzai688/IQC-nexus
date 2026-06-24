using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStandards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DynamicForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InspectionStandardId = table.Column<int>(type: "INTEGER", nullable: false),
                    FormConfigJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicForms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartCode = table.Column<string>(type: "TEXT", nullable: false),
                    PartName = table.Column<string>(type: "TEXT", nullable: false),
                    SupplierName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InspectionStandards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PartId = table.Column<int>(type: "INTEGER", nullable: false),
                    VersionNumber = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DocumentUrl = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionStandards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionStandards_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InspectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InspectionStandardId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", nullable: false),
                    Specification = table.Column<string>(type: "TEXT", nullable: false),
                    TolerancePlus = table.Column<double>(type: "REAL", nullable: false),
                    ToleranceMinus = table.Column<double>(type: "REAL", nullable: false),
                    ToolRequiredId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InspectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InspectionItems_InspectionStandards_InspectionStandardId",
                        column: x => x.InspectionStandardId,
                        principalTable: "InspectionStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionItems_InspectionStandardId",
                table: "InspectionItems",
                column: "InspectionStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_InspectionStandards_PartId",
                table: "InspectionStandards",
                column: "PartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DynamicForms");

            migrationBuilder.DropTable(
                name: "InspectionItems");

            migrationBuilder.DropTable(
                name: "InspectionStandards");

            migrationBuilder.DropTable(
                name: "Parts");
        }
    }
}

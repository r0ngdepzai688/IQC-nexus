using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNewModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterPlanRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UploadId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false),
                    Basic = table.Column<string>(type: "TEXT", nullable: false),
                    AreaRegion = table.Column<string>(type: "TEXT", nullable: false),
                    Grade = table.Column<string>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    QtyLpr = table.Column<int>(type: "INTEGER", nullable: false),
                    QtyLsr = table.Column<int>(type: "INTEGER", nullable: false),
                    PvrTargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PraTargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SraTargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PicIqc = table.Column<string>(type: "TEXT", nullable: false),
                    ActionStatus = table.Column<string>(type: "TEXT", nullable: false),
                    IsActivated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterPlanRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterPlanUploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UploadedBy = table.Column<string>(type: "TEXT", nullable: false),
                    TotalRecordsParsed = table.Column<int>(type: "INTEGER", nullable: false),
                    DeltaSummary = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterPlanUploads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectWorkspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceRecordId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false),
                    Sku = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    ActivatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CompletionPercentage = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectWorkspaces", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterPlanRecords");

            migrationBuilder.DropTable(
                name: "MasterPlanUploads");

            migrationBuilder.DropTable(
                name: "ProjectWorkspaces");
        }
    }
}

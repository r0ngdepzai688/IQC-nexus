using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdaptiveMasterPlanUpsert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BasicKey",
                table: "Staging_MasterPlan",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cat",
                table: "Staging_MasterPlan",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CatKey",
                table: "Staging_MasterPlan",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "MainLprLqvDate",
                table: "Staging_MasterPlan",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MainLsrDate",
                table: "Staging_MasterPlan",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetMasterPlanId",
                table: "Staging_MasterPlan",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TargetVersion",
                table: "Staging_MasterPlan",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BasicKey",
                table: "MasterPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cat",
                table: "MasterPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CatKey",
                table: "MasterPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "MainLprLqvDate",
                table: "MasterPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MainLsrDate",
                table: "MasterPlans",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "MasterPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "NoChangeRecords",
                table: "ImportBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HeaderMappingProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NormalizedHeaderPath = table.Column<string>(type: "TEXT", nullable: false),
                    CanonicalField = table.Column<string>(type: "TEXT", nullable: false),
                    DetectedDataType = table.Column<string>(type: "TEXT", nullable: false),
                    WorkbookFingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    ConfirmationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Confidence = table.Column<double>(type: "REAL", nullable: false),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeaderMappingProfiles", x => x.Id);
                });

            // The legacy schema has no trustworthy Cat source. Deliberately block this migration
            // when legacy rows exist instead of inferring Cat from SKU or leaving rows unkeyed.
            // Owners must classify/remove those rows in a reviewed preparatory data migration,
            // then rerun this schema migration. A clean database uses only the shared C#
            // MasterPlanBusinessKey normalizer for every persisted key.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE "__MasterPlanLegacyClassificationCheck"
                (
                    "UnclassifiedCount" INTEGER NOT NULL
                        CONSTRAINT "Legacy MasterPlan rows require approved Cat classification before adaptive upsert migration"
                        CHECK ("UnclassifiedCount" = 0)
                );

                INSERT INTO "__MasterPlanLegacyClassificationCheck" ("UnclassifiedCount")
                SELECT COUNT(*) FROM "MasterPlans" WHERE "Cat" = '';

                DROP TABLE "__MasterPlanLegacyClassificationCheck";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MasterPlans_BasicKey_CatKey",
                table: "MasterPlans",
                columns: new[] { "BasicKey", "CatKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeaderMappingProfiles_NormalizedHeaderPath_CanonicalField_WorkbookFingerprint",
                table: "HeaderMappingProfiles",
                columns: new[] { "NormalizedHeaderPath", "CanonicalField", "WorkbookFingerprint" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeaderMappingProfiles");

            migrationBuilder.DropIndex(
                name: "IX_MasterPlans_BasicKey_CatKey",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "BasicKey",
                table: "Staging_MasterPlan");

            migrationBuilder.DropColumn(
                name: "Cat",
                table: "Staging_MasterPlan");

            migrationBuilder.DropColumn(
                name: "CatKey",
                table: "Staging_MasterPlan");

            migrationBuilder.DropColumn(
                name: "MainLprLqvDate",
                table: "Staging_MasterPlan");

            migrationBuilder.DropColumn(
                name: "MainLsrDate",
                table: "Staging_MasterPlan");

            migrationBuilder.DropColumn(
                name: "TargetMasterPlanId",
                table: "Staging_MasterPlan");

            migrationBuilder.DropColumn(
                name: "TargetVersion",
                table: "Staging_MasterPlan");

            migrationBuilder.DropColumn(
                name: "BasicKey",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "Cat",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "CatKey",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "MainLprLqvDate",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "MainLsrDate",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "NoChangeRecords",
                table: "ImportBatches");
        }
    }
}

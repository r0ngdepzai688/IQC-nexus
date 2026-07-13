using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMasterPlanAndMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MasterPlanId",
                table: "ProjectMilestones",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MilestoneType",
                table: "ProjectMilestones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceBatchId",
                table: "ProjectMilestones",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetDate",
                table: "ProjectMilestones",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProjectMilestones",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ImportedStatus",
                table: "MasterPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastImportBatchId",
                table: "MasterPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remark",
                table: "MasterPlans",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MasterPlanId",
                table: "ProjectMilestones");

            migrationBuilder.DropColumn(
                name: "MilestoneType",
                table: "ProjectMilestones");

            migrationBuilder.DropColumn(
                name: "SourceBatchId",
                table: "ProjectMilestones");

            migrationBuilder.DropColumn(
                name: "TargetDate",
                table: "ProjectMilestones");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProjectMilestones");

            migrationBuilder.DropColumn(
                name: "ImportedStatus",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "LastImportBatchId",
                table: "MasterPlans");

            migrationBuilder.DropColumn(
                name: "Remark",
                table: "MasterPlans");
        }
    }
}

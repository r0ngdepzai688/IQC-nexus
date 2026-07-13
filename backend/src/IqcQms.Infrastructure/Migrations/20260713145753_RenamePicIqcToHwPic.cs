using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePicIqcToHwPic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PicIqc",
                table: "Staging_MasterPlan",
                newName: "HwPic");

            migrationBuilder.RenameColumn(
                name: "PicIqc",
                table: "MasterPlans",
                newName: "HwPic");

            migrationBuilder.RenameColumn(
                name: "PicIqc",
                table: "MasterPlanRecords",
                newName: "HwPic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HwPic",
                table: "Staging_MasterPlan",
                newName: "PicIqc");

            migrationBuilder.RenameColumn(
                name: "HwPic",
                table: "MasterPlans",
                newName: "PicIqc");

            migrationBuilder.RenameColumn(
                name: "HwPic",
                table: "MasterPlanRecords",
                newName: "PicIqc");
        }
    }
}

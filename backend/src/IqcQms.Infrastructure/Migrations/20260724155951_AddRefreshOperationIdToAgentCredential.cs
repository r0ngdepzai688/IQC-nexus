using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshOperationIdToAgentCredential : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentCredentials_AgentDeviceId",
                table: "AgentCredentials");

            migrationBuilder.AddColumn<string>(
                name: "RefreshOperationId",
                table: "AgentCredentials",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_AgentDeviceId_RefreshOperationId",
                table: "AgentCredentials",
                columns: new[] { "AgentDeviceId", "RefreshOperationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentCredentials_AgentDeviceId_RefreshOperationId",
                table: "AgentCredentials");

            migrationBuilder.DropColumn(
                name: "RefreshOperationId",
                table: "AgentCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_AgentDeviceId",
                table: "AgentCredentials",
                column: "AgentDeviceId");
        }
    }
}

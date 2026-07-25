using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayloadRetentionIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AgentPayloadSubmissions_CreatedAtUtc",
                table: "AgentPayloadSubmissions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPayloadReplayTombstones_TombstoneExpiresAtUtc",
                table: "AgentPayloadReplayTombstones",
                column: "TombstoneExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentPayloadSubmissions_CreatedAtUtc",
                table: "AgentPayloadSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_AgentPayloadReplayTombstones_TombstoneExpiresAtUtc",
                table: "AgentPayloadReplayTombstones");
        }
    }
}

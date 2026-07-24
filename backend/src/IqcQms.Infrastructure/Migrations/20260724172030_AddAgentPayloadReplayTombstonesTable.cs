using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentPayloadReplayTombstonesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentPayloadReplayTombstones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentDeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    PayloadSubmissionId = table.Column<string>(type: "TEXT", nullable: false),
                    Nonce = table.Column<string>(type: "TEXT", nullable: false),
                    CanonicalPayloadHash = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TombstoneExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPayloadReplayTombstones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentPayloadReplayTombstones_AgentDeviceId_Nonce",
                table: "AgentPayloadReplayTombstones",
                columns: new[] { "AgentDeviceId", "Nonce" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentPayloadReplayTombstones_AgentDeviceId_PayloadSubmissionId",
                table: "AgentPayloadReplayTombstones",
                columns: new[] { "AgentDeviceId", "PayloadSubmissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentPayloadReplayTombstones");
        }
    }
}

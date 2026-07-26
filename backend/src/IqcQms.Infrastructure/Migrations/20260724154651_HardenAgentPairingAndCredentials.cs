using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenAgentPairingAndCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AttemptCount",
                table: "AgentPairingRequests",
                newName: "State");

            migrationBuilder.AddColumn<int>(
                name: "FailedAttemptCount",
                table: "AgentPairingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedAttemptAtUtc",
                table: "AgentPairingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAtUtc",
                table: "AgentPairingRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxFailedAttempts",
                table: "AgentPairingRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsumedAtUtc",
                table: "AgentCredentials",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenFamilyId",
                table: "AgentCredentials",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPairingRequests_State",
                table: "AgentPairingRequests",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_TokenFamilyId",
                table: "AgentCredentials",
                column: "TokenFamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentPairingRequests_State",
                table: "AgentPairingRequests");

            migrationBuilder.DropIndex(
                name: "IX_AgentCredentials_TokenFamilyId",
                table: "AgentCredentials");

            migrationBuilder.DropColumn(
                name: "FailedAttemptCount",
                table: "AgentPairingRequests");

            migrationBuilder.DropColumn(
                name: "LastFailedAttemptAtUtc",
                table: "AgentPairingRequests");

            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                table: "AgentPairingRequests");

            migrationBuilder.DropColumn(
                name: "MaxFailedAttempts",
                table: "AgentPairingRequests");

            migrationBuilder.DropColumn(
                name: "ConsumedAtUtc",
                table: "AgentCredentials");

            migrationBuilder.DropColumn(
                name: "TokenFamilyId",
                table: "AgentCredentials");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "AgentPairingRequests",
                newName: "AttemptCount");
        }
    }
}

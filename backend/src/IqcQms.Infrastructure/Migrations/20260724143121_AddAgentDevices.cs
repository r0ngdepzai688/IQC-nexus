using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IqcQms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentDevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    AgentVersion = table.Column<string>(type: "TEXT", nullable: false),
                    ProtocolVersion = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    CapabilitiesJson = table.Column<string>(type: "TEXT", nullable: false),
                    PairedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConcurrencyVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentPairingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HashedCode = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConcurrencyVersion = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentPairingRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommittedImportRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportJobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceRecordIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemCode = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    InspectionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    MappingProfileVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CommittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommittedImportRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersistentImportAuditEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    FromState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ToState = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    SanitizedMetadataJson = table.Column<string>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistentImportAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersistentImportCommitReceipts",
                columns: table => new
                {
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ContentFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CommittedBy = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    InsertedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SkippedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CommittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistentImportCommitReceipts", x => x.IdempotencyKey);
                });

            migrationBuilder.CreateTable(
                name: "PersistentImportJobs",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OwnerUserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceKind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceDisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NormalizedContentFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MappingProfileId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    MappingProfileVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    ValidationProfileId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ValidationProfileVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    PreviewFingerprint = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    PreviewAttestationSignature = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    PreviewGeneratedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PreviewExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    IsPreviewInvalidated = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreviewInvalidationReason = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    MappedRecordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WarningCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BlockingErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCommitted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CommittedBy = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CommitIdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    ConcurrencyVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistentImportJobs", x => x.JobId);
                });

            migrationBuilder.CreateTable(
                name: "PersistentImportMappedPayloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    MappingProfileVersion = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CanonicalPayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistentImportMappedPayloads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersistentImportOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AggregateId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsDispatched = table.Column<bool>(type: "INTEGER", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DispatchedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistentImportOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersistentImportWorkItems",
                columns: table => new
                {
                    WorkItemId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    WorkType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    AvailableAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LeaseOwner = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ExpectedVersion = table.Column<long>(type: "INTEGER", nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    LastErrorCode = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistentImportWorkItems", x => x.WorkItemId);
                });

            migrationBuilder.CreateTable(
                name: "AgentCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgentDeviceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialIdentifier = table.Column<string>(type: "TEXT", nullable: false),
                    ProtectedVerifierHash = table.Column<string>(type: "TEXT", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RotationLineage = table.Column<string>(type: "TEXT", nullable: false),
                    IsReplayed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentCredentials_AgentDevices_AgentDeviceId",
                        column: x => x.AgentDeviceId,
                        principalTable: "AgentDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_AgentDeviceId",
                table: "AgentCredentials",
                column: "AgentDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_CredentialIdentifier",
                table: "AgentCredentials",
                column: "CredentialIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentCredentials_DeviceId",
                table: "AgentCredentials",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDevices_DeviceId",
                table: "AgentDevices",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentDevices_OwnerUserId",
                table: "AgentDevices",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentDevices_State",
                table: "AgentDevices",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPairingRequests_HashedCode",
                table: "AgentPairingRequests",
                column: "HashedCode");

            migrationBuilder.CreateIndex(
                name: "IX_AgentPairingRequests_OwnerUserId",
                table: "AgentPairingRequests",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommittedImportRecords_ImportJobId",
                table: "CommittedImportRecords",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportAuditEvents_JobId_OccurredAt",
                table: "PersistentImportAuditEvents",
                columns: new[] { "JobId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportCommitReceipts_JobId",
                table: "PersistentImportCommitReceipts",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportJobs_CommitIdempotencyKey",
                table: "PersistentImportJobs",
                column: "CommitIdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportJobs_CreatedAt",
                table: "PersistentImportJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportJobs_OwnerUserId",
                table: "PersistentImportJobs",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportJobs_State",
                table: "PersistentImportJobs",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportMappedPayloads_JobId",
                table: "PersistentImportMappedPayloads",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportOutboxMessages_IsDispatched_OccurredAtUtc",
                table: "PersistentImportOutboxMessages",
                columns: new[] { "IsDispatched", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportWorkItems_JobId",
                table: "PersistentImportWorkItems",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_PersistentImportWorkItems_State_AvailableAtUtc",
                table: "PersistentImportWorkItems",
                columns: new[] { "State", "AvailableAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentCredentials");

            migrationBuilder.DropTable(
                name: "AgentPairingRequests");

            migrationBuilder.DropTable(
                name: "CommittedImportRecords");

            migrationBuilder.DropTable(
                name: "PersistentImportAuditEvents");

            migrationBuilder.DropTable(
                name: "PersistentImportCommitReceipts");

            migrationBuilder.DropTable(
                name: "PersistentImportJobs");

            migrationBuilder.DropTable(
                name: "PersistentImportMappedPayloads");

            migrationBuilder.DropTable(
                name: "PersistentImportOutboxMessages");

            migrationBuilder.DropTable(
                name: "PersistentImportWorkItems");

            migrationBuilder.DropTable(
                name: "AgentDevices");
        }
    }
}

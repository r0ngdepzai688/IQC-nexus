using System.Data.Common;
using IqcQms.Api.Controllers;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Domain.Exceptions;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class TypedPayloadSecurityErrorTests : IDisposable
{
    private readonly DbConnection _connection;

    public TypedPayloadSecurityErrorTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options);
    }

    private NormalizedWorkbookUploadRequest CreateSampleRequest(string deviceId, string submissionId, string nonce)
    {
        return new NormalizedWorkbookUploadRequest
        {
            CanonicalSchemaVersion = "1.0",
            DeviceId = deviceId,
            PayloadSubmissionId = submissionId,
            Nonce = nonce,
            ServerImportJobId = Guid.NewGuid(),
            ProviderId = "TypedErrorProvider",
            ProviderVersion = "1.0.0",
            RecordCount = 10,
            NormalizedWorkbook = new NormalizedWorkbook
            {
                WorkbookName = "Sample.xlsx",
                Sheets = new List<NormalizedSheet>
                {
                    new NormalizedSheet
                    {
                        SheetName = "Sheet1",
                        Rows = new List<NormalizedRow>
                        {
                            new NormalizedRow
                            {
                                RowIndex = 1,
                                Cells = new List<NormalizedCell>
                                {
                                    new NormalizedCell { ColumnName = "Col1", ColumnIndex = 0, Value = "Val1", DataType = "String" }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    [Fact]
    public async Task SubmissionMismatch_ThrowsTypedException_AndControllerReturnsSanitizedConflict()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var controller = new AgentDevicesController(service, NullLogger<AgentDevicesController>.Instance);

        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_typed_mismatch"
        });

        var req1 = CreateSampleRequest(pairResp.DeviceId, "sub_typed_100", "nonce_typed_100_1234567");
        await service.UploadNormalizedWorkbookAsync(req1);

        // Submitting same submission ID with different nonce/content
        var req2 = CreateSampleRequest(pairResp.DeviceId, "sub_typed_100", "nonce_attacker_different");
        req2.NormalizedWorkbook.WorkbookName = "Tampered.xlsx";

        // Service throws typed exception
        var domainEx = await Assert.ThrowsAsync<PayloadSubmissionMismatchException>(() => service.UploadNormalizedWorkbookAsync(req2));
        Assert.Equal("SUBMISSION_MISMATCH", domainEx.ReasonCode);

        // Controller catches typed exception and returns sanitized HTTP 409 Conflict
        var actionResult = await controller.UploadNormalizedWorkbook(pairResp.DeviceId, req2);
        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        Assert.Equal(409, conflictResult.StatusCode);

        // Verify response body does not leak internal binding details, nonce, path, or raw details
        var json = System.Text.Json.JsonSerializer.Serialize(conflictResult.Value);
        Assert.Contains("Payload submission conflict detected.", json);
        Assert.DoesNotContain("nonce_attacker_different", json);
        Assert.DoesNotContain("Tampered.xlsx", json);
    }

    [Fact]
    public async Task NonceReplay_ThrowsTypedException_AndControllerReturnsSanitizedConflict()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var controller = new AgentDevicesController(service, NullLogger<AgentDevicesController>.Instance);

        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_typed_nonce"
        });

        var req1 = CreateSampleRequest(pairResp.DeviceId, "sub_typed_200", "nonce_replayed_12345678");
        await service.UploadNormalizedWorkbookAsync(req1);

        // Submitting same nonce with different submission ID
        var req2 = CreateSampleRequest(pairResp.DeviceId, "sub_typed_201_different", "nonce_replayed_12345678");

        var domainEx = await Assert.ThrowsAsync<PayloadNonceReplayException>(() => service.UploadNormalizedWorkbookAsync(req2));
        Assert.Equal("NONCE_REPLAY", domainEx.ReasonCode);

        var actionResult = await controller.UploadNormalizedWorkbook(pairResp.DeviceId, req2);
        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task TombstoneHit_ThrowsTypedException_AndControllerReturnsSanitizedConflict()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var controller = new AgentDevicesController(service, NullLogger<AgentDevicesController>.Instance);

        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_typed_tombstone"
        });

        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
        db.AgentPayloadReplayTombstones.Add(new AgentPayloadReplayTombstone
        {
            AgentDeviceId = device.Id,
            DeviceId = device.DeviceId,
            PayloadSubmissionId = "sub_tombstone_300",
            Nonce = "nonce_tombstone_300_123",
            CanonicalPayloadHash = "hash300",
            AcceptedAtUtc = DateTime.UtcNow.AddDays(-10),
            TombstoneExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var req = CreateSampleRequest(pairResp.DeviceId, "sub_tombstone_300", "nonce_tombstone_300_123");

        var domainEx = await Assert.ThrowsAsync<PayloadReplayTombstoneException>(() => service.UploadNormalizedWorkbookAsync(req));
        Assert.Equal("TOMBSTONE_HIT", domainEx.ReasonCode);

        var actionResult = await controller.UploadNormalizedWorkbook(pairResp.DeviceId, req);
        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult.Result);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public async Task ExpectedSecurityConflict_DoesNotReturnHttp500()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var controller = new AgentDevicesController(service, NullLogger<AgentDevicesController>.Instance);

        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_no_500"
        });

        var reqValid = CreateSampleRequest(pairResp.DeviceId, "sub_no_500", "nonce_no_500_123456789");
        await controller.UploadNormalizedWorkbook(pairResp.DeviceId, reqValid);

        // Replay with mismatching content
        var reqConflict = CreateSampleRequest(pairResp.DeviceId, "sub_no_500", "nonce_no_500_DIFFERENT");

        var actionResult = await controller.UploadNormalizedWorkbook(pairResp.DeviceId, reqConflict);
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);

        Assert.NotEqual(500, objectResult.StatusCode);
        Assert.Equal(409, objectResult.StatusCode);
    }
}

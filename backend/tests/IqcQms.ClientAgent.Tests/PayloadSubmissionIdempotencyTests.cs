using System.Data.Common;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class PayloadSubmissionIdempotencyTests : IDisposable
{
    private readonly DbConnection _connection;

    public PayloadSubmissionIdempotencyTests()
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
            ProviderId = "SyntheticProvider",
            ProviderVersion = "1.0.0",
            RecordCount = 10,
            NormalizedWorkbook = new NormalizedWorkbook
            {
                WorkbookName = "SampleData.xlsx",
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
                                    new NormalizedCell { ColumnName = "PartNo", ColumnIndex = 0, Value = "PN-100", DataType = "String" }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    [Fact]
    public async Task FirstValidSubmission_IsAcceptedAndCreatesRecord()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_sub_test_1"
        });

        var req = CreateSampleRequest(pairResp.DeviceId, "sub_id_100", "nonce_100");
        var resp = await service.UploadNormalizedWorkbookAsync(req);

        Assert.False(resp.IsDuplicateRetry);
        Assert.NotEqual(Guid.Empty, resp.UploadId);
        Assert.Equal("Accepted", resp.Status);

        var submissionRecord = await db.AgentPayloadSubmissions.FirstOrDefaultAsync(s => s.PayloadSubmissionId == "sub_id_100");
        Assert.NotNull(submissionRecord);
        Assert.Equal(resp.UploadId, submissionRecord.UploadId);
    }

    [Fact]
    public async Task DuplicatePayloadSubmission_ReturnsCommittedResult()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_sub_dup_1"
        });

        var req1 = CreateSampleRequest(pairResp.DeviceId, "sub_id_200", "nonce_200");

        // 1. Initial submission
        var resp1 = await service.UploadNormalizedWorkbookAsync(req1);

        // 2. Transport retried submission with SAME submissionId, SAME nonce, SAME content
        var resp2 = await service.UploadNormalizedWorkbookAsync(req1);

        Assert.True(resp2.IsDuplicateRetry);
        Assert.Equal(resp1.UploadId, resp2.UploadId);
        Assert.Equal(resp1.ServerImportJobId, resp2.ServerImportJobId);

        var count = await db.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == "sub_id_200");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MissingRequiredFields_ThrowsArgumentException()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);

        var reqMissingSubId = CreateSampleRequest("dev_test", "", "nonce_300");
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadNormalizedWorkbookAsync(reqMissingSubId));

        var reqMissingNonce = CreateSampleRequest("dev_test", "sub_300", "");
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadNormalizedWorkbookAsync(reqMissingNonce));
    }

    [Fact]
    public async Task SubmissionMismatch_Or_ReplayWithDifferentContent_IsRejected()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_mismatch_1"
        });

        var req1 = CreateSampleRequest(pairResp.DeviceId, "sub_id_400", "nonce_400");
        await service.UploadNormalizedWorkbookAsync(req1);

        // Attacker attempts to reuse sub_id_400 with different content/nonce
        var reqAttacker = CreateSampleRequest(pairResp.DeviceId, "sub_id_400", "nonce_attacker");
        reqAttacker.NormalizedWorkbook.WorkbookName = "ModifiedName.xlsx";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadNormalizedWorkbookAsync(reqAttacker));
        Assert.Contains("mismatch detected", ex.Message);
    }

    [Fact]
    public void CanonicalPayloadHasher_ProducesIdenticalHashForEquivalentPayload()
    {
        var jobId = Guid.NewGuid();
        var req1 = CreateSampleRequest("dev1", "sub1", "nonce1");
        req1.ServerImportJobId = jobId;

        var req2 = CreateSampleRequest("dev1", "sub1", "nonce1");
        req2.ServerImportJobId = jobId;

        var hash1 = CanonicalPayloadHasher.ComputeCanonicalHash(req1);
        var hash2 = CanonicalPayloadHasher.ComputeCanonicalHash(req2);

        Assert.Equal(hash1, hash2);
        Assert.NotEmpty(hash1);
    }
}

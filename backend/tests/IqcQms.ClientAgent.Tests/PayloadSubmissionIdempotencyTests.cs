using System.Data.Common;
using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Contracts;
using IqcQms.ClientAgent.Infrastructure.Queue;
using IqcQms.Domain.Entities.Agent;
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

        var req = CreateSampleRequest(pairResp.DeviceId, "sub_id_100", "nonce_100_123456789");
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

        var req1 = CreateSampleRequest(pairResp.DeviceId, "sub_id_200", "nonce_200_123456789");

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
    public async Task MissingRequiredFields_OrShortNonce_ThrowsArgumentException()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);

        var reqMissingSubId = CreateSampleRequest("dev_test", "", "nonce_300_123456789");
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadNormalizedWorkbookAsync(reqMissingSubId));

        var reqShortNonce = CreateSampleRequest("dev_test", "sub_300", "short");
        await Assert.ThrowsAsync<ArgumentException>(() => service.UploadNormalizedWorkbookAsync(reqShortNonce));
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

        var req1 = CreateSampleRequest(pairResp.DeviceId, "sub_id_400", "nonce_400_123456789");
        await service.UploadNormalizedWorkbookAsync(req1);

        // Attacker attempts to reuse sub_id_400 with different content/nonce
        var reqAttacker = CreateSampleRequest(pairResp.DeviceId, "sub_id_400", "nonce_attacker_12345678");
        reqAttacker.NormalizedWorkbook.WorkbookName = "ModifiedName.xlsx";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadNormalizedWorkbookAsync(reqAttacker));
        Assert.Contains("mismatch detected", ex.Message);
    }

    [Fact]
    public void CanonicalPayloadHasher_ExcludesServerImportJobIdFromDigest()
    {
        var canonicalizer = new NormalizedWorkbookCanonicalizer();
        var req1 = CreateSampleRequest("dev1", "sub1", "nonce1_1234567890");
        req1.ServerImportJobId = Guid.NewGuid();

        var req2 = CreateSampleRequest("dev1", "sub1", "nonce1_1234567890");
        req2.ServerImportJobId = Guid.NewGuid(); // Different server tracking job ID!

        var hash1 = CanonicalPayloadHasher.ComputeCanonicalHash(req1, canonicalizer);
        var hash2 = CanonicalPayloadHasher.ComputeCanonicalHash(req2, canonicalizer);

        // ServerImportJobId variation must NOT alter the canonical request hash!
        Assert.Equal(hash1, hash2);
        Assert.NotEmpty(hash1);
    }

    [Fact]
    public async Task LocalQueue_PersistsSubmissionIdentityAcrossFileReopen()
    {
        var tempDbDir = Path.Combine(Path.GetTempPath(), $"sqlite_queue_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDbDir);
            var testFile = Path.Combine(tempDbDir, "file.xlsx");
            File.WriteAllText(testFile, "content");

            var options = new AgentOptions { AllowedInputRoots = new List<string> { tempDbDir } };
            var logger = NullLogger<SqliteLocalAgentQueue>.Instance;

            string submissionId;
            string nonce;
            Guid serverJobId = Guid.NewGuid();

            // 1. Enqueue job in instance 1
            var queue1 = new SqliteLocalAgentQueue(tempDbDir, options, logger);
            await queue1.InitializeAsync();
            var item1 = await queue1.EnqueueJobAsync(serverJobId, "SyntheticNormalization", testFile);
            submissionId = item1.PayloadSubmissionId;
            nonce = item1.Nonce;
            Assert.NotEmpty(submissionId);
            Assert.NotEmpty(nonce);

            // 2. Re-open database file in instance 2 (simulating Agent process restart)
            var queue2 = new SqliteLocalAgentQueue(tempDbDir, options, logger);
            await queue2.InitializeAsync();
            var leasedItem = await queue2.AcquireNextLeaseAsync("worker-1", TimeSpan.FromMinutes(2));

            Assert.NotNull(leasedItem);
            Assert.Equal(serverJobId, leasedItem.ServerJobId);
            Assert.Equal(submissionId, leasedItem.PayloadSubmissionId);
            Assert.Equal(nonce, leasedItem.Nonce);
        }
        finally
        {
            if (Directory.Exists(tempDbDir))
            {
                try { Directory.Delete(tempDbDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task ReplayTombstone_PreventsResubmissionAfterArchival()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_tombstone_1"
        });

        // Add tombstone record
        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
        db.AgentPayloadReplayTombstones.Add(new AgentPayloadReplayTombstone
        {
            AgentDeviceId = device.Id,
            DeviceId = device.DeviceId,
            PayloadSubmissionId = "archived_sub_1",
            Nonce = "archived_nonce_123456789",
            CanonicalPayloadHash = "hash_123",
            AcceptedAtUtc = DateTime.UtcNow.AddDays(-10),
            TombstoneExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });
        await db.SaveChangesAsync();

        var req = CreateSampleRequest(pairResp.DeviceId, "archived_sub_1", "archived_nonce_123456789");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadNormalizedWorkbookAsync(req));

        Assert.Contains("tombstone", ex.Message);
    }
}

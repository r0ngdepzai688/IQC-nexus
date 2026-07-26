using System.Data.Common;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class AtomicDownstreamRollbackTests : IDisposable
{
    private readonly DbConnection _connection;

    public AtomicDownstreamRollbackTests()
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

    private NormalizedWorkbookUploadRequest CreateSampleRequest(string deviceId, string submissionId, string nonce, Guid serverJobId)
    {
        return new NormalizedWorkbookUploadRequest
        {
            CanonicalSchemaVersion = "1.0",
            DeviceId = deviceId,
            PayloadSubmissionId = submissionId,
            Nonce = nonce,
            ServerImportJobId = serverJobId,
            ProviderId = "RollbackTestProvider",
            ProviderVersion = "1.0.0",
            RecordCount = 10,
            NormalizedWorkbook = new NormalizedWorkbook
            {
                WorkbookName = "RollbackSample.xlsx",
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
                                    new NormalizedCell { ColumnName = "PartNo", ColumnIndex = 0, Value = "PN-R1", DataType = "String" }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    [Fact]
    public async Task FailureBeforeDownstreamRegistration_RollsBackSubmission()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_rollback_1"
        });

        var jobId = Guid.NewGuid();
        var req = CreateSampleRequest(pairResp.DeviceId, "sub_rollback_1", "nonce_rollback_1_12345", jobId);

        // Inject fault before downstream job is added
        service.TestHookBeforeDownstreamAdd = _ => throw new InvalidOperationException("Simulated fault before downstream job creation");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadNormalizedWorkbookAsync(req));

        // Assert: NO submission entity and NO downstream job remain
        var submissionCount = await db.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == "sub_rollback_1");
        Assert.Equal(0, submissionCount);

        var jobCount = await db.PersistentImportJobs.CountAsync(j => j.JobId == jobId.ToString());
        Assert.Equal(0, jobCount);
    }

    [Fact]
    public async Task FailureBeforeCommit_RollsBackSubmissionAndDownstreamJob()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_rollback_2"
        });

        var jobId = Guid.NewGuid();
        var req = CreateSampleRequest(pairResp.DeviceId, "sub_rollback_2", "nonce_rollback_2_12345", jobId);

        // Inject fault after downstream job is added but before SaveChanges
        service.TestHookBeforeSaveChanges = _ => throw new InvalidOperationException("Simulated fault before SaveChanges");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadNormalizedWorkbookAsync(req));

        var submissionCount = await db.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == "sub_rollback_2");
        Assert.Equal(0, submissionCount);

        var jobCount = await db.PersistentImportJobs.CountAsync(j => j.JobId == jobId.ToString());
        Assert.Equal(0, jobCount);
    }

    [Fact]
    public async Task FailureBeforeTransactionCommit_RollsBackSubmissionAndDownstreamJob()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_rollback_3"
        });

        var jobId = Guid.NewGuid();
        var req = CreateSampleRequest(pairResp.DeviceId, "sub_rollback_3", "nonce_rollback_3_12345", jobId);

        // Inject fault after SaveChanges but before Commit
        service.TestHookBeforeCommit = _ => throw new InvalidOperationException("Simulated fault before Commit");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadNormalizedWorkbookAsync(req));

        var submissionCount = await db.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == "sub_rollback_3");
        Assert.Equal(0, submissionCount);

        var jobCount = await db.PersistentImportJobs.CountAsync(j => j.JobId == jobId.ToString());
        Assert.Equal(0, jobCount);
    }

    [Fact]
    public async Task RetryAfterRolledBackAttempt_SucceedsExactlyOnce()
    {
        using var db = CreateDbContext();
        var service = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await service.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_rollback_4"
        });

        var jobId = Guid.NewGuid();
        var req = CreateSampleRequest(pairResp.DeviceId, "sub_rollback_4", "nonce_rollback_4_12345", jobId);

        // 1. First attempt fails due to simulated fault
        service.TestHookBeforeSaveChanges = _ => throw new InvalidOperationException("First attempt transient failure");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadNormalizedWorkbookAsync(req));

        // Clear test hook
        service.TestHookBeforeSaveChanges = null;

        // 2. Retry with same identity succeeds
        var response = await service.UploadNormalizedWorkbookAsync(req);

        Assert.False(response.IsDuplicateRetry);
        Assert.Equal(jobId, response.ServerImportJobId);

        // 3. Verify exactly 1 submission and 1 downstream job exist
        var submissionCount = await db.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == "sub_rollback_4");
        Assert.Equal(1, submissionCount);

        var jobCount = await db.PersistentImportJobs.CountAsync(j => j.JobId == jobId.ToString());
        Assert.Equal(1, jobCount);
    }
}

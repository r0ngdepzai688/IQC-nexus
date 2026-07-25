using System.Data.Common;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Domain.Exceptions;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class RelationalPayloadConcurrencyTests : IDisposable
{
    private readonly string _dbFilePath;
    private readonly string _connectionString;

    public RelationalPayloadConcurrencyTests()
    {
        _dbFilePath = Path.Combine(Path.GetTempPath(), $"relational_concurrency_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbFilePath};Pooling=False";

        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        if (File.Exists(_dbFilePath))
        {
            try { File.Delete(_dbFilePath); } catch { }
        }
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
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
            ProviderId = "RelationalConcurrencyProvider",
            ProviderVersion = "1.0.0",
            RecordCount = 25,
            NormalizedWorkbook = new NormalizedWorkbook
            {
                WorkbookName = "ConcurrentTest.xlsx",
                Sheets = new List<NormalizedSheet>
                {
                    new NormalizedSheet
                    {
                        SheetName = "Data",
                        Rows = new List<NormalizedRow>
                        {
                            new NormalizedRow
                            {
                                RowIndex = 1,
                                Cells = new List<NormalizedCell>
                                {
                                    new NormalizedCell { ColumnName = "ItemCode", ColumnIndex = 0, Value = "ITEM-001", DataType = "String" }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    [Fact]
    public async Task ConcurrentIdenticalSubmissions_CreateOneSubmissionAndOneDownstreamJob()
    {
        // 1. Setup paired active device
        using (var setupDb = CreateDbContext())
        {
            var setupService = new AgentService(setupDb, NullLogger<AgentService>.Instance);
            var pairing = await setupService.CreatePairingCodeAsync(1, "RelationalUser");
            await setupService.PairDeviceAsync(new AgentDevicePairRequest
            {
                PairingCode = pairing.PairingCode,
                DeviceId = "dev_rel_conc_1"
            });
        }

        var req1 = CreateSampleRequest("dev_rel_conc_1", "sub_rel_conc_100", "nonce_rel_conc_100_123456789");
        var req2 = CreateSampleRequest("dev_rel_conc_1", "sub_rel_conc_100", "nonce_rel_conc_100_123456789");
        req2.ServerImportJobId = req1.ServerImportJobId; // Same job ID

        // 2. Synchronize two concurrent requests using separate DbContext instances
        var barrier = new SemaphoreSlim(0, 2);

        var task1 = Task.Run(async () =>
        {
            using var db = CreateDbContext();
            var service = new AgentService(db, NullLogger<AgentService>.Instance);
            barrier.Release();
            await barrier.WaitAsync();
            return await service.UploadNormalizedWorkbookAsync(req1);
        });

        var task2 = Task.Run(async () =>
        {
            using var db = CreateDbContext();
            var service = new AgentService(db, NullLogger<AgentService>.Instance);
            barrier.Release();
            await barrier.WaitAsync();
            return await service.UploadNormalizedWorkbookAsync(req2);
        });

        var responses = await Task.WhenAll(task1, task2);

        // 3. Assert responses
        Assert.NotNull(responses[0]);
        Assert.NotNull(responses[1]);

        var original = responses.Single(r => !r.IsDuplicateRetry);
        var duplicate = responses.Single(r => r.IsDuplicateRetry);

        Assert.Equal(original.UploadId, duplicate.UploadId);
        Assert.Equal(original.ServerImportJobId, duplicate.ServerImportJobId);

        // 4. Assert exactly one AgentPayloadSubmission and exactly one downstream PersistentImportJob exist
        using (var verifyDb = CreateDbContext())
        {
            var submissionCount = await verifyDb.AgentPayloadSubmissions
                .CountAsync(s => s.PayloadSubmissionId == "sub_rel_conc_100");
            Assert.Equal(1, submissionCount);

            var jobIdStr = req1.ServerImportJobId.ToString();
            var jobCount = await verifyDb.PersistentImportJobs
                .CountAsync(j => j.JobId == jobIdStr);
            Assert.Equal(1, jobCount);
        }
    }

    [Fact]
    public async Task ConcurrentIdenticalSubmissions_ReturnSameCommittedResult()
    {
        using (var setupDb = CreateDbContext())
        {
            var setupService = new AgentService(setupDb, NullLogger<AgentService>.Instance);
            var pairing = await setupService.CreatePairingCodeAsync(1, "RelationalUser");
            await setupService.PairDeviceAsync(new AgentDevicePairRequest
            {
                PairingCode = pairing.PairingCode,
                DeviceId = "dev_rel_conc_2"
            });
        }

        var req1 = CreateSampleRequest("dev_rel_conc_2", "sub_rel_conc_200", "nonce_rel_conc_200_123456789");

        using var db1 = CreateDbContext();
        using var db2 = CreateDbContext();
        var service1 = new AgentService(db1, NullLogger<AgentService>.Instance);
        var service2 = new AgentService(db2, NullLogger<AgentService>.Instance);

        var task1 = service1.UploadNormalizedWorkbookAsync(req1);
        var task2 = service2.UploadNormalizedWorkbookAsync(req1);

        var responses = await Task.WhenAll(task1, task2);

        Assert.Equal(responses[0].UploadId, responses[1].UploadId);
        Assert.Equal(responses[0].ServerImportJobId, responses[1].ServerImportJobId);
        Assert.True(responses[0].IsDuplicateRetry ^ responses[1].IsDuplicateRetry);
    }

    [Fact]
    public async Task ConcurrentConflictingSubmissions_RejectLoserWithoutHttp500()
    {
        using (var setupDb = CreateDbContext())
        {
            var setupService = new AgentService(setupDb, NullLogger<AgentService>.Instance);
            var pairing = await setupService.CreatePairingCodeAsync(1, "RelationalUser");
            await setupService.PairDeviceAsync(new AgentDevicePairRequest
            {
                PairingCode = pairing.PairingCode,
                DeviceId = "dev_rel_conc_3"
            });
        }

        var reqWinning = CreateSampleRequest("dev_rel_conc_3", "sub_rel_conc_300", "nonce_rel_conc_300_123456789");
        var reqConflicting = CreateSampleRequest("dev_rel_conc_3", "sub_rel_conc_300", "nonce_rel_conc_300_ATTACK");
        reqConflicting.NormalizedWorkbook.WorkbookName = "ConflictingContent.xlsx";

        NormalizedWorkbookUploadResponse? winningResponse = null;
        Exception? losingException = null;

        var barrier = new SemaphoreSlim(0, 2);

        var task1 = Task.Run(async () =>
        {
            using var db = CreateDbContext();
            var service = new AgentService(db, NullLogger<AgentService>.Instance);
            barrier.Release();
            await barrier.WaitAsync();
            return await service.UploadNormalizedWorkbookAsync(reqWinning);
        });

        var task2 = Task.Run(async () =>
        {
            using var db = CreateDbContext();
            var service = new AgentService(db, NullLogger<AgentService>.Instance);
            barrier.Release();
            await barrier.WaitAsync();
            return await service.UploadNormalizedWorkbookAsync(reqConflicting);
        });

        try
        {
            winningResponse = await task1;
        }
        catch (Exception ex)
        {
            losingException = ex;
        }

        try
        {
            var res2 = await task2;
            if (winningResponse == null) winningResponse = res2;
        }
        catch (Exception ex)
        {
            if (losingException == null) losingException = ex;
        }

        // Assert: one succeeded, the loser was rejected with typed security exception (not unhandled DB error)
        Assert.NotNull(winningResponse);
        Assert.NotNull(losingException);
        Assert.IsAssignableFrom<PayloadSubmissionSecurityException>(losingException);

        using (var verifyDb = CreateDbContext())
        {
            var submissions = await verifyDb.AgentPayloadSubmissions
                .Where(s => s.PayloadSubmissionId == "sub_rel_conc_300")
                .ToListAsync();
            Assert.Single(submissions);
            Assert.Equal(winningResponse.UploadId, submissions[0].UploadId);
        }
    }
}

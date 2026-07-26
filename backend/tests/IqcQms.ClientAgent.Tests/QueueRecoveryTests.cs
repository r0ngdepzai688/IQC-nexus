using System.Data.Common;
using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Contracts;
using IqcQms.ClientAgent.Infrastructure.Queue;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class QueueRecoveryTests : IDisposable
{
    private readonly DbConnection _connection;

    public QueueRecoveryTests()
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

    [Fact]
    public async Task CommittedResponseLost_RetryCompletesQueueJob()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"queue_recovery_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var testFile = Path.Combine(tempDir, "sample.xlsx");
            File.WriteAllText(testFile, "test data");

            using var db = CreateDbContext();
            var service = new AgentService(db, NullLogger<AgentService>.Instance);
            var created = await service.CreatePairingCodeAsync(1, "UserA");
            var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
            {
                PairingCode = created.PairingCode,
                DeviceId = "dev_queue_rec_1"
            });

            var queue = new SqliteLocalAgentQueue(tempDir, new AgentOptions { AllowedInputRoots = new List<string> { tempDir } }, NullLogger<SqliteLocalAgentQueue>.Instance);
            await queue.InitializeAsync();

            var serverJobId = Guid.NewGuid();
            var localJob = await queue.EnqueueJobAsync(serverJobId, "SyntheticNormalization", testFile);

            var req = new NormalizedWorkbookUploadRequest
            {
                CanonicalSchemaVersion = "1.0",
                DeviceId = pairResp.DeviceId,
                PayloadSubmissionId = localJob.PayloadSubmissionId,
                Nonce = localJob.Nonce,
                ServerImportJobId = serverJobId,
                RecordCount = 5,
                NormalizedWorkbook = new NormalizedWorkbook
                {
                    WorkbookName = "sample.xlsx",
                    Sheets = new List<NormalizedSheet>
                    {
                        new NormalizedSheet
                        {
                            SheetName = "S1",
                            Rows = new List<NormalizedRow> { new NormalizedRow { RowIndex = 1, Cells = new List<NormalizedCell>() } }
                        }
                    }
                }
            };

            // 1. Initial attempt succeeds on server, but client loses response and retries
            var resp1 = await service.UploadNormalizedWorkbookAsync(req);
            Assert.False(resp1.IsDuplicateRetry);

            // 2. Client retries upload with identical payload submission identity
            var resp2 = await service.UploadNormalizedWorkbookAsync(req);
            Assert.True(resp2.IsDuplicateRetry);
            Assert.Equal(resp1.UploadId, resp2.UploadId);

            // 3. Client marks local queue job as complete
            await queue.CompleteJobAsync(localJob.LocalJobId);

            var pendingCount = await queue.GetPendingCountAsync();
            Assert.Equal(0, pendingCount);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    [Fact]
    public async Task ConcurrentDuplicateResponses_DoNotCompleteTwoJobs()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"queue_concurrent_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            var testFile = Path.Combine(tempDir, "sample.xlsx");
            File.WriteAllText(testFile, "test data");

            var queue = new SqliteLocalAgentQueue(tempDir, new AgentOptions { AllowedInputRoots = new List<string> { tempDir } }, NullLogger<SqliteLocalAgentQueue>.Instance);
            await queue.InitializeAsync();

            var serverJobId = Guid.NewGuid();
            var localJob = await queue.EnqueueJobAsync(serverJobId, "SyntheticNormalization", testFile);

            // Simulate two parallel workers receiving duplicate responses and completing queue job
            var t1 = queue.CompleteJobAsync(localJob.LocalJobId);
            var t2 = queue.CompleteJobAsync(localJob.LocalJobId);

            await Task.WhenAll(t1, t2);

            var pendingCount = await queue.GetPendingCountAsync();
            Assert.Equal(0, pendingCount);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}

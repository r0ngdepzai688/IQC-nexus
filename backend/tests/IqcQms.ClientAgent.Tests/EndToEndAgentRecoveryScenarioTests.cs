using System.Security.Cryptography;
using IqcQms.ClientAgent.Application.Config;
using IqcQms.ClientAgent.Application.Providers;
using IqcQms.ClientAgent.Application.Storage;
using IqcQms.ClientAgent.Contracts;
using IqcQms.ClientAgent.Infrastructure.Providers;
using IqcQms.ClientAgent.Infrastructure.Queue;
using IqcQms.ClientAgent.Infrastructure.Storage;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class EndToEndAgentRecoveryScenarioTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string _connectionString;
    private readonly string _tempQueueDbPath;

    public EndToEndAgentRecoveryScenarioTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"e2e_recovery_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_tempDbPath};Pooling=False";
        _tempQueueDbPath = Path.Combine(Path.GetTempPath(), $"e2e_queue_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
        if (File.Exists(_tempQueueDbPath))
        {
            try { File.Delete(_tempQueueDbPath); } catch { }
        }
    }

    private AppDbContext CreateServerDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ScenarioA_RefreshResponseLoss_RecoversExactCredentialsAcrossRestart()
    {
        using (var db = CreateServerDbContext())
        {
            await db.Database.EnsureCreatedAsync();
        }

        var securityOptions = Options.Create(new AgentSecurityOptions());
        var encService = new EnvelopeEncryptionService(securityOptions);
        var canonicalizer = new NormalizedWorkbookCanonicalizer();

        string deviceId = "dev_e2e_refresh_rec";
        AgentTokenRefreshResponse firstRefresh;

        using (var db = CreateServerDbContext())
        {
            var service = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), NullLogger<AgentService>.Instance);
            var pairCode = await service.CreatePairingCodeAsync(1, "UserA");
            var pairResp = await service.PairDeviceAsync(new AgentDevicePairRequest
            {
                DeviceId = deviceId,
                PairingCode = pairCode.PairingCode
            });

            firstRefresh = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
            {
                DeviceId = deviceId,
                RefreshToken = pairResp.RefreshToken,
                RefreshOperationId = "op_e2e_1"
            });
        }

        // 2. Second Refresh: Server commits new credentials, but HTTP response is dropped
        string lostOperationId = "op_e2e_lost_resp";
        AgentTokenRefreshResponse committedResponseOnServer;

        using (var db = CreateServerDbContext())
        {
            var service = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), NullLogger<AgentService>.Instance);
            committedResponseOnServer = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
            {
                DeviceId = deviceId,
                RefreshToken = firstRefresh.RefreshToken,
                RefreshOperationId = lostOperationId
            });
        }

        // 3. Client process restart simulation: Agent retries with same lostOperationId and old refresh token
        AgentTokenRefreshResponse recoveredResponse;
        using (var db = CreateServerDbContext())
        {
            var service = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), NullLogger<AgentService>.Instance);
            recoveredResponse = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
            {
                DeviceId = deviceId,
                RefreshToken = firstRefresh.RefreshToken,
                RefreshOperationId = lostOperationId
            });
        }

        // Assert: Recovered credentials match exact committed response
        Assert.Equal(committedResponseOnServer.AccessToken, recoveredResponse.AccessToken);
        Assert.Equal(committedResponseOnServer.RefreshToken, recoveredResponse.RefreshToken);

        // Next refresh using recovered token succeeds
        using (var db = CreateServerDbContext())
        {
            var service = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), NullLogger<AgentService>.Instance);
            var nextRefresh = await service.RefreshTokenAsync(new AgentTokenRefreshRequest
            {
                DeviceId = deviceId,
                RefreshToken = recoveredResponse.RefreshToken,
                RefreshOperationId = "op_e2e_subsequent"
            });

            Assert.NotNull(nextRefresh.AccessToken);
            Assert.NotEqual(recoveredResponse.AccessToken, nextRefresh.AccessToken);
        }
    }

    [Fact]
    public async Task ScenarioB_PayloadResponseLoss_RecoversDuplicateCommittedResultAcrossRestart()
    {
        using (var db = CreateServerDbContext())
        {
            await db.Database.EnsureCreatedAsync();
        }

        var securityOptions = Options.Create(new AgentSecurityOptions());
        var encService = new EnvelopeEncryptionService(securityOptions);
        var canonicalizer = new NormalizedWorkbookCanonicalizer();

        string deviceId = "dev_e2e_payload_rec";
        using (var db = CreateServerDbContext())
        {
            var service = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), NullLogger<AgentService>.Instance);
            var pairCode = await service.CreatePairingCodeAsync(1, "UserA");
            await service.PairDeviceAsync(new AgentDevicePairRequest
            {
                DeviceId = deviceId,
                PairingCode = pairCode.PairingCode
            });
        }

        var sampleFile = Path.Combine(Path.GetTempPath(), $"input_sample_{Guid.NewGuid():N}.csv");
        File.WriteAllText(sampleFile, "Item,Qty\nA,10");

        try
        {
            var agentOptions = new AgentOptions
            {
                AllowedInputRoots = new List<string> { Path.GetDirectoryName(sampleFile)! }
            };

            // 1. Enqueue job in SQLite local queue
            string queuedJobId;
            string submissionId;
            string nonce;

            var queue1 = new SqliteLocalAgentQueue(Path.GetDirectoryName(_tempQueueDbPath)!, agentOptions, NullLogger<SqliteLocalAgentQueue>.Instance);
            await queue1.InitializeAsync();
            var item = await queue1.EnqueueJobAsync(Guid.NewGuid(), "SyntheticNormalization", sampleFile);
            queuedJobId = item.LocalJobId;
            submissionId = item.PayloadSubmissionId;
            nonce = item.Nonce;

            var provider = new SyntheticClientDataProvider(NullLogger<SyntheticClientDataProvider>.Instance);
            var normResult = await provider.NormalizeAsync(new ClientNormalizationRequest
            {
                ServerImportJobId = Guid.NewGuid(),
                InputPathOrReference = sampleFile
            });
            var workbook = normResult.NormalizedWorkbook!;
            var payloadHash = Convert.ToHexString(SHA256.HashData(canonicalizer.CanonicalizeWorkbook(workbook)));

            var uploadRequest = new NormalizedWorkbookUploadRequest
            {
                DeviceId = deviceId,
                PayloadSubmissionId = submissionId,
                Nonce = nonce,
                ProviderId = "csv_provider",
                SourceFingerprint = canonicalizer.ComputeSourceFingerprint(workbook),
                CanonicalPayloadHash = payloadHash,
                RecordCount = workbook.Sheets.Sum(s => s.Rows.Count),
                ServerImportJobId = Guid.NewGuid(),
                NormalizedWorkbook = workbook
            };

            // 2. Upload to server: committed, but HTTP response simulates packet drop
            NormalizedWorkbookUploadResponse initialServerResponse;
            using (var db = CreateServerDbContext())
            {
                var service = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), NullLogger<AgentService>.Instance);
                initialServerResponse = await service.UploadNormalizedWorkbookAsync(uploadRequest);
            }

            // 3. Client process restart simulation: Local SQLite queue re-opened & job leased
            var restartedQueue = new SqliteLocalAgentQueue(Path.GetDirectoryName(_tempQueueDbPath)!, agentOptions, NullLogger<SqliteLocalAgentQueue>.Instance);
            await restartedQueue.InitializeAsync();

            var leasedItem = await restartedQueue.AcquireNextLeaseAsync("worker_1", TimeSpan.FromMinutes(5));
            Assert.NotNull(leasedItem);
            Assert.Equal(submissionId, leasedItem.PayloadSubmissionId);
            Assert.Equal(nonce, leasedItem.Nonce);

            // 4. Retry upload with exact same parameters
            NormalizedWorkbookUploadResponse retryServerResponse;
            using (var db = CreateServerDbContext())
            {
                var service = new AgentService(db, securityOptions, encService, canonicalizer, new RelationalConstraintViolationClassifier(), NullLogger<AgentService>.Instance);
                retryServerResponse = await service.UploadNormalizedWorkbookAsync(uploadRequest);
            }

            // Assert retry response returns duplicate acceptance with same UploadId and ServerImportJobId
            Assert.True(retryServerResponse.IsDuplicateRetry);
            Assert.Equal(initialServerResponse.UploadId, retryServerResponse.UploadId);
            Assert.Equal(initialServerResponse.ServerImportJobId, retryServerResponse.ServerImportJobId);

            // Complete queue item once
            await restartedQueue.CompleteJobAsync(leasedItem.LocalJobId);

            // Assert exactly 1 submission and 1 downstream job exist on server
            using (var db = CreateServerDbContext())
            {
                var submissionCount = await db.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == submissionId);
                Assert.Equal(1, submissionCount);

                var downstreamJobCount = await db.PersistentImportJobs.CountAsync(j => j.JobId == uploadRequest.ServerImportJobId.ToString());
                Assert.Equal(1, downstreamJobCount);
            }
        }
        finally
        {
            if (File.Exists(sampleFile))
            {
                try { File.Delete(sampleFile); } catch { }
            }
        }
    }

    [Fact]
    public async Task ScenarioC_PathChangedAfterEnqueue_ProcessingTimeValidationRejects()
    {
        await Task.CompletedTask;
        var sampleDir = Path.Combine(Path.GetTempPath(), $"e2e_path_change_{Guid.NewGuid():N}");
        Directory.CreateDirectory(sampleDir);
        var originalFile = Path.Combine(sampleDir, "enqueued.csv");
        File.WriteAllText(originalFile, "col1,col2\nval1,val2");

        try
        {
            var agentOptions = new AgentOptions
            {
                AllowedInputRoots = new List<string> { sampleDir }
            };

            var validator = new AllowedInputPathValidator();

            // 1. Valid at enqueue time
            var enqueueResult = validator.ValidatePath(originalFile, agentOptions.AllowedInputRoots);
            Assert.True(enqueueResult.IsAllowed);

            // 2. Path changes before processing time: file deleted or replaced with invalid path
            File.Delete(originalFile);

            // 3. Processing-time validation detects missing/invalid file
            var processingResult = validator.ValidatePath(originalFile, agentOptions.AllowedInputRoots);
            Assert.False(processingResult.IsAllowed);
            Assert.Equal(PathValidationReason.PathNotFound, processingResult.Reason);
        }
        finally
        {
            if (Directory.Exists(sampleDir))
            {
                try { Directory.Delete(sampleDir, true); } catch { }
            }
        }
    }
}

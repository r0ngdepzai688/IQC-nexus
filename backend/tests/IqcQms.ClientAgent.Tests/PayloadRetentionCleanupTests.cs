using System.Data.Common;
using IqcQms.ClientAgent.Contracts;
using IqcQms.Domain.Entities.Agent;
using IqcQms.Infrastructure.Config;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Security;
using IqcQms.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class PayloadRetentionCleanupTests : IDisposable
{
    private readonly DbConnection _connection;

    public PayloadRetentionCleanupTests()
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
    public void OptionsValidation_FailsFastOnInvalidValues()
    {
        var invalidFull = new AgentPayloadRetentionOptions { FullResultRetentionDays = 0 };
        Assert.Throws<InvalidOperationException>(() => invalidFull.Validate());

        var invalidReplay = new AgentPayloadRetentionOptions
        {
            FullResultRetentionDays = 90,
            ReplayTombstoneRetentionDays = 30 // Must be greater than FullResultRetentionDays!
        };
        Assert.Throws<InvalidOperationException>(() => invalidReplay.Validate());

        var validOptions = new AgentPayloadRetentionOptions
        {
            FullResultRetentionDays = 90,
            ReplayTombstoneRetentionDays = 365,
            CleanupBatchSize = 100
        };
        validOptions.Validate(); // Does not throw
    }

    [Fact]
    public async Task ExpiredSubmission_CreatesTombstoneBeforeDeletion()
    {
        using var db = CreateDbContext();
        var agentService = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await agentService.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await agentService.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_retention_1"
        });

        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);

        // Add a submission older than 90 days (e.g. 100 days old)
        var oldSubmission = new AgentPayloadSubmission
        {
            AgentDeviceId = device.Id,
            DeviceId = device.DeviceId,
            PayloadSubmissionId = "sub_expired_100",
            Nonce = "nonce_expired_100_1234567",
            SourceFingerprint = "fp_100",
            CanonicalPayloadHash = "hash_100",
            SchemaVersion = "1.0",
            ServerImportJobId = Guid.NewGuid(),
            UploadId = Guid.NewGuid(),
            State = AgentPayloadSubmissionState.Accepted,
            RecordCount = 5,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-100)
        };
        db.AgentPayloadSubmissions.Add(oldSubmission);
        await db.SaveChangesAsync();

        var retentionOptions = Options.Create(new AgentPayloadRetentionOptions
        {
            FullResultRetentionDays = 90,
            ReplayTombstoneRetentionDays = 365,
            CleanupBatchSize = 50
        });

        var retentionService = new AgentPayloadRetentionService(db, retentionOptions, new RelationalConstraintViolationClassifier(), NullLogger<AgentPayloadRetentionService>.Instance);

        var cleanedCount = await retentionService.CleanupExpiredSubmissionsAsync();
        Assert.Equal(1, cleanedCount);

        // Assert: Full AgentPayloadSubmission is removed
        var subAfter = await db.AgentPayloadSubmissions.FirstOrDefaultAsync(s => s.PayloadSubmissionId == "sub_expired_100");
        Assert.Null(subAfter);

        // Assert: Authoritative replay tombstone now exists
        var tombstone = await db.AgentPayloadReplayTombstones.FirstOrDefaultAsync(t => t.PayloadSubmissionId == "sub_expired_100");
        Assert.NotNull(tombstone);
        Assert.Equal(device.Id, tombstone.AgentDeviceId);
        Assert.Equal("nonce_expired_100_1234567", tombstone.Nonce);
        Assert.Equal("hash_100", tombstone.CanonicalPayloadHash);
        Assert.Equal(oldSubmission.CreatedAtUtc, tombstone.AcceptedAtUtc);
    }

    [Fact]
    public async Task CleanupIsIdempotent()
    {
        using var db = CreateDbContext();
        var agentService = new AgentService(db, NullLogger<AgentService>.Instance);
        var created = await agentService.CreatePairingCodeAsync(1, "UserA");
        var pairResp = await agentService.PairDeviceAsync(new AgentDevicePairRequest
        {
            PairingCode = created.PairingCode,
            DeviceId = "dev_retention_2"
        });

        var device = await db.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
        var oldSub = new AgentPayloadSubmission
        {
            AgentDeviceId = device.Id,
            DeviceId = device.DeviceId,
            PayloadSubmissionId = "sub_idempotent_1",
            Nonce = "nonce_idempotent_1234567",
            SourceFingerprint = "fp_idem",
            CanonicalPayloadHash = "hash_idem",
            SchemaVersion = "1.0",
            ServerImportJobId = Guid.NewGuid(),
            UploadId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-120)
        };
        db.AgentPayloadSubmissions.Add(oldSub);
        await db.SaveChangesAsync();

        var retentionOptions = Options.Create(new AgentPayloadRetentionOptions());
        var retentionService = new AgentPayloadRetentionService(db, retentionOptions, new RelationalConstraintViolationClassifier(), NullLogger<AgentPayloadRetentionService>.Instance);

        var count1 = await retentionService.CleanupExpiredSubmissionsAsync();
        Assert.Equal(1, count1);

        // Second run: no expired submissions remaining
        var count2 = await retentionService.CleanupExpiredSubmissionsAsync();
        Assert.Equal(0, count2);

        // Exactly 1 tombstone exists
        var tombstoneCount = await db.AgentPayloadReplayTombstones.CountAsync(t => t.PayloadSubmissionId == "sub_idempotent_1");
        Assert.Equal(1, tombstoneCount);
    }

    [Fact]
    public async Task ConcurrentCleanup_CreatesOneTombstone()
    {
        var tempDbPath = Path.Combine(Path.GetTempPath(), $"concurrent_cleanup_{Guid.NewGuid():N}.db");
        var connStr = $"Data Source={tempDbPath};Pooling=False";

        AppDbContext CreateFileDbContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connStr).Options;
            return new AppDbContext(opts);
        }

        try
        {
            using (var initDb = CreateFileDbContext())
            {
                initDb.Database.EnsureCreated();
                var agentService = new AgentService(initDb, NullLogger<AgentService>.Instance);
                var created = await agentService.CreatePairingCodeAsync(1, "UserA");
                var pairResp = await agentService.PairDeviceAsync(new AgentDevicePairRequest
                {
                    PairingCode = created.PairingCode,
                    DeviceId = "dev_retention_conc"
                });

                var device = await initDb.AgentDevices.FirstAsync(d => d.DeviceId == pairResp.DeviceId);
                var oldSub = new AgentPayloadSubmission
                {
                    AgentDeviceId = device.Id,
                    DeviceId = device.DeviceId,
                    PayloadSubmissionId = "sub_conc_clean_1",
                    Nonce = "nonce_conc_clean_1234567",
                    SourceFingerprint = "fp_conc",
                    CanonicalPayloadHash = "hash_conc",
                    SchemaVersion = "1.0",
                    ServerImportJobId = Guid.NewGuid(),
                    UploadId = Guid.NewGuid(),
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-100)
                };
                initDb.AgentPayloadSubmissions.Add(oldSub);
                await initDb.SaveChangesAsync();
            }

            var retentionOptions = Options.Create(new AgentPayloadRetentionOptions());

            var task1 = Task.Run(async () =>
            {
                using var db1 = CreateFileDbContext();
                var service1 = new AgentPayloadRetentionService(db1, retentionOptions, new RelationalConstraintViolationClassifier(), NullLogger<AgentPayloadRetentionService>.Instance);
                return await service1.CleanupExpiredSubmissionsAsync();
            });

            var task2 = Task.Run(async () =>
            {
                using var db2 = CreateFileDbContext();
                var service2 = new AgentPayloadRetentionService(db2, retentionOptions, new RelationalConstraintViolationClassifier(), NullLogger<AgentPayloadRetentionService>.Instance);
                return await service2.CleanupExpiredSubmissionsAsync();
            });

            await Task.WhenAll(task1, task2);

            // Assert: Exactly one tombstone exists and submission is removed
            using (var verifyDb = CreateFileDbContext())
            {
                var tombstoneCount = await verifyDb.AgentPayloadReplayTombstones.CountAsync(t => t.PayloadSubmissionId == "sub_conc_clean_1");
                Assert.Equal(1, tombstoneCount);

                var submissionCount = await verifyDb.AgentPayloadSubmissions.CountAsync(s => s.PayloadSubmissionId == "sub_conc_clean_1");
                Assert.Equal(0, submissionCount);
            }
        }
        finally
        {
            if (File.Exists(tempDbPath))
            {
                try { File.Delete(tempDbPath); } catch { }
            }
        }
    }

    [Fact]
    public async Task ExpiredTombstone_IsDeletedOnlyAfterReplayRetention()
    {
        using var db = CreateDbContext();

        // 1. Add active tombstone (expires in future)
        var activeTombstone = new AgentPayloadReplayTombstone
        {
            AgentDeviceId = Guid.NewGuid(),
            DeviceId = "dev_tomb_active",
            PayloadSubmissionId = "sub_active_tomb",
            Nonce = "nonce_active_123456",
            CanonicalPayloadHash = "hash1",
            AcceptedAtUtc = DateTime.UtcNow.AddDays(-100),
            TombstoneExpiresAtUtc = DateTime.UtcNow.AddDays(200) // Future
        };

        // 2. Add expired tombstone (expired 10 days ago)
        var expiredTombstone = new AgentPayloadReplayTombstone
        {
            AgentDeviceId = Guid.NewGuid(),
            DeviceId = "dev_tomb_expired",
            PayloadSubmissionId = "sub_expired_tomb",
            Nonce = "nonce_expired_123456",
            CanonicalPayloadHash = "hash2",
            AcceptedAtUtc = DateTime.UtcNow.AddDays(-400),
            TombstoneExpiresAtUtc = DateTime.UtcNow.AddDays(-10) // Past
        };

        db.AgentPayloadReplayTombstones.AddRange(activeTombstone, expiredTombstone);
        await db.SaveChangesAsync();

        var retentionOptions = Options.Create(new AgentPayloadRetentionOptions());
        var retentionService = new AgentPayloadRetentionService(db, retentionOptions, new RelationalConstraintViolationClassifier(), NullLogger<AgentPayloadRetentionService>.Instance);

        var deletedCount = await retentionService.CleanupExpiredTombstonesAsync();
        Assert.Equal(1, deletedCount);

        // Active tombstone remains
        var activeAfter = await db.AgentPayloadReplayTombstones.FirstOrDefaultAsync(t => t.PayloadSubmissionId == "sub_active_tomb");
        Assert.NotNull(activeAfter);

        // Expired tombstone deleted
        var expiredAfter = await db.AgentPayloadReplayTombstones.FirstOrDefaultAsync(t => t.PayloadSubmissionId == "sub_expired_tomb");
        Assert.Null(expiredAfter);
    }

    [Fact]
    public async Task TombstoneIndexesPreventDuplicateVerifierRecords()
    {
        using var db = CreateDbContext();
        var deviceId = Guid.NewGuid();

        var t1 = new AgentPayloadReplayTombstone
        {
            AgentDeviceId = deviceId,
            DeviceId = "dev_unique_tomb",
            PayloadSubmissionId = "sub_uniq_tomb",
            Nonce = "nonce_uniq_12345678",
            CanonicalPayloadHash = "hash_uniq",
            AcceptedAtUtc = DateTime.UtcNow,
            TombstoneExpiresAtUtc = DateTime.UtcNow.AddDays(365)
        };
        db.AgentPayloadReplayTombstones.Add(t1);
        await db.SaveChangesAsync();

        // Attempt duplicate (same AgentDeviceId + PayloadSubmissionId)
        var tDuplicate = new AgentPayloadReplayTombstone
        {
            AgentDeviceId = deviceId,
            DeviceId = "dev_unique_tomb",
            PayloadSubmissionId = "sub_uniq_tomb",
            Nonce = "nonce_different_12345678",
            CanonicalPayloadHash = "hash_uniq",
            AcceptedAtUtc = DateTime.UtcNow,
            TombstoneExpiresAtUtc = DateTime.UtcNow.AddDays(365)
        };

        db.AgentPayloadReplayTombstones.Add(tDuplicate);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var classifier = new RelationalConstraintViolationClassifier();
        Assert.True(classifier.IsUniqueConstraintViolation(ex));
    }
}

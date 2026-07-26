using System;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.DataPlatform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class QueueConcurrencyTests
{
    private static AppDbContext CreateMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task TwoWorkers_CannotLeaseSameWorkItemConcurrently()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db1 = CreateMemoryDb(dbName);
        using var db2 = CreateMemoryDb(dbName);

        var queue1 = new EfImportWorkQueue(db1, NullLogger<EfImportWorkQueue>.Instance);
        var queue2 = new EfImportWorkQueue(db2, NullLogger<EfImportWorkQueue>.Instance);

        var req = new EnqueueCommitWorkRequest("job-conc-1", "key-conc-1", 1, "user-1", "corr-1");
        await queue1.EnqueueCommitAsync(req);

        // Worker 1 acquires lease
        var worker1Leases = await queue1.AcquireLeasesAsync("worker-1", batchSize: 5);
        Assert.Single(worker1Leases);
        Assert.Equal("worker-1", worker1Leases[0].LeaseOwner);

        // Worker 2 attempts to acquire lease on same pending queue
        var worker2Leases = await queue2.AcquireLeasesAsync("worker-2", batchSize: 5);
        Assert.Empty(worker2Leases); // Worker 2 receives 0 items because it is leased by worker 1
    }

    [Fact]
    public async Task AbandonedLease_IsReclaimedByRecoverStaleLeases()
    {
        var dbName = Guid.NewGuid().ToString();
        using var db = CreateMemoryDb(dbName);
        var queue = new EfImportWorkQueue(db, NullLogger<EfImportWorkQueue>.Instance);

        var req = new EnqueueCommitWorkRequest("job-stale-1", "key-stale-1", 1, "user-1", "corr-1");
        var workItem = await queue.EnqueueCommitAsync(req);

        // Acquire lease with past expiration (simulating crash / abandoned lease)
        workItem.State = "Leased";
        workItem.LeaseOwner = "crashed-worker";
        workItem.LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        db.PersistentImportWorkItems.Update(workItem);
        await db.SaveChangesAsync();

        // Recover stale leases
        await queue.RecoverStaleLeasesAsync(TimeSpan.FromMinutes(2));

        var updated = await db.PersistentImportWorkItems.FindAsync(workItem.WorkItemId);
        Assert.NotNull(updated);
        Assert.Equal("Pending", updated.State);
        Assert.Null(updated.LeaseOwner);

        // New worker can now acquire the reclaimed lease
        var newLeases = await queue.AcquireLeasesAsync("worker-2", batchSize: 5);
        Assert.Single(newLeases);
        Assert.Equal("worker-2", newLeases[0].LeaseOwner);
    }

    [Fact]
    public async Task DuplicateEnqueue_SameJob_ReturnsSameWorkItemOrConflict()
    {
        using var db = CreateMemoryDb(Guid.NewGuid().ToString());
        var queue = new EfImportWorkQueue(db, NullLogger<EfImportWorkQueue>.Instance);

        var req1 = new EnqueueCommitWorkRequest("job-dup-1", "key-ident-1", 1, "user-1", "corr-1");
        var item1 = await queue.EnqueueCommitAsync(req1);

        // Enqueue with same idempotency key returns same item
        var req2 = new EnqueueCommitWorkRequest("job-dup-1", "key-ident-1", 1, "user-1", "corr-2");
        var item2 = await queue.EnqueueCommitAsync(req2);

        Assert.Equal(item1.WorkItemId, item2.WorkItemId);

        // Enqueue with different key throws CommitConflict
        var req3 = new EnqueueCommitWorkRequest("job-dup-1", "different-key", 1, "user-1", "corr-3");
        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() => queue.EnqueueCommitAsync(req3));
        Assert.Equal(ImportErrorCodes.CommitConflict, ex.Code);
    }
}

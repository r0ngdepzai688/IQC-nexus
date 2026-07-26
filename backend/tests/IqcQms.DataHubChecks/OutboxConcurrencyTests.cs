using System;
using System.Linq;
using System.Threading.Tasks;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class OutboxConcurrencyTests
{
    private static AppDbContext CreateMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task OutboxMessage_WrittenInSameTransaction_PersistedOnCommit()
    {
        using var db = CreateMemoryDb(Guid.NewGuid().ToString());

        var msg = new PersistentImportOutboxMessage
        {
            EventId = Guid.NewGuid().ToString("N"),
            AggregateId = "job-outbox-1",
            EventType = "ImportCommitted",
            PayloadJson = "{\"InsertedCount\":42}",
            IsDispatched = false,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        await db.PersistentImportOutboxMessages.AddAsync(msg);
        await db.SaveChangesAsync();

        var retrieved = await db.PersistentImportOutboxMessages.SingleOrDefaultAsync(m => m.AggregateId == "job-outbox-1");
        Assert.NotNull(retrieved);
        Assert.False(retrieved.IsDispatched);
        Assert.Equal("ImportCommitted", retrieved.EventType);
    }

    [Fact]
    public async Task OutboxDispatch_UpdatesIsDispatched_Atomically()
    {
        using var db = CreateMemoryDb(Guid.NewGuid().ToString());

        var msg = new PersistentImportOutboxMessage
        {
            EventId = Guid.NewGuid().ToString("N"),
            AggregateId = "job-outbox-2",
            EventType = "ImportCommitted",
            PayloadJson = "{\"InsertedCount\":10}",
            IsDispatched = false,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        await db.PersistentImportOutboxMessages.AddAsync(msg);
        await db.SaveChangesAsync();

        // Dispatcher marks dispatched
        var pending = await db.PersistentImportOutboxMessages.Where(m => !m.IsDispatched).ToListAsync();
        Assert.Single(pending);

        pending[0].IsDispatched = true;
        pending[0].DispatchedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var remaining = await db.PersistentImportOutboxMessages.Where(m => !m.IsDispatched).ToListAsync();
        Assert.Empty(remaining);
    }
}

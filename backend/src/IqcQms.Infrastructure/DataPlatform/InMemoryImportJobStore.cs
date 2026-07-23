using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class InMemoryImportJobStore : IImportJobStore
{
    private readonly ConcurrentDictionary<string, ImportJobStateRecord> _store = new(StringComparer.Ordinal);

    public Task SaveAsync(ImportJobStateRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        record.UpdatedAt = DateTimeOffset.UtcNow;
        _store[record.Job.JobId] = record;
        return Task.CompletedTask;
    }

    public Task<ImportJobStateRecord?> GetAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return Task.FromResult<ImportJobStateRecord?>(null);
        _store.TryGetValue(jobId, out var record);
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<ImportJobStateRecord>> ListAsync(string? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<ImportJobStateRecord> query = _store.Values;
        if (!string.IsNullOrWhiteSpace(ownerUserId))
        {
            query = query.Where(r => string.Equals(r.Job.OwnerUserId, ownerUserId, StringComparison.OrdinalIgnoreCase));
        }

        var list = query
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<ImportJobStateRecord>>(list);
    }
}

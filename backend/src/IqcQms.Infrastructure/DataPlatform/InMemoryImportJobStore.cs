using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;

namespace IqcQms.Infrastructure.DataPlatform;

/// <summary>
/// Thread-safe in-memory store for import job state records.
/// Development/testing limitation: State is process-local and lost on application restart.
/// Production database commitment is deferred per milestone design.
/// </summary>
public sealed class InMemoryImportJobStore : IImportJobStore
{
    private readonly ConcurrentDictionary<string, ImportJobStateRecord> _store = new(StringComparer.Ordinal);
    private readonly object _syncLock = new();

    public Task SaveAsync(ImportJobStateRecord record, long? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncLock)
        {
            if (_store.TryGetValue(record.Job.JobId, out var existing))
            {
                if (expectedVersion.HasValue && existing.Version != expectedVersion.Value)
                {
                    throw new ImportPlatformException(
                        ImportErrorCodes.CommitConflict,
                        $"Concurrency conflict on job '{record.Job.JobId}'. Expected version {expectedVersion.Value} but found {existing.Version}.");
                }

                record.Version = existing.Version + 1;
            }
            else
            {
                record.Version = 1;
            }

            record.UpdatedAt = DateTimeOffset.UtcNow;
            _store[record.Job.JobId] = CloneRecord(record);
        }

        return Task.CompletedTask;
    }

    public Task<ImportJobStateRecord?> GetAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return Task.FromResult<ImportJobStateRecord?>(null);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncLock)
        {
            if (_store.TryGetValue(jobId, out var record))
            {
                return Task.FromResult<ImportJobStateRecord?>(CloneRecord(record));
            }
        }

        return Task.FromResult<ImportJobStateRecord?>(null);
    }

    public Task<IReadOnlyList<ImportJobStateRecord>> ListAsync(string? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncLock)
        {
            IEnumerable<ImportJobStateRecord> query = _store.Values;
            if (!string.IsNullOrWhiteSpace(ownerUserId))
            {
                query = query.Where(r => string.Equals(r.Job.OwnerUserId, ownerUserId, StringComparison.OrdinalIgnoreCase));
            }

            var list = query
                .OrderByDescending(r => r.CreatedAt)
                .Select(CloneRecord)
                .ToList();

            return Task.FromResult<IReadOnlyList<ImportJobStateRecord>>(list);
        }
    }

    private static ImportJobStateRecord CloneRecord(ImportJobStateRecord rec)
    {
        return new ImportJobStateRecord
        {
            Job = rec.Job,
            Workbook = rec.Workbook,
            MappingProfile = rec.MappingProfile,
            MappingResult = rec.MappingResult,
            ValidationProfile = rec.ValidationProfile,
            ValidationResult = rec.ValidationResult,
            PreviewDetail = rec.PreviewDetail,
            Version = rec.Version,
            CreatedAt = rec.CreatedAt,
            UpdatedAt = rec.UpdatedAt
        };
    }
}

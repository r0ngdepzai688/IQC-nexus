using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Domain.Entities.DataHub;

namespace IqcQms.Application.DataPlatform;

public sealed record EnqueueCommitWorkRequest(
    string JobId,
    string IdempotencyKey,
    long ExpectedVersion,
    string ActorUserId,
    string CorrelationId);

public interface IImportWorkQueue
{
    Task<PersistentImportWorkItem> EnqueueCommitAsync(
        EnqueueCommitWorkRequest request,
        CancellationToken cancellationToken = default);

    Task<PersistentImportWorkItem?> GetActiveWorkItemAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersistentImportWorkItem>> AcquireLeasesAsync(
        string workerId,
        int batchSize = 5,
        TimeSpan? leaseDuration = null,
        CancellationToken cancellationToken = default);

    Task CompleteWorkItemAsync(
        string workItemId,
        string workerId,
        CancellationToken cancellationToken = default);

    Task FailWorkItemAsync(
        string workItemId,
        string workerId,
        string errorCode,
        string errorMessage,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default);

    Task RecoverStaleLeasesAsync(
        TimeSpan staleThreshold,
        CancellationToken cancellationToken = default);
}

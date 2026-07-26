using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class EfImportWorkQueue : IImportWorkQueue
{
    private readonly AppDbContext _context;
    private readonly ILogger<EfImportWorkQueue> _logger;

    public EfImportWorkQueue(AppDbContext context, ILogger<EfImportWorkQueue> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PersistentImportWorkItem> EnqueueCommitAsync(
        EnqueueCommitWorkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existingActive = await _context.PersistentImportWorkItems
            .SingleOrDefaultAsync(w => w.JobId == request.JobId && (w.State == "Pending" || w.State == "Leased"), cancellationToken);

        if (existingActive != null)
        {
            if (string.Equals(existingActive.IdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal))
            {
                return existingActive;
            }

            throw new ImportPlatformException(
                ImportErrorCodes.CommitConflict,
                $"An active commit work item for job '{request.JobId}' already exists.");
        }

        var workItem = new PersistentImportWorkItem
        {
            WorkItemId = Guid.NewGuid().ToString("N"),
            JobId = request.JobId,
            WorkType = "Commit",
            State = "Pending",
            AttemptCount = 0,
            MaxAttempts = 3,
            AvailableAtUtc = DateTimeOffset.UtcNow,
            CorrelationId = request.CorrelationId,
            IdempotencyKey = request.IdempotencyKey,
            ExpectedVersion = request.ExpectedVersion,
            ActorUserId = request.ActorUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await _context.PersistentImportWorkItems.AddAsync(workItem, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Enqueued commit work item '{WorkItemId}' for job '{JobId}'.", workItem.WorkItemId, request.JobId);
        return workItem;
    }

    public async Task<PersistentImportWorkItem?> GetActiveWorkItemAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PersistentImportWorkItems
            .AsNoTracking()
            .OrderByDescending(w => w.CreatedAtUtc)
            .FirstOrDefaultAsync(w => w.JobId == jobId, cancellationToken);
    }

    public async Task<IReadOnlyList<PersistentImportWorkItem>> AcquireLeasesAsync(
        string workerId,
        int batchSize = 5,
        TimeSpan? leaseDuration = null,
        CancellationToken cancellationToken = default)
    {
        var duration = leaseDuration ?? TimeSpan.FromMinutes(2);
        var now = DateTimeOffset.UtcNow;

        var candidateItems = await _context.PersistentImportWorkItems
            .Where(w => w.State == "Pending" && w.AvailableAtUtc <= now)
            .OrderBy(w => w.AvailableAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var leasedItems = new List<PersistentImportWorkItem>();

        foreach (var item in candidateItems)
        {
            item.State = "Leased";
            item.LeaseOwner = workerId;
            item.LeaseExpiresAtUtc = now.Add(duration);
            item.StartedAtUtc = now;
            _context.PersistentImportWorkItems.Update(item);
            leasedItems.Add(item);
        }

        if (leasedItems.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Worker '{WorkerId}' acquired leases for {Count} work items.", workerId, leasedItems.Count);
        }

        return leasedItems;
    }

    public async Task CompleteWorkItemAsync(
        string workItemId,
        string workerId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.PersistentImportWorkItems.SingleOrDefaultAsync(w => w.WorkItemId == workItemId, cancellationToken);
        if (item == null) return;

        item.State = "Completed";
        item.LeaseOwner = null;
        item.LeaseExpiresAtUtc = null;
        item.CompletedAtUtc = DateTimeOffset.UtcNow;

        _context.PersistentImportWorkItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Work item '{WorkItemId}' completed by worker '{WorkerId}'.", workItemId, workerId);
    }

    public async Task FailWorkItemAsync(
        string workItemId,
        string workerId,
        string errorCode,
        string errorMessage,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.PersistentImportWorkItems.SingleOrDefaultAsync(w => w.WorkItemId == workItemId, cancellationToken);
        if (item == null) return;

        item.AttemptCount += 1;
        item.LastErrorCode = errorCode;
        item.LastErrorMessage = errorMessage.Length > 500 ? errorMessage[..500] : errorMessage;
        item.LeaseOwner = null;
        item.LeaseExpiresAtUtc = null;

        if (item.AttemptCount >= item.MaxAttempts)
        {
            item.State = "Poison";
            ImportMetrics.PoisonWorkItems.Add(1);
            _logger.LogWarning("Work item '{WorkItemId}' reached max attempts ({MaxAttempts}) and is marked Poison.", workItemId, item.MaxAttempts);
        }
        else
        {
            item.State = "Pending";
            var delay = retryDelay ?? TimeSpan.FromSeconds(Math.Pow(2, item.AttemptCount) * 2);
            item.AvailableAtUtc = DateTimeOffset.UtcNow.Add(delay);
            ImportMetrics.BackgroundRetries.Add(1);
            _logger.LogInformation("Work item '{WorkItemId}' failed (Attempt {Attempt}/{Max}). Scheduled retry in {Delay}s.", workItemId, item.AttemptCount, item.MaxAttempts, delay.TotalSeconds);
        }

        _context.PersistentImportWorkItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecoverStaleLeasesAsync(
        TimeSpan staleThreshold,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredLeases = await _context.PersistentImportWorkItems
            .Where(w => w.State == "Leased" && w.LeaseExpiresAtUtc.HasValue && w.LeaseExpiresAtUtc.Value < now)
            .ToListAsync(cancellationToken);

        if (expiredLeases.Count > 0)
        {
            foreach (var item in expiredLeases)
            {
                item.State = "Pending";
                item.LeaseOwner = null;
                item.LeaseExpiresAtUtc = null;
                item.AvailableAtUtc = now;
                _context.PersistentImportWorkItems.Update(item);
            }

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Recovered {Count} stale work item leases.", expiredLeases.Count);
        }
    }
}

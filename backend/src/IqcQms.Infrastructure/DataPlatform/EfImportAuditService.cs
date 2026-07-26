using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class EfImportAuditService : IImportAuditService
{
    private readonly AppDbContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EfImportAuditService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AppendEventAsync(
        string jobId,
        string eventType,
        string actorUserId,
        string? fromState,
        string? toState,
        string code,
        string message,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobId);
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(actorUserId);

        string? metadataJson = metadata != null ? JsonSerializer.Serialize(metadata, JsonOptions) : null;

        var entity = new PersistentImportAuditEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            JobId = jobId,
            EventType = eventType,
            ActorUserId = actorUserId,
            FromState = fromState,
            ToState = toState,
            Code = code,
            Message = message,
            SanitizedMetadataJson = metadataJson,
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _context.PersistentImportAuditEvents.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PaginatedAuditResult> GetAuditTrailAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jobId);

        var job = await _context.PersistentImportJobs.AsNoTracking().SingleOrDefaultAsync(j => j.JobId == jobId, cancellationToken);
        if (job == null)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.InternalError,
                $"Import job '{jobId}' was not found.");
        }

        if (!isAdmin && !string.Equals(job.OwnerUserId, actorUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportPlatformException(
                ImportErrorCodes.SessionForbidden,
                "Access to audit trail for this import job is forbidden.");
        }

        var query = _context.PersistentImportAuditEvents
            .AsNoTracking()
            .Where(a => a.JobId == jobId)
            .OrderByDescending(a => a.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var validPageSize = pageSize > 0 ? pageSize : 50;
        var totalPages = (int)Math.Ceiling((double)totalCount / validPageSize);
        var validPage = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

        var items = await query
            .Skip((validPage - 1) * validPageSize)
            .Take(validPageSize)
            .Select(a => new ImportAuditEventDto(
                a.Id,
                a.EventId,
                a.JobId,
                a.EventType,
                a.ActorUserId,
                a.FromState,
                a.ToState,
                a.Code,
                a.Message,
                a.SanitizedMetadataJson,
                a.OccurredAt))
            .ToListAsync(cancellationToken);

        return new PaginatedAuditResult(totalCount, validPage, validPageSize, totalPages, items);
    }
}

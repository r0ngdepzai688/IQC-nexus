using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IqcQms.Application.DataPlatform;

public sealed record ImportAuditEventDto(
    long Id,
    string EventId,
    string JobId,
    string EventType,
    string ActorUserId,
    string? FromState,
    string? ToState,
    string Code,
    string Message,
    string? SanitizedMetadataJson,
    DateTimeOffset OccurredAt);

public sealed record PaginatedAuditResult(
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<ImportAuditEventDto> Items);

public interface IImportAuditService
{
    Task AppendEventAsync(
        string jobId,
        string eventType,
        string actorUserId,
        string? fromState,
        string? toState,
        string code,
        string message,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    Task<PaginatedAuditResult> GetAuditTrailAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);
}

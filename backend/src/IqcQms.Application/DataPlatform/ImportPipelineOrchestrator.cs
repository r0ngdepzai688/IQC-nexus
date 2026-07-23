using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IqcQms.Application.DataPlatform;

public sealed record PaginatedDiagnosticsResult(
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    IReadOnlyList<ValidationDiagnostic> Items);

public interface IImportPipelineOrchestrator
{
    Task<ImportJobStateRecord> CreateJobAsync(
        NormalizedWorkbook workbook,
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<MappingResult> ExecuteMappingAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        MappingProfile profile,
        CancellationToken cancellationToken = default);

    Task<ValidationResult> ExecuteValidationAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        ValidationProfile profile,
        CancellationToken cancellationToken = default);

    Task<ImportPreviewDetail> GeneratePreviewAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        int sampleSize = 50,
        CancellationToken cancellationToken = default);

    Task<ImportJobStateRecord> GetJobAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportJobStateRecord>> ListJobsAsync(
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<PaginatedDiagnosticsResult> GetDiagnosticsAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        int page = 1,
        int pageSize = 50,
        ValidationSeverity? minSeverity = null,
        CancellationToken cancellationToken = default);
}

public sealed class ImportPipelineOrchestrator : IImportPipelineOrchestrator
{
    private readonly IImportJobStore _store;
    private readonly IWorkbookMappingService _mappingService;
    private readonly IImportValidationEngine _validationEngine;
    private readonly IImportPreviewEngine _previewEngine;

    public ImportPipelineOrchestrator(
        IImportJobStore store,
        IWorkbookMappingService mappingService,
        IImportValidationEngine validationEngine,
        IImportPreviewEngine previewEngine)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mappingService = mappingService ?? throw new ArgumentNullException(nameof(mappingService));
        _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
        _previewEngine = previewEngine ?? throw new ArgumentNullException(nameof(previewEngine));
    }

    public async Task<ImportJobStateRecord> CreateJobAsync(
        NormalizedWorkbook workbook,
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        if (string.IsNullOrWhiteSpace(ownerUserId))
            throw new ArgumentException("Owner user ID is required.", nameof(ownerUserId));

        var jobId = $"job-{Guid.NewGuid():N}";
        var job = new ImportJob(jobId, ownerUserId);

        // Transition Created -> Inspecting -> ReadyForMapping
        job.TransitionTo(ImportJobState.Inspecting, cancellationToken);
        job.TransitionTo(ImportJobState.ReadyForMapping, cancellationToken);

        var record = new ImportJobStateRecord
        {
            Job = job,
            Workbook = workbook,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _store.SaveAsync(record, null, cancellationToken);
        return record;
    }

    public async Task<MappingResult> ExecuteMappingAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        MappingProfile profile,
        CancellationToken cancellationToken = default)
    {
        var record = await GetAuthorizedRecordAsync(jobId, actorUserId, isAdmin, cancellationToken);
        var expectedVersion = record.Version;

        if (record.Job.State is not (ImportJobState.ReadyForMapping or ImportJobState.Validating or ImportJobState.ReadyForReview))
        {
            throw new ImportPlatformException(
                ImportErrorCodes.InvalidTransition,
                $"Job {jobId} in state '{record.Job.State}' cannot execute mapping.");
        }

        var mappingResult = await _mappingService.ExecuteMappingAsync(record.Workbook, profile, cancellationToken);

        record.MappingProfile = profile;
        record.MappingResult = mappingResult;
        record.ValidationProfile = null;
        record.ValidationResult = null;
        record.PreviewDetail = null;

        if (record.Job.State == ImportJobState.ReadyForMapping)
        {
            record.Job.TransitionTo(ImportJobState.Validating, cancellationToken);
        }

        await _store.SaveAsync(record, expectedVersion, cancellationToken);
        return mappingResult;
    }

    public async Task<ValidationResult> ExecuteValidationAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        ValidationProfile profile,
        CancellationToken cancellationToken = default)
    {
        var record = await GetAuthorizedRecordAsync(jobId, actorUserId, isAdmin, cancellationToken);
        var expectedVersion = record.Version;

        if (record.MappingResult == null)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.MappingInvalid,
                "Mapping must be performed before running validation.");
        }

        if (record.Job.State != ImportJobState.Validating && record.Job.State != ImportJobState.ReadyForReview)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.InvalidTransition,
                $"Job {jobId} in state '{record.Job.State}' cannot run validation.");
        }

        var validationResult = await _validationEngine.ValidateAsync(record.MappingResult, profile, cancellationToken);

        record.ValidationProfile = profile;
        record.ValidationResult = validationResult;
        record.PreviewDetail = null;

        await _store.SaveAsync(record, expectedVersion, cancellationToken);
        return validationResult;
    }

    public async Task<ImportPreviewDetail> GeneratePreviewAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        int sampleSize = 50,
        CancellationToken cancellationToken = default)
    {
        var record = await GetAuthorizedRecordAsync(jobId, actorUserId, isAdmin, cancellationToken);
        var expectedVersion = record.Version;

        if (record.MappingResult == null)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.MappingInvalid,
                "Mapping must be completed before generating preview.");
        }

        if (record.ValidationResult == null)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.ValidationFailed,
                "Validation must be executed before generating preview.");
        }

        var previewDetail = _previewEngine.GeneratePreview(
            record.Job,
            record.Workbook,
            record.MappingResult,
            record.ValidationResult,
            sampleSize);

        record.PreviewDetail = previewDetail;

        if (record.Job.State == ImportJobState.Validating)
        {
            record.Job.MarkPreviewReady(previewDetail.Attestation.ContentFingerprint);
        }

        await _store.SaveAsync(record, expectedVersion, cancellationToken);
        return previewDetail;
    }

    public async Task<ImportJobStateRecord> GetJobAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        return await GetAuthorizedRecordAsync(jobId, actorUserId, isAdmin, cancellationToken);
    }

    public async Task<IReadOnlyList<ImportJobStateRecord>> ListJobsAsync(
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var owner = isAdmin ? null : actorUserId;
        return await _store.ListAsync(owner, cancellationToken);
    }

    public async Task<PaginatedDiagnosticsResult> GetDiagnosticsAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        int page = 1,
        int pageSize = 50,
        ValidationSeverity? minSeverity = null,
        CancellationToken cancellationToken = default)
    {
        var record = await GetAuthorizedRecordAsync(jobId, actorUserId, isAdmin, cancellationToken);

        if (record.ValidationResult == null)
        {
            return new PaginatedDiagnosticsResult(0, page, pageSize, 0, Array.Empty<ValidationDiagnostic>());
        }

        IEnumerable<ValidationDiagnostic> query = record.ValidationResult.Diagnostics;
        if (minSeverity.HasValue)
        {
            query = query.Where(d => d.Severity >= minSeverity.Value);
        }

        var list = query.ToList();
        var totalCount = list.Count;
        var validPageSize = pageSize > 0 ? pageSize : 50;
        var totalPages = (int)Math.Ceiling((double)totalCount / validPageSize);
        var validPage = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

        var pagedItems = list
            .Skip((validPage - 1) * validPageSize)
            .Take(validPageSize)
            .ToList();

        return new PaginatedDiagnosticsResult(totalCount, validPage, validPageSize, totalPages, pagedItems);
    }

    private async Task<ImportJobStateRecord> GetAuthorizedRecordAsync(
        string jobId,
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var record = await _store.GetAsync(jobId, cancellationToken);
        if (record == null)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.InternalError,
                $"Import job '{jobId}' was not found.");
        }

        if (!isAdmin && !string.Equals(record.Job.OwnerUserId, actorUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportPlatformException(
                ImportErrorCodes.SessionForbidden,
                "Access to this import job is forbidden.");
        }

        return record;
    }
}

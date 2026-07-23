using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class ImportCommitService : IImportCommitEngine
{
    private readonly AppDbContext _context;
    private readonly IImportJobStore _store;
    private readonly IPreviewInvalidationEngine _invalidationEngine;
    private readonly IImportAuditService _auditService;
    private readonly IFailureInjector? _failureInjector;
    private readonly ILogger<ImportCommitService> _logger;

    public ImportCommitService(
        AppDbContext context,
        IImportJobStore store,
        IPreviewInvalidationEngine invalidationEngine,
        IImportAuditService auditService,
        ILogger<ImportCommitService> logger,
        IFailureInjector? failureInjector = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _invalidationEngine = invalidationEngine ?? throw new ArgumentNullException(nameof(invalidationEngine));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _failureInjector = failureInjector;
    }

    public async Task<ImportCommitResult> ExecuteCommitAsync(
        ImportCommitRequest request,
        string actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.JobId))
            throw new ArgumentException("JobId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            throw new ArgumentException("IdempotencyKey is required.", nameof(request));

        // 1. Fetch job record
        var record = await _store.GetAsync(request.JobId, cancellationToken);
        if (record == null)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.InternalError,
                $"Import job '{request.JobId}' was not found.");
        }

        // 2. Ownership / Admin Check
        if (!isAdmin && !string.Equals(record.Job.OwnerUserId, actorUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ImportPlatformException(
                ImportErrorCodes.SessionForbidden,
                "Access to commit this import job is forbidden.");
        }

        // 3. Idempotency Check
        var existingReceipt = await _context.PersistentImportCommitReceipts
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.IdempotencyKey == request.IdempotencyKey, cancellationToken);

        if (existingReceipt != null)
        {
            if (!string.Equals(existingReceipt.JobId, request.JobId, StringComparison.Ordinal))
            {
                throw new ImportPlatformException(
                    ImportErrorCodes.CommitConflict,
                    $"Idempotency key '{request.IdempotencyKey}' collision with job '{existingReceipt.JobId}'.");
            }

            _logger.LogInformation("Idempotent commit replay for job '{JobId}' with key '{Key}'.", request.JobId, request.IdempotencyKey);

            return new ImportCommitResult(
                existingReceipt.JobId,
                existingReceipt.IdempotencyKey,
                existingReceipt.InsertedCount,
                existingReceipt.UpdatedCount,
                existingReceipt.SkippedCount,
                existingReceipt.CommittedAt,
                Replayed: true);
        }

        // 4. State Precondition Check
        if (record.Job.State != ImportJobState.ReadyForReview)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.InvalidTransition,
                $"Import job '{request.JobId}' in state '{record.Job.State}' is not eligible for commit. Must be in 'ReadyForReview'.");
        }

        // 5. Mapping & Validation Results Check
        if (record.MappingResult == null)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.MappingInvalid,
                "Mapping result is missing for this job.");
        }

        if (record.ValidationResult == null || record.ValidationResult.Summary.HasBlockingErrors)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.ValidationFailed,
                "Job contains blocking validation errors and cannot be committed.");
        }

        // 6. Attestation Validity Check
        if (!_invalidationEngine.IsPreviewValid(record))
        {
            throw new ImportPlatformException(
                ImportErrorCodes.PreviewExpired,
                "Preview attestation is stale, invalid, or expired. Regeneration of preview is required before commit.");
        }

        // 7. Concurrency Version Check
        if (record.Version != request.ExpectedVersion)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.CommitConflict,
                $"Concurrency conflict on job '{request.JobId}'. Expected version {request.ExpectedVersion} but found {record.Version}.");
        }

        // 8. Execute Transactional Commit
        using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        try
        {
            await _auditService.AppendEventAsync(
                request.JobId,
                "CommitRequested",
                actorUserId,
                record.Job.State.ToString(),
                "Committing",
                "COMMIT_REQUESTED",
                "Transactional commit requested.",
                new { IdempotencyKey = request.IdempotencyKey, ExpectedVersion = request.ExpectedVersion },
                cancellationToken);

            var dbJob = await _context.PersistentImportJobs
                .SingleOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (dbJob == null)
            {
                throw new ImportPlatformException(ImportErrorCodes.InternalError, "Persistent job entity not found.");
            }

            dbJob.State = "Committing";
            _context.PersistentImportJobs.Update(dbJob);
            await _context.SaveChangesAsync(cancellationToken);

            _failureInjector?.Trigger(FailureInjectionPoint.AfterCommittingState);

            // Create Committed Records
            var committedRecords = new List<CommittedImportRecord>();
            int index = 1;

            foreach (var mappedRec in record.MappingResult.Records)
            {
                var itemCodeField = mappedRec.Fields.FirstOrDefault(f => string.Equals(f.TargetField, "ItemCode", StringComparison.OrdinalIgnoreCase) || string.Equals(f.TargetField, "Part Number", StringComparison.OrdinalIgnoreCase))?.MappedValue?.ToString() ?? $"ITEM-{index:D4}";
                var qtyStr = mappedRec.Fields.FirstOrDefault(f => string.Equals(f.TargetField, "Quantity", StringComparison.OrdinalIgnoreCase))?.MappedValue?.ToString();
                int qty = int.TryParse(qtyStr, out var parsedQty) ? parsedQty : 1;

                var resultField = mappedRec.Fields.FirstOrDefault(f => string.Equals(f.TargetField, "Result", StringComparison.OrdinalIgnoreCase))?.MappedValue?.ToString() ?? "PASS";

                committedRecords.Add(new CommittedImportRecord
                {
                    ImportJobId = request.JobId,
                    SourceRecordIndex = mappedRec.RecordIndex,
                    ItemCode = itemCodeField,
                    Quantity = qty,
                    InspectionDate = DateTime.UtcNow.Date,
                    Result = resultField,
                    MappingProfileVersion = record.MappingResult.ProfileVersion,
                    CommittedAt = DateTimeOffset.UtcNow
                });
                index++;
            }

            await _context.CommittedImportRecords.AddRangeAsync(committedRecords, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _failureInjector?.Trigger(FailureInjectionPoint.AfterTargetRecordInsert);

            // Create Idempotency Receipt
            var receipt = new PersistentImportCommitReceipt
            {
                IdempotencyKey = request.IdempotencyKey,
                JobId = request.JobId,
                ContentFingerprint = record.PreviewDetail?.Attestation.ContentFingerprint ?? "fingerprint",
                CommittedBy = actorUserId,
                InsertedCount = committedRecords.Count,
                UpdatedCount = 0,
                SkippedCount = 0,
                CommittedAt = DateTimeOffset.UtcNow
            };

            await _context.PersistentImportCommitReceipts.AddAsync(receipt, cancellationToken);

            dbJob.State = "Completed";
            dbJob.IsCommitted = true;
            dbJob.CommittedAt = DateTimeOffset.UtcNow;
            dbJob.CommittedBy = actorUserId;
            dbJob.CommitIdempotencyKey = request.IdempotencyKey;
            dbJob.ConcurrencyVersion += 1;

            _context.PersistentImportJobs.Update(dbJob);

            _failureInjector?.Trigger(FailureInjectionPoint.BeforeCompletedTransition);

            await _context.SaveChangesAsync(cancellationToken);

            _failureInjector?.Trigger(FailureInjectionPoint.BeforeTransactionSave);

            await tx.CommitAsync(cancellationToken);

            await _auditService.AppendEventAsync(
                request.JobId,
                "CommitSucceeded",
                actorUserId,
                "Committing",
                "Completed",
                "COMMIT_SUCCEEDED",
                $"Successfully committed {committedRecords.Count} records.",
                new { IdempotencyKey = request.IdempotencyKey, InsertedCount = committedRecords.Count },
                cancellationToken);

            _logger.LogInformation("Import job '{JobId}' successfully committed by user '{User}'.", request.JobId, actorUserId);

            return new ImportCommitResult(
                request.JobId,
                request.IdempotencyKey,
                committedRecords.Count,
                0,
                0,
                DateTimeOffset.UtcNow,
                Replayed: false);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();

            _logger.LogError(ex, "Commit transaction failed for job '{JobId}'. Transaction rolled back.", request.JobId);

            await _auditService.AppendEventAsync(
                request.JobId,
                "CommitFailed",
                actorUserId,
                "Committing",
                record.Job.State.ToString(),
                "COMMIT_FAILED",
                $"Commit transaction failed and was rolled back: {ex.Message}",
                new { IdempotencyKey = request.IdempotencyKey },
                cancellationToken);

            if (ex is ImportPlatformException) throw;
            throw new ImportPlatformException(ImportErrorCodes.CommitFailed, $"Transactional commit failed and was completely rolled back: {ex.Message}");
        }
    }
}

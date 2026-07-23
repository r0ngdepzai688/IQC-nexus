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

public sealed class EfImportJobStore : IImportJobStore
{
    private readonly AppDbContext _context;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public EfImportJobStore(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveAsync(ImportJobStateRecord record, long? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await _context.PersistentImportJobs
            .SingleOrDefaultAsync(j => j.JobId == record.Job.JobId, cancellationToken);

        if (existing != null)
        {
            if (expectedVersion.HasValue && existing.ConcurrencyVersion != expectedVersion.Value)
            {
                throw new ImportPlatformException(
                    ImportErrorCodes.CommitConflict,
                    $"Concurrency conflict on job '{record.Job.JobId}'. Expected version {expectedVersion.Value} but found {existing.ConcurrencyVersion}.");
            }

            existing.State = record.Job.State.ToString();
            existing.MappingProfileId = record.MappingProfile?.ProfileId;
            existing.MappingProfileVersion = record.MappingProfile?.Version;
            existing.ValidationProfileId = record.ValidationProfile?.ProfileId;
            existing.ValidationProfileVersion = record.ValidationProfile?.Version;
            existing.PreviewFingerprint = record.PreviewDetail?.Attestation.ContentFingerprint;
            existing.PreviewAttestationSignature = record.PreviewDetail?.Attestation.Signature;
            existing.PreviewGeneratedAt = record.PreviewDetail?.Attestation.GeneratedAt;
            existing.PreviewExpiresAt = record.PreviewDetail?.Attestation.ExpiresAt;
            existing.MappedRecordCount = record.MappingResult?.MappedRecordCount ?? existing.MappedRecordCount;
            existing.WarningCount = record.ValidationResult?.Summary.WarningCount ?? existing.WarningCount;
            existing.ErrorCount = record.ValidationResult?.Summary.ErrorCount ?? existing.ErrorCount;
            existing.BlockingErrorCount = record.ValidationResult?.Summary.BlockingErrorCount ?? existing.BlockingErrorCount;
            existing.ConcurrencyVersion += 1;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            record.Version = existing.ConcurrencyVersion;

            _context.PersistentImportJobs.Update(existing);
        }
        else
        {
            var entity = new PersistentImportJob
            {
                JobId = record.Job.JobId,
                OwnerUserId = record.Job.OwnerUserId,
                State = record.Job.State.ToString(),
                SourceKind = record.Workbook.SourceKind.ToString(),
                SourceDisplayName = record.Workbook.SourceDisplayName,
                NormalizedContentFingerprint = ComputeWorkbookFingerprint(record.Workbook),
                MappingProfileId = record.MappingProfile?.ProfileId,
                MappingProfileVersion = record.MappingProfile?.Version,
                ValidationProfileId = record.ValidationProfile?.ProfileId,
                ValidationProfileVersion = record.ValidationProfile?.Version,
                PreviewFingerprint = record.PreviewDetail?.Attestation.ContentFingerprint,
                PreviewAttestationSignature = record.PreviewDetail?.Attestation.Signature,
                PreviewGeneratedAt = record.PreviewDetail?.Attestation.GeneratedAt,
                PreviewExpiresAt = record.PreviewDetail?.Attestation.ExpiresAt,
                MappedRecordCount = record.MappingResult?.MappedRecordCount ?? 0,
                WarningCount = record.ValidationResult?.Summary.WarningCount ?? 0,
                ErrorCount = record.ValidationResult?.Summary.ErrorCount ?? 0,
                BlockingErrorCount = record.ValidationResult?.Summary.BlockingErrorCount ?? 0,
                ConcurrencyVersion = 1,
                CreatedAt = record.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            record.Version = 1;
            await _context.PersistentImportJobs.AddAsync(entity, cancellationToken);
        }

        // Save mapped records snapshot payload if present
        if (record.MappingResult != null)
        {
            var existingPayload = await _context.PersistentImportMappedPayloads
                .SingleOrDefaultAsync(p => p.JobId == record.Job.JobId, cancellationToken);

            var jsonPayload = JsonSerializer.Serialize(record.MappingResult, JsonOptions);

            if (existingPayload != null)
            {
                existingPayload.MappingProfileVersion = record.MappingProfile?.Version ?? "1.0";
                existingPayload.CanonicalPayloadJson = jsonPayload;
                _context.PersistentImportMappedPayloads.Update(existingPayload);
            }
            else
            {
                var payloadEntity = new PersistentImportMappedPayload
                {
                    JobId = record.Job.JobId,
                    MappingProfileVersion = record.MappingProfile?.Version ?? "1.0",
                    CanonicalPayloadJson = jsonPayload,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _context.PersistentImportMappedPayloads.AddAsync(payloadEntity, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ImportJobStateRecord?> GetAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId)) return null;

        var entity = await _context.PersistentImportJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(j => j.JobId == jobId, cancellationToken);

        if (entity == null) return null;

        var payloadEntity = await _context.PersistentImportMappedPayloads
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.JobId == jobId, cancellationToken);

        MappingResult? mappingResult = null;
        if (payloadEntity != null && !string.IsNullOrEmpty(payloadEntity.CanonicalPayloadJson))
        {
            try
            {
                mappingResult = JsonSerializer.Deserialize<MappingResult>(payloadEntity.CanonicalPayloadJson, JsonOptions);
            }
            catch
            {
                // Fallback if payload deserialization fails
            }
        }

        Enum.TryParse<ImportJobState>(entity.State, true, out var state);
        Enum.TryParse<DataSourceProviderKind>(entity.SourceKind, true, out var kind);

        var job = new ImportJob(entity.JobId, entity.OwnerUserId);

        // Reconstitute state machine
        if (state != ImportJobState.Created)
        {
            ReconstituteJobState(job, state, entity.PreviewFingerprint);
        }

        var sheet = new NormalizedWorksheet(1, "Main", WorksheetVisibility.Visible, 0, 0, Array.Empty<string>(), Array.Empty<NormalizedRow>());
        var workbook = new NormalizedWorkbook("1.0", kind, entity.SourceDisplayName, new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());

        var valSummary = new ValidationSummary(
            entity.MappedRecordCount,
            entity.MappedRecordCount - (entity.ErrorCount + entity.BlockingErrorCount),
            entity.ErrorCount + entity.BlockingErrorCount,
            0,
            entity.WarningCount,
            entity.ErrorCount,
            entity.BlockingErrorCount);

        var validationProfile = !string.IsNullOrEmpty(entity.ValidationProfileId)
            ? new ValidationProfile(entity.ValidationProfileId, entity.ValidationProfileVersion ?? "1.0", "Validation Profile", Array.Empty<ValidationRuleConfig>())
            : null;

        var validationResult = validationProfile != null
            ? new ValidationResult(validationProfile.ProfileId, validationProfile.Version, valSummary, Array.Empty<ValidationDiagnostic>())
            : (entity.WarningCount > 0 || entity.ErrorCount > 0 || entity.BlockingErrorCount > 0 || entity.MappedRecordCount > 0
                ? new ValidationResult("val-default", "1.0", valSummary, Array.Empty<ValidationDiagnostic>())
                : null);

        var previewAttestation = !string.IsNullOrEmpty(entity.PreviewFingerprint)
            ? new ImportPreviewAttestation(
                entity.JobId,
                entity.OwnerUserId,
                entity.PreviewFingerprint,
                entity.MappingProfileVersion ?? "1.0",
                entity.ValidationProfileVersion ?? "1.0",
                entity.PreviewGeneratedAt ?? entity.CreatedAt,
                entity.PreviewExpiresAt ?? entity.CreatedAt.AddHours(1),
                entity.PreviewAttestationSignature ?? "")
            : null;

        var previewDetail = previewAttestation != null
            ? new ImportPreviewDetail(
                entity.JobId,
                entity.OwnerUserId,
                kind,
                entity.SourceDisplayName,
                entity.MappingProfileId ?? "m1",
                entity.MappingProfileVersion ?? "1.0",
                entity.ValidationProfileId ?? "v1",
                entity.ValidationProfileVersion ?? "1.0",
                entity.PreviewGeneratedAt ?? entity.CreatedAt,
                Array.Empty<WorksheetPreviewSummary>(),
                entity.MappedRecordCount,
                entity.MappedRecordCount,
                entity.WarningCount,
                entity.ErrorCount,
                entity.BlockingErrorCount,
                Array.Empty<RepresentativeRecord>(),
                Array.Empty<ValidationDiagnostic>(),
                previewAttestation)
            : null;

        var mappingProfile = !string.IsNullOrEmpty(entity.MappingProfileId)
            ? new MappingProfile(entity.MappingProfileId, entity.MappingProfileVersion ?? "1.0", "Mapping Profile", "Main", 1, true, Array.Empty<MappingRule>())
            : null;

        return new ImportJobStateRecord
        {
            Job = job,
            Workbook = workbook,
            MappingProfile = mappingProfile,
            MappingResult = mappingResult,
            ValidationProfile = validationProfile,
            ValidationResult = validationResult,
            PreviewDetail = previewDetail,
            Version = entity.ConcurrencyVersion,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<IReadOnlyList<ImportJobStateRecord>> ListAsync(string? ownerUserId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<PersistentImportJob> query = _context.PersistentImportJobs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(ownerUserId))
        {
            query = query.Where(j => j.OwnerUserId == ownerUserId);
        }

        var entities = await query.OrderByDescending(j => j.CreatedAt).ToListAsync(cancellationToken);
        var result = new List<ImportJobStateRecord>();

        foreach (var entity in entities)
        {
            Enum.TryParse<ImportJobState>(entity.State, true, out var state);
            Enum.TryParse<DataSourceProviderKind>(entity.SourceKind, true, out var kind);

            var job = new ImportJob(entity.JobId, entity.OwnerUserId);
            if (state != ImportJobState.Created)
            {
                ReconstituteJobState(job, state, entity.PreviewFingerprint);
            }

            var sheet = new NormalizedWorksheet(1, "Main", WorksheetVisibility.Visible, 0, 0, Array.Empty<string>(), Array.Empty<NormalizedRow>());
            var workbook = new NormalizedWorkbook("1.0", kind, entity.SourceDisplayName, new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());

            result.Add(new ImportJobStateRecord
            {
                Job = job,
                Workbook = workbook,
                Version = entity.ConcurrencyVersion,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            });
        }

        return result;
    }

    private static void ReconstituteJobState(ImportJob job, ImportJobState targetState, string? previewVersion)
    {
        switch (targetState)
        {
            case ImportJobState.Inspecting:
                job.TransitionTo(ImportJobState.Inspecting);
                break;
            case ImportJobState.ReadyForMapping:
                job.TransitionTo(ImportJobState.Inspecting);
                job.TransitionTo(ImportJobState.ReadyForMapping);
                break;
            case ImportJobState.Validating:
                job.TransitionTo(ImportJobState.Inspecting);
                job.TransitionTo(ImportJobState.ReadyForMapping);
                job.TransitionTo(ImportJobState.Validating);
                break;
            case ImportJobState.ReadyForReview:
                job.TransitionTo(ImportJobState.Inspecting);
                job.TransitionTo(ImportJobState.ReadyForMapping);
                job.TransitionTo(ImportJobState.Validating);
                if (!string.IsNullOrEmpty(previewVersion))
                    job.MarkPreviewReady(previewVersion);
                break;
            case ImportJobState.Committing:
                job.TransitionTo(ImportJobState.Inspecting);
                job.TransitionTo(ImportJobState.ReadyForMapping);
                job.TransitionTo(ImportJobState.Validating);
                if (!string.IsNullOrEmpty(previewVersion))
                    job.MarkPreviewReady(previewVersion);
                job.BeginCommit(previewVersion ?? "preview-v1", $"key-{Guid.NewGuid():N}");
                break;
            case ImportJobState.Completed:
                job.TransitionTo(ImportJobState.Inspecting);
                job.TransitionTo(ImportJobState.ReadyForMapping);
                job.TransitionTo(ImportJobState.Validating);
                if (!string.IsNullOrEmpty(previewVersion))
                    job.MarkPreviewReady(previewVersion);
                var key = $"key-{Guid.NewGuid():N}";
                job.BeginCommit(previewVersion ?? "preview-v1", key);
                job.CompleteCommit(key, 0, 0, 0);
                break;
            case ImportJobState.Cancelled:
                job.Cancel();
                break;
        }
    }

    private static string ComputeWorkbookFingerprint(NormalizedWorkbook wb)
    {
        var raw = $"{wb.ProtocolVersion}:{wb.SourceKind}:{wb.SourceDisplayName}:{wb.Worksheets.Count}";
        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

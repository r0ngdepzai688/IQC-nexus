using System;
using System.Threading;
using System.Threading.Tasks;

namespace IqcQms.Application.DataPlatform;

public static class InvalidationReasons
{
    public const string SourceContentChanged = "SOURCE_CONTENT_CHANGED";
    public const string MappingProfileChanged = "MAPPING_PROFILE_CHANGED";
    public const string ValidationProfileChanged = "VALIDATION_PROFILE_CHANGED";
    public const string OwnerScopeChanged = "OWNER_SCOPE_CHANGED";
    public const string AttestationExpired = "ATTESTATION_EXPIRED";
    public const string JobCancelled = "JOB_CANCELLED";
}

public interface IPreviewInvalidationEngine
{
    bool IsPreviewValid(ImportJobStateRecord record);
    bool InvalidatePreview(ImportJobStateRecord record, string reason);
}

public sealed class PreviewInvalidationEngine : IPreviewInvalidationEngine
{
    private readonly IPreviewAttestationService _attestationService;

    public PreviewInvalidationEngine(IPreviewAttestationService attestationService)
    {
        _attestationService = attestationService ?? throw new ArgumentNullException(nameof(attestationService));
    }

    public bool IsPreviewValid(ImportJobStateRecord record)
    {
        if (record == null || record.PreviewDetail == null || record.MappingResult == null || record.ValidationResult == null)
            return false;

        var preview = record.PreviewDetail;
        if (preview.Attestation == null)
            return false;

        if (DateTimeOffset.UtcNow > preview.Attestation.ExpiresAt)
            return false;

        if (record.Job.State != ImportJobState.ReadyForReview)
            return false;

        return _attestationService.VerifyAttestation(
            preview.Attestation,
            record.Job.JobId,
            record.Job.OwnerUserId,
            record.MappingResult.ProfileId,
            record.MappingResult.ProfileVersion,
            record.ValidationResult.ValidationProfileId,
            record.ValidationResult.ValidationProfileVersion,
            record.MappingResult.MappedRecordCount,
            record.ValidationResult.Summary.WarningCount,
            record.ValidationResult.Summary.ErrorCount,
            record.ValidationResult.Summary.BlockingErrorCount);
    }

    public bool InvalidatePreview(ImportJobStateRecord record, string reason)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.PreviewDetail == null && record.Job.State != ImportJobState.ReadyForReview)
            return false;

        record.PreviewDetail = null;

        if (record.Job.State == ImportJobState.ReadyForReview)
        {
            record.Job.TransitionTo(ImportJobState.Validating);
        }

        return true;
    }
}

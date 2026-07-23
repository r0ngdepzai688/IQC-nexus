using System;
using System.Collections.Generic;
using System.Linq;

namespace IqcQms.Application.DataPlatform;

public sealed record RepresentativeRecord(
    int RecordIndex,
    SourceCoordinate RowCoordinate,
    IReadOnlyList<MappedField> Fields,
    IReadOnlyList<ValidationDiagnostic> Diagnostics);

public sealed record WorksheetPreviewSummary(
    string WorksheetName,
    int RowCount,
    int ColumnCount);

public sealed record ImportPreviewAttestation(
    string JobId,
    string OwnerUserId,
    string ContentFingerprint,
    string MappingProfileVersion,
    string ValidationProfileVersion,
    DateTimeOffset GeneratedAt,
    DateTimeOffset ExpiresAt,
    string Signature);

public sealed record ImportPreviewDetail(
    string JobId,
    string OwnerUserId,
    DataSourceProviderKind ProviderKind,
    string SourceDisplayName,
    string MappingProfileId,
    string MappingProfileVersion,
    string ValidationProfileId,
    string ValidationProfileVersion,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<WorksheetPreviewSummary> Worksheets,
    int TotalRecordsProcessed,
    int SampledRecordsCount,
    int TotalWarningsCount,
    int TotalErrorsCount,
    int TotalBlockingErrorsCount,
    IReadOnlyList<RepresentativeRecord> RepresentativeRecords,
    IReadOnlyList<ValidationDiagnostic> DiagnosticsSample,
    ImportPreviewAttestation Attestation)
{
    public bool CanCommit => Attestation != null && TotalErrorsCount == 0 && TotalBlockingErrorsCount == 0;
}

public interface IImportPreviewEngine
{
    ImportPreviewDetail GeneratePreview(
        ImportJob job,
        NormalizedWorkbook workbook,
        MappingResult mappingResult,
        ValidationResult validationResult,
        int sampleSize = 50);

    bool VerifyAttestation(
        ImportPreviewDetail previewDetail,
        MappingResult mappingResult,
        ValidationResult validationResult);
}

public sealed class ImportPreviewEngine : IImportPreviewEngine
{
    private readonly IPreviewAttestationService _attestationService;

    public ImportPreviewEngine(IPreviewAttestationService attestationService)
    {
        _attestationService = attestationService ?? throw new ArgumentNullException(nameof(attestationService));
    }

    public ImportPreviewDetail GeneratePreview(
        ImportJob job,
        NormalizedWorkbook workbook,
        MappingResult mappingResult,
        ValidationResult validationResult,
        int sampleSize = 50)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(mappingResult);
        ArgumentNullException.ThrowIfNull(validationResult);

        var generatedAt = DateTimeOffset.UtcNow;

        var worksheetSummaries = workbook.Worksheets
            .Select(w => new WorksheetPreviewSummary(w.Name, w.RowCount, w.ColumnCount))
            .ToList();

        var sampleRecords = mappingResult.Records
            .Take(Math.Max(1, sampleSize))
            .Select(r => new RepresentativeRecord(
                r.RecordIndex,
                r.RowCoordinate,
                r.Fields,
                validationResult.Diagnostics.Where(d => d.Coordinate != null && d.Coordinate.RowNumber == r.RowCoordinate.RowNumber).ToList()))
            .ToList();

        var sampleDiags = validationResult.Diagnostics.Take(100).ToList();

        var attestation = _attestationService.CreateAttestation(
            job.JobId,
            job.OwnerUserId,
            mappingResult.ProfileId,
            mappingResult.ProfileVersion,
            validationResult.ValidationProfileId,
            validationResult.ValidationProfileVersion,
            mappingResult.MappedRecordCount,
            validationResult.Summary.WarningCount,
            validationResult.Summary.ErrorCount,
            validationResult.Summary.BlockingErrorCount);

        return new ImportPreviewDetail(
            job.JobId,
            job.OwnerUserId,
            workbook.SourceKind,
            workbook.SourceDisplayName,
            mappingResult.ProfileId,
            mappingResult.ProfileVersion,
            validationResult.ValidationProfileId,
            validationResult.ValidationProfileVersion,
            generatedAt,
            worksheetSummaries,
            mappingResult.MappedRecordCount,
            sampleRecords.Count,
            validationResult.Summary.WarningCount,
            validationResult.Summary.ErrorCount,
            validationResult.Summary.BlockingErrorCount,
            sampleRecords,
            sampleDiags,
            attestation);
    }

    public bool VerifyAttestation(
        ImportPreviewDetail previewDetail,
        MappingResult mappingResult,
        ValidationResult validationResult)
    {
        if (previewDetail == null || previewDetail.Attestation == null) return false;

        return _attestationService.VerifyAttestation(
            previewDetail.Attestation,
            previewDetail.JobId,
            previewDetail.OwnerUserId,
            mappingResult.ProfileId,
            mappingResult.ProfileVersion,
            validationResult.ValidationProfileId,
            validationResult.ValidationProfileVersion,
            mappingResult.MappedRecordCount,
            validationResult.Summary.WarningCount,
            validationResult.Summary.ErrorCount,
            validationResult.Summary.BlockingErrorCount);
    }
}

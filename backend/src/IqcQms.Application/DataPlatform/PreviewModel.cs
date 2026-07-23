using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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

    bool VerifyAttestation(ImportPreviewAttestation attestation);
}

public sealed class ImportPreviewEngine : IImportPreviewEngine
{
    private readonly string _signingSecret;

    public ImportPreviewEngine(string? signingSecret = null)
    {
        _signingSecret = signingSecret ?? "IqcNexus_Preview_Attestation_Signing_Secret_2026";
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
        var expiresAt = generatedAt.AddHours(2);

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

        var fingerprintSource = $"{job.JobId}:{job.OwnerUserId}:{mappingResult.ProfileId}:{mappingResult.ProfileVersion}:{validationResult.ValidationProfileId}:{validationResult.ValidationProfileVersion}:{mappingResult.MappedRecordCount}:{validationResult.Summary.WarningCount}:{validationResult.Summary.ErrorCount}:{validationResult.Summary.BlockingErrorCount}";
        var fingerprint = ComputeSha256(fingerprintSource);

        var signatureSource = $"{job.JobId}:{job.OwnerUserId}:{fingerprint}:{generatedAt.ToUnixTimeSeconds()}:{expiresAt.ToUnixTimeSeconds()}";
        var signature = ComputeHmacSha256(signatureSource, _signingSecret);

        var attestation = new ImportPreviewAttestation(
            job.JobId,
            job.OwnerUserId,
            fingerprint,
            mappingResult.ProfileVersion,
            validationResult.ValidationProfileVersion,
            generatedAt,
            expiresAt,
            signature);

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

    public bool VerifyAttestation(ImportPreviewAttestation attestation)
    {
        if (attestation == null) return false;
        if (DateTimeOffset.UtcNow > attestation.ExpiresAt) return false;

        var signatureSource = $"{attestation.JobId}:{attestation.OwnerUserId}:{attestation.ContentFingerprint}:{attestation.GeneratedAt.ToUnixTimeSeconds()}:{attestation.ExpiresAt.ToUnixTimeSeconds()}";
        var expectedSig = ComputeHmacSha256(signatureSource, _signingSecret);

        return string.Equals(expectedSig, attestation.Signature, StringComparison.Ordinal);
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ComputeHmacSha256(string raw, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

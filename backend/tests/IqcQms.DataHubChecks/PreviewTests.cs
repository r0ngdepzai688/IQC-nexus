using System;
using System.Collections.Generic;
using IqcQms.Application.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class PreviewTests
{
    [Fact]
    public void GeneratePreviewCreatesTamperEvidentAttestation()
    {
        var job = new ImportJob("job-001", "user-123");
        var sheet = new NormalizedWorksheet(1, "Sheet1", WorksheetVisibility.Visible, 5, 2, Array.Empty<string>(), Array.Empty<NormalizedRow>());
        var workbook = new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "data.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());

        var mappingResult = new MappingResult("map-v1", "1.0", 5, 5, Array.Empty<MappedRecord>(), Array.Empty<MappingDiagnostic>(), Array.Empty<string>(), Array.Empty<string>());
        var valSummary = new ValidationSummary(5, 5, 0, 0, 1, 0, 0);
        var valResult = new ValidationResult("val-v1", "1.0", valSummary, Array.Empty<ValidationDiagnostic>());

        var previewEngine = new ImportPreviewEngine("SecretKeyForTest");
        var preview = previewEngine.GeneratePreview(job, workbook, mappingResult, valResult, sampleSize: 10);

        Assert.NotNull(preview);
        Assert.Equal("job-001", preview.JobId);
        Assert.Equal("user-123", preview.OwnerUserId);
        Assert.True(preview.CanCommit);
        Assert.NotNull(preview.Attestation);

        var isValidSig = previewEngine.VerifyAttestation(preview.Attestation);
        Assert.True(isValidSig);
    }

    [Fact]
    public void TamperedAttestationFailsVerification()
    {
        var job = new ImportJob("job-002", "user-123");
        var sheet = new NormalizedWorksheet(1, "Sheet1", WorksheetVisibility.Visible, 1, 1, Array.Empty<string>(), Array.Empty<NormalizedRow>());
        var workbook = new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "data.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());

        var mappingResult = new MappingResult("map-v1", "1.0", 1, 1, Array.Empty<MappedRecord>(), Array.Empty<MappingDiagnostic>(), Array.Empty<string>(), Array.Empty<string>());
        var valSummary = new ValidationSummary(1, 1, 0, 0, 0, 0, 0);
        var valResult = new ValidationResult("val-v1", "1.0", valSummary, Array.Empty<ValidationDiagnostic>());

        var previewEngine = new ImportPreviewEngine("SecretKeyForTest");
        var preview = previewEngine.GeneratePreview(job, workbook, mappingResult, valResult);

        var tamperedAttestation = preview.Attestation with { ContentFingerprint = "tampered_fingerprint" };

        var isValidSig = previewEngine.VerifyAttestation(tamperedAttestation);
        Assert.False(isValidSig);
    }
}

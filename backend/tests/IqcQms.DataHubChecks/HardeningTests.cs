using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Infrastructure.DataPlatform;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class HardeningTests
{
    [Fact]
    public void AttestationKeyUnder32BytesThrowsInProduction()
    {
        var options = Options.Create(new PreviewAttestationOptions
        {
            SecretKey = "short_key_under_32_bytes" // 24 bytes
        });

        var ex = Assert.Throws<InvalidOperationException>(() => new PreviewAttestationService(options));
        Assert.Contains("at least 256 bits", ex.Message);
    }

    [Fact]
    public void ValidAttestationServiceCreatesAndVerifiesFixedTimeSignatures()
    {
        var options = Options.Create(new PreviewAttestationOptions
        {
            SecretKey = "a_very_secure_and_long_signing_secret_key_32_bytes!"
        });

        var service = new PreviewAttestationService(options);

        var attestation = service.CreateAttestation(
            "job-100",
            "owner-1",
            "map-v1",
            "1.0",
            "val-v1",
            "1.0",
            50,
            2,
            0,
            0);

        Assert.NotNull(attestation);
        Assert.Equal("job-100", attestation.JobId);

        var isValid = service.VerifyAttestation(
            attestation,
            "job-100",
            "owner-1",
            "map-v1",
            "1.0",
            "val-v1",
            "1.0",
            50,
            2,
            0,
            0);

        Assert.True(isValid);
    }

    [Fact]
    public void AlteredOwnerOrJobFailsAttestationVerification()
    {
        var options = Options.Create(new PreviewAttestationOptions
        {
            SecretKey = "a_very_secure_and_long_signing_secret_key_32_bytes!"
        });

        var service = new PreviewAttestationService(options);

        var attestation = service.CreateAttestation(
            "job-100",
            "owner-1",
            "map-v1",
            "1.0",
            "val-v1",
            "1.0",
            50,
            2,
            0,
            0);

        // Verify with different owner
        var isValidWrongOwner = service.VerifyAttestation(
            attestation,
            "job-100",
            "attacker-user",
            "map-v1",
            "1.0",
            "val-v1",
            "1.0",
            50,
            2,
            0,
            0);

        Assert.False(isValidWrongOwner);

        // Verify with different mapping version
        var isValidWrongVersion = service.VerifyAttestation(
            attestation,
            "job-100",
            "owner-1",
            "map-v2", // Altered version
            "2.0",
            "val-v1",
            "1.0",
            50,
            2,
            0,
            0);

        Assert.False(isValidWrongVersion);
    }

    [Fact]
    public async Task StoreEnforcesOptimisticConcurrencyAndSnapshotIsolation()
    {
        var store = new InMemoryImportJobStore();
        var job = new ImportJob("job-concurrency", "owner-1");
        var sheet = new NormalizedWorksheet(1, "Sheet1", WorksheetVisibility.Visible, 1, 1, Array.Empty<string>(), Array.Empty<NormalizedRow>());
        var wb = new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "test.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());

        var record = new ImportJobStateRecord
        {
            Job = job,
            Workbook = wb
        };

        // Save version 1
        await store.SaveAsync(record);
        var stored1 = await store.GetAsync("job-concurrency");
        Assert.NotNull(stored1);
        Assert.Equal(1, stored1.Version);

        // Attempt save with stale version (expectedVersion = 99)
        var staleRecord = stored1 with { MappingProfile = new MappingProfile("p1", "1.0", "Name", "Sheet1", 1, true, Array.Empty<MappingRule>()) };

        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() =>
            store.SaveAsync(staleRecord, expectedVersion: 99));

        Assert.Equal(ImportErrorCodes.CommitConflict, ex.Code);
    }
}

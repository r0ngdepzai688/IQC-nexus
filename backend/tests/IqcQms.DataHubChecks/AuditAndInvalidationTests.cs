using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.DataPlatform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class AuditAndInvalidationTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source=file:mem_audit_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;

        var context = new AppDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task AuditTrailAppendsAndRetrievesOrderedEvents()
    {
        using var db = CreateInMemoryDbContext();
        db.PersistentImportJobs.Add(new PersistentImportJob { JobId = "job-audit-1", OwnerUserId = "alex.engineer", State = "Created", CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var auditService = new EfImportAuditService(db);

        await auditService.AppendEventAsync("job-audit-1", "Created", "alex.engineer", null, "Created", "JOB_CREATED", "Job initialized.");
        await auditService.AppendEventAsync("job-audit-1", "Mapped", "alex.engineer", "ReadyForMapping", "Validating", "MAPPING_APPLIED", "Mapping profile applied.");

        var res = await auditService.GetAuditTrailAsync("job-audit-1", "alex.engineer", isAdmin: false);

        Assert.Equal(2, res.TotalCount);
        Assert.Equal("Mapped", res.Items[0].EventType); // Ordered descending by timestamp
        Assert.Equal("Created", res.Items[1].EventType);
    }

    [Fact]
    public void PreviewInvalidationDetectsStaleAndInvalidatedStates()
    {
        var attestationService = new PreviewAttestationService(Options.Create(new PreviewAttestationOptions { SecretKey = "secret_key_minimum_32_bytes_long_string!" }));
        var invalidationEngine = new PreviewInvalidationEngine(attestationService);

        var job = new ImportJob("job-inv-1", "user-1");
        job.TransitionTo(ImportJobState.Inspecting);
        job.TransitionTo(ImportJobState.ReadyForMapping);
        job.TransitionTo(ImportJobState.Validating);

        var attestation = attestationService.CreateAttestation("job-inv-1", "user-1", "m1", "1.0", "v1", "1.0", 10, 0, 0, 0);
        job.MarkPreviewReady(attestation.ContentFingerprint);

        var sheet = new NormalizedWorksheet(1, "Main", WorksheetVisibility.Visible, 1, 1, Array.Empty<string>(), Array.Empty<NormalizedRow>());
        var wb = new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "synthetic.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());
        var mapResult = new MappingResult("m1", "1.0", 10, 10, Array.Empty<MappedRecord>(), Array.Empty<MappingDiagnostic>(), Array.Empty<string>(), Array.Empty<string>());
        var valResult = new ValidationResult("v1", "1.0", new ValidationSummary(10, 10, 0, 0, 0, 0, 0), Array.Empty<ValidationDiagnostic>());

        var preview = new ImportPreviewDetail(
            "job-inv-1", "user-1", DataSourceProviderKind.Csv, "synthetic.csv",
            "m1", "1.0", "v1", "1.0", DateTimeOffset.UtcNow, Array.Empty<WorksheetPreviewSummary>(),
            10, 10, 0, 0, 0, Array.Empty<RepresentativeRecord>(), Array.Empty<ValidationDiagnostic>(), attestation);

        var record = new ImportJobStateRecord
        {
            Job = job,
            Workbook = wb,
            MappingResult = mapResult,
            ValidationResult = valResult,
            PreviewDetail = preview
        };

        Assert.True(invalidationEngine.IsPreviewValid(record));

        // Invalidate Preview
        invalidationEngine.InvalidatePreview(record, InvalidationReasons.MappingProfileChanged);

        Assert.Null(record.PreviewDetail);
        Assert.False(invalidationEngine.IsPreviewValid(record));
    }
}

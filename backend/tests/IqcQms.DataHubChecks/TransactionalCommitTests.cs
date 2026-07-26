using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.DataPlatform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class TransactionalCommitTests
{
    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source=file:mem_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;

        var context = new AppDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task SuccessfulTransactionalCommitInsertsRecordsAndReceipt()
    {
        using var db = CreateInMemoryDbContext();
        var store = new EfImportJobStore(db);
        var attestationService = new PreviewAttestationService(Options.Create(new PreviewAttestationOptions
        {
            SecretKey = "secret_key_minimum_32_bytes_long_string!"
        }));
        var previewEngine = new ImportPreviewEngine(attestationService);
        var invalidationEngine = new PreviewInvalidationEngine(attestationService);
        var auditService = new EfImportAuditService(db);
        var commitService = new ImportCommitService(db, store, invalidationEngine, auditService, NullLogger<ImportCommitService>.Instance);

        // 1. Setup Job
        var job = new ImportJob("job-commit-1", "user-1");
        job.TransitionTo(ImportJobState.Inspecting);
        job.TransitionTo(ImportJobState.ReadyForMapping);
        job.TransitionTo(ImportJobState.Validating);

        var sheet = new NormalizedWorksheet(1, "Main", WorksheetVisibility.Visible, 2, 2, Array.Empty<string>(), new[]
        {
            new NormalizedRow(1, new[] { new NormalizedCell(1, 1, NormalizedCellRawType.String, "Part Number", null, null, null, null, null, false), new NormalizedCell(1, 2, NormalizedCellRawType.String, "Qty", null, null, null, null, null, false) }, false),
            new NormalizedRow(2, new[]
            {
                new NormalizedCell(2, 1, NormalizedCellRawType.String, "PN-100", null, null, null, null, null, false),
                new NormalizedCell(2, 2, NormalizedCellRawType.String, "50", null, null, null, null, null, false)
            }, false)
        });
        var wb = new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "synthetic.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());

        var mappingProfile = new MappingProfile("map-1", "1.0", "Map", "Main", 1, true, new[]
        {
            new MappingRule("Part Number", "ItemCode", true, FieldTransformationType.TrimText),
            new MappingRule("Qty", "Quantity", true, FieldTransformationType.ParseInteger)
        });

        var mappingService = new WorkbookMappingService();
        var mappingResult = await mappingService.ExecuteMappingAsync(wb, mappingProfile);

        var valProfile = new ValidationProfile("val-1", "1.0", "Val", new[]
        {
            new ValidationRuleConfig("r1", "Qty", ValidationRuleKind.NumericRange, ValidationSeverity.Warning, "Quantity", MinNumeric: 1)
        });
        var valEngine = new ImportValidationEngine();
        var valResult = await valEngine.ValidateAsync(mappingResult, valProfile);

        var preview = previewEngine.GeneratePreview(job, wb, mappingResult, valResult);
        job.MarkPreviewReady(preview.Attestation.ContentFingerprint);

        var record = new ImportJobStateRecord
        {
            Job = job,
            Workbook = wb,
            MappingProfile = mappingProfile,
            MappingResult = mappingResult,
            ValidationProfile = valProfile,
            ValidationResult = valResult,
            PreviewDetail = preview
        };
        await store.SaveAsync(record);

        // 2. Execute Commit
        var commitReq = new ImportCommitRequest("job-commit-1", "idempotency-key-100", ExpectedVersion: 1);
        var result = await commitService.ExecuteCommitAsync(commitReq, "user-1", isAdmin: false);

        Assert.NotNull(result);
        Assert.Equal("job-commit-1", result.JobId);
        Assert.Equal("idempotency-key-100", result.IdempotencyKey);
        Assert.Equal(1, result.InsertedCount);
        Assert.False(result.Replayed);

        // 3. Verify Database Persistence
        var committedRecs = await db.CommittedImportRecords.Where(r => r.ImportJobId == "job-commit-1").ToListAsync();
        Assert.Single(committedRecs);
        Assert.Equal("PN-100", committedRecs[0].ItemCode);
        Assert.Equal(50, committedRecs[0].Quantity);

        var receipt = await db.PersistentImportCommitReceipts.SingleOrDefaultAsync(r => r.IdempotencyKey == "idempotency-key-100");
        Assert.NotNull(receipt);
        Assert.Equal("job-commit-1", receipt.JobId);
    }

    [Fact]
    public async Task IdempotentCommitReplaysSameResultWithoutDuplicateRecords()
    {
        using var db = CreateInMemoryDbContext();
        var store = new EfImportJobStore(db);
        var attestationService = new PreviewAttestationService(Options.Create(new PreviewAttestationOptions { SecretKey = "secret_key_minimum_32_bytes_long_string!" }));
        var previewEngine = new ImportPreviewEngine(attestationService);
        var invalidationEngine = new PreviewInvalidationEngine(attestationService);
        var auditService = new EfImportAuditService(db);
        var commitService = new ImportCommitService(db, store, invalidationEngine, auditService, NullLogger<ImportCommitService>.Instance);

        var job = new ImportJob("job-commit-2", "user-1");
        job.TransitionTo(ImportJobState.Inspecting);
        job.TransitionTo(ImportJobState.ReadyForMapping);
        job.TransitionTo(ImportJobState.Validating);

        var sheet = new NormalizedWorksheet(1, "Main", WorksheetVisibility.Visible, 2, 2, Array.Empty<string>(), new[] { new NormalizedRow(1, new[] { new NormalizedCell(1, 1, NormalizedCellRawType.String, "Part Number", null, null, null, null, null, false), new NormalizedCell(1, 2, NormalizedCellRawType.String, "Qty", null, null, null, null, null, false) }, false), new NormalizedRow(2, new[] { new NormalizedCell(2, 1, NormalizedCellRawType.String, "PN-200", null, null, null, null, null, false), new NormalizedCell(2, 2, NormalizedCellRawType.String, "10", null, null, null, null, null, false) }, false) });
        var wb = new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "synthetic.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());
        var mappingProfile = new MappingProfile("m1", "1.0", "Map", "Main", 1, true, new[] { new MappingRule("Part Number", "ItemCode", true, FieldTransformationType.TrimText) });
        var mappingResult = await new WorkbookMappingService().ExecuteMappingAsync(wb, mappingProfile);
        var valProfile = new ValidationProfile("v1", "1.0", "Val", Array.Empty<ValidationRuleConfig>());
        var valResult = await new ImportValidationEngine().ValidateAsync(mappingResult, valProfile);
        var preview = previewEngine.GeneratePreview(job, wb, mappingResult, valResult);
        job.MarkPreviewReady(preview.Attestation.ContentFingerprint);

        var record = new ImportJobStateRecord { Job = job, Workbook = wb, MappingProfile = mappingProfile, MappingResult = mappingResult, ValidationProfile = valProfile, ValidationResult = valResult, PreviewDetail = preview };
        await store.SaveAsync(record);

        // First Commit
        var req1 = new ImportCommitRequest("job-commit-2", "key-replay-1", ExpectedVersion: 1);
        var res1 = await commitService.ExecuteCommitAsync(req1, "user-1", isAdmin: false);
        Assert.False(res1.Replayed);

        // Second Commit (Same key)
        var req2 = new ImportCommitRequest("job-commit-2", "key-replay-1", ExpectedVersion: 2);
        var res2 = await commitService.ExecuteCommitAsync(req2, "user-1", isAdmin: false);
        Assert.True(res2.Replayed);
        Assert.Equal(res1.InsertedCount, res2.InsertedCount);

        // Verify committed record count remains 1
        var count = await db.CommittedImportRecords.CountAsync(r => r.ImportJobId == "job-commit-2");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task FailureInjectionRollsBackTransactionCompletely()
    {
        using var db = CreateInMemoryDbContext();
        var store = new EfImportJobStore(db);
        var attestationService = new PreviewAttestationService(Options.Create(new PreviewAttestationOptions { SecretKey = "secret_key_minimum_32_bytes_long_string!" }));
        var previewEngine = new ImportPreviewEngine(attestationService);
        var invalidationEngine = new PreviewInvalidationEngine(attestationService);
        var auditService = new EfImportAuditService(db);
        var failureInjector = new TestFailureInjector { InjectionPoint = FailureInjectionPoint.AfterTargetRecordInsert };

        var commitService = new ImportCommitService(db, store, invalidationEngine, auditService, NullLogger<ImportCommitService>.Instance, failureInjector);

        var job = new ImportJob("job-fail-1", "user-1");
        job.TransitionTo(ImportJobState.Inspecting);
        job.TransitionTo(ImportJobState.ReadyForMapping);
        job.TransitionTo(ImportJobState.Validating);

        var sheet = new NormalizedWorksheet(1, "Main", WorksheetVisibility.Visible, 2, 2, Array.Empty<string>(), new[] { new NormalizedRow(1, new[] { new NormalizedCell(1, 1, NormalizedCellRawType.String, "Part Number", null, null, null, null, null, false), new NormalizedCell(1, 2, NormalizedCellRawType.String, "Qty", null, null, null, null, null, false) }, false), new NormalizedRow(2, new[] { new NormalizedCell(2, 1, NormalizedCellRawType.String, "PN-300", null, null, null, null, null, false), new NormalizedCell(2, 2, NormalizedCellRawType.String, "20", null, null, null, null, null, false) }, false) });
        var wb = new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "synthetic.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());
        var mappingProfile = new MappingProfile("m1", "1.0", "Map", "Main", 1, true, new[] { new MappingRule("Part Number", "ItemCode", true, FieldTransformationType.TrimText) });
        var mappingResult = await new WorkbookMappingService().ExecuteMappingAsync(wb, mappingProfile);
        var valProfile = new ValidationProfile("v1", "1.0", "Val", Array.Empty<ValidationRuleConfig>());
        var valResult = await new ImportValidationEngine().ValidateAsync(mappingResult, valProfile);
        var preview = previewEngine.GeneratePreview(job, wb, mappingResult, valResult);
        job.MarkPreviewReady(preview.Attestation.ContentFingerprint);

        var record = new ImportJobStateRecord { Job = job, Workbook = wb, MappingProfile = mappingProfile, MappingResult = mappingResult, ValidationProfile = valProfile, ValidationResult = valResult, PreviewDetail = preview };
        await store.SaveAsync(record);

        // Attempt commit with failure injection
        var req = new ImportCommitRequest("job-fail-1", "key-fail-1", ExpectedVersion: 1);
        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() => commitService.ExecuteCommitAsync(req, "user-1", isAdmin: false));

        Assert.Equal(ImportErrorCodes.CommitFailed, ex.Code);

        // Verify complete transaction rollback
        var committedRecs = await db.CommittedImportRecords.Where(r => r.ImportJobId == "job-fail-1").ToListAsync();
        Assert.Empty(committedRecs);

        var receipt = await db.PersistentImportCommitReceipts.SingleOrDefaultAsync(r => r.IdempotencyKey == "key-fail-1");
        Assert.Null(receipt);
    }
}

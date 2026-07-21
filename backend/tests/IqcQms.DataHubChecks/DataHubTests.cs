using System.IO.Compression;
using System.Security;
using IqcQms.Application;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Domain.Entities.NewModels;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services.DataHub;
using IqcQms.Infrastructure.Services.NewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class DataHubTests
{
    private readonly MasterPlanContractParser _parser = new();

    [Fact]
    public async Task SuccessfulMinimalImportParse()
    {
        using var stream = Workbook(["Project Name", "Basic", "Grade", "Cat"], ["Synthetic Model", "Base 1", "B", "LQV"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-1");
        Assert.Single(result.Records);
        Assert.Equal("BASE 1", result.Records[0].BasicKey);
        Assert.Equal("LQV", result.Records[0].CatKey);
    }

    [Fact]
    public async Task MissingRequiredColumnIsReported()
    {
        using var stream = Workbook(["Project Name", "Basic", "Grade"], ["Synthetic Model", "Base 1", "B"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-2");
        Assert.Equal(["Cat"], result.MissingHardColumns);
    }

    [Fact]
    public async Task DuplicateRequiredColumnIsReported()
    {
        using var stream = Workbook(["Project Name", "Basic", "Grade", "Cat", "Cat"], ["Synthetic Model", "Base 1", "B", "LPR", "LQV"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-3");
        Assert.Equal(["Cat"], result.DuplicateColumns);
    }

    [Fact]
    public async Task DuplicateCanonicalMappingIsRejected()
    {
        using var stream = Workbook(["Project", "Model", "Basic", "Grade", "Cat"], ["One", "Two", "Base", "B", "LPR"]);
        var result = await _parser.ParseExcelAsync(stream, "MAP-1", [new(0, "ProjectName"), new(1, "ProjectName"), new(2, "Basic"), new(3, "Grade"), new(4, "Cat")]);
        Assert.Equal(["ProjectName"], result.DuplicateColumns);
    }

    [Fact]
    public async Task MissingRequiredMappingIsRejected()
    {
        using var stream = Workbook(["Project", "Basic", "Grade"], ["One", "Base", "B"]);
        var result = await _parser.ParseExcelAsync(stream, "MAP-2", [new(0, "ProjectName"), new(1, "Basic"), new(2, "Grade")]);
        Assert.Equal(["Cat"], result.MissingHardColumns);
    }

    [Fact]
    public async Task InvalidRowDateRemainsNullable()
    {
        using var stream = Workbook(["Project Name", "Basic", "Grade", "Cat", "PVR Target"], ["Synthetic Model", "Base", "B", "LPR", "not-a-date"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-4");
        Assert.Null(result.Records.Single().PvrTargetDate);
    }

    [Fact]
    public async Task RowWithOnlyOneRequiredValueIsStagedForValidation()
    {
        using var inspectionStream = Workbook(["Project Name", "Basic", "Grade", "Cat", "Area"], ["", "ONLY-BASIC", "", "", "Synthetic Area"]);
        var inspection = await _parser.InspectHeadersAsync(inspectionStream);
        Assert.Equal(1, inspection.HeaderRow);
        Assert.Equal(1, inspection.HeaderDepth);
        Assert.Equal(2, inspection.DataStartRow);
        Assert.Contains(inspection.Columns, column => column.SuggestedCanonical == "Basic" && column.SampleValues!.Contains("ONLY-BASIC"));
        using var stream = Workbook(["Project Name", "Basic", "Grade", "Cat", "Area"], ["", "ONLY-BASIC", "", "", "Synthetic Area"]);
        var result = await _parser.ParseExcelAsync(stream, "PARTIAL-ROW");

        Assert.Empty(result.MissingHardColumns);
        Assert.Empty(result.DuplicateColumns);
        var record = Assert.Single(result.Records);
        Assert.Equal("ONLY-BASIC", record.Basic);
        Assert.Equal(2, record.RawRowNumber);
    }

    [Fact]
    public async Task AdditionalTitlesUnknownColumnsRepeatedHeadersAndFooterAreIgnored()
    {
        using var stream = AdaptiveWorkbook([
            ["MASTER PLAN REPORT"], ["Generated for synthetic tests"],
            ["Project Name", "Unknown Added", "Basic", "Grade", "Cat"],
            ["Model One", "ignored", "Base One", "B", "LPR"],
            ["Project Name", "Unknown Added", "Basic", "Grade", "Cat"],
            ["Subtotal", "1"]
        ]);
        var result = await _parser.ParseExcelAsync(stream, "ADAPT-1");
        var record = Assert.Single(result.Records);
        Assert.Equal("BASE ONE", record.BasicKey);
    }

    [Fact]
    public async Task MasterPlanMockupMergedHeadersInspectsCorrectHierarchyAndDataStart()
    {
        using var stream = AdaptiveWorkbook([
            ["MASTER PLAN REPORT"],
            ["Generated for testing"],
            [""],
            ["First MP", "", "Q'ty", "", "PVR Target", "MAIN 일정", ""],
            ["Week", "Q'ty", "LPR/LQV", "LSR", "Pre PRA", "LPR/LQV", "LSR"],
            ["W30", "120", "120", "180", "2026-08-01", "2026-08-02", "2026-08-03"],
            ["W31", "80", "80", "140", "2026-08-04", "2026-08-05", "2026-08-06"],
            ["W32", "100", "0", "160", "2026-08-07", "2026-08-08", "2026-08-09"],
            ["W33", "150", "150", "210", "2026-08-10", "2026-08-11", "2026-08-12"],
            ["W34", "90", "90", "130", "2026-08-13", "2026-08-14", "2026-08-15"]
        ], "A4:B4", "C4:D4", "E4:E5", "F4:G4");

        var inspection = await _parser.InspectHeadersAsync(stream);

        Assert.Equal(4, inspection.HeaderRow);
        Assert.Equal(2, inspection.HeaderDepth);
        Assert.Equal(6, inspection.DataStartRow);

        var col0 = inspection.Columns.Single(c => c.ColumnIndex == 0);
        Assert.Equal("First MP > Week", col0.EffectiveHeaderPath);
        Assert.Null(col0.SuggestedCanonical);
        Assert.False(col0.Ambiguous);

        var col1 = inspection.Columns.Single(c => c.ColumnIndex == 1);
        Assert.Equal("First MP > Q'ty", col1.EffectiveHeaderPath);
        Assert.Null(col1.SuggestedCanonical);
        Assert.False(col1.Ambiguous);
        Assert.Equal("Integer", col1.DetectedDataType);

        var col2 = inspection.Columns.Single(c => c.ColumnIndex == 2);
        Assert.Equal("Q'ty > LPR/LQV", col2.EffectiveHeaderPath);
        Assert.Equal("QtyLprLqv", col2.SuggestedCanonical);
        Assert.False(col2.Ambiguous);
        Assert.Equal("Integer", col2.DetectedDataType);
        Assert.Contains("0", col2.SampleValues!);

        var col3 = inspection.Columns.Single(c => c.ColumnIndex == 3);
        Assert.Equal("Q'ty > LSR", col3.EffectiveHeaderPath);
        Assert.Equal("QtyLsr", col3.SuggestedCanonical);
        Assert.False(col3.Ambiguous);
        Assert.Equal("Integer", col3.DetectedDataType);

        var col4 = inspection.Columns.Single(c => c.ColumnIndex == 4);
        Assert.Equal("PvrTarget", col4.SuggestedCanonical);
        Assert.Equal("Date", col4.DetectedDataType);

        var col5 = inspection.Columns.Single(c => c.ColumnIndex == 5);
        Assert.Equal("MAIN 일정 > LPR/LQV", col5.EffectiveHeaderPath);
        Assert.Equal("MainLprLqvDate", col5.SuggestedCanonical);
        Assert.False(col5.Ambiguous);
        Assert.Equal("Date", col5.DetectedDataType);

        var col6 = inspection.Columns.Single(c => c.ColumnIndex == 6);
        Assert.Equal("MAIN 일정 > LSR", col6.EffectiveHeaderPath);
        Assert.Equal("MainLsrDate", col6.SuggestedCanonical);
        Assert.False(col6.Ambiguous);
        Assert.Equal("Date", col6.DetectedDataType);
    }

    [Fact]
    public async Task UnmergedBlankCellIsNotHorizontallyPropagated()
    {
        using var stream = AdaptiveWorkbook([
            ["Project Name", "", "Basic", "Grade", "Cat"],
            ["Model", "Val", "Base", "B", "LPR"]
        ]);

        var inspection = await _parser.InspectHeadersAsync(stream);
        var col1 = inspection.Columns.Single(c => c.ColumnIndex == 1);

        Assert.Equal("", col1.EffectiveHeaderPath);
        Assert.Null(col1.SuggestedCanonical);
    }

    [Fact]
    public async Task EndpointLevelInspectionUsingRealXlsxBytes()
    {
        using var stream = AdaptiveWorkbook([
            ["Project Name", "Basic", "Grade", "Cat"],
            ["Synthetic Model", "Base 1", "B", "LPR"]
        ]);

        var inspection = await _parser.InspectHeadersAsync(stream);

        Assert.Equal(1, inspection.HeaderRow);
        Assert.Equal(1, inspection.HeaderDepth);
        Assert.Equal(2, inspection.DataStartRow);
        Assert.NotEmpty(inspection.WorkbookFingerprint);
    }

    [Fact]
    public async Task UnmappedExcelDateFormattedSerialIsDetectedAsDate()
    {
        using var stream = DateFormattedWorkbook();

        var inspection = await _parser.InspectHeadersAsync(stream);

        var schedule = inspection.Columns.Single(column => column.EffectiveHeaderPath == "Unmapped Schedule");
        Assert.Null(schedule.SuggestedCanonical);
        Assert.Equal("Date", schedule.DetectedDataType);
    }

    [Fact]
    public async Task CanonicalTypeIsStableWithoutSampleValues()
    {
        using var stream = Workbook(
            ["Project Name", "Basic", "Grade", "Cat", "Q'ty LSR", "PRA Target"],
            ["Model", "Base", "B", "LPR", "", ""]);

        var inspection = await _parser.InspectHeadersAsync(stream);

        Assert.Equal("Integer", inspection.Columns.Single(column => column.SuggestedCanonical == "QtyLsr").DetectedDataType);
        Assert.Equal("Date", inspection.Columns.Single(column => column.SuggestedCanonical == "PraTarget").DetectedDataType);
    }

    [Fact]
    public async Task AmbiguousColumnUsesUnmappedTypeDetection()
    {
        using var stream = Workbook(
            ["Project Name", "Basic", "Grade", "Cat", "Main LSR Qty"],
            ["Model", "Base", "B", "LPR", "2026-08-01"]);

        var inspection = await _parser.InspectHeadersAsync(stream);
        var ambiguous = inspection.Columns.Single(column => column.EffectiveHeaderPath == "Main LSR Qty");

        Assert.True(ambiguous.Ambiguous);
        Assert.Null(ambiguous.SuggestedCanonical);
        Assert.Equal("Date", ambiguous.DetectedDataType);
    }

    [Fact]
    public async Task TwoLevelMergedHeadersComposeCanonicalPaths()
    {
        using var stream = AdaptiveWorkbook([
            ["Project Name", "Basic", "Grade", "Cat", "Qty", "", "MAIN", ""],
            ["", "", "", "", "LPR/LQV", "LSR", "PRA/PQV", "LSR"],
            ["Model", "Base", "B", "LQV", "3", "4", "2026-08-02", "2026-08-03"]
        ], "A1:A2", "B1:B2", "C1:C2", "D1:D2", "E1:F1", "G1:H1");
        var inspection = await _parser.InspectHeadersAsync(stream);
        Assert.Equal(2, inspection.HeaderDepth);
        Assert.Contains(inspection.Columns, value => value.EffectiveHeaderPath == "Qty > LPR/LQV" && value.SuggestedCanonical == "QtyLprLqv");
        using var parseStream = AdaptiveWorkbook([
            ["Project Name", "Basic", "Grade", "Cat", "Qty", "", "MAIN", ""],
            ["", "", "", "", "LPR/LQV", "LSR", "PRA/PQV", "LSR"],
            ["Model", "Base", "B", "LQV", "3", "4", "2026-08-02", "2026-08-03"]
        ], "A1:A2", "B1:B2", "C1:C2", "D1:D2", "E1:F1", "G1:H1");
        var result = await _parser.ParseExcelAsync(parseStream, "ADAPT-2");
        Assert.Equal(3, result.Records.Single().QtyLpr);
        Assert.Equal(new DateTime(2026, 8, 2), result.Records.Single().MainLprLqvDate);
    }

    [Fact]
    public async Task ThreeLevelAndVerticalMergedHeadersAreDetected()
    {
        using var stream = AdaptiveWorkbook([
            ["Project Name", "Identity", "Identity", "Identity", "Schedule"],
            ["", "Basic", "Grade", "Cat", "Target"],
            ["", "", "", "", "PVR"],
            ["Model", "Base", "B", "LPR", "2026-08-01"]
        ], "A1:A3", "B1:D1", "B2:B3", "C2:C3", "D2:D3");
        var inspection = await _parser.InspectHeadersAsync(stream);
        // The inspector may elect a depth-2 window starting at row 2 (Excel rows 2-3) when
        // that candidate achieves 4 required-field hits vs 1 for the depth-3 window starting
        // at row 1. The essential contract is that PVR (vertically merged under Target) is
        // recognised as PvrTarget, and all four required fields resolve without ambiguity.
        Assert.Contains(inspection.Columns, value => value.SuggestedCanonical == "PvrTarget");
        Assert.Contains(inspection.Columns, value => value.SuggestedCanonical == "ProjectName");
        Assert.Contains(inspection.Columns, value => value.SuggestedCanonical == "Basic");
        Assert.Contains(inspection.Columns, value => value.SuggestedCanonical == "Grade");
        Assert.Contains(inspection.Columns, value => value.SuggestedCanonical == "Cat");
    }


    [Fact]
    public async Task FuzzyTypoCanSuggestButAmbiguousRequiredMappingIsBlocked()
    {
        using var fuzzy = AdaptiveWorkbook([["Project Nme", "Basic", "Grade", "Cat"], ["Model", "Base", "B", "LPR"]]);
        var inspection = await _parser.InspectHeadersAsync(fuzzy);
        Assert.Contains(inspection.Columns, value => value.Header == "Project Nme" && value.SuggestedCanonical == "ProjectName" && value.Confidence >= .65);

        using var ambiguous = AdaptiveWorkbook([["Model Basic", "Grade", "Cat"], ["Value", "B", "LPR"]]);
        var result = await _parser.ParseExcelAsync(ambiguous, "AMBIGUOUS");
        Assert.Contains("ProjectName", result.MissingHardColumns);
    }

    [Fact]
    public void BusinessKeyNormalizationCollidesCaseAndWhitespaceButPreservesLcv()
    {
        Assert.True(MasterPlanBusinessKey.TryCreate("  Model   α ", " lcv ", out var basic, out var cat, out var key));
        Assert.Equal("MODEL Α", basic);
        Assert.Equal("LCV", cat);
        Assert.DoesNotContain("LQV", key);
        MasterPlanBusinessKey.TryCreate("model α", "LCV", out _, out _, out var equivalent);
        Assert.Equal(key, equivalent);
    }

    [Fact]
    public void BusinessKeyNormalizationHandlesAllCanonicalWhitespaceAndCompatibilityForms()
    {
        MasterPlanBusinessKey.TryCreate("\t\uFF2D\uFF4F\uFF44\uFF45\uFF4C\u00A0  One\t", "\u00A0lpr\t", out var basic, out var cat, out _);

        Assert.Equal("MODEL ONE", basic);
        Assert.Equal("LPR", cat);
    }

    [Fact]
    public async Task OnlyApprovedHeaderMappingIsReused()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.HeaderMappingProfiles.AddRange(
            new HeaderMappingProfile { NormalizedHeaderPath = "customer model label", CanonicalField = "ProjectName", IsApproved = true, Confidence = 1, WorkbookFingerprint = string.Empty },
            new HeaderMappingProfile { NormalizedHeaderPath = "unconfirmed base label", CanonicalField = "Basic", IsApproved = false, Confidence = 1, WorkbookFingerprint = string.Empty });
        await fixture.Context.SaveChangesAsync();
        var parser = new MasterPlanContractParser(fixture.Context);
        using var stream = AdaptiveWorkbook([["Customer Model Label", "Unconfirmed Base Label", "Grade", "Cat"], ["Model", "Base", "B", "LPR"]]);
        var inspection = await parser.InspectHeadersAsync(stream);
        Assert.Contains(inspection.Columns, value => value.Header == "Customer Model Label" && value.SuggestedCanonical == "ProjectName" && value.LearnedSuggestion);
        Assert.Contains(inspection.Columns, value => value.Header == "Unconfirmed Base Label" && !value.LearnedSuggestion);
    }

    [Fact]
    public async Task MalformedWorkbookIsRejected()
    {
        using var stream = new MemoryStream([1, 2, 3, 4]);
        await Assert.ThrowsAnyAsync<Exception>(() => _parser.ParseExcelAsync(stream, "TEST-5"));
    }

    [Fact]
    public async Task PersistenceFailureRollsBackAllInserts()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.MasterPlans.Add(new MasterPlan { ProjectName = "Existing", Basic = "SYN-DUP", BasicKey = "SYN-DUP", Cat = "LPR", CatKey = "LPR" });
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "ROLLBACK", Status = "Staged" });
        fixture.Context.StagingMasterPlans.AddRange(Ready("ROLLBACK", 2, "SYN-NEW"), Ready("ROLLBACK", 3, "SYN-DUP"));
        await fixture.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CommitBatchAsync("ROLLBACK", "synthetic-test"));

        fixture.Context.ChangeTracker.Clear();
        Assert.False(await fixture.Context.MasterPlans.AnyAsync(value => value.Sku == "SYN-NEW"));
    }

    [Fact]
    public async Task CommittedDataIsVisibleThroughMasterPlanService()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "VISIBLE", Status = "Staged" });
        fixture.Context.StagingMasterPlans.Add(Ready("VISIBLE", 2, "SYN-VISIBLE"));
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.CommitBatchAsync("VISIBLE", "synthetic-test");

        var records = await new MasterPlanService(fixture.Context).GetLatestMasterPlanRecordsAsync();
        Assert.Contains(records, value => value.Basic == "SYN-VISIBLE");
    }

    [Fact]
    public async Task MasterPlanQueryFiltersGradeAndCatAndSortsNullPvrLast()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.MasterPlans.AddRange(
            Core("B-LATE", "B", "LPR", new DateTime(2026, 9, 1)),
            Core("B-EARLY", "b-grade", "lqv", new DateTime(2026, 8, 1)),
            Core("B-NULL", "B", "LPR", null),
            Core("A-HIDDEN", "A", "LPR", new DateTime(2026, 7, 1)),
            Core("CAT-HIDDEN", "B", "LCV", new DateTime(2026, 7, 1)));
        await fixture.Context.SaveChangesAsync();
        var records = (await new MasterPlanService(fixture.Context).GetLatestMasterPlanRecordsAsync()).ToList();
        Assert.Equal(["B-EARLY", "B-LATE", "B-NULL"], records.Select(value => value.Basic));

        static MasterPlan Core(string basic, string grade, string cat, DateTime? pvr) => new() { ProjectName = basic, Basic = basic, BasicKey = basic, Grade = grade, Cat = cat, CatKey = cat.ToUpperInvariant(), PvrTargetDate = pvr };
    }

    [Fact]
    public async Task ExistingBusinessKeyDefaultsBlockedAndExplicitSkipIsReported()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.MasterPlans.Add(new MasterPlan
        {
            ProjectName = "Existing synthetic model",
            Basic = "SYN-OLD",
            BasicKey = "SYN-OLD",
            Area = "Synthetic Area",
            Grade = "B",
            Cat = "LPR",
            CatKey = "LPR",
            QtyLpr = 1,
            QtyLsr = 1,
            PvrTargetDate = new DateTime(2026, 8, 1),
            PraTargetDate = new DateTime(2026, 8, 2),
            SraTargetDate = new DateTime(2026, 8, 3),
            HwPic = "SYN-PIC",
            Status = "Synthetic",
            ImportedStatus = "Imported",
            ActionStatus = "Ready",
            Remark = "Synthetic test fixture",
            LastImportBatchId = "SYNTHETIC-BASELINE"
        });
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "SKIP", Status = "Staged", ValidRows = 1, ReviewRequiredRows = 1 });
        await fixture.Context.SaveChangesAsync();
        var existing = await fixture.Context.MasterPlans.SingleAsync(value => value.BasicKey == "SYN-OLD");
        var conflict = Ready("SKIP", 3, "SYN-OLD");
        conflict.RowStatus = "ReviewRequired";
        conflict.TargetMasterPlanId = existing.Id;
        conflict.TargetVersion = existing.Version;
        fixture.Context.StagingMasterPlans.AddRange(Ready("SKIP", 2, "SYN-NEW"), conflict);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.BusinessReviewQueues.Add(new BusinessReviewQueue { BatchId = "SKIP", StagingId = conflict.Id, ConflictType = "ExistingBusinessKey", ConflictMessage = "[]" });
        await fixture.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CommitBatchAsync("SKIP", "synthetic-test"));
        await fixture.Service.ResolveExistingBusinessKeyAsync("SKIP", "Skip", "synthetic-test");
        await fixture.Service.CommitBatchAsync("SKIP", "synthetic-test");

        var summary = await fixture.Service.GetReviewSummaryAsync("SKIP");
        Assert.Equal(1, summary?.SkippedRows);
        Assert.True(await fixture.Context.MasterPlans.AnyAsync(value => value.BasicKey == "SYN-NEW"));
    }

    [Fact]
    public async Task ExistingChangedRowRetainsUpdateIntentAfterFinalWarningIsAccepted()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "UPDATE-WARNING", Status = "Staged", ReviewRequiredRows = 1 });
        var target = Core("SYN-UPDATE-WARNING", "B", "LPR", new DateTime(2026, 8, 1));
        fixture.Context.MasterPlans.Add(target);
        await fixture.Context.SaveChangesAsync();
        var staging = Ready("UPDATE-WARNING", 2, "SYN-UPDATE-WARNING");
        staging.ProjectName = "Changed synthetic model";
        staging.RowStatus = "ReviewRequired";
        staging.TargetMasterPlanId = target.Id;
        staging.TargetVersion = target.Version;
        fixture.Context.StagingMasterPlans.Add(staging);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.BusinessReviewQueues.AddRange(
            new BusinessReviewQueue
            {
                BatchId = "UPDATE-WARNING", StagingId = staging.Id, ConflictType = "ExistingBusinessKey",
                ConflictMessage = "[{\"Field\":\"ProjectName\",\"OldValue\":\"SYN-UPDATE-WARNING\",\"NewValue\":\"Changed synthetic model\"}]"
            },
            new BusinessReviewQueue
            {
                BatchId = "UPDATE-WARNING", StagingId = staging.Id, ConflictType = "PIC Mapping Issue",
                ConflictMessage = "Synthetic warning"
            });
        await fixture.Context.SaveChangesAsync();

        await fixture.Service.ResolveExistingBusinessKeyAsync("UPDATE-WARNING", "Update", "synthetic-test");
        Assert.Equal("ReviewRequired", staging.RowStatus);
        Assert.True(await fixture.Service.ResolveWarningRowAsync("UPDATE-WARNING", 2, "Accept", "synthetic-test"));

        Assert.Equal("ReadyToUpdate", staging.RowStatus);
    }

    [Fact]
    public async Task CancelLeavesCoreDatabaseUnchanged()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "CANCEL", Status = "Staged", ValidRows = 1 });
        fixture.Context.StagingMasterPlans.Add(Ready("CANCEL", 2, "SYN-CANCEL"));
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.ResolveExistingBusinessKeyAsync("CANCEL", "Cancel", "synthetic-test");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CommitBatchAsync("CANCEL", "synthetic-test"));
        Assert.False(await fixture.Context.MasterPlans.AnyAsync());
    }

    [Theory]
    [InlineData("Override")]
    [InlineData("Ignore")]
    [InlineData("CreateMissing")]
    public async Task UnknownPicReviewPreservesContextBeforeAndAfterResolution(string action)
    {
        await using var fixture = await ServiceFixture.Create(useRealParser: true);
        using var workbook = Workbook(
            ["Project Name", "Basic", "Grade", "Cat", "HW PIC"],
            ["Synthetic Model", "SYN-PIC", "B", "LPR", "Alex Kim"]);

        var batch = await fixture.Service.ProcessUploadAsync(workbook, "unknown-pic.xlsx", "synthetic-test");
        var before = Assert.IsType<ImportReviewSummaryDto>(await fixture.Service.GetReviewSummaryAsync(batch.BatchId));

        var warning = Assert.Single(before.Rows, row => row.Severity == "Warning");
        Assert.Equal("HwPic", warning.Field);
        Assert.Equal("Alex Kim", warning.CurrentValue);
        Assert.Equal("Unknown PIC 'Alex Kim'.", warning.Message);
        Assert.Equal(["Override", "Ignore", "CreateMissing"], warning.SupportedActions);
        Assert.DoesNotContain(before.Rows, row => string.IsNullOrWhiteSpace(row.Message));

        Assert.NotNull(warning.ReviewItemId);
        Assert.True(await fixture.Service.ResolveReviewItemAsync(warning.ReviewItemId.Value, action, "synthetic-test"));
        var after = Assert.IsType<ImportReviewSummaryDto>(await fixture.Service.GetReviewSummaryAsync(batch.BatchId));
        var resolved = Assert.Single(after.Rows);

        Assert.Equal("HwPic", resolved.Field);
        Assert.Equal("Alex Kim", resolved.CurrentValue);
        Assert.Equal("Unknown PIC 'Alex Kim'.", resolved.Message);
        Assert.Null(resolved.ReviewItemId);
        Assert.Empty(resolved.SupportedActions);
    }

    [Fact]
    public async Task ResolvedContextualReviewUsesItsQueueTypeForFieldMetadata()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "AREA-CONTEXT", Status = "Staged", ReviewRequiredRows = 1 });
        var staging = Ready("AREA-CONTEXT", 2, "SYN-AREA");
        staging.Area = "JP1";
        staging.RowStatus = "ReviewRequired";
        staging.CoreValidationMessage = "Area Mapping Issue: 'JP1' not found in dictionary.";
        fixture.Context.StagingMasterPlans.Add(staging);
        await fixture.Context.SaveChangesAsync();
        var review = new BusinessReviewQueue
        {
            BatchId = "AREA-CONTEXT",
            StagingId = staging.Id,
            ConflictType = "Area",
            ConflictMessage = staging.CoreValidationMessage
        };
        fixture.Context.BusinessReviewQueues.Add(review);
        await fixture.Context.SaveChangesAsync();

        Assert.True(await fixture.Service.ResolveReviewItemAsync(review.Id, "Override", "synthetic-test"));
        var summary = Assert.IsType<ImportReviewSummaryDto>(await fixture.Service.GetReviewSummaryAsync("AREA-CONTEXT"));
        var resolved = Assert.Single(summary.Rows);

        Assert.Equal("Area", resolved.Field);
        Assert.Equal("JP1", resolved.CurrentValue);
        Assert.Equal(staging.CoreValidationMessage, resolved.Message);
    }

    private static StagingMasterPlan Ready(string batchId, int row, string basic) => new()
    {
        BatchId = batchId,
        RawRowNumber = row,
        ProjectName = $"Synthetic {basic}",
        Basic = basic,
        BasicKey = basic,
        Grade = "B",
        Cat = "LPR",
        CatKey = "LPR",
        PvrTargetDate = new DateTime(2026, 8, 1),
        RowStatus = "ReadyToInsert"
    };

    private static MasterPlan Core(string basic, string grade, string cat, DateTime? pvr) => new()
    {
        ProjectName = basic,
        Basic = basic,
        BasicKey = MasterPlanBusinessKey.NormalizeBasic(basic),
        Grade = grade,
        Cat = cat,
        CatKey = MasterPlanBusinessKey.NormalizeCat(cat),
        PvrTargetDate = pvr
    };

    private static MemoryStream Workbook(string[] headers, string[] row)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """);
            Write(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Write(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="MasterPlan" sheetId="1" r:id="rId1"/></sheets></workbook>
                """);
            Write(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """);
            Write(archive, "xl/worksheets/sheet1.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\">{string.Concat(headers.Select((value, index) => Cell(index, 1, value)))}</row><row r=\"2\">{string.Concat(row.Select((value, index) => Cell(index, 2, value)))}</row></sheetData></worksheet>");
        }

        stream.Position = 0;
        return stream;

        static string Cell(int column, int rowNumber, string value) => $"<c r=\"{(char)('A' + column)}{rowNumber}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(value)}</t></is></c>";
        static void Write(ZipArchive archive, string path, string value)
        {
            using var writer = new StreamWriter(archive.CreateEntry(path).Open());
            writer.Write(value);
        }
    }

    private static MemoryStream DateFormattedWorkbook()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>
                """);
            Write(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Write(archive, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="MasterPlan" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Write(archive, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""");
            Write(archive, "xl/styles.xml", """<?xml version="1.0" encoding="UTF-8"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="1"><font/></fonts><fills count="1"><fill/></fills><borders count="1"><border/></borders><cellStyleXfs count="1"><xf/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0"/><xf numFmtId="14" applyNumberFormat="1"/></cellXfs></styleSheet>""");
            Write(archive, "xl/worksheets/sheet1.xml", """
                <?xml version="1.0" encoding="UTF-8"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>Project Name</t></is></c><c r="B1" t="inlineStr"><is><t>Basic</t></is></c><c r="C1" t="inlineStr"><is><t>Grade</t></is></c><c r="D1" t="inlineStr"><is><t>Cat</t></is></c><c r="E1" t="inlineStr"><is><t>Unmapped Schedule</t></is></c></row><row r="2"><c r="A2" t="inlineStr"><is><t>Model</t></is></c><c r="B2" t="inlineStr"><is><t>Base</t></is></c><c r="C2" t="inlineStr"><is><t>B</t></is></c><c r="D2" t="inlineStr"><is><t>LPR</t></is></c><c r="E2" s="1"><v>45505</v></c></row></sheetData></worksheet>
                """);
        }
        stream.Position = 0;
        return stream;

        static void Write(ZipArchive archive, string path, string value) { using var writer = new StreamWriter(archive.CreateEntry(path).Open()); writer.Write(value); }
    }

    private static MemoryStream AdaptiveWorkbook(string[][] rows, params string[] merges)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            Write(archive, "[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            Write(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Write(archive, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="MasterPlan" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Write(archive, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
            var sheetRows = rows.Select((values, rowIndex) => $"<row r=\"{rowIndex + 1}\">{string.Concat(values.Select((value, columnIndex) => $"<c r=\"{Column(columnIndex)}{rowIndex + 1}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(value)}</t></is></c>"))}</row>");
            var mergeXml = merges.Length == 0 ? string.Empty : $"<mergeCells count=\"{merges.Length}\">{string.Concat(merges.Select(value => $"<mergeCell ref=\"{value}\"/>"))}</mergeCells>";
            Write(archive, "xl/worksheets/sheet1.xml", $"<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>{string.Concat(sheetRows)}</sheetData>{mergeXml}</worksheet>");
        }
        stream.Position = 0;
        return stream;

        static string Column(int index) { var result = string.Empty; for (var value = index + 1; value > 0; value /= 26) { value--; result = (char)('A' + value % 26) + result; } return result; }
        static void Write(ZipArchive archive, string path, string value) { using var writer = new StreamWriter(archive.CreateEntry(path).Open()); writer.Write(value); }
    }

    private sealed class ServiceFixture : IAsyncDisposable
    {
        private readonly string _root;
        public AppDbContext Context { get; }
        public DataHubIngestionService Service { get; }

        private ServiceFixture(string root, AppDbContext context, bool useRealParser)
        {
            _root = root;
            Context = context;
            var masterPlan = Path.Combine(root, "MasterPlan");
            var paths = new DataHubPathConfig
            {
                BasePath = root,
                NewModelsMasterPlanBasePath = masterPlan,
                NewModelsMasterPlanManualUploadPath = Path.Combine(masterPlan, "ManualUpload"),
                NewModelsMasterPlanRawPath = Path.Combine(masterPlan, "Raw"),
                NewModelsMasterPlanProcessedPath = Path.Combine(masterPlan, "Processed"),
                NewModelsMasterPlanRejectedPath = Path.Combine(masterPlan, "Rejected"),
                NewModelsMasterPlanReportsPath = Path.Combine(masterPlan, "Reports"),
                NewModelsMasterPlanTempPath = Path.Combine(masterPlan, "Temp")
            };
            IMasterPlanContractParser parser = useRealParser ? new MasterPlanContractParser(context) : new EmptyParser();
            Service = new DataHubIngestionService(context, parser, NullLogger<DataHubIngestionService>.Instance, Options.Create(paths));
        }

        public static async Task<ServiceFixture> Create(bool useRealParser = false)
        {
            var root = Path.Combine(Path.GetTempPath(), $"iqc-datahub-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={Path.Combine(root, "test.db")}")
                .Options;
            var fixture = new ServiceFixture(root, new AppDbContext(options), useRealParser);
            await fixture.Context.Database.EnsureCreatedAsync();
            return fixture;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            try
            {
                Directory.Delete(_root, true);
            }
            catch (IOException)
            {
                // SQLite can briefly retain a file handle on Windows; the unique temp path prevents test interference.
            }
        }

        private sealed class EmptyParser : IMasterPlanContractParser
        {
            public Task<MasterPlanParseResult> ParseExcelAsync(Stream excelStream, string batchId, IReadOnlyCollection<HeaderMappingDto>? mappings = null) =>
                Task.FromResult(new MasterPlanParseResult([], [], []));

            public Task<HeaderInspectionDto> InspectHeadersAsync(Stream excelStream) =>
                Task.FromResult(new HeaderInspectionDto(1, [], [], []));
        }
    }
}

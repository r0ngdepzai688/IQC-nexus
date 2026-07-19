using System.IO.Compression;
using System.Security;
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
        using var stream = Workbook(["Project Name", "SKU", "PVR Target"], ["Synthetic Model", "SYN-SKU-1", "2026-08-01"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-1");
        Assert.Single(result.Records);
        Assert.Equal("SYN-SKU-1", result.Records[0].Sku);
    }

    [Fact]
    public async Task MissingRequiredColumnIsReported()
    {
        using var stream = Workbook(["Project Name", "SKU"], ["Synthetic Model", "SYN-SKU-1"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-2");
        Assert.Equal(["PVRTarget"], result.MissingHardColumns);
    }

    [Fact]
    public async Task DuplicateRequiredColumnIsReported()
    {
        using var stream = Workbook(["Project Name", "SKU", "SKU", "PVR Target"], ["Synthetic Model", "SYN-1", "SYN-2", "2026-08-01"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-3");
        Assert.Equal(["SKU"], result.DuplicateColumns);
    }

    [Fact]
    public async Task DuplicateCanonicalMappingIsRejected()
    {
        using var stream = Workbook(["Project", "Model", "SKU", "PVR"], ["One", "Two", "SYN-1", "2026-08-01"]);
        var result = await _parser.ParseExcelAsync(stream, "MAP-1", [new(0, "ProjectName"), new(1, "ProjectName"), new(2, "SKU"), new(3, "PVRTarget")]);
        Assert.Equal(["ProjectName"], result.DuplicateColumns);
    }

    [Fact]
    public async Task MissingRequiredMappingIsRejected()
    {
        using var stream = Workbook(["Project", "SKU", "Date"], ["One", "SYN-1", "2026-08-01"]);
        var result = await _parser.ParseExcelAsync(stream, "MAP-2", [new(0, "ProjectName"), new(1, "SKU")]);
        Assert.Equal(["PVRTarget"], result.MissingHardColumns);
    }

    [Fact]
    public async Task InvalidRowDateRemainsNullable()
    {
        using var stream = Workbook(["Project Name", "SKU", "PVR Target"], ["Synthetic Model", "SYN-SKU-1", "99월 99일"]);
        var result = await _parser.ParseExcelAsync(stream, "TEST-4");
        Assert.Null(result.Records.Single().PvrTargetDate);
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
        fixture.Context.MasterPlans.Add(new MasterPlan { ProjectName = "Existing", Sku = "SYN-DUP" });
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
        Assert.Contains(records, value => value.Sku == "SYN-VISIBLE");
    }

    [Fact]
    public async Task ExistingSkuDefaultsBlockedAndExplicitSkipIsReported()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "SKIP", Status = "Staged", ValidRows = 1, ReviewRequiredRows = 1 });
        fixture.Context.StagingMasterPlans.AddRange(
            Ready("SKIP", 2, "SYN-NEW"),
            new StagingMasterPlan
            {
                BatchId = "SKIP", RawRowNumber = 3, ProjectName = "Existing", Sku = "SYN-OLD",
                RowStatus = "ReviewRequired",
                CoreValidationMessage = "SKU already exists; imports do not overwrite existing Master Plan records"
            });
        await fixture.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CommitBatchAsync("SKIP", "synthetic-test"));
        await fixture.Service.ResolveExistingSkuAsync("SKIP", "Skip", "synthetic-test");
        await fixture.Service.CommitBatchAsync("SKIP", "synthetic-test");

        var summary = await fixture.Service.GetReviewSummaryAsync("SKIP");
        Assert.Equal(1, summary?.SkippedRows);
        Assert.True(await fixture.Context.MasterPlans.AnyAsync(value => value.Sku == "SYN-NEW"));
    }

    [Fact]
    public async Task CancelLeavesCoreDatabaseUnchanged()
    {
        await using var fixture = await ServiceFixture.Create();
        fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "CANCEL", Status = "Staged", ValidRows = 1 });
        fixture.Context.StagingMasterPlans.Add(Ready("CANCEL", 2, "SYN-CANCEL"));
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.ResolveExistingSkuAsync("CANCEL", "Cancel", "synthetic-test");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.CommitBatchAsync("CANCEL", "synthetic-test"));
        Assert.False(await fixture.Context.MasterPlans.AnyAsync());
    }

    private static StagingMasterPlan Ready(string batchId, int row, string sku) => new()
    {
        BatchId = batchId,
        RawRowNumber = row,
        ProjectName = $"Synthetic {sku}",
        Sku = sku,
        PvrTargetDate = new DateTime(2026, 8, 1),
        RowStatus = "ReadyToInsert"
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

    private sealed class ServiceFixture : IAsyncDisposable
    {
        private readonly string _root;
        public AppDbContext Context { get; }
        public DataHubIngestionService Service { get; }

        private ServiceFixture(string root, AppDbContext context)
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
            Service = new DataHubIngestionService(context, new EmptyParser(), NullLogger<DataHubIngestionService>.Instance, Options.Create(paths));
        }

        public static async Task<ServiceFixture> Create()
        {
            var root = Path.Combine(Path.GetTempPath(), $"iqc-datahub-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={Path.Combine(root, "test.db")}")
                .Options;
            var fixture = new ServiceFixture(root, new AppDbContext(options));
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

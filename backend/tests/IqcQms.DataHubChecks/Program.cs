using System.IO.Compression;
using System.Security;
using IqcQms.Infrastructure.Services.DataHub;
using IqcQms.Application.Interfaces.DataHub;
using IqcQms.Domain.Entities.DataHub;
using IqcQms.Domain.Entities.NewModels;
using IqcQms.Infrastructure.Data;
using IqcQms.Infrastructure.Services.NewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var parser = new MasterPlanContractParser();
var passed = 0;

await Check("successful minimal import parse", async () =>
{
    using var stream = Workbook(["Project Name", "SKU", "PVR Target"], ["Synthetic Model", "SYN-SKU-1", "2026-08-01"]);
    var result = await parser.ParseExcelAsync(stream, "TEST-1");
    Assert(result.Records.Count == 1 && result.Records[0].Sku == "SYN-SKU-1", "expected one parsed row");
});

await Check("missing required column", async () =>
{
    using var stream = Workbook(["Project Name", "SKU"], ["Synthetic Model", "SYN-SKU-1"]);
    var result = await parser.ParseExcelAsync(stream, "TEST-2");
    Assert(result.MissingHardColumns.SequenceEqual(["PVRTarget"]), "expected PVRTarget to be missing");
});

await Check("duplicate required column", async () =>
{
    using var stream = Workbook(["Project Name", "SKU", "SKU", "PVR Target"], ["Synthetic Model", "SYN-1", "SYN-2", "2026-08-01"]);
    var result = await parser.ParseExcelAsync(stream, "TEST-3");
    Assert(result.DuplicateColumns.SequenceEqual(["SKU"]), "expected duplicate SKU header");
});

await Check("duplicate canonical mapping rejected", async () =>
{
    using var stream = Workbook(["Project", "Model", "SKU", "PVR"], ["One", "Two", "SYN-1", "2026-08-01"]);
    var result = await parser.ParseExcelAsync(stream, "MAP-1", [new(0, "ProjectName"), new(1, "ProjectName"), new(2, "SKU"), new(3, "PVRTarget")]);
    Assert(result.DuplicateColumns.SequenceEqual(["ProjectName"]), "duplicate canonical mapping was accepted");
});

await Check("missing required mapping rejected", async () =>
{
    using var stream = Workbook(["Project", "SKU", "Date"], ["One", "SYN-1", "2026-08-01"]);
    var result = await parser.ParseExcelAsync(stream, "MAP-2", [new(0, "ProjectName"), new(1, "SKU")]);
    Assert(result.MissingHardColumns.SequenceEqual(["PVRTarget"]), "missing required mapping was accepted");
});

await Check("invalid row date remains nullable", async () =>
{
    using var stream = Workbook(["Project Name", "SKU", "PVR Target"], ["Synthetic Model", "SYN-SKU-1", "99월 99일"]);
    var result = await parser.ParseExcelAsync(stream, "TEST-4");
    Assert(result.Records.Single().PvrTargetDate is null, "invalid date must not be coerced");
});

await Check("malformed workbook is rejected", async () =>
{
    using var stream = new MemoryStream([1, 2, 3, 4]);
    try
    {
        await parser.ParseExcelAsync(stream, "TEST-5");
        throw new Exception("malformed workbook was accepted");
    }
    catch (Exception ex) when (ex.Message != "malformed workbook was accepted") { }
});

await Check("rollback on persistence failure", async () =>
{
    await using var fixture = await ServiceFixture.Create();
    fixture.Context.MasterPlans.Add(new MasterPlan { ProjectName = "Existing", Sku = "SYN-DUP" });
    fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "ROLLBACK", Status = "Staged" });
    fixture.Context.StagingMasterPlans.AddRange(
        Ready("ROLLBACK", 2, "SYN-NEW"),
        Ready("ROLLBACK", 3, "SYN-DUP"));
    await fixture.Context.SaveChangesAsync();
    try { await fixture.Service.CommitBatchAsync("ROLLBACK", "synthetic-test"); } catch (InvalidOperationException) { }
    fixture.Context.ChangeTracker.Clear();
    Assert(!await fixture.Context.MasterPlans.AnyAsync(value => value.Sku == "SYN-NEW"), "first insert survived rollback");
});

await Check("committed data is visible through Master Plan service", async () =>
{
    await using var fixture = await ServiceFixture.Create();
    fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "VISIBLE", Status = "Staged" });
    fixture.Context.StagingMasterPlans.Add(Ready("VISIBLE", 2, "SYN-VISIBLE"));
    await fixture.Context.SaveChangesAsync();
    await fixture.Service.CommitBatchAsync("VISIBLE", "synthetic-test");
    var records = await new MasterPlanService(fixture.Context).GetLatestMasterPlanRecordsAsync();
    Assert(records.Any(value => value.Sku == "SYN-VISIBLE"), "committed SKU was not returned by Data Hub visibility API service");
});

await Check("existing SKU defaults blocked and explicit Skip reports it", async () =>
{
    await using var fixture = await ServiceFixture.Create();
    fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "SKIP", Status = "Staged", ValidRows = 1, ReviewRequiredRows = 1 });
    fixture.Context.StagingMasterPlans.AddRange(
        Ready("SKIP", 2, "SYN-NEW"),
        new StagingMasterPlan { BatchId = "SKIP", RawRowNumber = 3, ProjectName = "Existing", Sku = "SYN-OLD", RowStatus = "ReviewRequired", CoreValidationMessage = "SKU already exists; imports do not overwrite existing Master Plan records" });
    await fixture.Context.SaveChangesAsync();
    try { await fixture.Service.CommitBatchAsync("SKIP", "synthetic-test"); throw new Exception("blocked batch committed"); } catch (InvalidOperationException) { }
    await fixture.Service.ResolveExistingSkuAsync("SKIP", "Skip", "synthetic-test");
    await fixture.Service.CommitBatchAsync("SKIP", "synthetic-test");
    var summary = await fixture.Service.GetReviewSummaryAsync("SKIP");
    Assert(summary?.SkippedRows == 1 && await fixture.Context.MasterPlans.AnyAsync(value => value.Sku == "SYN-NEW"), "skip resolution was not reported or new row was not committed");
});

await Check("Cancel leaves core database unchanged", async () =>
{
    await using var fixture = await ServiceFixture.Create();
    fixture.Context.ImportBatches.Add(new ImportBatch { BatchId = "CANCEL", Status = "Staged", ValidRows = 1 });
    fixture.Context.StagingMasterPlans.Add(Ready("CANCEL", 2, "SYN-CANCEL"));
    await fixture.Context.SaveChangesAsync();
    await fixture.Service.ResolveExistingSkuAsync("CANCEL", "Cancel", "synthetic-test");
    try { await fixture.Service.CommitBatchAsync("CANCEL", "synthetic-test"); } catch (InvalidOperationException) { }
    Assert(!await fixture.Context.MasterPlans.AnyAsync(), "cancel mutated core records");
});

Console.WriteLine($"Data Hub focused checks passed: {passed}/11");

async Task Check(string name, Func<Task> test)
{
    await test();
    passed++;
    Console.WriteLine($"PASS: {name}");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static StagingMasterPlan Ready(string batchId, int row, string sku) => new()
{
    BatchId = batchId, RawRowNumber = row, ProjectName = $"Synthetic {sku}", Sku = sku,
    PvrTargetDate = new DateTime(2026, 8, 1), RowStatus = "ReadyToInsert"
};

static MemoryStream Workbook(string[] headers, string[] row)
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
        var cells = string.Concat(headers.Select((value, index) => Cell(index, 1, value))) + string.Concat(row.Select((value, index) => Cell(index, 2, value)));
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

sealed class ServiceFixture : IAsyncDisposable
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
            BasePath = root, NewModelsMasterPlanBasePath = masterPlan,
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
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={Path.Combine(root, "test.db")}").Options;
        var fixture = new ServiceFixture(root, new AppDbContext(options));
        await fixture.Context.Database.EnsureCreatedAsync();
        return fixture;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        try { Directory.Delete(_root, true); } catch (IOException) { }
    }

    private sealed class EmptyParser : IMasterPlanContractParser
    {
        public Task<MasterPlanParseResult> ParseExcelAsync(Stream excelStream, string batchId, IReadOnlyCollection<HeaderMappingDto>? mappings = null) =>
            Task.FromResult(new MasterPlanParseResult([], [], []));
        public Task<HeaderInspectionDto> InspectHeadersAsync(Stream excelStream) =>
            Task.FromResult(new HeaderInspectionDto(1, [], [], []));
    }
}

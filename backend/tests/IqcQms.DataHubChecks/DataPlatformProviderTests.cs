using System.IO.Compression;
using System.Text;
using IqcQms.Application.DataPlatform;
using IqcQms.Infrastructure.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class DataPlatformProviderTests
{
    [Theory]
    [InlineData("a,b\r\n\"x,y\",\"line1\nline2\"\r\n", "x,y", "line1\nline2")]
    [InlineData("a,b\n\"say \"\"hello\"\"\",\n", "say \"hello\"", "")]
    [InlineData("a,b\n1,\n", "1", "")]
    public async Task CsvPreservesQuotedFieldsNewlinesEscapedQuotesAndEmptyCells(
        string csv,
        string expectedFirst,
        string expectedSecond)
    {
        var workbook = await NormalizeCsv(csv);

        Assert.Equal(expectedFirst, workbook.Worksheets[0].Rows[1].Cells[0].StringValue);
        Assert.Equal(expectedSecond, workbook.Worksheets[0].Rows[1].Cells[1].StringValue);
        Assert.Equal(2, workbook.Worksheets[0].Rows[1].RowNumber);
    }

    [Fact]
    public async Task CsvAcceptsUtf8BomAndKeepsDuplicateHeaders()
    {
        var payload = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes("Tên,Tên\nMột,Hai\n"))
            .ToArray();
        var workbook = await NormalizeCsv(payload);

        Assert.Equal(["Tên", "Tên"], workbook.Worksheets[0].Rows[0].Cells.Select(cell => cell.StringValue));
        Assert.Equal("Một", workbook.Worksheets[0].Rows[1].Cells[0].StringValue);
    }

    [Fact]
    public async Task CsvSafelyDetectsSemicolonDelimiter()
    {
        var workbook = await NormalizeCsv("a;b;c\n1;2;3\n");
        Assert.Equal(3, workbook.Worksheets[0].ColumnCount);
        Assert.Equal("2", workbook.Worksheets[0].Rows[1].Cells[1].StringValue);
    }

    [Fact]
    public async Task CsvEnforcesLimits()
    {
        var exception = await Assert.ThrowsAsync<ImportPlatformException>(() =>
            NormalizeCsv("a,b\n1,2\n", new ImportPlatformLimits(MaximumRowsPerWorksheet: 1)));
        Assert.Equal(ImportErrorCodes.RowLimitExceeded, exception.Code);
    }

    [Fact]
    public async Task XlsxPreservesWorksheetsVisibilityMergeCoordinatesFormatsAndNumericSerial()
    {
        var bytes = CreateWorkbook();
        var provider = new ExcelDataSourceProvider();
        await using var stream = new MemoryStream(bytes);
        var workbook = await provider.NormalizeAsync(
            Context(DataSourceProviderKind.Excel, "synthetic.xlsx", stream));

        Assert.Equal(2, workbook.Worksheets.Count);
        var first = workbook.Worksheets[0];
        Assert.Equal(WorksheetVisibility.Visible, first.Visibility);
        Assert.Contains("A1:B1", first.MergedRanges);
        Assert.Equal("A1:B1", first.Rows[0].Cells[0].MergedRange);
        Assert.True(first.Rows[0].Cells[0].IsMergedAnchor);
        Assert.Equal(2, first.Rows[1].RowNumber);
        Assert.Equal(1, first.Rows[1].Cells[0].ColumnNumber);
        Assert.Equal(NormalizedCellRawType.Numeric, first.Rows[1].Cells[0].RawType);
        Assert.Equal(45_000d, first.Rows[1].Cells[0].NumericValue);
        Assert.NotNull(first.Rows[1].Cells[0].NumberFormat);
        Assert.Equal(WorksheetVisibility.Hidden, workbook.Worksheets[1].Visibility);
    }

    [Fact]
    public void LifecycleRejectsPreviewBypassAndTerminalTransitions()
    {
        Assert.False(ImportJobTransitionGuard.CanTransition(ImportJobState.ReadyForMapping, ImportJobState.Committing));
        Assert.False(ImportJobTransitionGuard.CanTransition(ImportJobState.Completed, ImportJobState.Created));
        Assert.True(ImportJobTransitionGuard.CanTransition(ImportJobState.ReadyForReview, ImportJobState.Committing));
        Assert.Throws<InvalidOperationException>(() =>
            ImportJobTransitionGuard.EnsureCanTransition(ImportJobState.Created, ImportJobState.Completed));
    }

    [Fact]
    public async Task RegistryRejectsUnregisteredNascaProvider()
    {
        var registry = new DataSourceProviderRegistry([new CsvDataSourceProvider(), new ExcelDataSourceProvider()]);
        await using var stream = new MemoryStream([1]);
        var exception = await Assert.ThrowsAsync<ImportPlatformException>(() =>
            registry.NormalizeAsync(Context(DataSourceProviderKind.NascaExcel, "protected.xlsx", stream)));
        Assert.Equal(ImportErrorCodes.ProviderNotFound, exception.Code);
    }

    private static Task<NormalizedWorkbook> NormalizeCsv(
        string text,
        ImportPlatformLimits? limits = null) =>
        NormalizeCsv(Encoding.UTF8.GetBytes(text), limits);

    private static async Task<NormalizedWorkbook> NormalizeCsv(
        byte[] bytes,
        ImportPlatformLimits? limits = null)
    {
        var provider = new CsvDataSourceProvider();
        await using var stream = new MemoryStream(bytes);
        return await provider.NormalizeAsync(
            Context(DataSourceProviderKind.Csv, "synthetic.csv", stream, limits));
    }

    private static DataSourceProviderContext Context(
        DataSourceProviderKind kind,
        string fileName,
        Stream stream,
        ImportPlatformLimits? limits = null) =>
        new(
            new DataSourceDescriptor(kind, fileName, fileName, null, stream.Length),
            stream,
            limits ?? ImportPlatformLimits.Default);

    private static byte[] CreateWorkbook()
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);
            Add(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            Add(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Visible" sheetId="1" r:id="rId1"/><sheet name="Hidden" sheetId="2" state="hidden" r:id="rId2"/></sheets>
                </workbook>
                """);
            Add(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
                  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            Add(archive, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <numFmts count="1"><numFmt numFmtId="164" formatCode="yyyy-mm-dd"/></numFmts>
                  <fonts count="1"><font/></fonts><fills count="1"><fill/></fills><borders count="1"><border/></borders>
                  <cellStyleXfs count="1"><xf/></cellStyleXfs>
                  <cellXfs count="2"><xf/><xf numFmtId="164" applyNumberFormat="1"/></cellXfs>
                </styleSheet>
                """);
            Add(archive, "xl/worksheets/sheet1.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <dimension ref="A1:B2"/><sheetData>
                    <row r="1"><c r="A1" t="inlineStr"><is><t>Merged</t></is></c><c r="B1"/></row>
                    <row r="2"><c r="A2" s="1"><v>45000</v></c><c r="B2" t="b"><v>1</v></c></row>
                  </sheetData><mergeCells count="1"><mergeCell ref="A1:B1"/></mergeCells>
                </worksheet>
                """);
            Add(archive, "xl/worksheets/sheet2.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><dimension ref="A1"/><sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>Hidden</t></is></c></row></sheetData></worksheet>
                """);
        }
        return output.ToArray();
    }

    private static void Add(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}

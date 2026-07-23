using System.IO.Compression;
using System.Text;
using IqcQms.Application.DataPlatform;
using IqcQms.Infrastructure.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class DataPlatformAcceptanceTests
{
    [Fact]
    public async Task CsvWithoutBomPreservesCoordinatesAndTreatsFirstRowAsData()
    {
        var workbook = await NormalizeCsv("duplicate,duplicate\nvalue,second\n");

        var sheet = Assert.Single(workbook.Worksheets);
        Assert.Equal(1, sheet.Rows[0].RowNumber);
        Assert.Equal(1, sheet.Rows[0].Cells[0].ColumnNumber);
        Assert.Equal("duplicate", sheet.Rows[0].Cells[0].StringValue);
        Assert.Equal("duplicate", sheet.Rows[0].Cells[1].StringValue);
        Assert.Equal(2, sheet.Rows[1].RowNumber);
    }

    [Fact]
    public async Task CsvCancellationUsesStableSanitizedError()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = await Assert.ThrowsAsync<ImportPlatformException>(() =>
            NormalizeCsv("a,b\n1,2\n", cancellationToken: cancellation.Token));

        Assert.Equal(ImportErrorCodes.Cancelled, exception.Code);
        Assert.Equal("The import was cancelled.", exception.Message);
    }

    [Fact]
    public async Task CsvMalformedQuotedFieldDoesNotExposeCellValue()
    {
        const string syntheticCellValue = "SENSITIVE_SYNTHETIC_VALUE";

        var exception = await Assert.ThrowsAsync<ImportPlatformException>(() =>
            NormalizeCsv($"a,b\n\"{syntheticCellValue},value\n"));

        Assert.Equal(ImportErrorCodes.NormalizationFailed, exception.Code);
        Assert.DoesNotContain(syntheticCellValue, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task XlsxPreservesEmptyRowsAndCachedFormulaValues()
    {
        var workbook = await NormalizeExcel(CreateWorkbook());
        var first = workbook.Worksheets[0];

        Assert.True(first.Rows[2].IsEmpty);
        Assert.Equal(3, first.Rows[2].RowNumber);
        var formulaCell = first.Rows[3].Cells[0];
        Assert.Equal(4, formulaCell.RowNumber);
        Assert.Equal(1, formulaCell.ColumnNumber);
        Assert.Equal(NormalizedCellRawType.Numeric, formulaCell.RawType);
        Assert.Equal(90_000d, formulaCell.NumericValue);
        Assert.Null(formulaCell.Formula);
        Assert.Contains(workbook.Diagnostics, item => item.Code == "EXCEL_FORMULA_TEXT_UNAVAILABLE");
    }

    [Theory]
    [InlineData(1, 100, 100, 100, ImportErrorCodes.WorksheetLimitExceeded)]
    [InlineData(10, 3, 100, 100, ImportErrorCodes.RowLimitExceeded)]
    [InlineData(10, 100, 1, 100, ImportErrorCodes.ColumnLimitExceeded)]
    [InlineData(10, 100, 100, 3, ImportErrorCodes.CellLimitExceeded)]
    public async Task XlsxEnforcesStructuralLimits(
        int worksheets,
        int rows,
        int columns,
        long cells,
        string expectedCode)
    {
        var limits = new ImportPlatformLimits(
            MaximumWorksheets: worksheets,
            MaximumRowsPerWorksheet: rows,
            MaximumColumnsPerWorksheet: columns,
            MaximumCells: cells);

        var exception = await Assert.ThrowsAsync<ImportPlatformException>(() =>
            NormalizeExcel(CreateWorkbook(), limits));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Theory]
    [InlineData(ImportJobState.Created, ImportJobState.Inspecting)]
    [InlineData(ImportJobState.Inspecting, ImportJobState.ReadyForMapping)]
    [InlineData(ImportJobState.ReadyForMapping, ImportJobState.Validating)]
    [InlineData(ImportJobState.Validating, ImportJobState.ReadyForReview)]
    [InlineData(ImportJobState.ReadyForReview, ImportJobState.Committing)]
    [InlineData(ImportJobState.Committing, ImportJobState.Completed)]
    public void LifecycleAllowsOnlyExplicitHappyPathTransitions(
        ImportJobState current,
        ImportJobState next)
    {
        ImportJobTransitionGuard.EnsureCanTransition(current, next);
    }

    [Fact]
    public void LifecycleRequiresPreviewBeforeCommit()
    {
        Assert.False(ImportJobTransitionGuard.CanTransition(
            ImportJobState.Validating,
            ImportJobState.Committing));
        Assert.False(ImportJobTransitionGuard.CanTransition(
            ImportJobState.ReadyForMapping,
            ImportJobState.Committing));
    }

    [Fact]
    public async Task RegistryLeavesAgentAndNascaKindsAsPlaceholders()
    {
        var registry = new DataSourceProviderRegistry(
            [new CsvDataSourceProvider(), new ExcelDataSourceProvider()]);

        foreach (var kind in new[]
                 {
                     DataSourceProviderKind.ClientAgentExcel,
                     DataSourceProviderKind.NascaExcel
                 })
        {
            await using var stream = new MemoryStream([1]);
            var exception = await Assert.ThrowsAsync<ImportPlatformException>(() =>
                registry.NormalizeAsync(Context(kind, "synthetic.xlsx", stream)));
            Assert.Equal(ImportErrorCodes.ProviderNotFound, exception.Code);
        }
    }

    private static Task<NormalizedWorkbook> NormalizeCsv(
        string text,
        ImportPlatformLimits? limits = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var stream = new MemoryStream(bytes);
        return new CsvDataSourceProvider().NormalizeAsync(
            Context(DataSourceProviderKind.Csv, "synthetic.csv", stream, limits),
            cancellationToken);
    }

    private static Task<NormalizedWorkbook> NormalizeExcel(
        byte[] bytes,
        ImportPlatformLimits? limits = null)
    {
        var stream = new MemoryStream(bytes);
        return new ExcelDataSourceProvider().NormalizeAsync(
            Context(DataSourceProviderKind.Excel, "synthetic.xlsx", stream, limits));
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
                  <dimension ref="A1:B4"/><sheetData>
                    <row r="1"><c r="A1" t="inlineStr"><is><t>Merged</t></is></c><c r="B1"/></row>
                    <row r="2"><c r="A2" s="1"><v>45000</v></c><c r="B2" t="b"><v>1</v></c></row>
                    <row r="4"><c r="A4"><f>A2*2</f><v>90000</v></c><c r="B4"/></row>
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

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Infrastructure.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class ParserAbuseTests
{
    [Fact]
    public async Task Normalize_ExtensionContentMismatch_FailsSafely()
    {
        // CSV content passed to Excel provider
        var csvBytes = Encoding.UTF8.GetBytes("Header1,Header2\nValue1,Value2");
        var descriptor = new DataSourceDescriptor(DataSourceProviderKind.Excel, "mismatched.xlsx", "mismatched.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", csvBytes.Length);
        var provider = new ExcelDataSourceProvider();
        var context = new DataSourceProviderContext(descriptor, new MemoryStream(csvBytes), ImportPlatformLimits.Default);

        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() => provider.NormalizeAsync(context));
        Assert.Equal(ImportErrorCodes.NormalizationFailed, ex.Code);
    }

    [Fact]
    public async Task Normalize_UnterminatedCsvQuote_ThrowsNormalizationFailed()
    {
        var csvBytes = Encoding.UTF8.GetBytes("Part Number,Qty\n\"Unterminated Quote,10");
        var descriptor = new DataSourceDescriptor(DataSourceProviderKind.Csv, "unterminated.csv", "unterminated.csv", "text/csv", csvBytes.Length);
        var provider = new CsvDataSourceProvider();
        var context = new DataSourceProviderContext(descriptor, new MemoryStream(csvBytes), ImportPlatformLimits.Default);

        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() => provider.NormalizeAsync(context));
        Assert.Equal(ImportErrorCodes.NormalizationFailed, ex.Code);
        Assert.Contains("unterminated quoted field", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Normalize_RowLimitExceeded_ThrowsRowLimitExceeded()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Part Number,Qty");
        for (int i = 0; i < 15; i++)
        {
            sb.AppendLine($"PN-{i},10");
        }

        var limits = new ImportPlatformLimits(
            MaximumPayloadBytes: 10_000_000,
            MaximumWorksheets: 5,
            MaximumRowsPerWorksheet: 10, // Max 10 rows limit
            MaximumColumnsPerWorksheet: 10,
            MaximumCells: 100);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var descriptor = new DataSourceDescriptor(DataSourceProviderKind.Csv, "limit.csv", "limit.csv", "text/csv", bytes.Length);
        var provider = new CsvDataSourceProvider();
        var context = new DataSourceProviderContext(descriptor, new MemoryStream(bytes), limits);

        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() => provider.NormalizeAsync(context));
        Assert.Equal(ImportErrorCodes.RowLimitExceeded, ex.Code);
    }

    [Fact]
    public async Task Normalize_ColumnLimitExceeded_ThrowsColumnLimitExceeded()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 20; i++)
        {
            sb.Append($"Col{i},");
        }
        sb.AppendLine();

        var limits = new ImportPlatformLimits(
            MaximumPayloadBytes: 10_000_000,
            MaximumWorksheets: 5,
            MaximumRowsPerWorksheet: 100,
            MaximumColumnsPerWorksheet: 10, // Max 10 columns limit
            MaximumCells: 1000);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var descriptor = new DataSourceDescriptor(DataSourceProviderKind.Csv, "collimit.csv", "collimit.csv", "text/csv", bytes.Length);
        var provider = new CsvDataSourceProvider();
        var context = new DataSourceProviderContext(descriptor, new MemoryStream(bytes), limits);

        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() => provider.NormalizeAsync(context));
        Assert.Equal(ImportErrorCodes.ColumnLimitExceeded, ex.Code);
    }

    [Fact]
    public async Task Normalize_CancellationRequested_ThrowsCancelledException()
    {
        var bytes = Encoding.UTF8.GetBytes("Part Number,Qty\nPN-1,10\nPN-2,20");
        var descriptor = new DataSourceDescriptor(DataSourceProviderKind.Csv, "cancel.csv", "cancel.csv", "text/csv", bytes.Length);
        var provider = new CsvDataSourceProvider();
        var context = new DataSourceProviderContext(descriptor, new MemoryStream(bytes), ImportPlatformLimits.Default);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Assert.ThrowsAsync<ImportPlatformException>(() => provider.NormalizeAsync(context, cts.Token));
        Assert.Equal(ImportErrorCodes.Cancelled, ex.Code);
    }
}

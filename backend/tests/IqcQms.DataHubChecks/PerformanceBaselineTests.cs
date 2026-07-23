using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using IqcQms.Infrastructure.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class PerformanceBaselineTests
{
    [Theory]
    [InlineData(1000)]
    [InlineData(10000)]
    public async Task BenchmarkSyntheticCsvImportPipeline(int recordCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Part Number,Qty,Inspection Date,Result");
        for (int i = 1; i <= recordCount; i++)
        {
            sb.AppendLine($"PN-{i:D6},{i % 100 + 1},2026-07-24,PASS");
        }

        var csvBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var descriptor = new DataSourceDescriptor(DataSourceProviderKind.Csv, $"synthetic_{recordCount}.csv", "synthetic.csv", "text/csv", csvBytes.Length);
        var provider = new CsvDataSourceProvider();
        var context = new DataSourceProviderContext(descriptor, new MemoryStream(csvBytes), ImportPlatformLimits.Default);

        // 1. Normalization
        var sw = Stopwatch.StartNew();
        var workbook = await provider.NormalizeAsync(context);
        sw.Stop();
        var normTimeMs = sw.ElapsedMilliseconds;

        Assert.NotNull(workbook);
        Assert.Single(workbook.Worksheets);

        // 2. Mapping
        var mappingProfile = new MappingProfile("map-perf", "1.0", "Perf Map", "CSV", 1, true, new[]
        {
            new MappingRule("Part Number", "ItemCode", true, FieldTransformationType.TrimText),
            new MappingRule("Qty", "Quantity", true, FieldTransformationType.ParseInteger),
            new MappingRule("Result", "Result", false, FieldTransformationType.TrimText)
        });

        var mappingService = new WorkbookMappingService();
        sw.Restart();
        var mappingResult = await mappingService.ExecuteMappingAsync(workbook, mappingProfile);
        sw.Stop();
        var mapTimeMs = sw.ElapsedMilliseconds;

        Assert.Equal(recordCount, mappingResult.MappedRecordCount);

        // 3. Validation
        var valProfile = new ValidationProfile("val-perf", "1.0", "Perf Val", new[]
        {
            new ValidationRuleConfig("r1", "Qty", ValidationRuleKind.NumericRange, ValidationSeverity.Warning, "Quantity", MinNumeric: 1)
        });

        var valEngine = new ImportValidationEngine();
        sw.Restart();
        var valResult = await valEngine.ValidateAsync(mappingResult, valProfile);
        sw.Stop();
        var valTimeMs = sw.ElapsedMilliseconds;

        Assert.Equal(recordCount, valResult.Summary.TotalRecordsEvaluated);

        // Verify baseline timing constraints (10,000 records should process under 3,000ms)
        Assert.True(normTimeMs < 3000, $"Normalization for {recordCount} records took {normTimeMs}ms (expected < 3000ms).");
        Assert.True(mapTimeMs < 3000, $"Mapping for {recordCount} records took {mapTimeMs}ms (expected < 3000ms).");
        Assert.True(valTimeMs < 3000, $"Validation for {recordCount} records took {valTimeMs}ms (expected < 3000ms).");
    }
}

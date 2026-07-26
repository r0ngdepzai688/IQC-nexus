using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class MappingTests
{
    private static NormalizedWorkbook CreateSampleWorkbook()
    {
        var cellsHeader = new List<NormalizedCell>
        {
            new(1, 1, NormalizedCellRawType.String, "Part Number", null, null, null, null, null, false),
            new(1, 2, NormalizedCellRawType.String, "Quantity", null, null, null, null, null, false),
            new(1, 3, NormalizedCellRawType.String, "Unit Price", null, null, null, null, null, false),
            new(1, 4, NormalizedCellRawType.String, "Production Date", null, null, null, null, null, false),
            new(1, 5, NormalizedCellRawType.String, "Unmapped Header", null, null, null, null, null, false)
        };

        var cellsRow2 = new List<NormalizedCell>
        {
            new(2, 1, NormalizedCellRawType.String, "  PN-1001  ", null, null, null, null, null, false),
            new(2, 2, NormalizedCellRawType.Numeric, "50", 50.0, null, null, null, null, false),
            new(2, 3, NormalizedCellRawType.String, "12.50", null, null, null, null, null, false),
            new(2, 4, NormalizedCellRawType.String, "2026-07-24", null, null, null, null, null, false),
            new(2, 5, NormalizedCellRawType.String, "Extra Info", null, null, null, null, null, false)
        };

        var row1 = new NormalizedRow(1, cellsHeader, false);
        var row2 = new NormalizedRow(2, cellsRow2, false);

        var sheet = new NormalizedWorksheet(1, "DataSheet", WorksheetVisibility.Visible, 2, 5, Array.Empty<string>(), new[] { row1, row2 });
        return new NormalizedWorkbook("1.0", DataSourceProviderKind.Csv, "sample.csv", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());
    }

    [Fact]
    public async Task ExactHeaderMappingAndCoordinatePreservationWork()
    {
        var service = new WorkbookMappingService();
        var wb = CreateSampleWorkbook();

        var rules = new List<MappingRule>
        {
            new("Part Number", "PartNo", IsRequired: true, TransformationType: FieldTransformationType.TrimText),
            new("Quantity", "Qty", IsRequired: true, TransformationType: FieldTransformationType.ParseInteger),
            new("Unit Price", "Price", IsRequired: false, TransformationType: FieldTransformationType.ParseDecimal, CultureName: "en-US")
        };

        var profile = new MappingProfile("p1", "1.0", "Test Profile", "DataSheet", 1, false, rules, new[] { "Unmapped Header" });

        var result = await service.ExecuteMappingAsync(wb, profile);

        Assert.False(result.HasBlockingErrors);
        Assert.Equal(1, result.MappedRecordCount);

        var record = result.Records[0];
        Assert.Equal(3, record.Fields.Count);

        var partNoField = record.Fields[0];
        Assert.Equal("PartNo", partNoField.TargetField);
        Assert.Equal("PN-1001", partNoField.MappedValue);
        Assert.Equal("  PN-1001  ", partNoField.OriginalNormalizedValue);
        Assert.Equal(2, partNoField.Coordinate.RowNumber);
        Assert.Equal(1, partNoField.Coordinate.ColumnNumber);

        var qtyField = record.Fields[1];
        Assert.Equal(50, qtyField.MappedValue);
    }

    [Fact]
    public async Task DuplicateTargetMappingProducesErrorDiagnostic()
    {
        var service = new WorkbookMappingService();
        var wb = CreateSampleWorkbook();

        var rules = new List<MappingRule>
        {
            new("Part Number", "PartNo", IsRequired: true),
            new("Quantity", "PartNo", IsRequired: false) // Duplicate target
        };

        var profile = new MappingProfile("p1", "1.0", "Test Profile", "DataSheet", 1, false, rules);

        var result = await service.ExecuteMappingAsync(wb, profile);

        Assert.True(result.HasBlockingErrors);
        Assert.Contains(result.Diagnostics, d => d.Code == WorkbookMappingService.CodeDuplicateTargetField);
    }

    [Fact]
    public async Task MissingWorksheetProducesErrorDiagnostic()
    {
        var service = new WorkbookMappingService();
        var wb = CreateSampleWorkbook();

        var profile = new MappingProfile("p1", "1.0", "Test Profile", "NonExistentSheet", 1, false, new List<MappingRule>());

        var result = await service.ExecuteMappingAsync(wb, profile);

        Assert.True(result.HasBlockingErrors);
        Assert.Contains(result.Diagnostics, d => d.Code == WorkbookMappingService.CodeWorksheetNotFound);
    }

    [Fact]
    public async Task NumericValueNotSilentlyConvertedToDate()
    {
        var service = new WorkbookMappingService();
        var cells = new List<NormalizedCell>
        {
            new(1, 1, NormalizedCellRawType.String, "DateCol", null, null, null, null, null, false),
            new(2, 1, NormalizedCellRawType.Numeric, "45123.5", 45123.5, null, null, null, null, false)
        };
        var row1 = new NormalizedRow(1, new[] { cells[0] }, false);
        var row2 = new NormalizedRow(2, new[] { cells[1] }, false);
        var sheet = new NormalizedWorksheet(1, "Sheet1", WorksheetVisibility.Visible, 2, 1, Array.Empty<string>(), new[] { row1, row2 });
        var wb = new NormalizedWorkbook("1.0", DataSourceProviderKind.Excel, "test.xlsx", new[] { sheet }, Array.Empty<NormalizationDiagnostic>(), new Dictionary<string, string>());

        var profile = new MappingProfile("p1", "1.0", "Test", "Sheet1", 1, false, new[]
        {
            new MappingRule("DateCol", "TargetDate", TransformationType: FieldTransformationType.ParseDateTime)
        });

        var result = await service.ExecuteMappingAsync(wb, profile);

        var rec = result.Records[0];
        Assert.True(rec.Fields[0].HasTransformationError);
        Assert.Null(rec.Fields[0].MappedValue);
    }

    [Fact]
    public async Task CancellationIsHonored()
    {
        var service = new WorkbookMappingService();
        var wb = CreateSampleWorkbook();
        var profile = new MappingProfile("p1", "1.0", "Test", "DataSheet", 1, false, new List<MappingRule>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.ExecuteMappingAsync(wb, profile, cts.Token));
    }
}

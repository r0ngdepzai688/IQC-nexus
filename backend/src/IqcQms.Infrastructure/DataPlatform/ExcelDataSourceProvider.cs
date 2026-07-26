using System.Globalization;
using System.Text;
using ExcelDataReader;
using IqcQms.Application.DataPlatform;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class ExcelDataSourceProvider : IDataSourceProvider
{
    private static readonly HashSet<string> Extensions =
        new(StringComparer.OrdinalIgnoreCase) { ".xlsx" };

    public DataSourceProviderKind Kind => DataSourceProviderKind.Excel;
    public DataSourceProviderCapabilities Capabilities =>
        DataSourceProviderCapabilities.LocalFile |
        DataSourceProviderCapabilities.MultipleWorksheets |
        DataSourceProviderCapabilities.WorksheetVisibility |
        DataSourceProviderCapabilities.MergedRanges |
        DataSourceProviderCapabilities.NumberFormats;

    public bool CanHandle(DataSourceDescriptor source) =>
        source.Kind == Kind &&
        !string.IsNullOrWhiteSpace(source.FileName) &&
        Extensions.Contains(Path.GetExtension(source.FileName));

    public async Task<NormalizedWorkbook> NormalizeAsync(
        DataSourceProviderContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await ProviderInput.ReadBoundedAsync(context, cancellationToken);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var worksheets = new List<NormalizedWorksheet>();
            var diagnostics = new List<NormalizationDiagnostic>();
            long totalCells = 0;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (worksheets.Count >= context.Limits.MaximumWorksheets)
                    throw new ImportPlatformException(ImportErrorCodes.WorksheetLimitExceeded, "The workbook worksheet limit was exceeded.");

                var sheetIndex = worksheets.Count;
                var merges = (reader.MergeCells ?? [])
                    .Select(range => new MergeInfo(
                        range.FromRow + 1,
                        range.ToRow + 1,
                        range.FromColumn + 1,
                        range.ToColumn + 1))
                    .ToList();
                var rows = new List<NormalizedRow>();
                var maximumColumn = 0;
                while (reader.Read())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (rows.Count >= context.Limits.MaximumRowsPerWorksheet)
                        throw new ImportPlatformException(ImportErrorCodes.RowLimitExceeded, "The workbook row limit was exceeded.");
                    if (reader.FieldCount > context.Limits.MaximumColumnsPerWorksheet)
                        throw new ImportPlatformException(ImportErrorCodes.ColumnLimitExceeded, "The workbook column limit was exceeded.");

                    var rowNumber = rows.Count + 1;
                    maximumColumn = Math.Max(maximumColumn, reader.FieldCount);
                    totalCells += reader.FieldCount;
                    if (totalCells > context.Limits.MaximumCells)
                        throw new ImportPlatformException(ImportErrorCodes.CellLimitExceeded, "The workbook cell limit was exceeded.");

                    var cells = new List<NormalizedCell>(reader.FieldCount);
                    for (var column = 0; column < reader.FieldCount; column++)
                    {
                        var value = reader.GetValue(column);
                        var merge = merges.FirstOrDefault(item => item.Contains(rowNumber, column + 1));
                        cells.Add(CreateCell(
                            rowNumber,
                            column + 1,
                            value,
                            reader.GetNumberFormatString(column),
                            merge));
                    }
                    rows.Add(new NormalizedRow(rowNumber, cells, cells.All(cell => cell.RawType == NormalizedCellRawType.Empty)));
                }

                var visibility = reader.VisibleState switch
                {
                    "hidden" => WorksheetVisibility.Hidden,
                    "veryhidden" => WorksheetVisibility.VeryHidden,
                    _ => WorksheetVisibility.Visible
                };
                worksheets.Add(new NormalizedWorksheet(
                    sheetIndex,
                    string.IsNullOrWhiteSpace(reader.Name) ? $"Sheet {sheetIndex + 1}" : reader.Name,
                    visibility,
                    rows.Count,
                    maximumColumn,
                    merges.Select(item => item.Address).ToList(),
                    rows));
            } while (reader.NextResult());

            if (worksheets.Count == 0 || worksheets.All(sheet => sheet.RowCount == 0))
                throw new ImportPlatformException(ImportErrorCodes.WorkbookEmpty, "The workbook contains no rows.");

            diagnostics.Add(new NormalizationDiagnostic(
                "EXCEL_FORMULA_TEXT_UNAVAILABLE",
                "The current safe reader exposes cached formula values but not formula text.",
                DiagnosticSeverity.Information));
            return new NormalizedWorkbook(
                NormalizedWorkbookProtocol.CurrentVersion,
                Kind,
                context.Source.DisplayName,
                worksheets,
                diagnostics,
                new Dictionary<string, string> { ["format"] = "xlsx" });
        }
        catch (OperationCanceledException exception)
        {
            throw ProviderInput.Cancelled(exception);
        }
        catch (ImportPlatformException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.NormalizationFailed,
                "The Excel workbook could not be normalized.",
                exception);
        }
    }

    private static NormalizedCell CreateCell(
        int row,
        int column,
        object? value,
        string? numberFormat,
        MergeInfo? merge)
    {
        (NormalizedCellRawType rawType, string? stringValue, double? numericValue, bool? booleanValue) values = value switch
        {
            null or DBNull => (NormalizedCellRawType.Empty, null, null, null),
            string text => (NormalizedCellRawType.String, text, null, null),
            bool boolean => (NormalizedCellRawType.Boolean, null, null, (bool?)boolean),
            byte number => (NormalizedCellRawType.Numeric, null, (double?)number, null),
            short number => (NormalizedCellRawType.Numeric, null, (double?)number, null),
            int number => (NormalizedCellRawType.Numeric, null, (double?)number, null),
            long number => (NormalizedCellRawType.Numeric, null, (double?)number, null),
            float number => (NormalizedCellRawType.Numeric, null, (double?)number, null),
            double number => (NormalizedCellRawType.Numeric, null, (double?)number, null),
            decimal number => (NormalizedCellRawType.Numeric, null, (double?)number, null),
            // Excel serials formatted as dates can be surfaced as DateTime by the reader.
            // Preserve the serial as a number; interpretation remains a mapping concern.
            DateTime date => (NormalizedCellRawType.Numeric, null, (double?)date.ToOADate(), null),
            _ => (NormalizedCellRawType.Unknown, Convert.ToString(value, CultureInfo.InvariantCulture), null, null)
        };
        var (rawType, stringValue, numericValue, booleanValue) = values;
        return new NormalizedCell(
            row,
            column,
            rawType,
            stringValue,
            numericValue,
            booleanValue,
            null,
            numberFormat,
            merge?.Address,
            merge is not null && merge.FromRow == row && merge.FromColumn == column);
    }

    private sealed record MergeInfo(int FromRow, int ToRow, int FromColumn, int ToColumn)
    {
        public bool Contains(int row, int column) =>
            row >= FromRow && row <= ToRow && column >= FromColumn && column <= ToColumn;

        public string Address =>
            $"{ColumnName(FromColumn)}{FromRow}:{ColumnName(ToColumn)}{ToRow}";

        private static string ColumnName(int column)
        {
            var result = string.Empty;
            while (column > 0)
            {
                column--;
                result = (char)('A' + column % 26) + result;
                column /= 26;
            }
            return result;
        }
    }
}

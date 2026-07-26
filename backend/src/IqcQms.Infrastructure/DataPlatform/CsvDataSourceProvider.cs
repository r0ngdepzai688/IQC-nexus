using System.Globalization;
using System.Text;
using IqcQms.Application.DataPlatform;

namespace IqcQms.Infrastructure.DataPlatform;

public sealed class CsvDataSourceProvider : IDataSourceProvider
{
    private static readonly char[] CandidateDelimiters = [',', ';', '\t', '|'];

    public DataSourceProviderKind Kind => DataSourceProviderKind.Csv;
    public DataSourceProviderCapabilities Capabilities => DataSourceProviderCapabilities.LocalFile;

    public bool CanHandle(DataSourceDescriptor source) =>
        source.Kind == Kind &&
        (string.IsNullOrWhiteSpace(source.FileName) ||
         string.Equals(Path.GetExtension(source.FileName), ".csv", StringComparison.OrdinalIgnoreCase));

    public async Task<NormalizedWorkbook> NormalizeAsync(
        DataSourceProviderContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await ProviderInput.ReadBoundedAsync(context, cancellationToken);
            var text = DecodeUtf8(bytes);
            var delimiter = DetectDelimiter(text);
            var parsedRows = Parse(text, delimiter, cancellationToken);
            if (parsedRows.Count > context.Limits.MaximumRowsPerWorksheet)
                throw new ImportPlatformException(ImportErrorCodes.RowLimitExceeded, "The CSV row limit was exceeded.");

            var width = parsedRows.Select(row => row.Count).DefaultIfEmpty().Max();
            if (width > context.Limits.MaximumColumnsPerWorksheet)
                throw new ImportPlatformException(ImportErrorCodes.ColumnLimitExceeded, "The CSV column limit was exceeded.");
            if ((long)parsedRows.Count * width > context.Limits.MaximumCells)
                throw new ImportPlatformException(ImportErrorCodes.CellLimitExceeded, "The CSV cell limit was exceeded.");

            var rows = parsedRows.Select((fields, rowIndex) =>
                new NormalizedRow(
                    rowIndex + 1,
                    fields.Select((val, columnIndex) =>
                    {
                        var sanitizedValue = NeutralizeFormula(val);
                        return new NormalizedCell(
                            rowIndex + 1,
                            columnIndex + 1,
                            sanitizedValue.Length == 0 ? NormalizedCellRawType.Empty : NormalizedCellRawType.String,
                            sanitizedValue,
                            null,
                            null,
                            null,
                            null,
                            null,
                            false);
                    }).ToList(),
                    fields.All(string.IsNullOrEmpty))).ToList();

            var worksheet = new NormalizedWorksheet(0, "CSV", WorksheetVisibility.Visible, rows.Count, width, [], rows);
            return new NormalizedWorkbook(
                NormalizedWorkbookProtocol.CurrentVersion,
                Kind,
                context.Source.DisplayName,
                [worksheet],
                [new NormalizationDiagnostic("CSV_DELIMITER", $"Delimiter U+{(int)delimiter:X4} was selected.")],
                new Dictionary<string, string>
                {
                    ["encoding"] = "utf-8",
                    ["delimiter"] = delimiter.ToString(CultureInfo.InvariantCulture)
                });
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
                "The CSV payload could not be normalized.",
                exception);
        }
    }

    private static string NeutralizeFormula(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.StartsWith('=') || text.StartsWith('+') || text.StartsWith('-') || text.StartsWith('@'))
        {
            return "'" + text;
        }
        return text;
    }

    private static string DecodeUtf8(byte[] bytes)
    {
        try
        {
            var offset = bytes.Length >= 3 &&
                         bytes[0] == 0xEF &&
                         bytes[1] == 0xBB &&
                         bytes[2] == 0xBF
                ? 3
                : 0;
            return new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset);
        }
        catch (DecoderFallbackException exception)
        {
            throw new ImportPlatformException(
                ImportErrorCodes.NormalizationFailed,
                "The CSV payload must be valid UTF-8.",
                exception);
        }
    }

    private static char DetectDelimiter(string text)
    {
        var sample = text.Length <= 65_536 ? text : text[..65_536];
        var scores = CandidateDelimiters
            .Select(delimiter =>
            {
                var rows = Parse(sample, delimiter, CancellationToken.None);
                var widths = rows.Take(10).Select(row => row.Count).ToList();
                var mode = widths.GroupBy(value => value).OrderByDescending(group => group.Count()).ThenByDescending(group => group.Key).FirstOrDefault();
                return (delimiter, score: mode is null || mode.Key <= 1 ? 0 : mode.Count() * mode.Key);
            })
            .OrderByDescending(item => item.score)
            .ToList();
        return scores[0].score == 0 ? ',' : scores[0].delimiter;
    }

    private static List<List<string>> Parse(string text, char delimiter, CancellationToken cancellationToken)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            if ((index & 0xFFF) == 0) cancellationToken.ThrowIfCancellationRequested();
            var current = text[index];
            if (quoted)
            {
                if (current == '"' && index + 1 < text.Length && text[index + 1] == '"')
                {
                    field.Append('"');
                    index++;
                }
                else if (current == '"') quoted = false;
                else field.Append(current);
                continue;
            }

            if (current == '"' && field.Length == 0) quoted = true;
            else if (current == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (current is '\r' or '\n')
            {
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = [];
            }
            else field.Append(current);
        }

        if (quoted)
            throw new ImportPlatformException(ImportErrorCodes.NormalizationFailed, "The CSV payload contains an unterminated quoted field.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }
        return rows;
    }
}

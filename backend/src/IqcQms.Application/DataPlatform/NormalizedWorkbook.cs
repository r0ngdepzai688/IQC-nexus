namespace IqcQms.Application.DataPlatform;

public static class NormalizedWorkbookProtocol
{
    public const string CurrentVersion = "1.0";
    public static bool IsSupported(string version) =>
        string.Equals(version, CurrentVersion, StringComparison.Ordinal);
}

public enum DataSourceProviderKind
{
    Csv,
    Excel,
    ClientAgentExcel,
    NascaExcel,
    Api,
    Clipboard
}

public enum WorksheetVisibility
{
    Visible,
    Hidden,
    VeryHidden
}

public enum NormalizedCellRawType
{
    Empty,
    String,
    Numeric,
    Boolean,
    Error,
    DateTime,
    Unknown
}

public sealed record NormalizedWorkbook(
    string ProtocolVersion,
    DataSourceProviderKind SourceKind,
    string SourceDisplayName,
    IReadOnlyList<NormalizedWorksheet> Worksheets,
    IReadOnlyList<NormalizationDiagnostic> Diagnostics,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record NormalizedWorksheet(
    int Index,
    string Name,
    WorksheetVisibility Visibility,
    int RowCount,
    int ColumnCount,
    IReadOnlyList<string> MergedRanges,
    IReadOnlyList<NormalizedRow> Rows);

public sealed record NormalizedRow(
    int RowNumber,
    IReadOnlyList<NormalizedCell> Cells,
    bool IsEmpty);

public sealed record NormalizedCell(
    int RowNumber,
    int ColumnNumber,
    NormalizedCellRawType RawType,
    string? StringValue,
    double? NumericValue,
    bool? BooleanValue,
    string? Formula,
    string? NumberFormat,
    string? MergedRange,
    bool IsMergedAnchor);

public sealed record NormalizationDiagnostic(
    string Code,
    string Message,
    DiagnosticSeverity Severity = DiagnosticSeverity.Information,
    int? WorksheetIndex = null,
    int? RowNumber = null,
    int? ColumnNumber = null);

public enum DiagnosticSeverity
{
    Information,
    Warning,
    Error
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IqcQms.Application.DataPlatform;

public enum FieldTransformationType
{
    None,
    TrimText,
    NormalizeLineEndings,
    ParseInteger,
    ParseDecimal,
    ParseBoolean,
    ParseDateTime,
    LookupDictionary
}

public sealed record MappingRule(
    string SourceColumnIdentifier,
    string TargetField,
    bool IsRequired = false,
    FieldTransformationType TransformationType = FieldTransformationType.None,
    string? DefaultValue = null,
    string? CultureName = null,
    string? DateTimeFormat = null,
    IReadOnlyDictionary<string, string>? LookupDictionary = null);

public sealed record MappingProfile(
    string ProfileId,
    string Version,
    string Name,
    string? WorksheetName,
    int HeaderRowNumber,
    bool CaseInsensitiveHeaderMatching,
    IReadOnlyList<MappingRule> Rules,
    IReadOnlyList<string>? IgnoredSourceColumns = null);

public sealed record SourceCoordinate(
    int WorksheetIndex,
    string WorksheetName,
    int RowNumber,
    int ColumnNumber);

public sealed record MappedField(
    string TargetField,
    string SourceColumnName,
    SourceCoordinate Coordinate,
    string? OriginalNormalizedValue,
    object? MappedValue,
    NormalizedCellRawType MappedType,
    bool HasTransformationError);

public sealed record MappingDiagnostic(
    string Code,
    string Message,
    DiagnosticSeverity Severity,
    SourceCoordinate? Coordinate = null,
    string? TargetField = null);

public sealed record MappedRecord(
    int RecordIndex,
    SourceCoordinate RowCoordinate,
    IReadOnlyList<MappedField> Fields,
    IReadOnlyList<MappingDiagnostic> Diagnostics);

public sealed record MappingResult(
    string ProfileId,
    string ProfileVersion,
    int TotalRowsProcessed,
    int MappedRecordCount,
    IReadOnlyList<MappedRecord> Records,
    IReadOnlyList<MappingDiagnostic> Diagnostics,
    IReadOnlyList<string> UnmappedSourceColumns,
    IReadOnlyList<string> IgnoredSourceColumns)
{
    public bool HasBlockingErrors =>
        Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error) ||
        Records.Any(r => r.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error));
}

public interface IWorkbookMappingService
{
    Task<MappingResult> ExecuteMappingAsync(
        NormalizedWorkbook workbook,
        MappingProfile profile,
        CancellationToken cancellationToken = default);
}

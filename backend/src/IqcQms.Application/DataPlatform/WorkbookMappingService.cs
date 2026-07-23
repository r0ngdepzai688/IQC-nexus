using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IqcQms.Application.DataPlatform;

public sealed class WorkbookMappingService : IWorkbookMappingService
{
    public const string CodeWorksheetNotFound = "MAPPING_WORKSHEET_NOT_FOUND";
    public const string CodeHeaderRowNotFound = "MAPPING_HEADER_ROW_NOT_FOUND";
    public const string CodeDuplicateTargetField = "MAPPING_DUPLICATE_TARGET_FIELD";
    public const string CodeRequiredFieldMissing = "MAPPING_REQUIRED_FIELD_MISSING";
    public const string CodeTransformationFailed = "MAPPING_TRANSFORMATION_FAILED";
    public const string CodeUnmappedColumn = "MAPPING_UNMAPPED_COLUMN";

    public Task<MappingResult> ExecuteMappingAsync(
        NormalizedWorkbook workbook,
        MappingProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbook);
        ArgumentNullException.ThrowIfNull(profile);

        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new List<MappingDiagnostic>();

        // 1. Locate Worksheet
        NormalizedWorksheet? targetWorksheet = null;
        if (!string.IsNullOrWhiteSpace(profile.WorksheetName))
        {
            var comparison = profile.CaseInsensitiveHeaderMatching
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            targetWorksheet = workbook.Worksheets.FirstOrDefault(w => string.Equals(w.Name, profile.WorksheetName, comparison));
        }
        else
        {
            targetWorksheet = workbook.Worksheets.FirstOrDefault(w => w.Visibility == WorksheetVisibility.Visible)
                ?? workbook.Worksheets.FirstOrDefault();
        }

        if (targetWorksheet is null)
        {
            diagnostics.Add(new MappingDiagnostic(
                CodeWorksheetNotFound,
                $"Worksheet '{profile.WorksheetName ?? "default"}' was not found in the workbook.",
                DiagnosticSeverity.Error));

            return Task.FromResult(new MappingResult(
                profile.ProfileId,
                profile.Version,
                0,
                0,
                Array.Empty<MappedRecord>(),
                diagnostics,
                Array.Empty<string>(),
                profile.IgnoredSourceColumns ?? Array.Empty<string>()));
        }

        // 2. Validate Duplicate Targets
        var duplicateTargets = profile.Rules
            .GroupBy(r => r.TargetField, profile.CaseInsensitiveHeaderMatching ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var dup in duplicateTargets)
        {
            diagnostics.Add(new MappingDiagnostic(
                CodeDuplicateTargetField,
                $"Multiple rules map to the same target field '{dup}'. Duplicate target mappings are not permitted.",
                DiagnosticSeverity.Error,
                TargetField: dup));
        }

        // 3. Locate Header Row
        var headerRowNumber = profile.HeaderRowNumber > 0 ? profile.HeaderRowNumber : 1;
        var headerRow = targetWorksheet.Rows.FirstOrDefault(r => r.RowNumber == headerRowNumber);

        if (headerRow is null || headerRow.IsEmpty)
        {
            diagnostics.Add(new MappingDiagnostic(
                CodeHeaderRowNotFound,
                $"Header row #{headerRowNumber} was not found or is empty in worksheet '{targetWorksheet.Name}'.",
                DiagnosticSeverity.Error));

            return Task.FromResult(new MappingResult(
                profile.ProfileId,
                profile.Version,
                0,
                0,
                Array.Empty<MappedRecord>(),
                diagnostics,
                Array.Empty<string>(),
                profile.IgnoredSourceColumns ?? Array.Empty<string>()));
        }

        // Build header lookup (ColumnNumber -> HeaderName)
        var columnHeaderMap = new Dictionary<int, string>();
        foreach (var cell in headerRow.Cells)
        {
            var val = cell.StringValue?.Trim();
            if (!string.IsNullOrEmpty(val))
            {
                columnHeaderMap[cell.ColumnNumber] = val;
            }
        }

        // 4. Resolve Column Mappings
        var ruleColumnMap = new List<(MappingRule Rule, int ColumnNumber, string SourceColumnName)>();
        var ignoredSet = new HashSet<string>(
            profile.IgnoredSourceColumns ?? Array.Empty<string>(),
            profile.CaseInsensitiveHeaderMatching ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        var mappedColumnNumbers = new HashSet<int>();

        foreach (var rule in profile.Rules)
        {
            int matchedColNum = -1;
            string matchedColName = rule.SourceColumnIdentifier;

            // Try matching by exact 1-indexed column number or letter if identifier is numeric
            if (int.TryParse(rule.SourceColumnIdentifier, out var colIdx) && columnHeaderMap.ContainsKey(colIdx))
            {
                matchedColNum = colIdx;
                matchedColName = columnHeaderMap[colIdx];
            }
            else
            {
                var stringComp = profile.CaseInsensitiveHeaderMatching
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                var entry = columnHeaderMap.FirstOrDefault(kvp => string.Equals(kvp.Value, rule.SourceColumnIdentifier, stringComp));
                if (entry.Key > 0)
                {
                    matchedColNum = entry.Key;
                    matchedColName = entry.Value;
                }
            }

            if (matchedColNum > 0)
            {
                ruleColumnMap.Add((rule, matchedColNum, matchedColName));
                mappedColumnNumbers.Add(matchedColNum);
            }
            else if (rule.IsRequired)
            {
                diagnostics.Add(new MappingDiagnostic(
                    CodeRequiredFieldMissing,
                    $"Required source column '{rule.SourceColumnIdentifier}' for target field '{rule.TargetField}' was not found in worksheet headers.",
                    DiagnosticSeverity.Error,
                    TargetField: rule.TargetField));
            }
        }

        // Identify unmapped source columns
        var unmappedSourceColumns = columnHeaderMap
            .Where(kvp => !mappedColumnNumbers.Contains(kvp.Key) && !ignoredSet.Contains(kvp.Value))
            .Select(kvp => kvp.Value)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        // 5. Process Data Rows
        var dataRows = targetWorksheet.Rows
            .Where(r => r.RowNumber > headerRowNumber && !r.IsEmpty)
            .OrderBy(r => r.RowNumber)
            .ToList();

        var records = new List<MappedRecord>();
        int recordIndex = 0;

        foreach (var row in dataRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            recordIndex++;

            var recordDiagnostics = new List<MappingDiagnostic>();
            var mappedFields = new List<MappedField>();

            var rowCoord = new SourceCoordinate(targetWorksheet.Index, targetWorksheet.Name, row.RowNumber, 1);

            foreach (var (rule, colNum, colName) in ruleColumnMap)
            {
                var coord = new SourceCoordinate(targetWorksheet.Index, targetWorksheet.Name, row.RowNumber, colNum);
                var cell = row.Cells.FirstOrDefault(c => c.ColumnNumber == colNum);

                string? rawStringValue = ExtractRawStringValue(cell);
                (object? transformedValue, bool hasError, string? errorMsg) = TransformValue(
                    rawStringValue, cell, rule, coord);

                if (hasError && !string.IsNullOrEmpty(errorMsg))
                {
                    recordDiagnostics.Add(new MappingDiagnostic(
                        CodeTransformationFailed,
                        errorMsg,
                        DiagnosticSeverity.Error,
                        Coordinate: coord,
                        TargetField: rule.TargetField));
                }

                // Check required field
                if (rule.IsRequired && (transformedValue is null || (transformedValue is string s && string.IsNullOrWhiteSpace(s))))
                {
                    recordDiagnostics.Add(new MappingDiagnostic(
                        CodeRequiredFieldMissing,
                        $"Field '{rule.TargetField}' is required but received a null or empty value at row {row.RowNumber}, column {colNum}.",
                        DiagnosticSeverity.Error,
                        Coordinate: coord,
                        TargetField: rule.TargetField));
                }

                var mappedType = cell?.RawType ?? NormalizedCellRawType.Empty;
                mappedFields.Add(new MappedField(
                    rule.TargetField,
                    colName,
                    coord,
                    rawStringValue,
                    transformedValue,
                    mappedType,
                    hasError));
            }

            records.Add(new MappedRecord(recordIndex, rowCoord, mappedFields, recordDiagnostics));
        }

        var finalResult = new MappingResult(
            profile.ProfileId,
            profile.Version,
            dataRows.Count,
            records.Count,
            records,
            diagnostics,
            unmappedSourceColumns,
            ignoredSet.OrderBy(s => s, StringComparer.Ordinal).ToList());

        return Task.FromResult(finalResult);
    }

    private static string? ExtractRawStringValue(NormalizedCell? cell)
    {
        if (cell is null || cell.RawType == NormalizedCellRawType.Empty)
            return null;

        if (cell.StringValue != null)
            return cell.StringValue;

        if (cell.NumericValue.HasValue)
            return cell.NumericValue.Value.ToString(CultureInfo.InvariantCulture);

        if (cell.BooleanValue.HasValue)
            return cell.BooleanValue.Value ? "true" : "false";

        return null;
    }

    private static (object? MappedValue, bool HasError, string? ErrorMessage) TransformValue(
        string? rawString,
        NormalizedCell? cell,
        MappingRule rule,
        SourceCoordinate coord)
    {
        var val = rawString;
        var culture = !string.IsNullOrWhiteSpace(rule.CultureName)
            ? CultureInfo.GetCultureInfo(rule.CultureName)
            : CultureInfo.InvariantCulture;

        // Default value handling if empty/null
        if (string.IsNullOrWhiteSpace(val))
        {
            if (rule.DefaultValue != null)
            {
                val = rule.DefaultValue;
            }
            else
            {
                return (null, false, null);
            }
        }

        switch (rule.TransformationType)
        {
            case FieldTransformationType.None:
                return (val, false, null);

            case FieldTransformationType.TrimText:
                return (val?.Trim(), false, null);

            case FieldTransformationType.NormalizeLineEndings:
                return (val != null ? Regex.Replace(val, @"\r\n|\r", "\n") : null, false, null);

            case FieldTransformationType.ParseInteger:
                if (cell != null && cell.RawType == NormalizedCellRawType.DateTime)
                {
                    return (null, true, $"Cannot convert Date value to Integer at row {coord.RowNumber}, column {coord.ColumnNumber}.");
                }
                if (cell != null && cell.NumericValue.HasValue)
                {
                    var num = cell.NumericValue.Value;
                    if (Math.Abs(num % 1) < double.Epsilon && num >= int.MinValue && num <= int.MaxValue)
                        return ((int)num, false, null);
                }
                if (int.TryParse(val, NumberStyles.Integer, culture, out var parsedInt))
                {
                    return (parsedInt, false, null);
                }
                return (null, true, $"Value '{val}' could not be parsed as Integer using culture '{culture.Name}'.");

            case FieldTransformationType.ParseDecimal:
                if (cell != null && cell.RawType == NormalizedCellRawType.DateTime)
                {
                    return (null, true, $"Cannot convert Date value to Decimal at row {coord.RowNumber}, column {coord.ColumnNumber}.");
                }
                if (cell != null && cell.NumericValue.HasValue)
                {
                    return ((decimal)cell.NumericValue.Value, false, null);
                }
                if (decimal.TryParse(val, NumberStyles.Number, culture, out var parsedDec))
                {
                    return (parsedDec, false, null);
                }
                return (null, true, $"Value '{val}' could not be parsed as Decimal using culture '{culture.Name}'.");

            case FieldTransformationType.ParseBoolean:
                if (val != null)
                {
                    var clean = val.Trim().ToLowerInvariant();
                    if (clean is "true" or "1" or "yes" or "y") return (true, false, null);
                    if (clean is "false" or "0" or "no" or "n") return (false, false, null);
                }
                return (null, true, $"Value '{val}' could not be parsed as Boolean.");

            case FieldTransformationType.ParseDateTime:
                if (cell != null && cell.RawType == NormalizedCellRawType.Numeric)
                {
                    return (null, true, $"Cannot silently convert Numeric value to DateTime at row {coord.RowNumber}, column {coord.ColumnNumber}. Explicit date format or string representation is required.");
                }
                if (!string.IsNullOrWhiteSpace(rule.DateTimeFormat))
                {
                    if (DateTime.TryParseExact(val, rule.DateTimeFormat, culture, DateTimeStyles.None, out var dtExact))
                    {
                        return (dtExact, false, null);
                    }
                }
                else if (DateTime.TryParse(val, culture, DateTimeStyles.None, out var dtParsed))
                {
                    return (dtParsed, false, null);
                }
                return (null, true, $"Value '{val}' could not be parsed as DateTime using culture '{culture.Name}'.");

            case FieldTransformationType.LookupDictionary:
                if (rule.LookupDictionary != null && val != null)
                {
                    var lookupKey = rule.LookupDictionary.Keys.FirstOrDefault(k => string.Equals(k, val, StringComparison.OrdinalIgnoreCase));
                    if (lookupKey != null && rule.LookupDictionary.TryGetValue(lookupKey, out var mappedDictVal))
                    {
                        return (mappedDictVal, false, null);
                    }
                    return (null, true, $"Value '{val}' was not found in the lookup dictionary for field '{rule.TargetField}'.");
                }
                return (val, false, null);

            default:
                return (val, false, null);
        }
    }
}

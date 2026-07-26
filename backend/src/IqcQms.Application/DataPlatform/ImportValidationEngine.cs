using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace IqcQms.Application.DataPlatform;

public sealed class ImportValidationEngine : IImportValidationEngine
{
    public const string CodeRequiredMissing = "VAL_REQUIRED_MISSING";
    public const string CodeTextLengthExceeded = "VAL_TEXT_LENGTH_EXCEEDED";
    public const string CodeTextTooShort = "VAL_TEXT_TOO_SHORT";
    public const string CodeNumericOutOfRange = "VAL_NUMERIC_OUT_OF_RANGE";
    public const string CodeDateOutOfRange = "VAL_DATE_OUT_OF_RANGE";
    public const string CodeAllowedValuesViolation = "VAL_ALLOWED_VALUES_VIOLATION";
    public const string CodeRegexMismatch = "VAL_REGEX_MISMATCH";
    public const string CodeRegexTimeout = "VAL_REGEX_TIMEOUT";
    public const string CodeDuplicateValue = "VAL_DUPLICATE_VALUE";
    public const string CodeCrossFieldViolation = "VAL_CROSS_FIELD_VIOLATION";
    public const string CodeDiagnosticCapReached = "VAL_DIAGNOSTIC_CAP_REACHED";

    public Task<ValidationResult> ValidateAsync(
        MappingResult mappingResult,
        ValidationProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mappingResult);
        ArgumentNullException.ThrowIfNull(profile);

        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new List<ValidationDiagnostic>();
        var maxDiags = profile.MaximumDiagnostics > 0 ? profile.MaximumDiagnostics : 1000;

        // 1. Pass-through diagnostics from Mapping phase if any
        foreach (var mapDiag in mappingResult.Diagnostics)
        {
            if (diagnostics.Count >= maxDiags) break;
            diagnostics.Add(new ValidationDiagnostic(
                "MAPPING_PHASE",
                mapDiag.Code,
                mapDiag.Message,
                mapDiag.Severity == DiagnosticSeverity.Error ? ValidationSeverity.Error : ValidationSeverity.Warning,
                ValidationScope.Workbook,
                mapDiag.Coordinate,
                mapDiag.TargetField));
        }

        // 2. Pre-process Unique Value Rules across all records
        var uniqueRules = profile.Rules.Where(r => r.Kind == ValidationRuleKind.UniqueValue && !string.IsNullOrEmpty(r.TargetField)).ToList();
        var uniqueFieldTrackers = uniqueRules.ToDictionary(
            r => r.TargetField!,
            r => new Dictionary<string, List<(int RecordIndex, SourceCoordinate Coord)>>(StringComparer.OrdinalIgnoreCase));

        if (uniqueRules.Count > 0)
        {
            foreach (var record in mappingResult.Records)
            {
                foreach (var rule in uniqueRules)
                {
                    var field = record.Fields.FirstOrDefault(f => string.Equals(f.TargetField, rule.TargetField, StringComparison.OrdinalIgnoreCase));
                    if (field?.MappedValue != null)
                    {
                        var strVal = field.MappedValue.ToString()?.Trim();
                        if (!string.IsNullOrEmpty(strVal))
                        {
                            var tracker = uniqueFieldTrackers[rule.TargetField!];
                            if (!tracker.TryGetValue(strVal, out var list))
                            {
                                list = new List<(int, SourceCoordinate)>();
                                tracker[strVal] = list;
                            }
                            list.Add((record.RecordIndex, field.Coordinate));
                        }
                    }
                }
            }

            foreach (var rule in uniqueRules)
            {
                var tracker = uniqueFieldTrackers[rule.TargetField!];
                foreach (var entry in tracker.Where(e => e.Value.Count > 1))
                {
                    foreach (var occurrence in entry.Value)
                    {
                        if (diagnostics.Count >= maxDiags) break;
                        diagnostics.Add(new ValidationDiagnostic(
                            rule.RuleId,
                            CodeDuplicateValue,
                            $"Duplicate value for unique field '{rule.TargetField}' detected at record #{occurrence.RecordIndex}.",
                            rule.Severity,
                            ValidationScope.Field,
                            occurrence.Coord,
                            rule.TargetField,
                            new Dictionary<string, string>
                            {
                                ["TargetField"] = rule.TargetField!,
                                ["DuplicatesCount"] = entry.Value.Count.ToString(CultureInfo.InvariantCulture)
                            }));
                    }
                }
            }
        }

        // 3. Evaluate Per-Record Rules
        var invalidRecordIndexes = new HashSet<int>();

        foreach (var record in mappingResult.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (diagnostics.Count >= maxDiags)
            {
                diagnostics.Add(new ValidationDiagnostic(
                    "SYSTEM",
                    CodeDiagnosticCapReached,
                    $"Maximum diagnostic limit ({maxDiags}) reached. Further diagnostics were suppressed.",
                    ValidationSeverity.Warning,
                    ValidationScope.Workbook));
                break;
            }

            // Include mapping errors from the record
            foreach (var recDiag in record.Diagnostics)
            {
                if (recDiag.Severity == DiagnosticSeverity.Error)
                {
                    invalidRecordIndexes.Add(record.RecordIndex);
                }
            }

            foreach (var rule in profile.Rules)
            {
                if (rule.Kind == ValidationRuleKind.UniqueValue) continue;

                if (!string.IsNullOrEmpty(rule.TargetField))
                {
                    var field = record.Fields.FirstOrDefault(f => string.Equals(f.TargetField, rule.TargetField, StringComparison.OrdinalIgnoreCase));
                    EvaluateFieldRule(rule, field, record, diagnostics, invalidRecordIndexes, maxDiags);
                }
            }
        }

        // Compute Summary
        int infoCount = diagnostics.Count(d => d.Severity == ValidationSeverity.Information);
        int warnCount = diagnostics.Count(d => d.Severity == ValidationSeverity.Warning);
        int errorCount = diagnostics.Count(d => d.Severity == ValidationSeverity.Error);
        int blockingCount = diagnostics.Count(d => d.Severity == ValidationSeverity.BlockingError);

        var summary = new ValidationSummary(
            mappingResult.MappedRecordCount,
            mappingResult.MappedRecordCount - invalidRecordIndexes.Count,
            invalidRecordIndexes.Count,
            infoCount,
            warnCount,
            errorCount,
            blockingCount);

        return Task.FromResult(new ValidationResult(
            profile.ProfileId,
            profile.Version,
            summary,
            diagnostics));
    }

    private static void EvaluateFieldRule(
        ValidationRuleConfig rule,
        MappedField? field,
        MappedRecord record,
        List<ValidationDiagnostic> diagnostics,
        HashSet<int> invalidRecordIndexes,
        int maxDiags)
    {
        if (diagnostics.Count >= maxDiags) return;

        var val = field?.MappedValue;
        var coord = field?.Coordinate ?? record.RowCoordinate;
        var fieldName = rule.TargetField ?? "Field";

        switch (rule.Kind)
        {
            case ValidationRuleKind.RequiredValue:
                if (val is null || (val is string s && string.IsNullOrWhiteSpace(s)))
                {
                    AddDiagnostic(rule, CodeRequiredMissing, $"Field '{fieldName}' is required.", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                }
                break;

            case ValidationRuleKind.TextLength:
                if (val is string textVal && !string.IsNullOrEmpty(textVal))
                {
                    if (rule.MinLength.HasValue && textVal.Length < rule.MinLength.Value)
                    {
                        AddDiagnostic(rule, CodeTextTooShort, $"Field '{fieldName}' length ({textVal.Length}) is shorter than minimum required length ({rule.MinLength.Value}).", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                    }
                    if (rule.MaxLength.HasValue && textVal.Length > rule.MaxLength.Value)
                    {
                        AddDiagnostic(rule, CodeTextLengthExceeded, $"Field '{fieldName}' length ({textVal.Length}) exceeds maximum allowed length ({rule.MaxLength.Value}).", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                    }
                }
                break;

            case ValidationRuleKind.NumericRange:
                if (val != null)
                {
                    if (double.TryParse(val.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var numVal))
                    {
                        if (rule.MinNumeric.HasValue && numVal < rule.MinNumeric.Value)
                        {
                            AddDiagnostic(rule, CodeNumericOutOfRange, $"Field '{fieldName}' value ({numVal}) is below minimum ({rule.MinNumeric.Value}).", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                        }
                        if (rule.MaxNumeric.HasValue && numVal > rule.MaxNumeric.Value)
                        {
                            AddDiagnostic(rule, CodeNumericOutOfRange, $"Field '{fieldName}' value ({numVal}) exceeds maximum ({rule.MaxNumeric.Value}).", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                        }
                    }
                }
                break;

            case ValidationRuleKind.DateRange:
                if (val is DateTime dtVal)
                {
                    if (rule.MinDate.HasValue && dtVal < rule.MinDate.Value)
                    {
                        AddDiagnostic(rule, CodeDateOutOfRange, $"Field '{fieldName}' date ({dtVal:yyyy-MM-dd}) is earlier than minimum allowed ({rule.MinDate.Value:yyyy-MM-dd}).", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                    }
                    if (rule.MaxDate.HasValue && dtVal > rule.MaxDate.Value)
                    {
                        AddDiagnostic(rule, CodeDateOutOfRange, $"Field '{fieldName}' date ({dtVal:yyyy-MM-dd}) is later than maximum allowed ({rule.MaxDate.Value:yyyy-MM-dd}).", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                    }
                }
                break;

            case ValidationRuleKind.AllowedValues:
                if (val != null && rule.AllowedValues != null && rule.AllowedValues.Count > 0)
                {
                    var strVal = val.ToString();
                    if (!rule.AllowedValues.Contains(strVal, StringComparer.OrdinalIgnoreCase))
                    {
                        AddDiagnostic(rule, CodeAllowedValuesViolation, $"Field '{fieldName}' value is not in the allowed value set.", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                    }
                }
                break;

            case ValidationRuleKind.RegexPattern:
                if (val != null && !string.IsNullOrEmpty(rule.RegexPattern))
                {
                    try
                    {
                        var timeout = TimeSpan.FromMilliseconds(rule.RegexTimeoutMs > 0 ? rule.RegexTimeoutMs : 100);
                        var isMatch = Regex.IsMatch(val.ToString() ?? "", rule.RegexPattern, RegexOptions.None, timeout);
                        if (!isMatch)
                        {
                            AddDiagnostic(rule, CodeRegexMismatch, $"Field '{fieldName}' value does not match expected format pattern.", rule.Severity, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        AddDiagnostic(rule, CodeRegexTimeout, $"Regex validation for field '{fieldName}' timed out.", ValidationSeverity.Error, ValidationScope.Field, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                    }
                }
                break;

            case ValidationRuleKind.CrossFieldComparison:
                if (!string.IsNullOrEmpty(rule.SecondaryTargetField))
                {
                    var secField = record.Fields.FirstOrDefault(f => string.Equals(f.TargetField, rule.SecondaryTargetField, StringComparison.OrdinalIgnoreCase));
                    if (val != null && secField?.MappedValue != null)
                    {
                        bool valid = EvaluateComparison(val, secField.MappedValue, rule.ComparisonOperator ?? "<=");
                        if (!valid)
                        {
                            AddDiagnostic(rule, CodeCrossFieldViolation, $"Cross-field condition ({fieldName} {rule.ComparisonOperator ?? "<="} {rule.SecondaryTargetField}) failed at record #{record.RecordIndex}.", rule.Severity, ValidationScope.Record, coord, fieldName, diagnostics, invalidRecordIndexes, record.RecordIndex);
                        }
                    }
                }
                break;
        }
    }

    private static bool EvaluateComparison(object val1, object val2, string op)
    {
        if (double.TryParse(val1.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d1) &&
            double.TryParse(val2.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d2))
        {
            return op switch
            {
                "<=" => d1 <= d2,
                ">=" => d1 >= d2,
                "<" => d1 < d2,
                ">" => d1 > d2,
                "==" => Math.Abs(d1 - d2) < double.Epsilon,
                "!=" => Math.Abs(d1 - d2) >= double.Epsilon,
                _ => true
            };
        }
        return true;
    }

    private static void AddDiagnostic(
        ValidationRuleConfig rule,
        string code,
        string message,
        ValidationSeverity severity,
        ValidationScope scope,
        SourceCoordinate coord,
        string targetField,
        List<ValidationDiagnostic> diagnostics,
        HashSet<int> invalidRecordIndexes,
        int recordIndex)
    {
        if (severity is ValidationSeverity.Error or ValidationSeverity.BlockingError)
        {
            invalidRecordIndexes.Add(recordIndex);
        }

        diagnostics.Add(new ValidationDiagnostic(
            rule.RuleId,
            code,
            message,
            severity,
            scope,
            coord,
            targetField));
    }
}

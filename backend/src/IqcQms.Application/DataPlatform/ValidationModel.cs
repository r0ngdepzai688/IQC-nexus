using System;
using System.Collections.Generic;

namespace IqcQms.Application.DataPlatform;

public enum ValidationSeverity
{
    Information,
    Warning,
    Error,
    BlockingError
}

public enum ValidationScope
{
    Field,
    Record,
    Worksheet,
    Workbook
}

public enum ValidationRuleKind
{
    RequiredValue,
    TextLength,
    NumericRange,
    DateRange,
    AllowedValues,
    RegexPattern,
    UniqueValue,
    CrossFieldComparison
}

public sealed record ValidationRuleConfig(
    string RuleId,
    string Name,
    ValidationRuleKind Kind,
    ValidationSeverity Severity,
    string? TargetField,
    string? SecondaryTargetField = null,
    int? MinLength = null,
    int? MaxLength = null,
    double? MinNumeric = null,
    double? MaxNumeric = null,
    DateTime? MinDate = null,
    DateTime? MaxDate = null,
    IReadOnlyList<string>? AllowedValues = null,
    string? RegexPattern = null,
    int RegexTimeoutMs = 100,
    string? ComparisonOperator = null); // "<=", ">=", "==", "!="

public sealed record ValidationProfile(
    string ProfileId,
    string Version,
    string Name,
    IReadOnlyList<ValidationRuleConfig> Rules,
    int MaximumDiagnostics = 1000);

public sealed record ValidationDiagnostic(
    string RuleId,
    string Code,
    string Message,
    ValidationSeverity Severity,
    ValidationScope Scope,
    SourceCoordinate? Coordinate = null,
    string? TargetField = null,
    IReadOnlyDictionary<string, string>? SanitizedParameters = null);

public sealed record ValidationSummary(
    int TotalRecordsEvaluated,
    int ValidRecords,
    int InvalidRecords,
    int InformationCount,
    int WarningCount,
    int ErrorCount,
    int BlockingErrorCount)
{
    public bool HasBlockingErrors => BlockingErrorCount > 0;
    public bool IsValid => ErrorCount == 0 && BlockingErrorCount == 0;
}

public sealed record ValidationResult(
    string ValidationProfileId,
    string ValidationProfileVersion,
    ValidationSummary Summary,
    IReadOnlyList<ValidationDiagnostic> Diagnostics);

public interface IImportValidationEngine
{
    Task<ValidationResult> ValidateAsync(
        MappingResult mappingResult,
        ValidationProfile profile,
        CancellationToken cancellationToken = default);
}

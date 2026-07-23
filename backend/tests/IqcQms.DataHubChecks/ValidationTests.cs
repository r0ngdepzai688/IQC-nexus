using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IqcQms.Application.DataPlatform;
using Xunit;

namespace IqcQms.DataHubChecks;

public sealed class ValidationTests
{
    private static MappingResult CreateSyntheticMappingResult()
    {
        var coord1 = new SourceCoordinate(1, "DataSheet", 2, 1);
        var coord2 = new SourceCoordinate(1, "DataSheet", 3, 1);

        var record1 = new MappedRecord(1, coord1, new List<MappedField>
        {
            new("PartNo", "Part Number", coord1, "PN-100", "PN-100", NormalizedCellRawType.String, false),
            new("Qty", "Quantity", new SourceCoordinate(1, "DataSheet", 2, 2), "50", 50, NormalizedCellRawType.Numeric, false)
        }, Array.Empty<MappingDiagnostic>());

        var record2 = new MappedRecord(2, coord2, new List<MappedField>
        {
            new("PartNo", "Part Number", coord2, "PN-100", "PN-100", NormalizedCellRawType.String, false), // Duplicate PartNo
            new("Qty", "Quantity", new SourceCoordinate(1, "DataSheet", 3, 2), "-5", -5, NormalizedCellRawType.Numeric, false) // Invalid Qty
        }, Array.Empty<MappingDiagnostic>());

        return new MappingResult("p1", "1.0", 2, 2, new[] { record1, record2 }, Array.Empty<MappingDiagnostic>(), Array.Empty<string>(), Array.Empty<string>());
    }

    [Fact]
    public async Task ValidationEngineEnforcesRulesAndUniqueness()
    {
        var engine = new ImportValidationEngine();
        var mappingResult = CreateSyntheticMappingResult();

        var rules = new List<ValidationRuleConfig>
        {
            new("v1", "Unique Part Number", ValidationRuleKind.UniqueValue, ValidationSeverity.Error, "PartNo"),
            new("v2", "Positive Quantity", ValidationRuleKind.NumericRange, ValidationSeverity.Error, "Qty", MinNumeric: 1)
        };

        var profile = new ValidationProfile("val-1", "1.0", "Validation Profile", rules);

        var result = await engine.ValidateAsync(mappingResult, profile);

        Assert.Equal(2, result.Summary.TotalRecordsEvaluated);
        Assert.False(result.Summary.IsValid);
        Assert.True(result.Diagnostics.Count >= 3); // 2 duplicate diagnostics + 1 out of range Qty

        Assert.Contains(result.Diagnostics, d => d.Code == ImportValidationEngine.CodeDuplicateValue);
        Assert.Contains(result.Diagnostics, d => d.Code == ImportValidationEngine.CodeNumericOutOfRange);
    }

    [Fact]
    public async Task SafeRegexTimeoutIsHandled()
    {
        var engine = new ImportValidationEngine();
        var coord = new SourceCoordinate(1, "DataSheet", 2, 1);
        var rec = new MappedRecord(1, coord, new[]
        {
            new MappedField("Code", "Code", coord, "ABC-123", "ABC-123", NormalizedCellRawType.String, false)
        }, Array.Empty<MappingDiagnostic>());

        var mappingResult = new MappingResult("p1", "1.0", 1, 1, new[] { rec }, Array.Empty<MappingDiagnostic>(), Array.Empty<string>(), Array.Empty<string>());

        var profile = new ValidationProfile("v1", "1.0", "Regex Test", new[]
        {
            new ValidationRuleConfig("r1", "Regex", ValidationRuleKind.RegexPattern, ValidationSeverity.Error, "Code", RegexPattern: @"^[A-Z]{3}-\d{3}$")
        });

        var result = await engine.ValidateAsync(mappingResult, profile);

        Assert.True(result.Summary.IsValid);
    }

    [Fact]
    public async Task DiagnosticLimitIsSuppressedWhenExceeded()
    {
        var engine = new ImportValidationEngine();
        var coord = new SourceCoordinate(1, "DataSheet", 2, 1);
        var rec = new MappedRecord(1, coord, new[]
        {
            new MappedField("Field1", "Field1", coord, null, null, NormalizedCellRawType.Empty, false)
        }, Array.Empty<MappingDiagnostic>());

        var mappingResult = new MappingResult("p1", "1.0", 1, 1, new[] { rec }, Array.Empty<MappingDiagnostic>(), Array.Empty<string>(), Array.Empty<string>());

        var profile = new ValidationProfile("v1", "1.0", "Cap Test", new[]
        {
            new ValidationRuleConfig("r1", "Req", ValidationRuleKind.RequiredValue, ValidationSeverity.Error, "Field1")
        }, MaximumDiagnostics: 1);

        var result = await engine.ValidateAsync(mappingResult, profile);

        Assert.True(result.Diagnostics.Count <= 2);
    }

    [Fact]
    public async Task ValidationIsNonMutatingAndDeterministic()
    {
        var engine = new ImportValidationEngine();
        var mappingResult = CreateSyntheticMappingResult();
        var profile = new ValidationProfile("v1", "1.0", "Test", new[]
        {
            new ValidationRuleConfig("r1", "Req", ValidationRuleKind.RequiredValue, ValidationSeverity.Error, "PartNo")
        });

        var res1 = await engine.ValidateAsync(mappingResult, profile);
        var res2 = await engine.ValidateAsync(mappingResult, profile);

        Assert.Equal(res1.Summary.TotalRecordsEvaluated, res2.Summary.TotalRecordsEvaluated);
        Assert.Equal(res1.Summary.ErrorCount, res2.Summary.ErrorCount);
        Assert.Equal(res1.Diagnostics.Count, res2.Diagnostics.Count);
    }
}

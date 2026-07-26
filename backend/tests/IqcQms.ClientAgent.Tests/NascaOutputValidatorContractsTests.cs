using System.Reflection;
using IqcQms.ClientAgent.Application.Nasca;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class NascaOutputValidatorContractsTests
{
    [Fact]
    public void Outcome_Enum_Contains_All_Required_Values()
    {
        var values = Enum.GetNames(typeof(NascaOutputValidationOutcome));

        Assert.Contains("Valid", values);
        Assert.Contains("Missing", values);
        Assert.Contains("Empty", values);
        Assert.Contains("Unstable", values);
        Assert.Contains("OutsideApprovedRoot", values);
        Assert.Contains("TraversalDetected", values);
        Assert.Contains("ReparsePointDetected", values);
        Assert.Contains("UnexpectedDirectory", values);
        Assert.Contains("MaximumDepthExceeded", values);
        Assert.Contains("TooManyFiles", values);
        Assert.Contains("SingleFileSizeExceeded", values);
        Assert.Contains("TotalSizeExceeded", values);
        Assert.Contains("CorrelationMismatch", values);
        Assert.Contains("DuplicateOutput", values);
        Assert.Contains("Cancelled", values);
        Assert.Contains("ValidationTimedOut", values);
        Assert.Contains("AccessDenied", values);
        Assert.Contains("QuarantineRequired", values);
        Assert.Contains("UnknownFailure", values);
    }

    [Fact]
    public void Result_Valid_Invariants_Are_Accepted()
    {
        var result = new NascaOutputValidationResult
        {
            Outcome = NascaOutputValidationOutcome.Valid,
            SanitizedReasonCode = "VALIDATED",
            ValidatedFileCount = 1,
            ValidatedTotalSizeBytes = 123,
            ValidationStartedUtc = DateTime.UtcNow,
            ValidationCompletedUtc = DateTime.UtcNow.AddSeconds(1),
            Descriptors = new[]
            {
                new NascaValidatedFileDescriptor
                {
                    RelativePath = "output/data.json",
                    FileSizeBytes = 123,
                    Sha256Hash = "abc123",
                    FileIndex = 0,
                    ValidatedUtc = DateTime.UtcNow
                }
            }
        };

        result.ValidateInvariants();

        Assert.True(result.IsValid);
        Assert.False(result.IsRetryable);
        Assert.False(result.RequiresQuarantine);
    }

    [Fact]
    public void Result_Negative_File_Count_Is_Rejected()
    {
        var result = new NascaOutputValidationResult
        {
            Outcome = NascaOutputValidationOutcome.Missing,
            SanitizedReasonCode = "MISSING",
            ValidatedFileCount = -1,
            ValidatedTotalSizeBytes = 0,
            ValidationStartedUtc = DateTime.UtcNow,
            ValidationCompletedUtc = DateTime.UtcNow
        };

        Assert.Throws<InvalidOperationException>(() => result.ValidateInvariants());
    }

    [Fact]
    public void Result_Negative_Total_Size_Is_Rejected()
    {
        var result = new NascaOutputValidationResult
        {
            Outcome = NascaOutputValidationOutcome.Empty,
            SanitizedReasonCode = "EMPTY",
            ValidatedFileCount = 0,
            ValidatedTotalSizeBytes = -1,
            ValidationStartedUtc = DateTime.UtcNow,
            ValidationCompletedUtc = DateTime.UtcNow
        };

        Assert.Throws<InvalidOperationException>(() => result.ValidateInvariants());
    }

    [Fact]
    public void Result_Completed_Before_Started_Is_Rejected()
    {
        var start = DateTime.UtcNow;
        var result = new NascaOutputValidationResult
        {
            Outcome = NascaOutputValidationOutcome.Unstable,
            SanitizedReasonCode = "UNSTABLE",
            ValidationStartedUtc = start,
            ValidationCompletedUtc = start.AddSeconds(-1)
        };

        Assert.Throws<InvalidOperationException>(() => result.ValidateInvariants());
    }

    [Fact]
    public void Valid_Cannot_Require_Quarantine()
    {
        var result = new NascaOutputValidationResult
        {
            Outcome = NascaOutputValidationOutcome.Valid,
            SanitizedReasonCode = "VALIDATED"
        };

        result.ValidateInvariants();
        Assert.False(result.RequiresQuarantine);
    }

    [Fact]
    public void Valid_Cannot_Be_Retryable()
    {
        var result = new NascaOutputValidationResult
        {
            Outcome = NascaOutputValidationOutcome.Valid,
            SanitizedReasonCode = "VALIDATED"
        };

        result.ValidateInvariants();
        Assert.False(result.IsRetryable);
    }

    [Theory]
    [InlineData(NascaOutputValidationOutcome.Missing)]
    [InlineData(NascaOutputValidationOutcome.Empty)]
    [InlineData(NascaOutputValidationOutcome.Unstable)]
    [InlineData(NascaOutputValidationOutcome.OutsideApprovedRoot)]
    [InlineData(NascaOutputValidationOutcome.TraversalDetected)]
    [InlineData(NascaOutputValidationOutcome.ReparsePointDetected)]
    [InlineData(NascaOutputValidationOutcome.UnexpectedDirectory)]
    [InlineData(NascaOutputValidationOutcome.MaximumDepthExceeded)]
    [InlineData(NascaOutputValidationOutcome.TooManyFiles)]
    [InlineData(NascaOutputValidationOutcome.SingleFileSizeExceeded)]
    [InlineData(NascaOutputValidationOutcome.TotalSizeExceeded)]
    [InlineData(NascaOutputValidationOutcome.CorrelationMismatch)]
    [InlineData(NascaOutputValidationOutcome.DuplicateOutput)]
    [InlineData(NascaOutputValidationOutcome.Cancelled)]
    [InlineData(NascaOutputValidationOutcome.ValidationTimedOut)]
    [InlineData(NascaOutputValidationOutcome.AccessDenied)]
    [InlineData(NascaOutputValidationOutcome.QuarantineRequired)]
    [InlineData(NascaOutputValidationOutcome.UnknownFailure)]
    public void Invalid_Outcomes_Cannot_Report_IsValid(NascaOutputValidationOutcome outcome)
    {
        var result = new NascaOutputValidationResult
        {
            Outcome = outcome,
            SanitizedReasonCode = "NON_VALID"
        };

        result.ValidateInvariants();
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Request_Required_Identities_Cannot_Be_Empty()
    {
        var request = new NascaOutputValidationRequest
        {
            CorrelationId = "",
            ExecutionId = "",
            AttemptNumber = 1,
            WorkDirectoryId = "",
            OutputRoot = "",
            AgentWorkspaceId = "",
            Options = new NascaOutputValidationOptions()
        };

        Assert.Throws<InvalidOperationException>(() => request.ValidateInvariants());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Request_AttemptNumber_Must_Be_Greater_Than_Or_Equal_To_1(int attemptNumber)
    {
        var request = new NascaOutputValidationRequest
        {
            CorrelationId = "corr",
            ExecutionId = "exec",
            AttemptNumber = attemptNumber,
            WorkDirectoryId = "work_123",
            OutputRoot = "C:\\safe\\output",
            AgentWorkspaceId = "agent_workspace",
            Options = new NascaOutputValidationOptions()
        };

        Assert.Throws<InvalidOperationException>(() => request.ValidateInvariants());
    }

    [Fact]
    public void Result_Has_No_Raw_Exception_Property()
    {
        var exceptionProperty = typeof(NascaOutputValidationResult)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType == typeof(Exception));

        Assert.Null(exceptionProperty);
    }

    [Fact]
    public void Contracts_Contain_No_Vendor_Specific_Assumptions_In_Type_Names()
    {
        static bool containsVendorSpecificTerm(string text)
        {
            var lowered = text.ToLowerInvariant();
            return lowered.Contains("nascaexcel") ||
                   lowered.Contains(".xlsx") ||
                   lowered.Contains(".xls") ||
                   lowered.Contains("workbookschema") ||
                   lowered.Contains("vendorschema");
        }

        var contractTypes = new[]
        {
            typeof(INascaOutputValidator),
            typeof(NascaOutputValidationRequest),
            typeof(NascaOutputValidationResult),
            typeof(NascaOutputValidationOutcome),
            typeof(NascaValidatedFileDescriptor),
            typeof(NascaOutputValidationOptions)
        };

        foreach (var type in contractTypes)
        {
            Assert.False(containsVendorSpecificTerm(type.Name));
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.False(containsVendorSpecificTerm(prop.Name));
            }
        }
    }
}

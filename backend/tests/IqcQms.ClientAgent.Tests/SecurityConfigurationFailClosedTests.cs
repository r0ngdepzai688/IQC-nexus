using IqcQms.ClientAgent.Application.Config;
using IqcQms.Infrastructure.Config;
using IqcQms.Infrastructure.Security;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class SecurityConfigurationFailClosedTests
{
    [Fact]
    public void ProductionMode_MissingPairingPepper_FailsFast()
    {
        var options = new AgentSecurityOptions { PairingPepper = "" };
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("PairingPepper MUST be explicitly configured", ex.Message);
    }

    [Fact]
    public void ProductionMode_WeakPairingPepper_FailsFast()
    {
        var options = new AgentSecurityOptions { PairingPepper = "short" };
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("at least 16 characters", ex.Message);
    }

    [Fact]
    public void ProductionMode_MissingEnvelopeKey_FailsFast()
    {
        var options = new AgentSecurityOptions
        {
            PairingPepper = "ValidProductionPepper12345",
            EnvelopeEncryptionKey = ""
        };
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("EnvelopeEncryptionKey MUST be explicitly configured", ex.Message);
    }

    [Fact]
    public void ProductionMode_MalformedEnvelopeKey_FailsFast()
    {
        var options = new AgentSecurityOptions
        {
            PairingPepper = "ValidProductionPepper12345",
            EnvelopeEncryptionKey = "INVALID_NOT_HEX_OR_BASE64!!!"
        };
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("encoding is malformed", ex.Message);
    }

    [Fact]
    public void ProductionMode_InvalidPayloadRetentionOptions_FailsFast()
    {
        var invalidFull = new AgentPayloadRetentionOptions { FullResultRetentionDays = -1 };
        Assert.Throws<InvalidOperationException>(() => invalidFull.Validate());

        var invalidTombstone = new AgentPayloadRetentionOptions
        {
            FullResultRetentionDays = 90,
            ReplayTombstoneRetentionDays = 60 // Invalid: must be > 90
        };
        var ex = Assert.Throws<InvalidOperationException>(() => invalidTombstone.Validate());
        Assert.Contains("ReplayTombstoneRetentionDays must be strictly greater than FullResultRetentionDays", ex.Message);
    }

    [Fact]
    public void ProductionMode_InvalidAllowedInputRoots_FailsFast()
    {
        var options = new AgentOptions { AllowedInputRoots = new List<string>() };
        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("AllowedInputRoots MUST be non-empty", ex.Message);
    }

    [Fact]
    public void ProductionMode_InaccessibleConfiguredRoot_FailsFast()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid():N}");
        var options = new AgentOptions
        {
            AllowedInputRoots = new List<string> { nonExistentPath }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("does not exist or is inaccessible", ex.Message);
    }

    [Fact]
    public void ProductionMode_MalformedServerBaseUrl_FailsFast()
    {
        var optionsHttp = new AgentOptions { ServerBaseUrl = "http://unsecure-server.com" };
        var exHttp = Assert.Throws<InvalidOperationException>(() => optionsHttp.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("MUST use HTTPS", exHttp.Message);

        var optionsMalformed = new AgentOptions { ServerBaseUrl = "not-a-valid-url" };
        var exMalformed = Assert.Throws<InvalidOperationException>(() => optionsMalformed.Validate(isDevelopmentOrTesting: false));
        Assert.Contains("must be a valid absolute URI", exMalformed.Message);
    }
}

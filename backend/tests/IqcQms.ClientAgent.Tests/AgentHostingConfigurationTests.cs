using IqcQms.ClientAgent.Application.Config;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class AgentHostingConfigurationTests
{
    [Fact]
    public void DefaultOptions_HasWindowsServiceDisabledByDefault()
    {
        var options = new AgentOptions();
        Assert.False(options.EnableCompatibilityWindowsService);
        Assert.Equal("default", options.AgentProfile);
    }

    [Fact]
    public void ProductionHttpsValidation_FailsFastOnHttpUrl()
    {
        var options = new AgentOptions
        {
            ServerBaseUrl = "http://production.nexus.company.internal/api"
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: false));
    }

    [Fact]
    public void ProfileNameTraversal_FailsFastOnStartupValidation()
    {
        var options = new AgentOptions
        {
            AgentProfile = "../bad_profile"
        };

        Assert.Throws<InvalidOperationException>(() => options.Validate(isDevelopmentOrTesting: true));
    }
}

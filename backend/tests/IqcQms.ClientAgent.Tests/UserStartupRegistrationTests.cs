using IqcQms.ClientAgent.Application.Startup;
using IqcQms.ClientAgent.Infrastructure.Startup;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class UserStartupRegistrationTests
{
    [Fact]
    public void DescribeRegistrationInfo_SafelyQuotesExecutablePathAndProfile()
    {
        var registration = new InMemoryUserStartupRegistration();
        var exePath = @"C:\Program Files\IQC Nexus\ClientAgent\IqcQms.ClientAgent.exe";
        var info = registration.DescribeRegistrationInfo("Line-01", exePath);

        Assert.Equal("line-01", info.ProfileName);
        Assert.Equal("IqcQmsClientAgent_line-01", info.KeyName);
        Assert.Equal($"\"{Path.GetFullPath(exePath)}\" --profile \"line-01\"", info.QuotedCommandLine);
    }

    [Theory]
    [InlineData(@"C:\Path\exe.exe & calc.exe")]
    [InlineData(@"C:\Path\exe.exe | powershell")]
    [InlineData(@"C:\Path\exe.exe ; notepad")]
    [InlineData("C:\\Path\\exe.exe\nrm -rf /")]
    public void SanitizeAndQuotePath_RejectsCommandInjectionCharacters(string badPath)
    {
        Assert.Throws<ArgumentException>(() => WindowsHkcuRunStartupRegistration.SanitizeAndQuotePath(badPath));
    }

    [Fact]
    public async Task InMemoryRegistration_IdempotentEnableAndDisable()
    {
        var registration = new InMemoryUserStartupRegistration();
        var fakeExe = Path.Combine(Path.GetTempPath(), $"fake_agent_{Guid.NewGuid():N}.exe");
        await File.WriteAllTextAsync(fakeExe, "dummy");

        try
        {
            var status1 = registration.CheckStatus("default", fakeExe);
            Assert.Equal(UserStartupState.NotRegistered, status1);

            await registration.EnableAsync("default", fakeExe);
            var status2 = registration.CheckStatus("default", fakeExe);
            Assert.Equal(UserStartupState.Enabled, status2);

            await registration.DisableAsync("default");
            var status3 = registration.CheckStatus("default", fakeExe);
            Assert.Equal(UserStartupState.NotRegistered, status3);
        }
        finally
        {
            if (File.Exists(fakeExe)) File.Delete(fakeExe);
        }
    }
}

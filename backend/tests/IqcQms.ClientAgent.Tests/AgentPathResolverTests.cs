using IqcQms.ClientAgent.Infrastructure.Storage;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class AgentPathResolverTests : IDisposable
{
    private readonly string _tempBaseDir;

    public AgentPathResolverTests()
    {
        _tempBaseDir = Path.Combine(Path.GetTempPath(), $"path_resolver_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempBaseDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBaseDir))
        {
            try { Directory.Delete(_tempBaseDir, true); } catch { }
        }
    }

    [Fact]
    public void ValidProfileNames_AreNormalizedAndResolved()
    {
        var resolver1 = new AgentPathResolver("default", _tempBaseDir);
        Assert.Equal("default", resolver1.ProfileName);
        Assert.EndsWith("device_identity.dpapi", resolver1.IdentityFilePath);
        Assert.EndsWith("device_credentials.dpapi", resolver1.CredentialsFilePath);

        var resolver2 = new AgentPathResolver("Line-01_Test", _tempBaseDir);
        Assert.Equal("line-01_test", resolver2.ProfileName);
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("..\\outside")]
    [InlineData("profile/../../dir")]
    [InlineData("profile:name")]
    [InlineData("profile name with spaces")]
    [InlineData("profile@bad!")]
    public void InvalidProfileNames_ThrowArgumentException(string invalidProfile)
    {
        Assert.Throws<ArgumentException>(() => new AgentPathResolver(invalidProfile, _tempBaseDir));
    }

    [Fact]
    public void TwoProfiles_HaveIsolatedDirectories()
    {
        var p1 = new AgentPathResolver("profile_a", _tempBaseDir);
        var p2 = new AgentPathResolver("profile_b", _tempBaseDir);

        Assert.NotEqual(p1.RootDataDirectory, p2.RootDataDirectory);
        Assert.NotEqual(p1.QueueDatabasePath, p2.QueueDatabasePath);
    }

    [Fact]
    public void GetNormalizedProfilePath_RejectsTraversalSubpaths()
    {
        var resolver = new AgentPathResolver("default", _tempBaseDir);

        Assert.Throws<ArgumentException>(() => resolver.GetNormalizedProfilePath("../outside.txt"));
        Assert.Throws<ArgumentException>(() => resolver.GetNormalizedProfilePath("sub/../../outside.txt"));

        var validPath = resolver.GetNormalizedProfilePath("valid_subfile.txt");
        Assert.StartsWith(resolver.RootDataDirectory, validPath);
    }
}

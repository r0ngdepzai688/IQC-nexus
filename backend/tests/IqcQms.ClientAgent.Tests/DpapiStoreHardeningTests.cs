using IqcQms.ClientAgent.Infrastructure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class DpapiStoreHardeningTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _identityPath;
    private readonly string _credentialsPath;

    public DpapiStoreHardeningTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dpapi_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _identityPath = Path.Combine(_tempDir, "device_identity.dpapi");
        _credentialsPath = Path.Combine(_tempDir, "device_credentials.dpapi");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task MissingBlob_ProducesControlledUnpairedState()
    {
        var store = new WindowsDpapiDeviceIdentityStore(_identityPath, _credentialsPath, NullLogger<WindowsDpapiDeviceIdentityStore>.Instance);

        var creds = await store.LoadCredentialsAsync();
        Assert.Null(creds.AccessToken);
        Assert.Null(creds.RefreshToken);
    }

    [Fact]
    public async Task CorruptBlob_ReturnsControlledFailureWithoutCrashing()
    {
        await File.WriteAllBytesAsync(_credentialsPath, new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFF, 0xEE });
        var store = new WindowsDpapiDeviceIdentityStore(_identityPath, _credentialsPath, NullLogger<WindowsDpapiDeviceIdentityStore>.Instance);

        if (OperatingSystem.IsWindows())
        {
            // On Windows, corrupt DPAPI blob will fail to decrypt cleanly and return null credentials without throwing unhandled exceptions to caller
            var creds = await store.LoadCredentialsAsync();
            Assert.Null(creds.AccessToken);
            Assert.Null(creds.RefreshToken);
        }
    }

    [Fact]
    public async Task DpapiCurrentUser_Roundtrip_OnWindows()
    {
        if (!OperatingSystem.IsWindows()) return;

        var store = new WindowsDpapiDeviceIdentityStore(_identityPath, _credentialsPath, NullLogger<WindowsDpapiDeviceIdentityStore>.Instance);

        var identity = await store.GetOrCreateIdentityAsync();
        Assert.NotNull(identity);
        Assert.NotEmpty(identity.DeviceId);

        await store.SaveCredentialsAsync("acc_123", DateTime.UtcNow.AddMinutes(15), "ref_456", DateTime.UtcNow.AddDays(7));

        var loadedCreds = await store.LoadCredentialsAsync();
        Assert.Equal("acc_123", loadedCreds.AccessToken);
        Assert.Equal("ref_456", loadedCreds.RefreshToken);
    }
}

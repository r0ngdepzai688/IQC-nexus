using IqcQms.ClientAgent.Application.Identity;
using IqcQms.ClientAgent.Infrastructure.Identity;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class DeviceIdentityTests
{
    [Fact]
    public async Task GetOrCreateIdentity_GeneratesRandomGuidDeviceId_WithoutMachineFingerprinting()
    {
        var store = new InMemoryDeviceIdentityStore();
        var identity1 = await store.GetOrCreateIdentityAsync();

        Assert.NotNull(identity1);
        Assert.StartsWith("dev_test_", identity1.DeviceId);
        Assert.DoesNotContain(Environment.MachineName, identity1.DeviceId);
        Assert.DoesNotContain(Environment.UserName, identity1.DeviceId);

        // Subsequent call returns same identity
        var identity2 = await store.GetOrCreateIdentityAsync();
        Assert.Equal(identity1.DeviceId, identity2.DeviceId);
    }

    [Fact]
    public async Task ResetIdentity_ClearsIdentityAndCredentials()
    {
        var store = new InMemoryDeviceIdentityStore();
        var id1 = await store.GetOrCreateIdentityAsync();
        await store.SaveCredentialsAsync("token1", DateTime.UtcNow.AddHours(1), "refresh1", DateTime.UtcNow.AddDays(7));

        await store.ResetIdentityAsync();

        var id2 = await store.GetOrCreateIdentityAsync();
        Assert.NotEqual(id1.DeviceId, id2.DeviceId);

        var creds = await store.LoadCredentialsAsync();
        Assert.Null(creds.AccessToken);
        Assert.Null(creds.RefreshToken);
    }
}

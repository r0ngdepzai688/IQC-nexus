using IqcQms.ClientAgent.Infrastructure.Runtime;
using Xunit;

namespace IqcQms.ClientAgent.Tests;

public class SingleInstanceLockTests
{
    [Fact]
    public async Task InMemorySingleInstanceLock_FirstAcquires_SecondRejected()
    {
        using var lock1 = new InMemorySingleInstanceLock("profile_alpha");
        using var lock2 = new InMemorySingleInstanceLock("profile_alpha");

        var acquired1 = await lock1.TryAcquireAsync();
        Assert.True(acquired1);
        Assert.True(lock1.IsAcquired);

        var acquired2 = await lock2.TryAcquireAsync();
        Assert.False(acquired2);
        Assert.False(lock2.IsAcquired);

        lock1.Release();

        var acquired2Retry = await lock2.TryAcquireAsync();
        Assert.True(acquired2Retry);
        Assert.True(lock2.IsAcquired);
    }

    [Fact]
    public async Task InMemorySingleInstanceLock_SeparateProfilesDoNotCollide()
    {
        using var lockA = new InMemorySingleInstanceLock("profile_a");
        using var lockB = new InMemorySingleInstanceLock("profile_b");

        var acquiredA = await lockA.TryAcquireAsync();
        var acquiredB = await lockB.TryAcquireAsync();

        Assert.True(acquiredA);
        Assert.True(acquiredB);
    }
}

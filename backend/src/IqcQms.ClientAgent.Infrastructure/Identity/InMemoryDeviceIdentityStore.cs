using IqcQms.ClientAgent.Application.Identity;

namespace IqcQms.ClientAgent.Infrastructure.Identity;

public class InMemoryDeviceIdentityStore : IDeviceIdentityStore, ISecureCredentialStore
{
    private DeviceIdentity? _identity;
    private (string? AccessToken, DateTime AccessExpiry, string? RefreshToken, DateTime RefreshExpiry) _credentials;

    public Task<DeviceIdentity> GetOrCreateIdentityAsync(CancellationToken cancellationToken = default)
    {
        _identity ??= new DeviceIdentity
        {
            DeviceId = $"dev_test_{Guid.NewGuid():N}",
            DisplayName = "TestDevice",
            CreatedAtUtc = DateTime.UtcNow
        };
        return Task.FromResult(_identity);
    }

    public Task ResetIdentityAsync(CancellationToken cancellationToken = default)
    {
        _identity = null;
        _credentials = (null, DateTime.MinValue, null, DateTime.MinValue);
        return Task.CompletedTask;
    }

    public Task SaveCredentialsAsync(string accessToken, DateTime accessExpiry, string refreshToken, DateTime refreshExpiry, CancellationToken cancellationToken = default)
    {
        _credentials = (accessToken, accessExpiry, refreshToken, refreshExpiry);
        return Task.CompletedTask;
    }

    public Task<(string? AccessToken, DateTime AccessExpiry, string? RefreshToken, DateTime RefreshExpiry)> LoadCredentialsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_credentials);
    }

    public Task ClearCredentialsAsync(CancellationToken cancellationToken = default)
    {
        _credentials = (null, DateTime.MinValue, null, DateTime.MinValue);
        return Task.CompletedTask;
    }
}

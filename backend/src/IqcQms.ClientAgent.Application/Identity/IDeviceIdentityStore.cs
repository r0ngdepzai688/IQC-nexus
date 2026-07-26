namespace IqcQms.ClientAgent.Application.Identity;

public class DeviceIdentity
{
    public string DeviceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface IDeviceIdentityStore
{
    Task<DeviceIdentity> GetOrCreateIdentityAsync(CancellationToken cancellationToken = default);
    Task ResetIdentityAsync(CancellationToken cancellationToken = default);
}

public interface ISecureCredentialStore
{
    Task SaveCredentialsAsync(string accessToken, DateTime accessExpiry, string refreshToken, DateTime refreshExpiry, CancellationToken cancellationToken = default);
    Task<(string? AccessToken, DateTime AccessExpiry, string? RefreshToken, DateTime RefreshExpiry)> LoadCredentialsAsync(CancellationToken cancellationToken = default);
    Task ClearCredentialsAsync(CancellationToken cancellationToken = default);
}

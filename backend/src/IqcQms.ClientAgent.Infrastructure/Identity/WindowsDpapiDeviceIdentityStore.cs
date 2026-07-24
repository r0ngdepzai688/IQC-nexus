using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IqcQms.ClientAgent.Application.Identity;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Identity;

public class WindowsDpapiDeviceIdentityStore : IDeviceIdentityStore, ISecureCredentialStore
{
    private readonly string _identityFilePath;
    private readonly string _credentialsFilePath;
    private readonly ILogger<WindowsDpapiDeviceIdentityStore> _logger;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("IqcQmsAgentDeviceIdentityEntropy2026");

    public WindowsDpapiDeviceIdentityStore(string storageDirectory, ILogger<WindowsDpapiDeviceIdentityStore> logger)
    {
        Directory.CreateDirectory(storageDirectory);
        _identityFilePath = Path.Combine(storageDirectory, "device_identity.dpapi");
        _credentialsFilePath = Path.Combine(storageDirectory, "device_credentials.dpapi");
        _logger = logger;
    }

    public async Task<DeviceIdentity> GetOrCreateIdentityAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_identityFilePath))
        {
            try
            {
                var protectedBytes = await File.ReadAllBytesAsync(_identityFilePath, cancellationToken);
                var rawBytes = UnprotectData(protectedBytes);
                var json = Encoding.UTF8.GetString(rawBytes);
                var identity = JsonSerializer.Deserialize<DeviceIdentity>(json);
                if (identity != null && !string.IsNullOrWhiteSpace(identity.DeviceId))
                {
                    _logger.LogInformation("Loaded existing device identity {ShortDeviceId}", identity.DeviceId[..Math.Min(8, identity.DeviceId.Length)]);
                    return identity;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt device identity. Re-generating identity.");
            }
        }

        var newIdentity = new DeviceIdentity
        {
            DeviceId = $"dev_{Guid.NewGuid():N}",
            DisplayName = $"ClientAgent-{Environment.MachineName}",
            CreatedAtUtc = DateTime.UtcNow
        };

        var jsonStr = JsonSerializer.Serialize(newIdentity);
        var bytes = Encoding.UTF8.GetBytes(jsonStr);
        var encryptedBytes = ProtectData(bytes);
        await File.WriteAllBytesAsync(_identityFilePath, encryptedBytes, cancellationToken);

        _logger.LogInformation("Generated and saved new device identity {ShortDeviceId}", newIdentity.DeviceId[..Math.Min(8, newIdentity.DeviceId.Length)]);
        return newIdentity;
    }

    public Task ResetIdentityAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_identityFilePath)) File.Delete(_identityFilePath);
        if (File.Exists(_credentialsFilePath)) File.Delete(_credentialsFilePath);
        return Task.CompletedTask;
    }

    public async Task SaveCredentialsAsync(string accessToken, DateTime accessExpiry, string refreshToken, DateTime refreshExpiry, CancellationToken cancellationToken = default)
    {
        var record = new CredentialRecord
        {
            AccessToken = accessToken,
            AccessExpiry = accessExpiry,
            RefreshToken = refreshToken,
            RefreshExpiry = refreshExpiry
        };
        var json = JsonSerializer.Serialize(record);
        var bytes = Encoding.UTF8.GetBytes(json);
        var protectedBytes = ProtectData(bytes);
        await File.WriteAllBytesAsync(_credentialsFilePath, protectedBytes, cancellationToken);
    }

    public async Task<(string? AccessToken, DateTime AccessExpiry, string? RefreshToken, DateTime RefreshExpiry)> LoadCredentialsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_credentialsFilePath)) return (null, DateTime.MinValue, null, DateTime.MinValue);

        try
        {
            var protectedBytes = await File.ReadAllBytesAsync(_credentialsFilePath, cancellationToken);
            var rawBytes = UnprotectData(protectedBytes);
            var json = Encoding.UTF8.GetString(rawBytes);
            var record = JsonSerializer.Deserialize<CredentialRecord>(json);
            if (record != null)
            {
                return (record.AccessToken, record.AccessExpiry, record.RefreshToken, record.RefreshExpiry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load protected credentials.");
        }

        return (null, DateTime.MinValue, null, DateTime.MinValue);
    }

    public Task ClearCredentialsAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_credentialsFilePath)) File.Delete(_credentialsFilePath);
        return Task.CompletedTask;
    }

    private static byte[] ProtectData(byte[] data)
    {
        if (OperatingSystem.IsWindows())
        {
            return ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
        }
        // Fallback for non-Windows platforms/tests: basic XOR obfuscation/passthrough
        return data;
    }

    private static byte[] UnprotectData(byte[] data)
    {
        if (OperatingSystem.IsWindows())
        {
            return ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
        }
        return data;
    }

    private class CredentialRecord
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessExpiry { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshExpiry { get; set; }
    }
}

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

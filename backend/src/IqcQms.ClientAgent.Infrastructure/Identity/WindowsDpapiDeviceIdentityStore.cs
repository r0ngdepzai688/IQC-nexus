using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IqcQms.ClientAgent.Application.Identity;
using IqcQms.ClientAgent.Application.Storage;
using Microsoft.Extensions.Logging;

namespace IqcQms.ClientAgent.Infrastructure.Identity;

public class WindowsDpapiDeviceIdentityStore : IDeviceIdentityStore, ISecureCredentialStore
{
    private readonly string _identityFilePath;
    private readonly string _credentialsFilePath;
    private readonly ILogger<WindowsDpapiDeviceIdentityStore> _logger;
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("IqcQmsAgentDeviceIdentityEntropy2026");

    public WindowsDpapiDeviceIdentityStore(IAgentPathResolver pathResolver, ILogger<WindowsDpapiDeviceIdentityStore> logger)
    {
        _identityFilePath = pathResolver.IdentityFilePath;
        _credentialsFilePath = pathResolver.CredentialsFilePath;
        _logger = logger;
    }

    public WindowsDpapiDeviceIdentityStore(string identityFilePath, string credentialsFilePath, ILogger<WindowsDpapiDeviceIdentityStore> logger)
    {
        _identityFilePath = identityFilePath;
        _credentialsFilePath = credentialsFilePath;
        _logger = logger;
    }

    public async Task<DeviceIdentity> GetOrCreateIdentityAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(_identityFilePath))
        {
            try
            {
                var protectedBytes = await File.ReadAllBytesAsync(_identityFilePath, cancellationToken);
                if (protectedBytes.Length == 0)
                {
                    _logger.LogWarning("Identity file is empty. Creating new device identity.");
                }
                else
                {
                    var rawBytes = UnprotectData(protectedBytes);
                    var json = Encoding.UTF8.GetString(rawBytes);
                    var identity = JsonSerializer.Deserialize<DeviceIdentity>(json);
                    if (identity != null && !string.IsNullOrWhiteSpace(identity.DeviceId))
                    {
                        _logger.LogInformation("Loaded device identity {ShortDeviceId}", identity.DeviceId[..Math.Min(8, identity.DeviceId.Length)]);
                        return identity;
                    }
                }
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Failed to decrypt device identity protected blob. Blob corrupt or scope mismatch. Re-generating identity.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading identity store file. Re-generating identity.");
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
        var protectedBytesToSave = ProtectData(bytes);

        await SaveFileAtomicallyAsync(_identityFilePath, protectedBytesToSave, cancellationToken);

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

        await SaveFileAtomicallyAsync(_credentialsFilePath, protectedBytes, cancellationToken);
        _logger.LogInformation("Successfully saved DPAPI protected session credentials.");
    }

    public async Task<(string? AccessToken, DateTime AccessExpiry, string? RefreshToken, DateTime RefreshExpiry)> LoadCredentialsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_credentialsFilePath))
        {
            return (null, DateTime.MinValue, null, DateTime.MinValue);
        }

        try
        {
            var protectedBytes = await File.ReadAllBytesAsync(_credentialsFilePath, cancellationToken);
            if (protectedBytes.Length == 0)
            {
                return (null, DateTime.MinValue, null, DateTime.MinValue);
            }

            var rawBytes = UnprotectData(protectedBytes);
            var json = Encoding.UTF8.GetString(rawBytes);
            var record = JsonSerializer.Deserialize<CredentialRecord>(json);
            if (record != null)
            {
                return (record.AccessToken, record.AccessExpiry, record.RefreshToken, record.RefreshExpiry);
            }
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to decrypt credential blob (CryptographicException). Credentials corrupt or scope mismatch.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading protected credential store file.");
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
            // Explicitly DataProtectionScope.CurrentUser (NO LocalMachine fallback)
            return ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
        }

        throw new PlatformNotSupportedException("Windows DPAPI CurrentUser encryption is only supported on Windows operating systems.");
    }

    private static byte[] UnprotectData(byte[] data)
    {
        if (OperatingSystem.IsWindows())
        {
            // Explicitly DataProtectionScope.CurrentUser (NO LocalMachine fallback)
            return ProtectedData.Unprotect(data, Entropy, DataProtectionScope.CurrentUser);
        }

        throw new PlatformNotSupportedException("Windows DPAPI CurrentUser decryption is only supported on Windows operating systems.");
    }

    private static async Task SaveFileAtomicallyAsync(string filePath, byte[] data, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

        var tempPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await fs.WriteAsync(data, cancellationToken);
                await fs.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            throw;
        }
    }

    private class CredentialRecord
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessExpiry { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshExpiry { get; set; }
    }
}

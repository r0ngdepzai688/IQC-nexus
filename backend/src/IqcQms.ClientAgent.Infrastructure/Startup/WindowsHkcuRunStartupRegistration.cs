using System.Runtime.Versioning;
using IqcQms.ClientAgent.Application.Startup;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace IqcQms.ClientAgent.Infrastructure.Startup;

public class WindowsHkcuRunStartupRegistration : IUserStartupRegistration
{
    private const string HkcuRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private readonly ILogger<WindowsHkcuRunStartupRegistration> _logger;

    public WindowsHkcuRunStartupRegistration(ILogger<WindowsHkcuRunStartupRegistration> logger)
    {
        _logger = logger;
    }

    public UserStartupRegistrationInfo DescribeRegistrationInfo(string profileName, string executablePath)
    {
        var cleanProfile = SanitizeProfileName(profileName);
        var cleanExe = SanitizeAndQuotePath(executablePath);

        var keyName = $"IqcQmsClientAgent_{cleanProfile}";
        var quotedCmd = $"{cleanExe} --profile \"{cleanProfile}\"";

        var state = CheckStatus(cleanProfile, executablePath);

        return new UserStartupRegistrationInfo
        {
            ProfileName = cleanProfile,
            KeyName = keyName,
            ExecutablePath = executablePath,
            QuotedCommandLine = quotedCmd,
            State = state
        };
    }

    public UserStartupState CheckStatus(string profileName, string executablePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return UserStartupState.NotRegistered;
        }

        var cleanProfile = SanitizeProfileName(profileName);
        var keyName = $"IqcQmsClientAgent_{cleanProfile}";

        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(HkcuRunKeyPath, writable: false);
            var val = runKey?.GetValue(keyName) as string;

            if (string.IsNullOrWhiteSpace(val))
            {
                return UserStartupState.NotRegistered;
            }

            if (!File.Exists(executablePath))
            {
                return UserStartupState.StaleExecutablePath;
            }

            return UserStartupState.Enabled;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading HKCU Run startup registry key for profile '{Profile}'", cleanProfile);
            return UserStartupState.NotRegistered;
        }
    }

    public Task EnableAsync(string profileName, string executablePath, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Per-user HKCU Run startup registration is supported on Windows only.");
            return Task.CompletedTask;
        }

        var info = DescribeRegistrationInfo(profileName, executablePath);

        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(HkcuRunKeyPath, writable: true);
            if (runKey == null)
            {
                throw new InvalidOperationException($"Failed to open HKCU registry key '{HkcuRunKeyPath}' for writing.");
            }

            runKey.SetValue(info.KeyName, info.QuotedCommandLine, RegistryValueKind.String);
            _logger.LogInformation("Successfully registered per-user startup entry '{KeyName}' -> {CommandLine}", info.KeyName, info.QuotedCommandLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register HKCU Run startup entry for profile '{Profile}'", info.ProfileName);
            throw;
        }

        return Task.CompletedTask;
    }

    public Task DisableAsync(string profileName, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        var cleanProfile = SanitizeProfileName(profileName);
        var keyName = $"IqcQmsClientAgent_{cleanProfile}";

        try
        {
            using var runKey = Registry.CurrentUser.OpenSubKey(HkcuRunKeyPath, writable: true);
            if (runKey?.GetValue(keyName) != null)
            {
                runKey.DeleteValue(keyName, false);
                _logger.LogInformation("Disabled per-user startup entry '{KeyName}'", keyName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove HKCU Run startup entry for profile '{Profile}'", cleanProfile);
            throw;
        }

        return Task.CompletedTask;
    }

    public static string SanitizeProfileName(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName)) return "default";
        var clean = profileName.Trim().ToLowerInvariant();
        if (clean.Contains("..") || clean.Contains('/') || clean.Contains('\\') || clean.Contains('"'))
        {
            throw new ArgumentException($"Invalid profile name '{profileName}': illegal characters detected.", nameof(profileName));
        }
        return clean;
    }

    public static string SanitizeAndQuotePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Executable path cannot be empty.", nameof(path));
        }

        if (path.Contains('\n') || path.Contains('\r') || path.Contains('&') || path.Contains('|') || path.Contains(';'))
        {
            throw new ArgumentException($"Executable path '{path}' contains dangerous command injection characters.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);
        return $"\"{fullPath}\"";
    }
}

public class InMemoryUserStartupRegistration : IUserStartupRegistration
{
    private readonly Dictionary<string, (string ExePath, string QuotedCmd)> _registry = new(StringComparer.OrdinalIgnoreCase);

    public UserStartupRegistrationInfo DescribeRegistrationInfo(string profileName, string executablePath)
    {
        var cleanProfile = WindowsHkcuRunStartupRegistration.SanitizeProfileName(profileName);
        var cleanExe = WindowsHkcuRunStartupRegistration.SanitizeAndQuotePath(executablePath);
        var keyName = $"IqcQmsClientAgent_{cleanProfile}";
        var quotedCmd = $"{cleanExe} --profile \"{cleanProfile}\"";
        var state = CheckStatus(cleanProfile, executablePath);

        return new UserStartupRegistrationInfo
        {
            ProfileName = cleanProfile,
            KeyName = keyName,
            ExecutablePath = executablePath,
            QuotedCommandLine = quotedCmd,
            State = state
        };
    }

    public UserStartupState CheckStatus(string profileName, string executablePath)
    {
        var cleanProfile = WindowsHkcuRunStartupRegistration.SanitizeProfileName(profileName);
        if (_registry.TryGetValue(cleanProfile, out var reg))
        {
            if (!File.Exists(executablePath))
            {
                return UserStartupState.StaleExecutablePath;
            }
            return UserStartupState.Enabled;
        }
        return UserStartupState.NotRegistered;
    }

    public Task EnableAsync(string profileName, string executablePath, CancellationToken cancellationToken = default)
    {
        var info = DescribeRegistrationInfo(profileName, executablePath);
        _registry[info.ProfileName] = (executablePath, info.QuotedCommandLine);
        return Task.CompletedTask;
    }

    public Task DisableAsync(string profileName, CancellationToken cancellationToken = default)
    {
        var cleanProfile = WindowsHkcuRunStartupRegistration.SanitizeProfileName(profileName);
        _registry.Remove(cleanProfile);
        return Task.CompletedTask;
    }
}

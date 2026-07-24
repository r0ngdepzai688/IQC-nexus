using System.Text.RegularExpressions;
using IqcQms.ClientAgent.Application.Storage;

namespace IqcQms.ClientAgent.Infrastructure.Storage;

public class AgentPathResolver : IAgentPathResolver
{
    private static readonly Regex ValidProfileRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);

    public string ProfileName { get; }
    public string RootDataDirectory { get; }
    public string IdentityFilePath { get; }
    public string CredentialsFilePath { get; }
    public string QueueDatabasePath { get; }
    public string LogsDirectory { get; }
    public string LockFilePath { get; }

    public AgentPathResolver(string? profileName = null, string? customBaseRoot = null)
    {
        ProfileName = NormalizeAndValidateProfileName(profileName);
        RootDataDirectory = ResolveRootDataDirectory(ProfileName, customBaseRoot);

        Directory.CreateDirectory(RootDataDirectory);

        IdentityFilePath = GetNormalizedProfilePath("device_identity.dpapi");
        CredentialsFilePath = GetNormalizedProfilePath("device_credentials.dpapi");
        QueueDatabasePath = GetNormalizedProfilePath("agent_queue.db");
        LogsDirectory = GetNormalizedProfilePath("logs");
        LockFilePath = GetNormalizedProfilePath("agent_runtime.lock");

        Directory.CreateDirectory(LogsDirectory);
    }

    public string GetNormalizedProfilePath(string subPath)
    {
        if (string.IsNullOrWhiteSpace(subPath))
        {
            throw new ArgumentException("Subpath cannot be empty.", nameof(subPath));
        }

        // Detect traversal characters in subPath
        if (subPath.Contains("..") || subPath.Contains('/') || subPath.Contains('\\'))
        {
            var fileNameOnly = Path.GetFileName(subPath);
            if (string.IsNullOrWhiteSpace(fileNameOnly) || fileNameOnly != subPath)
            {
                throw new ArgumentException($"Subpath '{subPath}' contains invalid path traversal characters.", nameof(subPath));
            }
        }

        var fullPath = Path.GetFullPath(Path.Combine(RootDataDirectory, subPath));
        if (!fullPath.StartsWith(RootDataDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path traversal detected: '{fullPath}' is outside root data directory.");
        }

        return fullPath;
    }

    public static string NormalizeAndValidateProfileName(string? profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return "default";
        }

        var trimmed = profileName.Trim();
        if (trimmed.Contains("..") || trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains(':'))
        {
            throw new ArgumentException($"Profile name '{profileName}' contains path traversal or illegal characters.", nameof(profileName));
        }

        if (!ValidProfileRegex.IsMatch(trimmed))
        {
            throw new ArgumentException($"Profile name '{profileName}' contains invalid characters. Only alphanumeric, hyphens, and underscores are allowed.", nameof(profileName));
        }

        return trimmed.ToLowerInvariant();
    }

    private static string ResolveRootDataDirectory(string profileName, string? customBaseRoot)
    {
        string baseDir;
        if (!string.IsNullOrWhiteSpace(customBaseRoot))
        {
            baseDir = Path.GetFullPath(customBaseRoot);
        }
        else
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                localAppData = Path.Combine(userProfile, ".iqc-nexus");
            }
            baseDir = Path.Combine(localAppData, "IQC Nexus", "ClientAgent");
        }

        var profileDir = Path.GetFullPath(Path.Combine(baseDir, profileName));
        return profileDir;
    }
}

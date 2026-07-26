using IqcQms.ClientAgent.Application.Storage;

namespace IqcQms.ClientAgent.Application.Config;

public class AgentOptions
{
    public string AgentProfile { get; set; } = "default";
    public string ServerBaseUrl { get; set; } = "https://localhost:7142";
    public string? PairingCode { get; set; }
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public string LocalStorePath { get; set; } = "";
    public List<string> AllowedInputRoots { get; set; } = new() { "./synthetic_inputs" };
    public string ProtocolVersion { get; set; } = "1.0";
    public long MaxNormalizedPayloadBytes { get; set; } = 10 * 1024 * 1024;
    public bool EnableCompatibilityWindowsService { get; set; } = false;

    public void Validate(bool isDevelopmentOrTesting = false)
    {
        if (string.IsNullOrWhiteSpace(ServerBaseUrl) || !Uri.TryCreate(ServerBaseUrl, UriKind.Absolute, out var parsedUri))
        {
            throw new InvalidOperationException("AgentOptions:ServerBaseUrl must be a valid absolute URI.");
        }

        if (!isDevelopmentOrTesting && !string.Equals(parsedUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("AgentOptions:ServerBaseUrl MUST use HTTPS in non-development environment.");
        }

        if (string.IsNullOrWhiteSpace(AgentProfile))
        {
            AgentProfile = "default";
        }

        var trimmedProfile = AgentProfile.Trim();
        if (trimmedProfile.Contains("..") || trimmedProfile.Contains('/') || trimmedProfile.Contains('\\'))
        {
            throw new InvalidOperationException($"AgentOptions:AgentProfile '{AgentProfile}' contains invalid path traversal characters.");
        }

        if (HeartbeatIntervalSeconds <= 0)
        {
            HeartbeatIntervalSeconds = 30;
        }

        if (!isDevelopmentOrTesting)
        {
            if (AllowedInputRoots == null || AllowedInputRoots.Count == 0)
            {
                throw new InvalidOperationException("AgentOptions:AllowedInputRoots MUST be non-empty in production.");
            }
        }

        var normalizedRoots = new List<string>();
        if (AllowedInputRoots != null)
        {
            foreach (var root in AllowedInputRoots)
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    if (!isDevelopmentOrTesting)
                        throw new InvalidOperationException("AgentOptions:AllowedInputRoots contains blank or empty entry.");
                    continue;
                }

                if (!isDevelopmentOrTesting)
                {
                    if (!Path.IsPathRooted(root))
                    {
                        throw new InvalidOperationException($"Configured AllowedInputRoot '{root}' must be an absolute path in production.");
                    }

                    var rootPath = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var rootDirectoryInfo = new DirectoryInfo(rootPath);
                    if (rootDirectoryInfo.Parent == null || rootPath.EndsWith(":") || rootPath.Length <= 3)
                    {
                        throw new InvalidOperationException($"Root drive '{root}' is not permitted as AllowedInputRoot in production.");
                    }

                    if (!Directory.Exists(rootPath))
                    {
                        throw new InvalidOperationException($"Configured AllowedInputRoot '{rootPath}' does not exist or is inaccessible.");
                    }
                }

                var full = Path.GetFullPath(root);
                normalizedRoots.Add(full);
            }
        }
        AllowedInputRoots = normalizedRoots;
    }

    public bool IsPathAllowed(string filePath, IAllowedInputPathValidator? validator = null)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        if (validator != null)
        {
            return validator.ValidatePath(filePath, AllowedInputRoots).IsAllowed;
        }

        try
        {
            var fullCandidate = Path.GetFullPath(filePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var r in AllowedInputRoots)
            {
                var fullRoot = Path.GetFullPath(r).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (fullCandidate.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) ||
                    fullCandidate.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}

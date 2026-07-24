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
        if (string.IsNullOrWhiteSpace(ServerBaseUrl))
        {
            throw new InvalidOperationException("AgentOptions:ServerBaseUrl is required.");
        }

        if (!isDevelopmentOrTesting && !ServerBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
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

        // Normalize allowed roots
        var normalizedRoots = new List<string>();
        foreach (var root in AllowedInputRoots)
        {
            if (!string.IsNullOrWhiteSpace(root))
            {
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

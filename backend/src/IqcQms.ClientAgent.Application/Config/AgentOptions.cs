namespace IqcQms.ClientAgent.Application.Config;

public class AgentOptions
{
    public string ServerBaseUrl { get; set; } = "https://localhost:7142";
    public string? PairingCode { get; set; }
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public string LocalStorePath { get; set; } = "./data";
    public List<string> AllowedInputRoots { get; set; } = new() { "./data", "./synthetic_inputs" };
    public string ProtocolVersion { get; set; } = "1.0";
    public long MaxNormalizedPayloadBytes { get; set; } = 10 * 1024 * 1024;

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

        if (HeartbeatIntervalSeconds <= 0)
        {
            HeartbeatIntervalSeconds = 30;
        }

        if (string.IsNullOrWhiteSpace(LocalStorePath))
        {
            throw new InvalidOperationException("AgentOptions:LocalStorePath is required.");
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

    public bool IsPathAllowed(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        try
        {
            var fullPath = Path.GetFullPath(filePath);
            return AllowedInputRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}

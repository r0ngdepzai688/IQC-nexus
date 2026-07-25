namespace IqcQms.ClientAgent.Application.Config;

public class NascaOptions
{
    public bool Enabled { get; set; } = false;
    public string ExecutablePath { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string InputDirectory { get; set; } = string.Empty;
    public string OutputDirectory { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaximumConcurrentJobs { get; set; } = 1;

    // Phase 3A.1 Identity & Verification Fields
    public string ExpectedProductName { get; set; } = string.Empty;
    public string ExpectedPublisher { get; set; } = string.Empty;
    public List<string> AllowedProductVersions { get; set; } = new();
    public bool RequireAuthenticodeSignature { get; set; } = false;

    public void Validate(bool isProduction = false)
    {
        if (!Enabled)
        {
            // When disabled, no executable or path validation is required
            return;
        }

        if (TimeoutSeconds <= 0 || TimeoutSeconds > 600)
        {
            throw new InvalidOperationException("NascaOptions:TimeoutSeconds must be positive and bounded between 1 and 600 seconds.");
        }

        if (MaximumConcurrentJobs <= 0 || MaximumConcurrentJobs > 10)
        {
            throw new InvalidOperationException("NascaOptions:MaximumConcurrentJobs must be positive and bounded between 1 and 10.");
        }

        if (isProduction)
        {
            if (string.IsNullOrWhiteSpace(ExecutablePath) || !Path.IsPathRooted(ExecutablePath))
            {
                throw new InvalidOperationException("NascaOptions:ExecutablePath MUST be an absolute path when enabled in production.");
            }

            if (!File.Exists(ExecutablePath))
            {
                throw new InvalidOperationException($"NascaOptions:ExecutablePath '{ExecutablePath}' does not exist on target host.");
            }

            var exeDir = Path.GetDirectoryName(Path.GetFullPath(ExecutablePath)) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(InputDirectory))
            {
                var normInput = Path.GetFullPath(InputDirectory);
                if (exeDir.StartsWith(normInput, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("NascaOptions:ExecutablePath cannot be located inside a writable input directory.");
                }
            }

            if (string.IsNullOrWhiteSpace(OutputDirectory) || !Path.IsPathRooted(OutputDirectory))
            {
                throw new InvalidOperationException("NascaOptions:OutputDirectory MUST be an absolute path when enabled in production.");
            }
        }
    }
}

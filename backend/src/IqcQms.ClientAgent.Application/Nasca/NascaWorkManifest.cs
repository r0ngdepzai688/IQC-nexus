namespace IqcQms.ClientAgent.Application.Nasca;

public enum NascaWorkLifecycle
{
    Active = 0,
    RecoveryRequired,
    Completed,
    Failed,
    Quarantined,
    Expired
}

public class NascaInputMetadata
{
    public string StagedFileName { get; set; } = "input.dat";
    public string OriginalHashSha256 { get; set; } = string.Empty;
    public string StagedHashSha256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public class NascaWorkManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string CorrelationId { get; set; } = string.Empty;
    public string WorkDirectoryId { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = string.Empty;
    public int AttemptNumber { get; set; } = 1;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public NascaWorkLifecycle CurrentLifecycle { get; set; } = NascaWorkLifecycle.Active;
    public NascaInputMetadata InputMetadata { get; set; } = new();
    public string RetentionCategory { get; set; } = "Standard";
}

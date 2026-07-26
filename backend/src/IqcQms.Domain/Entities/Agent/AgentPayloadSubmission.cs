namespace IqcQms.Domain.Entities.Agent;

public enum AgentPayloadSubmissionState
{
    Pending,
    Accepted,
    Completed,
    Rejected
}

public class AgentPayloadSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentDeviceId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string PayloadSubmissionId { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string SourceFingerprint { get; set; } = string.Empty;
    public string CanonicalPayloadHash { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = "1.0";
    public Guid ServerImportJobId { get; set; }
    public Guid UploadId { get; set; }
    public AgentPayloadSubmissionState State { get; set; } = AgentPayloadSubmissionState.Accepted;
    public int RecordCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int ConcurrencyVersion { get; set; } = 1;
}

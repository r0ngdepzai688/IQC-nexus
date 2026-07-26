namespace IqcQms.Domain.Entities.Agent;

public class AgentPayloadReplayTombstone
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentDeviceId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string PayloadSubmissionId { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string CanonicalPayloadHash { get; set; } = string.Empty;
    public DateTime AcceptedAtUtc { get; set; }
    public DateTime TombstoneExpiresAtUtc { get; set; }
}

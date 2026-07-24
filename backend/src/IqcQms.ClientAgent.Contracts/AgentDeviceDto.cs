namespace IqcQms.ClientAgent.Contracts;

public class AgentDeviceDto
{
    public Guid Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "1.0";
    public string State { get; set; } = "Active";
    public AgentCapabilitiesDto Capabilities { get; set; } = new();
    public DateTime PairedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

namespace IqcQms.Domain.Entities.Agent;

public enum AgentDeviceState
{
    Active = 1,
    Offline = 2,
    Revoked = 3,
    Incompatible = 4
}

public class AgentDevice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeviceId { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "1.0";
    public AgentDeviceState State { get; set; } = AgentDeviceState.Active;
    public string CapabilitiesJson { get; set; } = "{}";
    public DateTime PairedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public int ConcurrencyVersion { get; set; } = 1;

    public List<AgentCredential> Credentials { get; set; } = new();
}

namespace IqcQms.ClientAgent.Contracts;

public class AgentHeartbeatRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "1.0";
    public string Status { get; set; } = "Online";
    public AgentCapabilitiesDto Capabilities { get; set; } = new();
    public int SafeQueueCount { get; set; }
    public DateTime? LastCompletedJobUtc { get; set; }
    public string? LastErrorCode { get; set; }
}

public class AgentHeartbeatResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public DateTime AcknowledgedAtUtc { get; set; } = DateTime.UtcNow;
    public int NextHeartbeatIntervalSeconds { get; set; } = 30;
    public string State { get; set; } = "Active";
}

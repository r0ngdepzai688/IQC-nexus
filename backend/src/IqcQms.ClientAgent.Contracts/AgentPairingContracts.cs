namespace IqcQms.ClientAgent.Contracts;

public class AgentPairingCreateRequest
{
    public string? OwnerDisplayName { get; set; }
}

public class AgentPairingCreateResponse
{
    public string PairingCode { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int OwnerUserId { get; set; }
}

public class AgentCapabilitiesDto
{
    public bool SyntheticProviderSupported { get; set; } = true;
    public bool CsvProviderSupported { get; set; } = true;
    public bool NascaProviderSupported { get; set; } = false;
    public bool ExcelComSupported { get; set; } = false;
    public bool ExcelAutomationSupported { get; set; } = false;
    public List<string> SupportedSchemaVersions { get; set; } = new() { "1.0" };
    public long MaxPayloadBytes { get; set; } = 10 * 1024 * 1024;
}

public class AgentDevicePairRequest
{
    public string PairingCode { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "1.0";
    public AgentCapabilitiesDto Capabilities { get; set; } = new();
}

public class AgentDevicePairResponse
{
    public string DeviceId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshExpiresAtUtc { get; set; }
    public string AgentVersion { get; set; } = "1.0.0";
    public string ProtocolVersion { get; set; } = "1.0";
}

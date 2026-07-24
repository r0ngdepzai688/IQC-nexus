namespace IqcQms.Domain.Entities.Agent;

public class AgentCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentDeviceId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string CredentialIdentifier { get; set; } = string.Empty;
    public string ProtectedVerifierHash { get; set; } = string.Empty;
    public string TokenFamilyId { get; set; } = string.Empty;
    public string RefreshOperationId { get; set; } = string.Empty;
    public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string RotationLineage { get; set; } = string.Empty;
    public bool IsReplayed { get; set; }

    public AgentDevice? Device { get; set; }
}

namespace IqcQms.Domain.Entities.Agent;

public class AgentRefreshOperationResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AgentDeviceId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string TokenFamilyId { get; set; } = string.Empty;
    public string RefreshOperationId { get; set; } = string.Empty;
    public byte[] EncryptedPayload { get; set; } = Array.Empty<byte>();
    public byte[] Nonce { get; set; } = Array.Empty<byte>();
    public byte[] Tag { get; set; } = Array.Empty<byte>();
    public int KeyVersion { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
}

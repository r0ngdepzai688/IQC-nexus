namespace IqcQms.Domain.Entities.Agent;

public class AgentPairingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string HashedCode { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public int ConcurrencyVersion { get; set; } = 1;
}

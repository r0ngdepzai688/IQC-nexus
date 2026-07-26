namespace IqcQms.Domain.Entities.Agent;

public enum AgentPairingRequestState
{
    Pending = 1,
    Consumed = 2,
    Expired = 3,
    Locked = 4,
    Cancelled = 5
}

public class AgentPairingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string HashedCode { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public AgentPairingRequestState State { get; set; } = AgentPairingRequestState.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime? LockedAtUtc { get; set; }
    public int FailedAttemptCount { get; set; }
    public int MaxFailedAttempts { get; set; } = 5;
    public DateTime? LastFailedAttemptAtUtc { get; set; }
    public int ConcurrencyVersion { get; set; } = 1;
}

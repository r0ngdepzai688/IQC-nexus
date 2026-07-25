namespace IqcQms.Application.Services;

public interface IAgentPayloadRetentionService
{
    Task<int> CleanupExpiredSubmissionsAsync(CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredTombstonesAsync(CancellationToken cancellationToken = default);
}

namespace IqcQms.ClientAgent.Application.Nasca;

public class NascaExecutionStateRecord
{
    public string CorrelationId { get; set; } = string.Empty;
    public string QueueItemId { get; set; } = string.Empty;
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString("N");
    public int AttemptNumber { get; set; } = 1;
    public NascaExecutionState CurrentState { get; set; } = NascaExecutionState.Queued;
    public int SchemaVersion { get; set; } = 1;
    public string WorkDirectoryId { get; set; } = string.Empty;
    public string? SanitizedReasonCode { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public interface INascaExecutionStateStore
{
    Task<NascaExecutionStateRecord> CreateInitialStateAsync(string correlationId, string queueItemId, string workDirectoryId, CancellationToken cancellationToken = default);
    Task<NascaExecutionStateRecord?> GetStateAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<NascaExecutionStateRecord> TransitionStateAsync(string correlationId, NascaExecutionState expectedFrom, NascaExecutionState targetTo, string? sanitizedReasonCode = null, CancellationToken cancellationToken = default);
    Task<NascaExecutionStateRecord> RecordNewAttemptAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NascaExecutionStateRecord>> GetRecoverableExecutionsAsync(CancellationToken cancellationToken = default);
    Task<NascaExecutionStateRecord> MarkRecoveryRequiredAsync(string correlationId, string sanitizedReasonCode, CancellationToken cancellationToken = default);
}

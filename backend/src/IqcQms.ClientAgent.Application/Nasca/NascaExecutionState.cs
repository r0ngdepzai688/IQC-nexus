namespace IqcQms.ClientAgent.Application.Nasca;

public enum NascaExecutionState
{
    Queued = 0,
    InputValidated,
    InputStaged,
    ExecutionPending,
    ExecutionStarted,
    OutputPending,
    OutputValidated,
    Normalized,
    Submitting,
    Completed,
    RetryableFailure,
    PermanentFailure,
    RecoveryRequired,
    Cancelled
}

public static class NascaExecutionStateValidator
{
    private static readonly Dictionary<NascaExecutionState, HashSet<NascaExecutionState>> AllowedTransitions = new()
    {
        { NascaExecutionState.Queued, new() { NascaExecutionState.InputValidated, NascaExecutionState.Cancelled, NascaExecutionState.PermanentFailure } },
        { NascaExecutionState.InputValidated, new() { NascaExecutionState.InputStaged, NascaExecutionState.Cancelled, NascaExecutionState.PermanentFailure } },
        { NascaExecutionState.InputStaged, new() { NascaExecutionState.ExecutionPending, NascaExecutionState.Cancelled, NascaExecutionState.PermanentFailure } },
        { NascaExecutionState.ExecutionPending, new() { NascaExecutionState.ExecutionStarted, NascaExecutionState.Cancelled, NascaExecutionState.PermanentFailure } },
        { NascaExecutionState.ExecutionStarted, new() { NascaExecutionState.OutputPending, NascaExecutionState.RetryableFailure, NascaExecutionState.PermanentFailure, NascaExecutionState.RecoveryRequired, NascaExecutionState.Cancelled } },
        { NascaExecutionState.OutputPending, new() { NascaExecutionState.OutputValidated, NascaExecutionState.RetryableFailure, NascaExecutionState.PermanentFailure, NascaExecutionState.RecoveryRequired, NascaExecutionState.Cancelled } },
        { NascaExecutionState.OutputValidated, new() { NascaExecutionState.Normalized, NascaExecutionState.Submitting, NascaExecutionState.Cancelled, NascaExecutionState.PermanentFailure } },
        { NascaExecutionState.Normalized, new() { NascaExecutionState.Submitting, NascaExecutionState.Cancelled, NascaExecutionState.PermanentFailure } },
        { NascaExecutionState.Submitting, new() { NascaExecutionState.Completed, NascaExecutionState.RetryableFailure, NascaExecutionState.PermanentFailure, NascaExecutionState.Cancelled } },
        { NascaExecutionState.RetryableFailure, new() { NascaExecutionState.ExecutionPending, NascaExecutionState.PermanentFailure, NascaExecutionState.Cancelled } },
        { NascaExecutionState.RecoveryRequired, new() { NascaExecutionState.ExecutionPending, NascaExecutionState.Submitting, NascaExecutionState.PermanentFailure, NascaExecutionState.Cancelled } },
        { NascaExecutionState.Completed, new() },
        { NascaExecutionState.PermanentFailure, new() },
        { NascaExecutionState.Cancelled, new() }
    };

    public static bool CanTransition(NascaExecutionState from, NascaExecutionState to)
    {
        if (from == to) return true; // Idempotent same-state check
        return AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static void EnsureValidTransition(NascaExecutionState from, NascaExecutionState to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException($"Illegal state transition: Cannot transition from '{from}' to '{to}'.");
        }
    }
}

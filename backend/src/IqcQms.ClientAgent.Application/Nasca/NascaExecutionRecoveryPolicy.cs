namespace IqcQms.ClientAgent.Application.Nasca;

public enum RecoveryAction
{
    ResumeCurrentStep,
    MarkRecoveryRequired,
    SkipToSubmission,
    ReconcileIdempotently,
    NoAction,
    EligibleForRetry,
    TerminalNoRetry
}

public class NascaExecutionRecoveryPolicy
{
    public static RecoveryAction DetermineRestartAction(NascaExecutionState currentState)
    {
        return currentState switch
        {
            NascaExecutionState.Queued or
            NascaExecutionState.InputValidated or
            NascaExecutionState.InputStaged or
            NascaExecutionState.ExecutionPending => RecoveryAction.ResumeCurrentStep,

            NascaExecutionState.ExecutionStarted or
            NascaExecutionState.OutputPending => RecoveryAction.MarkRecoveryRequired,

            NascaExecutionState.OutputValidated or
            NascaExecutionState.Normalized => RecoveryAction.SkipToSubmission,

            NascaExecutionState.Submitting => RecoveryAction.ReconcileIdempotently,

            NascaExecutionState.Completed => RecoveryAction.NoAction,

            NascaExecutionState.RetryableFailure or
            NascaExecutionState.RecoveryRequired => RecoveryAction.EligibleForRetry,

            NascaExecutionState.PermanentFailure or
            NascaExecutionState.Cancelled => RecoveryAction.TerminalNoRetry,

            _ => RecoveryAction.TerminalNoRetry
        };
    }
}

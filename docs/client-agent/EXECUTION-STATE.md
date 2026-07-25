# IQC Nexus Client Agent — Durable Execution State & Restart Recovery (Phase 3A.4)

## Overview

Phase 3A.4 introduces a vendor-neutral, durable execution-state persistence model (`INascaExecutionStateStore`) and restart recovery policy (`NascaExecutionRecoveryPolicy`) for NASCA-bound queue items.

## Execution Identities

- **`QueueItemId`**: Authoritative queue item identity (`LocalJobItem.QueueItemId`).
- **`CorrelationId`**: Monotonically stable identity across all retry attempts for a logical job. Derived safely from `PayloadSubmissionId` or `QueueItemId`.
- **`ExecutionId`**: Opaque attempt identifier (`Guid.NewGuid().ToString("N")`) regenerated per execution attempt.
- **`AttemptNumber`**: Monotonic attempt counter (1-indexed).
- **`WorkDirectoryId`**: Opaque work directory identifier derived safely from `CorrelationId` (`nasca_work_<CorrelationId>`).

## State Machine & Allowed Transitions

```
[Queued] -> [InputValidated] -> [InputStaged] -> [ExecutionPending] -> [ExecutionStarted] -> [OutputPending] -> [OutputValidated] -> [Normalized] -> [Submitting] -> [Completed]
                                                                          |                        |
                                                                          +-----> [RetryableFailure] <----+
                                                                          |            |
                                                                          +-----> [RecoveryRequired]
                                                                          |
                                                                          +-----> [PermanentFailure]
```

## Restart Recovery Classification (`NascaExecutionRecoveryPolicy`)

| Current Persisted State | Recovery Action | Behavior |
| :--- | :--- | :--- |
| `Queued`, `InputValidated`, `InputStaged`, `ExecutionPending` | `ResumeCurrentStep` | Resume execution flow using existing queue lease. |
| `ExecutionStarted`, `OutputPending` | `MarkRecoveryRequired` | Mark state as `RecoveryRequired` because actual vendor process outcome is unknown. |
| `OutputValidated`, `Normalized` | `SkipToSubmission` | Skip re-execution and proceed directly to submission. |
| `Submitting` | `ReconcileIdempotently` | Check backend idempotency using `PayloadSubmissionId` and `Nonce`. |
| `Completed` | `NoAction` | Execution terminal; no action needed. |
| `RetryableFailure`, `RecoveryRequired` | `EligibleForRetry` | Eligible for retry if attempt count is within limit. |
| `PermanentFailure`, `Cancelled` | `TerminalNoRetry` | Terminal failure; no automatic retry. |

## Data Persistence & Security Boundary

- **Store**: `SqliteNascaExecutionStateStore` (`nasca_state.db` in local user AppData).
- **Atomic Updates**: SQLite transactions with compare-and-set queries (`WHERE CorrelationId = @CorrelationId AND CurrentState = @FromState`).
- **Data Safety**:
  - NO workbook cell contents persisted.
  - NO secrets, passwords, or raw access tokens persisted.
  - Schema version `1` enforced. Corrupt records fail closed.

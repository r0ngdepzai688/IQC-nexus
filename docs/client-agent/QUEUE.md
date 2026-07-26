# IQC Nexus Client Agent — Queue Architecture & Execution State (Phase 3A.4)

## Queue Orchestration & Execution Lifecycle

Queue processing integrates `INascaExecutionStateStore` to track durable lifecycle states (`Queued` -> `InputStaged` -> `ExecutionPending` -> `OutputValidated` -> `Submitting` -> `Completed`).

- **Durable Identity**: `CorrelationId` is preserved across all retries.
- **Attempt Tracking**: `AttemptNumber` increments monotonically per retry.
- **Restart Recovery**: Restarts inspect `NascaExecutionStates` table. Incomplete executions in `ExecutionStarted` transition safely to `RecoveryRequired` without re-running execution blindly.

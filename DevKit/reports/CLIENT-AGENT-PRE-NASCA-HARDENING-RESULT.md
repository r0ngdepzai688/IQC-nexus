# Milestone Result: Client Agent Pre-NASCA Hardening (Phase 2D.2)

**Date:** July 25, 2026
**Status:** PHASE 2A, 2B, 2B.1, 2B.2, 2C, 2D, 2D.1 & 2D.2 COMPLETED & VERIFIED
**Branch:** `feature/client-agent-pre-nasca-hardening`

---

## Phase 2D.2 Objectives & Execution Summary

Phase 2D.2 implemented relational concurrency proof, provider-aware constraint classification, same-transaction downstream job registration and atomic rollback, typed security errors, and dual-tier retention cleanup with replay tombstones.

| Feature | Architecture & Implementation | Status | Evidence |
| :--- | :--- | :--- | :--- |
| **Relational Concurrency Proof** | True multi-threaded tests using separate `DbContext` scopes over relational SQLite database | **COMPLETED** | `RelationalPayloadConcurrencyTests.cs` |
| **Constraint Classification** | `IRelationalConstraintViolationClassifier` & `RelationalConstraintViolationClassifier` inspecting DB error codes | **COMPLETED** | `RelationalConstraintViolationClassifier.cs`, `ConstraintViolationClassificationTests.cs` |
| **Atomic Downstream Rollback** | Same-transaction boundary for `AgentPayloadSubmission` and `PersistentImportJob` with fault injection tests | **COMPLETED** | `AgentService.cs`, `AtomicDownstreamRollbackTests.cs` |
| **Typed Security Errors** | Typed domain exceptions (`PayloadSubmissionMismatchException`, `PayloadNonceReplayException`, etc.) mapped to HTTP 409/400/404 | **COMPLETED** | `PayloadSubmissionExceptions.cs`, `AgentDevicesController.cs`, `TypedPayloadSecurityErrorTests.cs` |
| **Retention Operations** | Configurable `AgentPayloadRetentionOptions` (90d full / 365d tombstone) and atomic batch cleanup service | **COMPLETED** | `AgentPayloadRetentionService.cs`, `PayloadRetentionCleanupTests.cs` |
| **Queue & Lost Response Recovery** | Local queue persistence across restart and idempotent retry completion | **COMPLETED** | `SqliteLocalAgentQueue.cs`, `QueueRecoveryTests.cs` |

---

## Test Verification Totals

- **Client Agent Tests**: **83 Passed / 0 Failed**
- **DataHub Checks**: **89 Passed / 0 Failed**
- **Api Integration Tests**: **39 Passed / 0 Failed**
- **Api Auth Checks**: **10 Passed / 0 Failed**
- **Seeder Safety Checks**: **4 Passed / 0 Failed**
- **Total Backend Tests**: **225 Passed / 0 Failed**
- **win-x64 Publish Build**: **Succeeded with 0 Errors**

---

## Residual Risk Disclosure

1. **Finite Replay Retention Window**: Once a replay tombstone expires past `ReplayTombstoneRetentionDays` (default 365 days), replay protection for that specific nonce terminates upon tombstone deletion.
2. **Clock Synchronization**: System clock drift across API nodes must be managed via NTP to maintain consistent retention cutoff calculations.

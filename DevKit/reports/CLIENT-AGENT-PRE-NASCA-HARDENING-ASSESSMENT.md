# Client Agent Pre-NASCA Hardening Assessment (Phase 2D.1 & 2D.2)

**Date:** July 25, 2026
**Status:** Phase 2D.2 Implementation & Verification Complete
**Branch:** `feature/client-agent-pre-nasca-hardening`
**Scope:** Relational Concurrency Proof, Constraint Classification, Atomic Downstream Rollback, Typed Security Errors, and Retention Operations

---

## Phase 2D.1 & 2D.2 Inspection Findings Matrix

| # | Inspection Item | Phase 2D.1 Status | Phase 2D.2 Resolved Status |
| :- | :--- | :--- | :--- |
| 1 | **`ServerImportJobId` in Digest** | Excluded from `CanonicalPayloadHash` | Binds strictly client identity parameters; ignores server tracking GUIDs |
| 2 | **Relational DB Provider** | SQLite in-memory & file DB | SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) used across unit/integration environments |
| 3 | **Relational Concurrency Proof** | Unverified | Proved via multi-threaded `Task.WhenAll` across separate `DbContext` instances over relational DB |
| 4 | **Unique-Constraint Classification** | Indiscriminate `DbUpdateException` catch | Provider-aware `IRelationalConstraintViolationClassifier` inspects extended DB error codes |
| 5 | **Downstream Transaction Rollback** | Separate calls | Same-transaction boundary for `AgentPayloadSubmission` and `PersistentImportJob` with fault injection rollback proof |
| 6 | **Typed Security Errors** | Generic `InvalidOperationException` | Exposes typed domain exceptions (`PayloadSubmissionMismatchException`, `PayloadNonceReplayException`, `PayloadReplayTombstoneException`) mapped to HTTP 409/400/404 |
| 7 | **Retention Operations** | Schema & tombstone table created | `AgentPayloadRetentionService` with `AgentPayloadRetentionOptions` (90d full / 365d tombstone) and atomic cleanup transactions |
| 8 | **Cleanup Expiration Indexes** | Missing indexes | Additive EF migration `20260725100000_AddPayloadRetentionIndexes` added indexes on `CreatedAtUtc` & `TombstoneExpiresAtUtc` |

---

## Completed Architectural Implementation (Phase 2D.2)

1. **Relational Concurrency Proof**: Verified overlapping concurrent requests produce exactly 1 `AgentPayloadSubmission` and 1 downstream `PersistentImportJob`.
2. **Provider-Aware Classifier**: `IRelationalConstraintViolationClassifier` prevents non-unique DB errors (FK, null constraint, connection drops) from false duplicate classification.
3. **Atomic Downstream Rollback**: Proved via fault injection that any failure prior to transaction commit rolls back both submission and downstream job records.
4. **Typed API Security Errors**: Mapped typed security exceptions through API pipeline to return generic, sanitized responses without leaking nonces, digests, or paths.
5. **Retention Cleanup Transactions**: Atomic batch cleanup creates tombstones before deleting full submission records, ensuring fail-safe idempotency.

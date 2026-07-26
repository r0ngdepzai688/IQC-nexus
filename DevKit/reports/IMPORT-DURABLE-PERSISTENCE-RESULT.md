# Import Durable Persistence & Transactional Commit Result Report

**Date:** July 24, 2026  
**Repository:** `D:\Code_viber\Portal`  
**Branch:** `feature/import-durable-persistence`  
**Status:** Completed & Fully Verified  

---

## 1. Architectural Summary

This milestone establishes durable persistence, preview invalidation, idempotent transactional commit, append-only audit trail, and rollback verification for IQC Nexus import workflows.

---

## 2. Durable Entities & Database Migration

* **`PersistentImportJob`**: Primary job entity tracking state, provider metadata, profile versions, preview attestation fingerprints, and optimistic concurrency version (`ConcurrencyVersion`).
* **`PersistentImportMappedPayload`**: Bounded, canonical JSON representation of mapped records required for commit execution.
* **`CommittedImportRecord`**: Generic synthetic target record (`ItemCode`, `Quantity`, `InspectionDate`, `Result`) for verifying commit behavior.
* **`PersistentImportCommitReceipt`**: Durable idempotency receipt preventing duplicate commits.
* **`PersistentImportAuditEvent`**: Append-only audit trail tracking all job lifecycle state transitions.

---

## 3. Preview Invalidation Engine

* Existing previews and attestations are invalidated when:
  1. Source workbook content changes.
  2. Mapping profile or rules change.
  3. Validation profile or rules change.
  4. Preview attestation expires (2 hours default).
  5. Job is cancelled or failed.
* Invalidation atomically resets job state from `ReadyForReview` to `Validating`.

---

## 4. Transactional Commit & Idempotency

* **Single Database Transaction**: `AppDbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted)`.
* **Idempotency Guarantee**: Commit request with identical `idempotencyKey` and `jobId` returns prior result (`Replayed = true`) without duplicating target records.
* **Rollback Proof**: Failure injection testing (`TestFailureInjector`) proves 100% rollback of state changes, target record inserts, and idempotency receipts upon transaction failure.

---

## 5. Audit Trail

* Append-only events for `Created`, `Mapped`, `Validated`, `PreviewGenerated`, `PreviewInvalidated`, `CommitRequested`, `CommitSucceeded`, `CommitFailed`, `Cancelled`.
* Includes actor identity, timestamp, state transition, stable code, and sanitized metadata.

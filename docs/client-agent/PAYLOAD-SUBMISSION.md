# IQC Nexus Client Agent — Payload Replay Protection & Idempotent Submission (Phase 2D.2)

## Overview

The IQC Nexus Client Agent provides complete atomic payload acceptance, canonical SHA-256 digest verification, local queue durability, provider-aware constraint classification, typed security errors, same-transaction downstream job creation, and dual-tier retention operations with replay tombstones.

## Architectural Components

### 1. Dedicated Canonicalizer (`INormalizedWorkbookCanonicalizer`)
All normalized workbook payloads are serialized into a deterministic UTF-8 binary representation ([NormalizedWorkbookCanonicalizer.cs](file:///D:/Code_viber/Portal/backend/src/IqcQms.Infrastructure/Security/NormalizedWorkbookCanonicalizer.cs)):
- **Ordering**: Explicit sorting by `SheetName` (`Ordinal`), `RowIndex`, and `ColumnIndex`/`ColumnName`.
- **Formatting**: Invariant culture for numbers, ISO 8601 UTC for dates, uppercase `TRUE`/`FALSE` for booleans, explicit null vs empty string tags, and Form C Unicode normalization.
- **`SourceFingerprint`**: Derived as `SHA-256("v1:" + canonicalWorkbookBytes)`.

### 2. Corrected Canonical Request Digest
Calculated server-side via `CanonicalPayloadHasher.ComputeCanonicalHash`:
- Binds: `CanonicalizationVersion`, `SchemaVersion`, `DeviceId`, `PayloadSubmissionId`, `Nonce`, `SourceFingerprint`, and `CanonicalWorkbookBytes`.
- Excludes: Server-generated result fields such as `ServerImportJobId` and `UploadId` to maintain strict client request identity across retries.

### 3. Provider-Aware Constraint Classification (`IRelationalConstraintViolationClassifier`)
Does not treat all `DbUpdateException` instances indiscriminately as duplicate races:
- Inspects database extended error codes (e.g., SQLite 2067 `SQLITE_CONSTRAINT_UNIQUE`, 1555 `SQLITE_CONSTRAINT_PRIMARYKEY`, 787 `SQLITE_CONSTRAINT_FOREIGNKEY`, 1299 `SQLITE_CONSTRAINT_NOTNULL`).
- Distinguishes unique key violations from foreign-key failures, null constraint violations, connection drops, and transaction failures.
- Only expected unique violations on `(AgentDeviceId, PayloadSubmissionId)` or `(AgentDeviceId, Nonce)` trigger duplicate-resolution logic.
- All non-unique database errors immediately roll back and preserve the original exception without false idempotency masking.

### 4. Same-Transaction Downstream Job Creation & Atomic Rollback
- Submission acceptance (`AgentPayloadSubmission`) and downstream job registration (`PersistentImportJob`) execute within the exact same database transaction boundary.
- If any stage fails before commit (or if a transient error occurs), both records roll back atomically.
- Proved via fault injection tests at every transaction checkpoint.

### 5. Typed Security Outcomes & API Error Pipeline
Replaces generic `InvalidOperationException` with typed domain exceptions:
- `PayloadSubmissionValidationException` -> HTTP 400 (`BadRequest`)
- `PayloadSubmissionOwnershipException` -> HTTP 404 (`NotFound`)
- `PayloadSubmissionMismatchException` -> HTTP 409 (`Conflict`)
- `PayloadNonceReplayException` -> HTTP 409 (`Conflict`)
- `PayloadReplayTombstoneException` -> HTTP 409 (`Conflict`)

API responses return generic, deterministic message bodies without disclosing internal binding details, nonces, paths, or device identifiers, while logging sanitized reason codes.

### 6. Dual-Tier Retention Policy (`AgentPayloadRetentionOptions`) & Cleanup Transaction
Configurable parameters:
- `FullResultRetentionDays`: 90 days (default)
- `ReplayTombstoneRetentionDays`: 365 days (default)
- `CleanupBatchSize`: 100
- `CleanupInterval`: 1 hour

Atomic Cleanup Transaction Flow:
1. Query full submission records older than `FullResultRetentionDays`.
2. Begin transaction;
3. Create authoritative `AgentPayloadReplayTombstone` preserving `AgentDeviceId`, `PayloadSubmissionId`, `Nonce`, `CanonicalPayloadHash`, `AcceptedAtUtc`, and `TombstoneExpiresAtUtc`;
4. Delete full submission record;
5. Commit atomically.

Tombstone Expiry:
- Expired tombstones are purged after `ReplayTombstoneRetentionDays`.
- Replay protection terminates upon tombstone deletion; indefinite replay prevention is not claimed beyond configured retention.

## Residual Risks & Operational Assumptions
- **Finite Tombstone Window**: Once a tombstone expires after `ReplayTombstoneRetentionDays` (365 days), re-submitting an identical `PayloadSubmissionId`/`Nonce` could be treated as a new submission.
- **Clock Drift**: Node system clocks must be synchronized via NTP to maintain accurate retention cutoff calculation.
- **Concurrent Node Race**: Distributed nodes rely on database unique constraints to arbitrate cleanup races.

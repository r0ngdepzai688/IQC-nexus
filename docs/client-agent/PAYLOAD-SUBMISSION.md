# IQC Nexus Client Agent — Payload Replay Protection & Idempotent Submission (Phase 2D.1)

## Overview

The IQC Nexus Client Agent provides complete atomic payload acceptance, canonical SHA-256 digest verification, local queue durability, and replay tombstone protection.

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

### 3. Authoritative Relational Insert & Concurrency Arbitration
- Submissions are inserted into `AgentPayloadSubmission` within a database transaction.
- **Race Conflict Resolution**: Catches `DbUpdateException` on unique constraint races (`(AgentDeviceId, PayloadSubmissionId)` or `(AgentDeviceId, Nonce)`). On conflict, queries the authoritative submission record. If identical, returns the original committed response with `IsDuplicateRetry = true` without throwing HTTP 500.

### 4. Retention & Replay Tombstones (`AgentPayloadReplayTombstone`)
- Full submission results are retained during `FullResultRetention`.
- After full submission cleanup, a compact `AgentPayloadReplayTombstone` record is retained to prevent re-acceptance of archived submissions.

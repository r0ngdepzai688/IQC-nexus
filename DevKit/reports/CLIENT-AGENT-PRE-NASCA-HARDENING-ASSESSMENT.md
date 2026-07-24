# Client Agent Pre-NASCA Hardening Assessment

**Date:** July 25, 2026
**Status:** Phase 2D.1 Read-Only Inspection Complete
**Branch:** `feature/client-agent-pre-nasca-hardening`
**Scope:** Assessment of Payload Idempotency Correctness, Concurrency, and Retention Completion

---

## Phase 2D.1 Inspection Findings Matrix

| # | Inspection Item | Initial Status | Findings & Required Architectural Fix |
| :- | :--- | :--- | :--- |
| 1 | **`ServerImportJobId` in Digest** | **INCORRECT** | `ServerImportJobId` was included in `CanonicalPayloadHash`. It is a server-side job tracking ID and must be removed from the request payload digest. |
| 2 | **`ServerImportJobId` Lifecycle** | **MIXED** | `ServerImportJobId` is passed in the request DTO for server job mapping, but must not affect client request payload identity. |
| 3 | **Relational Unique-Constraint Conflict Handling** | **MISSING** | Pre-query (`FirstOrDefaultAsync`) did not catch `DbUpdateException` when concurrent duplicate requests raced past the pre-query. |
| 4 | **Concurrent Pre-Query Race Exposure** | **SECURITY RISK** | Two parallel threads could both pass the pre-query check simultaneously. The second thread threw uncaught `DbUpdateException` (HTTP 500). |
| 5 | **Downstream Job Registration Boundary** | **PARTIAL** | Submission acceptance and downstream job registration must commit within the same database transaction. |
| 6 | **`SourceFingerprint` String Escaping** | **UNSAFE** | String concatenation (`WB:name;SH:name;...`) was unescaped and susceptible to delimiter ambiguity. Must be replaced with SHA-256 over canonical workbook bytes. |
| 7 | **Sheet, Row, Cell Order Canonicalization** | **INCOMPLETE** | Ordering must be explicitly sorted by `SheetName`, `RowIndex`, and `ColumnIndex`/`ColumnName`. |
| 8 | **Data Type Canonicalization** | **INCOMPLETE** | Numeric, date/time (ISO 8601 UTC), boolean, null vs. empty string, and Unicode normalization must be explicitly handled by `INormalizedWorkbookCanonicalizer`. |
| 9 | **Local Queue Durability** | **VERIFIED IN FILE** | `SqliteLocalAgentQueue` stores queue items in SQLite file, but requires explicit close/reopen durability unit tests. |
| 10 | **Retention Cleanup & Replay Tombstones** | **MISSING** | Lacked compact `AgentPayloadReplayTombstone` entity and explicit retention TTLs (`FullResultRetention`, `ReplayTombstoneRetention`). |

---

## Required Architectural Fixes for Phase 2D.1

1. **Dedicated Canonicalizer**: Implement `INormalizedWorkbookCanonicalizer` producing deterministic UTF-8 bytes for `SourceFingerprint` and `CanonicalPayloadHash`.
2. **Corrected Request Digest**: Bind `(CanonicalizationVersion, SchemaVersion, DeviceId, PayloadSubmissionId, Nonce, SourceFingerprintVersion, SourceFingerprint, CanonicalWorkbookBytes)`. Exclude `ServerImportJobId`, `UploadId`, and timestamps.
3. **DbUpdateException Race Resolution**: Catch `DbUpdateException` on duplicate concurrent inserts, query authoritative record, and return duplicate response if identical.
4. **Typed API Security Errors**: Return typed errors (`PayloadSubmissionMismatch`, `PayloadNonceReplay`, `PayloadSubmissionNotOwned`, etc.).
5. **Replay Tombstones & Retention**: Persist compact `AgentPayloadReplayTombstone` upon full payload record cleanup.

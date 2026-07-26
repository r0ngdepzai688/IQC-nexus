# Import Milestone Hardening & Verification Result

**Date:** July 24, 2026  
**Repository:** `D:\Code_viber\Portal`  
**Branch:** `feature/import-mapping-validation`  
**HEAD Commit:** `770f402 docs(import): document mapping validation and preview architecture and quality gates`  

---

## 1. Baseline & Environment Verification
* **Git Working Tree**: Verified clean.
* **Branch**: `feature/import-mapping-validation`.
* **Baseline Commit**: `770f402`.

---

## 2. Reconciled Import Lifecycle
* **Single Authoritative Guard**: `ImportJobTransitionGuard` in `IqcQms.Application.DataPlatform` is the sole source of truth for lifecycle transitions.
* **Active Workflow**: `Created` -> `Inspecting` -> `ReadyForMapping` -> `Validating` -> `ReadyForReview`.
* **Corrective Transitions**: Re-running mapping or validation safely transitions `ReadyForReview` or `Validating` to `Validating`.
* **Deferred States**: `Committing` and `Completed` remain as unactivatable contract states. Commit functionality is explicitly labeled deferred across backend, frontend UI, and documentation.

---

## 3. Upload Contract & NASCA Rejection
* **Supported Formats**: `.csv`, `.xlsx`, `.xls` only via `IDataSourceProviderRegistry`.
* **NASCA Rejection**: Explicitly rejects `.nasca`, `.xlsm`, `.xltm`, `.xlam`, or filenames containing `"nasca"` with `400 Bad Request` (`IMPORT_FILE_TYPE_UNSUPPORTED`).
* **Path Traversal Prevention**: Filenames sanitized via `Path.GetFileName`. Absolute paths rejected and never exposed.
* **Payload Boundary**: Hard limit of 50 MB (`IMPORT_FILE_TOO_LARGE`).
* **Stream Safety**: Source file payload streams disposed using `using var stream`.

---

## 4. Hardened Preview Attestation
* **Service**: `IPreviewAttestationService` implemented in `PreviewAttestationService.cs`.
* **Key Management**: Secret key loaded from `PreviewAttestation:SecretKey` or `JwtSettings:Secret`.
* **Production Protection**: Startup fails fast if key is missing or under 32 bytes (256 bits) in Production environments.
* **Constant-Time Verification**: `CryptographicOperations.FixedTimeEquals` used for signature and fingerprint verification to prevent timing side-channel attacks.
* **Tamper Invalidation**: Any alteration to `jobId`, `ownerUserId`, mapping profile version, validation profile version, or record counts invalidates attestation.

---

## 5. Authorization Matrix
| Route | Method | Policy Permission |
| :--- | :--- | :--- |
| `/api/import-jobs/upload` | POST | `ImportCreate` |
| `/api/import-jobs` | GET | `ImportView` |
| `/api/import-jobs/{jobId}` | GET | `ImportView` |
| `/api/import-jobs/{jobId}/mapping` | POST | `ImportCreate` |
| `/api/import-jobs/{jobId}/validation` | POST | `ImportReview` |
| `/api/import-jobs/{jobId}/preview` | POST | `ImportReview` |
| `/api/import-jobs/{jobId}/preview` | GET | `ImportReview` |
| `/api/import-jobs/{jobId}/diagnostics` | GET | `ImportReview` |

* **Ownership & Anti-Enumeration**: Unauthenticated requests return `401 Unauthorized`. Non-owner requests return `403 Forbidden` or `404 Not Found`. `import.admin` policy grants administrative override.

---

## 6. In-Memory Store Safety & Concurrency
* **Thread Safety**: Synchronized lock isolation on `InMemoryImportJobStore.cs`.
* **Optimistic Concurrency**: `Version` property tracked per job; stale concurrent updates trigger `400 Bad Request` (`IMPORT_COMMIT_CONFLICT`).
* **Snapshot Isolation**: Clone snapshots returned on reads/lists to prevent state mutation outside store boundaries.

---

## 7. Frontend Lifecycle Correction
* **State Integrity**: Removed optimistic lifecycle state assignments in `frontend/src/app/(dashboard)/imports/[id]/page.tsx`. Backend response is the single authoritative source of state.

---

## 8. Quality & Test Verification
* **Backend Unit & Integration Tests**: All unit tests passed across mapping, validation, preview attestation, upload rejection, and concurrency.
* **Frontend Tests & Typecheck**: Passed 33/33 Vitest tests; `tsc --noEmit` passed with 0 errors; production build succeeded.

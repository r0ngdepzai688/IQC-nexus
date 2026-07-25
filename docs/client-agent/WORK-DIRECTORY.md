# IQC Nexus Client Agent — Secure Work Directory Lifecycle Architecture (Phase 3A.5)

## Overview

Phase 3A.5 establishes an isolated, secure work directory lifecycle (`INascaWorkDirectoryManager`) for NASCA-bound execution tasks.

## Directory Layout

Work directories are isolated under `%LocalAppData%\IqcQmsAgent\NascaWork\`:

```text
LocalAppData
└── IqcQmsAgent
    └── NascaWork
        └── <OpaqueWorkDirectoryId>
            ├── input
            │   └── input.dat
            ├── output
            ├── state
            ├── quarantine
            └── manifest.json
```

- **Folder Naming**: `OpaqueWorkDirectoryId` is derived solely from `CorrelationId` (`work_<safe_correlationId>`).
- **Path Sanitization**: Directory names NEVER contain workbook filenames, user-supplied paths, or vendor filenames.

## Work Manifest Schema (`NascaWorkManifest`)

- **SchemaVersion**: `1`
- **CorrelationId**: Unique stable job identity.
- **ExecutionId**: Unique attempt identity.
- **AttemptNumber**: Monotonic attempt counter.
- **CreatedUtc / UpdatedUtc**: ISO 8601 timestamps.
- **CurrentLifecycle**: `Active`, `RecoveryRequired`, `Completed`, `Failed`, `Quarantined`, `Expired`.
- **InputMetadata**:
  - `StagedFileName`: `"input.dat"`
  - `OriginalHashSha256`: SHA-256 hash of original input.
  - `StagedHashSha256`: SHA-256 hash of staged copy (must match original).
  - `FileSizeBytes`: File size in bytes.
- **RetentionCategory**: `"Standard"`, `"Recovery"`, `"Quarantine"`.

## Atomic Staging Algorithm

1. **Source Revalidation**: Ensure source file exists, is inside allowed root, and is readable.
2. **Safe Temporary Staging**: Copy source to `input/staging_<guid>.tmp`.
3. **Integrity Hash Check**: Calculate SHA-256 hash of staged copy and verify against source hash.
4. **Atomic Rename**: Move `staging_<guid>.tmp` to `input/input.dat`.
5. **Immutability**: Original source input file is NEVER modified or written to.

## Lifecycle & Retention Policy

- **`Active`**: In-progress executions. Work directories are NEVER deleted during cleanup.
- **`Completed`**: Cleaned up after standard retention period (e.g. 7 days).
- **`RecoveryRequired`**: Retained longer (e.g. 30 days) for operational investigation.
- **`Quarantined`**: Retained indefinitely for security review; NEVER silently deleted.

## Quarantine Policy

Triggered on:
- Corrupt or invalid `manifest.json`.
- `CorrelationId` identity mismatch.
- Staging hash mismatch.
- Path traversal or root escape attempt.

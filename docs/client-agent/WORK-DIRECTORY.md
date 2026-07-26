# IQC Nexus Client Agent — Secure Work Directory Architecture (Phase 3A.5 Security Closure)

## Overview

Phase 3A.5 Security Closure establishes opaque work directory identities, reparse-point defense (`INascaPathSecurityGuard`), TOCTOU mitigation, startup discovery containment, and quarantine isolation for NASCA-bound execution tasks.

## Directory Layout

Work directories are isolated under `%LocalAppData%\IqcQmsAgent\NascaWork\`:

```text
LocalAppData
└── IqcQmsAgent
    └── NascaWork
        └── work_<32_lowercase_hex_chars>
            ├── input
            │   └── input.dat
            ├── output
            ├── state
            ├── quarantine
            └── manifest.json
```

## Security Design Principles

1. **Opaque Workspace Identifier**:
   - Format: `work_` + 32 random lowercase hex characters (`Guid.NewGuid().ToString("N").ToLowerInvariant()`).
   - Does NOT contain `CorrelationId`, `QueueItemId`, `ExecutionId`, or workbook filename.
   - Identifier is mapped in `NascaExecutionStateRecord` and `manifest.json`.

2. **Reparse-Point & Junction Defense (`NascaPathSecurityGuard`)**:
   - Inspects existing path components from root down to target file.
   - Rejects `FileAttributes.ReparsePoint` (symbolic links, junctions, mount points).
   - Fails closed on access denied or unexpected filesystem errors.

3. **TOCTOU Protections**:
   - Source revalidated immediately prior to open using `FileShare.Read`.
   - Temporary file `staging_<guid>.tmp` written, flushed to disk, verified for containment, and atomically renamed to `input.dat`.
   - Final SHA-256 hash verified against original source.
   - *Residual Risk*: Pre-open filesystem races remain possible without kernel-level file handle binding; mitigated by local UserAppData DACL boundaries.

4. **Cleanup Containment**:
   - Direct workspace enumeration from work root only.
   - Rejects candidate directories not matching `work_[a-f0-9]{32}`.
   - Rejects reparse-point directories.
   - NEVER deletes the work root itself or parent directories.

5. **Quarantine Handling**:
   - Suspect or corrupt manifest directories are marked `CurrentLifecycle = Quarantined` in place without overwriting untrusted JSON as authoritative.

# IQC Nexus Client Agent — User-Scoped Local Storage & DPAPI

## User-Scoped Data Directory

All runtime storage resolves under the current user's Local Application Data directory:

`%LOCALAPPDATA%\IQC Nexus\ClientAgent\<profile>`

### Directory Structure

```
%LOCALAPPDATA%\IQC Nexus\ClientAgent\<profile>\
├── device_identity.dpapi     (Protected via DPAPI CurrentUser)
├── device_credentials.dpapi  (Protected via DPAPI CurrentUser)
├── agent_queue.db            (Local SQLite Durable Job Queue)
├── agent_runtime.lock        (Single-instance runtime lock metadata)
└── logs\                     (Agent log files)
```

## Security Hardening
1. **DPAPI CurrentUser Scope**: Explicitly uses `DataProtectionScope.CurrentUser`. No `LocalMachine` or plaintext fallback.
2. **Atomic Writes**: Writes to `.tmp` file, flushes, and moves to target location (`File.Move(..., overwrite: true)`).
3. **Profile Path Traversal Protection**: Profile names are sanitized and validated against `^[a-zA-Z0-9_-]+$`. Path traversal sequences (`..`, `/`, `\`) are strictly rejected.

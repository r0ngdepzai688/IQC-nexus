# IQC Nexus Client Agent — Pre-NASCA Release Checklist

## 1. Secret Provisioning & Production Configuration

- [ ] **Pairing Pepper**: `AgentSecurityOptions:PairingPepper` MUST be configured in environment variables or Azure Key Vault with a strong random string (minimum 16 characters / 128 bits). Default fallback string is rejected in `Production`.
- [ ] **Envelope Encryption Key**: `AgentSecurityOptions:EnvelopeEncryptionKey` MUST be configured with a 256-bit key (32 bytes in Hex or Base64). Default key is rejected in `Production`.
- [ ] **Allowed Input Roots**: `AgentOptions:AllowedInputRoots` MUST contain non-empty, existing absolute directories on target host machines. Relative paths and root drives (e.g. `C:\`) are strictly forbidden in `Production`.
- [ ] **Server Base URL**: `AgentOptions:ServerBaseUrl` MUST use HTTPS in `Production`.

## 2. Database Migrations & Retention

- [ ] **Database Migration Execution**: Execute `dotnet ef database update` against production SQL Server / PostgreSQL. Ensure all EF migrations (`20260724143121_AddAgentDevices` through `20260725100000_AddPayloadRetentionIndexes`) complete cleanly without error.
- [ ] **EnsureCreated Prohibition**: Verify production API startup does NOT invoke `EnsureCreated()`.
- [ ] **Retention Configuration**: Verify `AgentPayloadRetentionOptions`:
  - `FullResultRetentionDays`: Default 90 days.
  - `ReplayTombstoneRetentionDays`: Default 365 days (`ReplayTombstoneRetentionDays > FullResultRetentionDays`).
- [ ] **Background Retention Worker**: Verify `AgentPayloadRetentionBackgroundWorker` is running on the backend host to purge expired payload records, purge tombstones, and clean 120s refresh operation recovery envelopes.

## 3. Time Synchronization & UTC Compliance

- [ ] **NTP Synchronization**: Host machines running Client Agent and API server nodes MUST sync clocks via NTP.
- [ ] **UTC Timestamp Verification**: All database timestamps (`CreatedAtUtc`, `ExpiresAtUtc`, `LastSeenAtUtc`) persist strictly in UTC.

## 4. Per-User Hosting & DPAPI Storage

- [ ] **Interactive Per-User Execution**: Client Agent executes as an interactive per-user process under the logged-in Windows user context.
- [ ] **HKCU Startup Registration**: Verify `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` entry points to `IqcQms.ClientAgent.exe`.
- [ ] **DPAPI Storage Security**: Verify `%LocalAppData%\IqcQmsAgent\device_identity.dpapi` is created with `DataProtectionScope.CurrentUser`.
- [ ] **Single Instance Lock**: Per-user mutex `Global\IqcQmsClientAgent_<Profile>_<UserSID>` prevents duplicate instances per logged-in session.

## 5. Security & Redaction Audits

- [ ] **Log Location & Redaction**: Audit application logs to confirm no access tokens, refresh tokens, pairing codes, pairing peppers, envelope keys, cell values, or raw payload bytes are logged.
- [ ] **API Error Masking**: Verify API error responses for security conflicts (409 Conflict, 400 Bad Request, 404 Not Found) return sanitized generic messages without stack traces or internal paths.
- [ ] **Path Reparse Validation**: Verify input path validation checks for symbolic link escapes, junctions, alternate data streams, and device namespace paths (`\\.\`).

## 6. Operational Procedures

- [ ] **Pairing / Reset Procedure**: Perform pairing code generation from Portal UI (`/agent-devices`), input into Client Agent, and confirm pairing completion.
- [ ] **Credential Revocation Procedure**: Admin click "Revoke" on Portal UI (`/agent-devices`). Confirm immediate invalidation of access and refresh tokens.
- [ ] **Queued Job & Lost Response Recovery**: Confirm local SQLite queue (`SqliteLocalAgentQueue`) recovers un-acked jobs upon Client Agent process restart.
- [ ] **NASCA / Excel COM Verification**: Confirm publish directory contains NO NASCA or Excel COM assemblies (`Microsoft.Office.Interop.Excel.dll`).

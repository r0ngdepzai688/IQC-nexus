# Client Agent Pre-NASCA Hardening Assessment

**Date:** July 24, 2026  
**Status:** Assessment Updated (Architecture Mismatch Corrected)  
**Branch:** `feature/client-agent-pre-nasca-hardening`  
**Scope:** Architectural, security, and test gap assessment of Client Agent Foundation for interactive per-user execution  

---

## Assessment Matrix

| # | Inspection Item | Classification | Key Findings & Root Cause Analysis |
| :- | :--- | :--- | :--- |
| 1 | **Hosting Model** | **ARCHITECTURE MISMATCH** | **Correction**: Previously classified as VERIFIED, but Windows Service hosting (Session 0, LocalSystem, NetworkService) is **incompatible** with the future NASCA provider. Future NASCA and Excel COM automation depend directly on the logged-in interactive Windows user's desktop session, profile, DPAPI `CurrentUser` context, mapped drives, and Excel license activation. The Agent must run as an interactive per-user background process. |
| 2 | **Device Identity** | **IMPLEMENTED BUT NEEDS MORE TESTS** | Cryptographically random GUID device ID (`dev_...`) stored securely via DPAPI `CurrentUser` scope (`WindowsDpapiDeviceIdentityStore`). Identity and credential persistence implemented. Test suite currently uses `InMemoryDeviceIdentityStore`; file-based DPAPI roundtrip, corrupt store recovery, and atomic writing require dedicated unit/integration tests. (Note: Service-account profile loading is no longer recommended as a production requirement). |
| 3 | **Pairing** | **SECURITY RISK** | Single-use 10-minute pairing codes generated and hashed with SHA256/BCrypt. **Security Vulnerability**: Endpoint `POST /api/agent-devices/pair` lacks rate limiting. Additionally, `AgentService.cs` only increments `matchedReq.AttemptCount` AFTER verifying a valid code match (`matchedReq == null` check returns before incrementing), allowing unthrottled brute-force attacks against 6-digit pairing codes without attempt tracking or lockout. |
| 4 | **Credential Rotation** | **IMPLEMENTED BUT NEEDS MORE TESTS** | Short-lived (15 min) access tokens and rotating refresh tokens (7 days) with automatic replay detection revoking device identity upon reused refresh token. **Gap**: `ConcurrencyVersion` properties exist on `AgentDevice` and `AgentPairingRequest`, but EF Core `OnModelCreating` does not map `IsConcurrencyToken()`, leaving concurrent token refresh requests unconstrained by EF concurrency checks. |
| 5 | **Queue** | **VERIFIED** | `SqliteLocalAgentQueue` provides local SQLite job storage, atomic transaction leasing (`AcquireNextLeaseAsync`), exponential retry backoff, stale lease recovery (`RecoverStaleLeasesAsync`), and poison state transition after 5 failed attempts. Fully covered in `LocalAgentQueueTests.cs`. |
| 6 | **AllowedInputRoots** | **SECURITY RISK** | `AgentOptions.IsPathAllowed` uses `Path.GetFullPath` and prefix checking against `AllowedInputRoots`. **Security Vulnerability**: On Windows, `Path.GetFullPath` does not resolve NTFS junction points or directory symlinks to their target (`FileSystemInfo.ResolveLinkTarget` is not called). A symlink or junction created inside an allowed directory pointing to restricted system paths (e.g. `C:\Windows`) bypasses allowed root checks. |
| 7 | **Payload** | **IMPLEMENTED BUT NEEDS MORE TESTS** | `NormalizedWorkbook` schema contract, JSON serialization, `CanonicalSchemaVersion = "1.0"` validation, and 10 MB payload limits implemented. **Gap**: `NormalizedWorkbookUploadRequest` includes `Nonce` and `SourceFingerprint`, but server-side nonce persistence and request deduplication/idempotency enforcement are not yet implemented or tested against replayed payload uploads. |
| 8 | **Heartbeat** | **VERIFIED** | Outbound periodic heartbeat loop in `Worker.cs` with exponential backoff and jitter. Server updates `LastSeenAtUtc` and derives `Offline` state after 5 minutes. Incompatible protocol versions ("2.0") and revoked device states are properly handled. |
| 9 | **EF Migration** | **VERIFIED** | Migration `20260724143121_AddAgentDevices` adds `AgentDevices`, `AgentCredentials`, and `AgentPairingRequests` tables with appropriate unique indexes. Reversible and additive. |
| 10 | **Logging** | **VERIFIED** | Safe logging policy implemented across `AgentService`, `Worker`, and `AgentApiClient`. Sensitive data (pairing codes, tokens, cell values, local paths) is excluded from log outputs. Verified by `SafeLoggingTests.cs`. |

---

## Summary of Classifications

- **VERIFIED (4):** Queue, Heartbeat, EF Migration, Logging.
- **IMPLEMENTED BUT NEEDS MORE TESTS (3):** Device Identity, Credential Rotation, Payload.
- **SECURITY RISK (2):** Pairing (Rate limiting & attempt counter logic), AllowedInputRoots (Junction/Symlink traversal).
- **ARCHITECTURE MISMATCH (1):** Hosting Model (Windows Service is not the primary NASCA execution model).
- **MISSING (0):** None.

---

## Detailed Item Breakdown

### 1. Hosting Model — ARCHITECTURE MISMATCH
- The previous assessment classified Windows Service hosting as VERIFIED. This is an **Architecture Mismatch** for the future NASCA provider.
- Future NASCA and Excel COM automation require execution inside the logged-in interactive Windows user's session to access:
  - User's Windows Profile & Environment
  - DPAPI `CurrentUser` encryption key context
  - Mapped Network Drives & User Credentials
  - User's Excel Application installation, COM registration, and license activation context
  - Session 1+ interactive desktop context
- Session 0, `LocalSystem`, `NetworkService`, or headless service accounts are NOT supported as the primary production hosting model.
- The Agent executable must be refactored to run as an interactive per-user background process.

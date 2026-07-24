# Client Agent Pre-NASCA Hardening Assessment

**Date:** July 24, 2026
**Status:** Phase 2B Read-Only Inspection Complete
**Branch:** `feature/client-agent-pre-nasca-hardening`
**Scope:** Architectural, security, and test gap assessment of Client Agent Foundation

---

## Assessment Matrix (Hosting & Core Foundation)

| # | Inspection Item | Classification | Key Findings & Root Cause Analysis |
| :- | :--- | :--- | :--- |
| 1 | **Hosting Model** | **ARCHITECTURE MISMATCH** | Windows Service hosting (Session 0, LocalSystem, NetworkService) is **incompatible** with the future NASCA provider. Future NASCA and Excel COM automation depend directly on the logged-in interactive Windows user's desktop session, profile, DPAPI `CurrentUser` context, mapped drives, and Excel license activation. Interactive per-user host implemented in Phase 2A. |
| 2 | **Device Identity** | **VERIFIED** | Cryptographically random GUID device ID (`dev_...`) stored securely via DPAPI `CurrentUser` scope (`WindowsDpapiDeviceIdentityStore`). Hardened in Phase 2A. |
| 3 | **Queue** | **VERIFIED** | `SqliteLocalAgentQueue` provides local SQLite job storage, atomic transaction leasing (`AcquireNextLeaseAsync`), exponential retry backoff, stale lease recovery (`RecoverStaleLeasesAsync`), and poison state transition after 5 attempts. |
| 4 | **AllowedInputRoots** | **SECURITY RISK** | `AgentOptions.IsPathAllowed` uses `Path.GetFullPath` and prefix checking against `AllowedInputRoots`. On Windows, `Path.GetFullPath` does not resolve NTFS junction points or directory symlinks to their target (`FileSystemInfo.ResolveLinkTarget` is not called). |
| 5 | **Payload** | **IMPLEMENTED BUT NEEDS MORE TESTS** | `NormalizedWorkbook` schema contract, JSON serialization, `CanonicalSchemaVersion = "1.0"` validation, and 10 MB payload limits implemented. Server-side nonce persistence and request deduplication/idempotency enforcement are not yet implemented. |
| 6 | **Heartbeat** | **VERIFIED** | Outbound periodic heartbeat loop in `Worker.cs` with exponential backoff and jitter. Server updates `LastSeenAtUtc` and derives `Offline` state after 5 minutes. |
| 7 | **EF Migration** | **VERIFIED** | Migration `20260724143121_AddAgentDevices` adds `AgentDevices`, `AgentCredentials`, and `AgentPairingRequests` tables with appropriate unique indexes. |
| 8 | **Logging** | **VERIFIED** | Safe logging policy implemented across `AgentService`, `Worker`, and `AgentApiClient`. Sensitive data excluded from log outputs. |

---

## Phase 2B Inspection Matrix (Pairing Security & Refresh Rotation)

| # | Sub-System Item | Classification | Inspection Findings & Analysis |
| :- | :--- | :--- | :--- |
| 9 | **Pairing Code Generation** | **VERIFIED** | Uses `RandomNumberGenerator.Fill` producing high-entropy 6-digit numeric codes (100000..999999) with 10-minute expiry. Plaintext code returned only once on creation. |
| 10 | **Pairing Hash Storage** | **SECURITY RISK** | Currently stored using plain SHA256 without a server-side pepper or secret verification key. Requires HMAC-SHA256 with server-side pepper to protect against offline code table dictionary attacks. |
| 11 | **Pairing Attempt Throttling** | **SECURITY RISK** | `AgentService.cs` only increments `matchedReq.AttemptCount` AFTER verifying a valid code match (`matchedReq == null` check returns before incrementing). Invalid pairing attempts do not increment attempt counts or trigger lockout against brute-force attacks. ASP.NET rate limiting policy is missing on `/api/agent-devices/pair`. |
| 12 | **Pairing Atomic Consumption** | **SECURITY RISK** | `PairDeviceAsync` retrieves active pairing requests into memory without EF transaction locks or concurrency token checks. Concurrent pairing requests using the same code could race. |
| 13 | **Refresh-Token Hashing** | **VERIFIED** | High-entropy random refresh tokens (`agt_ref_...`) stored server-side as SHA256 hashes (`ProtectedVerifierHash`). Plaintext token returned only on issuance/rotation. |
| 14 | **Refresh-Token Atomic Rotation** | **SECURITY RISK** | `RefreshTokenAsync` updates credential records and device status, but EF Core model does not configure `IsConcurrencyToken()` on `AgentDevice.ConcurrencyVersion` or `AgentCredential.ConcurrencyVersion`. Parallel concurrent refresh requests could race. |
| 15 | **Token-Family Replay Revocation** | **VERIFIED** | `RefreshTokenAsync` tracks `RotationLineage`. If an already-consumed refresh token is presented, all credentials for that device are revoked and device state transitions to `Revoked`. |
| 16 | **EF Concurrency Mapping** | **SECURITY RISK** | `ConcurrencyVersion` property exists on `AgentDevice` and `AgentPairingRequest` domain models, but `AppDbContext.cs` lacks `IsConcurrencyToken()` configuration for agent entities. |

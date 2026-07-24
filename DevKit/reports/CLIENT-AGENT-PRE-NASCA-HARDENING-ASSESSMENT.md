# Client Agent Pre-NASCA Hardening Assessment

**Date:** July 24, 2026
**Status:** Phase 2B.2 Read-Only Inspection Complete
**Branch:** `feature/client-agent-pre-nasca-hardening`
**Scope:** Assessment of Refresh Token Lost-Response Recovery & Idempotency Contract

---

## Phase 2B.2 Inspection Matrix (Lost-Response Recovery & Idempotency)

| # | Inspection Item | Initial Status | Findings & Required Architectural Fix |
| :- | :--- | :--- | :--- |
| 1 | **Duplicate Response Content** | **INSUFFICIENT** | In Phase 2B.1, duplicate responses returned empty string `AccessToken` and `RefreshToken` (`IsDuplicateRetry = true`). This left the client stranded without valid credentials if the first HTTP response was lost. |
| 2 | **Lost-Response Authentication** | **FAILURE** | A client that lost the original HTTP response could not authenticate or rotate after receiving empty tokens on duplicate retry. |
| 3 | **`RefreshOperationId` Generation** | **DTO DEFAULT** | `AgentTokenRefreshRequest` had a default `Guid.NewGuid()` initializer, allowing the server/DTO to generate fallback IDs implicitly instead of enforcing client generation. |
| 4 | **Database Unique Index** | **NON-UNIQUE** | `(AgentDeviceId, RefreshOperationId)` index was registered as a standard index, not explicitly `.IsUnique()`. |
| 5 | **Agent Transport Retry Retention** | **PARTIAL** | `Worker.cs` created `currentOpId` inside the loop body without maintaining a persistent operation state across network retry loops. |

---

## Required Architectural Design (AES-256-GCM Encrypted Recovery Envelope)

1. **Short-Lived Encrypted Recovery Envelope**: Store `AgentRefreshOperationResult` inside the database transaction during rotation using AES-256-GCM.
2. **Exact Successor Recovery**: On duplicate retry with the same `RefreshOperationId` within 120s TTL, decrypt the envelope and return the exact original committed `AccessToken` and `RefreshToken`.
3. **Explicit Unique Idempotency Constraint**: Enforce `.IsUnique()` on `(AgentDeviceId, RefreshOperationId)` in `AppDbContext.cs`.
4. **Client-Driven Operation Lifecycle**: Remove default property initializers from DTOs. Validate `RefreshOperationId` format server-side and retain across Agent transport retries.

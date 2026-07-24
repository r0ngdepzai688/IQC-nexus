# Architecture Assessment: IQC Nexus Windows Client Agent Foundation

**Date:** July 24, 2026  
**Status:** Completed  
**Repository Baseline:** `import-production-readiness-complete`  
**Branch:** `feature/client-agent-foundation`  

---

## Executive Summary

This document presents the architectural assessment and design decisions for the IQC Nexus Client Agent foundation. The agent enables secure, automated data ingestion and status reporting from client environments to the IQC Nexus Enterprise Platform without exposing sensitive credentials, raw workbooks, or unauthenticated shell capabilities.

---

## 1. Repository Conventions & Technology Stack

| Capability | Repository Pattern | Client Agent Standard |
| :--- | :--- | :--- |
| **Framework Target** | .NET 8.0 (`net8.0`) | .NET 8.0 (`net8.0`) |
| **Architecture Pattern** | Clean Architecture (Domain, App, Infra, Api) | Clean Architecture (`ClientAgent`, `ClientAgent.Application`, `ClientAgent.Infrastructure`, `ClientAgent.Contracts`, `ClientAgent.Tests`) |
| **Authentication** | JWT Bearer / Short-Lived Tokens | Short-lived Session Tokens + Refresh Credential Rotation |
| **Serialization** | `System.Text.Json` | `System.Text.Json` |
| **Server Persistence** | EF Core + SQLite (`IqcQmsDbContext`) | Additive EF Core Entities (`AgentDevice`, `AgentCredential`, `AgentPairingRequest`) |
| **Local Agent Queue** | N/A (New component) | Local SQLite Durable Queue with atomic leasing and crash recovery |
| **Local Secure Storage** | N/A (New component) | DPAPI (`System.Security.Cryptography.ProtectedData`) for Windows; isolated file/in-memory for tests |
| **Logging** | `ILogger<T>` | Structured `ILogger<T>` with strict redaction policies (no credentials, pairing codes, or path data) |

---

## 2. Key Architecture Decisions

### 2.1 Worker Service vs Console-Hosted Strategy
- **Decision:** Dual-host Worker Service using `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Hosting.WindowsServices`.
- **Rationale:** Allows interactive CLI execution during development and seamless Windows Service execution when installed in production environments.

### 2.2 Device Identity & DPAPI Storage
- **Decision:** Cryptographically random GUID device ID created at initial registration.
- **Rationale:** Identity is NEVER derived from hardware fingerprints (MAC, motherboard serial, hostname, user name). Stored using Windows DPAPI (`WindowsDpapiDeviceIdentityStore`) on Windows and isolated mock implementations during testing (`InMemoryDeviceIdentityStore`).

### 2.3 Single-Use Pairing & Session Credential Protocol
- **Decision:** Short-lived 6-digit or 8-character pairing codes created by authenticated admins/managers in Portal.
- **Rationale:**
  1. Portal generates code (hashed server-side with BCrypt/SHA256, 10-minute expiry, rate-limited, single-use).
  2. Agent submits pairing code, device ID, version, and capabilities.
  3. Server verifies pairing code, creates `AgentDevice`, issues short-lived session token (e.g. 15 min expiry) + refresh credential.
  4. Pairing code is immediately invalidated upon first use.
  5. Agent rotates refresh credentials on each refresh call; old credentials are invalidated with replay detection.

### 2.4 Heartbeat Protocol & Outbound-Only Design
- **Decision:** Agent initiates periodic outbound HTTPS POST heartbeats with configurable interval and exponential backoff jitter.
- **Rationale:** No inbound ports or network listeners are opened by the agent. Heartbeat transmits status, version, capability flags, and safe queue metrics. Offline status is derived server-side when `LastSeenAtUtc` exceeds the threshold.

### 2.5 Provider Boundary & Synthetic Data Isolation
- **Decision:** Clear separation via `IClientDataProvider` interface.
- **Rationale:** The initial foundation includes `SyntheticClientDataProvider` only. NASCA, Excel COM, formulas, and workbook decryption remain deferred. Normalized payloads adhere strictly to `NormalizedWorkbook` contracts.

### 2.6 Local Durable Job Queue Strategy
- **Decision:** Local SQLite database at agent runtime path (`%LocalAppData%\IqcQmsAgent\agent_queue.db` or configured path).
- **Rationale:** Provides crash safety, atomic lease locking, poison message isolation, and bounded retries without requiring external database services on client machines.

---

## 3. Boundary Verification & Compliance

- **No Office COM / Excel Interop:** Standard .NET 8 without `Microsoft.Office.Interop` or `Excel.Application`.
- **No NASCA Integration:** Deferred.
- **No Direct Shell Execution:** Agent does not accept arbitrary remote commands or scripts.
- **Strict HTTPS:** Outbound HTTPS only to configured IQC Nexus backend API.

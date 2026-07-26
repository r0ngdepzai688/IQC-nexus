# AI_CONTEXT.md

Fast-loading context for AI coding agents working in **IQC Nexus Portal**.  
Goal: allow a new agent to become productive quickly while preserving accepted architecture.

---

## 1) Project Purpose

IQC Nexus Portal is an enterprise system that combines:

- Backend services (.NET 8)
- A Windows Client Agent
- Existing frontend
- DevKit quality/report artifacts
- Documentation and automated tests

Current program objective is controlled, architecture-safe delivery of the client-agent execution track.  
The active workstream remains pre-vendor-assumption and fail-closed by design.

---

## 2) Current Architecture (High-Level)

### Backend
- ASP.NET Core API in `backend/src/IqcQms.Api`
- Existing contracts/authentication are established and must remain backward compatible.

### Client Agent
- Windows per-user background process model.
- Local persistence and execution orchestration split between:
  - `IqcQms.ClientAgent.Application` (contracts + typed models/interfaces)
  - `IqcQms.ClientAgent.Infrastructure` (implementations).

### Frontend
- Existing architecture under `frontend/`.
- Not current target for the active phase unless explicitly instructed.

### Core execution characteristics
- Strongly typed runtime decisions and execution states.
- Durable execution state (SQLite-backed in current implementation path).
- Opaque work directories with strict containment/security checks.
- Atomic staging and SHA-256 verification patterns.
- Restart recovery model.
- Fail-closed production posture.

---

## 3) Repository Layout (Practical Map)

- `backend/`
  - `src/IqcQms.Api/` — API service
  - `src/IqcQms.ClientAgent/` — agent host
  - `src/IqcQms.ClientAgent.Application/` — agent contracts/domain interfaces
  - `src/IqcQms.ClientAgent.Infrastructure/` — agent infrastructure implementations
  - `tests/` — test projects (`IqcQms.ClientAgent.Tests`, API checks, integration checks)
- `frontend/` — existing UI app and tests
- `docs/` — core architecture/security/operational documentation
- `docs/client-agent/` — client-agent architecture/security/runtime docs
- `DevKit/quality/` — quality gates/checklists
- `DevKit/reports/` — assessments/results
- `PROJECT_STATUS.md` — project status summary for agents
- `ARCHITECTURE_DECISIONS.md` — stable ADR-style architecture decisions
- `DEVELOPMENT_ROADMAP.md` — authoritative roadmap timeline

---

## 4) Current Phase

**Current phase:** **Phase 3A.6.6**
**Focus:** Validation Descriptor Persistence

Do not skip ahead to later phases unless explicitly requested.

---

## 5) Completed Phases

Completed and accepted:

- Phase 2D.2
- Phase 2E
- Phase 3A
- Phase 3A.1
- Phase 3A.2
- Phase 3A.3
- Phase 3A.4
- Phase 3A.5
- Phase 3A.5 Security Closure
- Phase 3A.6.1
- Phase 3A.6.2
- Phase 3A.6.3
- Phase 3A.6.4
- Phase 3A.6.5 (Output Stability-Window Validation)

These are stable baselines, not redesign candidates.

---

## 6) Architecture Constraints (Must Preserve)

Never change without explicit instruction:

- Interactive per-user Windows Client Agent model
- DPAPI CurrentUser usage
- LocalAppData storage model
- Single-instance Client Agent
- Existing authentication/pairing/refresh-rotation model
- Existing queue architecture
- Upload idempotency model
- AllowedInputRoots/path-security posture
- Existing API contracts
- Existing frontend architecture

No parallel architecture rewrites. Reuse existing abstractions/services.

---

## 7) Security Constraints (Hard Rules)

Never introduce:

- `Process.Start`
- Shell-based launch workarounds
- Excel COM / `Microsoft.Office.Interop.Excel`
- UI automation hacks
- Vendor-specific assumptions not yet verified
- Fake production behavior/data paths

Maintain:

- Fail-closed decisions
- Path containment
- Reparse-point defense
- Sanitized logging
- Deterministic and restart-safe behavior

---

## 8) Known Unknowns (Intentional)

The following are intentionally unverified and must not be invented:

- NASCA CLI contract details
- Vendor API/IPC/COM model
- Output schema and completion semantics
- Exit code semantics
- Output filenames/extensions
- Licensing/runtime packaging behavior
- Vendor process model in production

Unknown vendor behavior must remain unknown until explicitly validated.

---

## 9) Coding Conventions (Repository Direction)

Prefer:

- Small focused classes
- Single responsibility
- Dependency injection
- Strong typing (enums/contracts)
- `CancellationToken`
- `TimeProvider` when time abstraction is needed
- Deterministic behavior and explicit transitions
- Backward-compatible, additive changes

Avoid:

- Global mutable state
- Large unrelated refactors
- Architecture drift
- Mixing unrelated concerns in one change set

---

## 10) Testing Conventions

- Add/update tests for new behavior.
- Prefer deterministic unit tests first.
- Preserve fail-closed and security-path coverage.
- Keep fake runner/test doubles in test assembly only.
- For docs-only changes: verify requirement compliance, consistency, and cross-doc alignment.

---

## 11) Current Roadmap

- **Current:** Phase 3A.6 (Vendor-Neutral Output Validation)
- **Next:** Phase 3A.7
- **Then:** Frontend F1 → F2 → F3 → F4 → Publish Website → Phase 3B → Frontend F5

Follow sequence unless explicit re-prioritization is approved.

---

## 12) Important Documentation

Core orientation set:

- `PROJECT_STATUS.md`
- `ARCHITECTURE_DECISIONS.md`
- `DEVELOPMENT_ROADMAP.md`
- `docs/client-agent/ARCHITECTURE.md`
- `docs/client-agent/WORK-DIRECTORY.md`
- `docs/client-agent/EXECUTION-STATE.md`
- `docs/client-agent/INPUT-PATH-SECURITY.md`
- `docs/client-agent/LOCAL-STORAGE.md`
- `docs/client-agent/SECURITY.md`
- `docs/client-agent/NASCA-INTEGRATION.md`
- `docs/client-agent/QUEUE.md`
- `DevKit/reports/CLIENT-AGENT-NASCA-INTEGRATION-ASSESSMENT.md`
- `DevKit/reports/CLIENT-AGENT-NASCA-INTEGRATION-RESULT.md`

---

## 13) Files Never to Modify Without Approval

Treat these as controlled documents/areas requiring explicit approval before modification:

- `ARCHITECTURE_DECISIONS.md`
- `PROJECT_STATUS.md`
- `DEVELOPMENT_ROADMAP.md`
- `docs/client-agent/ARCHITECTURE.md`
- `docs/client-agent/SECURITY.md`
- `docs/client-agent/WORK-DIRECTORY.md`
- `docs/client-agent/EXECUTION-STATE.md`
- `docs/client-agent/NASCA-INTEGRATION.md`
- `docs/client-agent/INPUT-PATH-SECURITY.md`
- `docs/security/AUTHENTICATION-AND-AUTHORIZATION.md`
- Core agent composition/entry points:
  - `backend/src/IqcQms.ClientAgent/Program.cs`
  - `backend/src/IqcQms.ClientAgent/Worker.cs`
- Security-critical infrastructure paths under:
  - `backend/src/IqcQms.ClientAgent.Infrastructure/Nasca/`
  - `backend/src/IqcQms.ClientAgent.Infrastructure/Storage/`

---

## 14) Read These Documents Next (Ranked)

1. `ARCHITECTURE_DECISIONS.md`  
2. `PROJECT_STATUS.md`  
3. `DEVELOPMENT_ROADMAP.md`  
4. `docs/client-agent/ARCHITECTURE.md`  
5. `docs/client-agent/SECURITY.md`  
6. `docs/client-agent/WORK-DIRECTORY.md`  
7. `docs/client-agent/EXECUTION-STATE.md`  
8. `docs/client-agent/INPUT-PATH-SECURITY.md`  
9. `docs/client-agent/NASCA-INTEGRATION.md`  
10. `docs/client-agent/QUEUE.md`  
11. `docs/client-agent/LOCAL-STORAGE.md`  
12. `DevKit/reports/CLIENT-AGENT-NASCA-INTEGRATION-ASSESSMENT.md`  
13. `DevKit/reports/CLIENT-AGENT-NASCA-INTEGRATION-RESULT.md`

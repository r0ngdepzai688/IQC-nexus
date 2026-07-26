# Project

**Name:** IQC Nexus Portal

**Short description:**  
Enterprise platform comprising a .NET 8 backend, a Windows Client Agent, and an existing frontend. The current backend/agent architecture has completed multiple accepted hardening phases and is operating in a fail-closed, vendor-neutral pre-integration state for NASCA execution.

**Repository layout (high level):**
- `backend/` — .NET solution and services
  - `src/IqcQms.Api` — backend API
  - `src/IqcQms.ClientAgent` — Windows agent host
  - `src/IqcQms.ClientAgent.Application` — agent application contracts/domain services
  - `src/IqcQms.ClientAgent.Infrastructure` — infrastructure implementations
  - `tests/` — automated tests (including client-agent tests)
- `frontend/` — existing frontend codebase
- `docs/` — architecture, security, operations, client-agent references
- `DevKit/` — quality gates, assessments, and reports
- `fixtures/`, `scripts/`, `memory-bank/` — test/support/project context assets

**Main technologies:**
- .NET 8 / C#
- ASP.NET Core (backend API)
- Windows-focused background agent services
- SQLite (local durable state in agent components)
- Automated test suites (xUnit-style .NET tests)

**Target platforms:**
- Backend: server-hosted .NET runtime
- Client Agent: Windows (interactive per-user model)
- Frontend: existing web frontend stack (already established)

---

# Current Status

**Current development branch:** Not verified in this session (branch discovery not executed)

**Current development phase:** Phase 3A.6

**Current milestone:** Vendor-Neutral Output Validation

**Current objective:**  
Extend/complete output validation behavior in the existing client-agent NASCA architecture without changing accepted architecture, without enabling vendor execution, and while preserving fail-closed production behavior.

---

# Accepted Phases

## Phase 2D.2
Phase 2D.2 established foundational reliability and architecture alignment for the platform’s import/agent direction. Core boundaries between API, agent application contracts, and infrastructure were reinforced to keep responsibilities explicit and testable.  
**Guarantees introduced/solidified:** early fail-safe posture, separation of concerns, and architecture consistency required for later hardening phases.

## Phase 2E
Phase 2E advanced production-readiness structure and operational hardening patterns used by downstream phases. It consolidated implementation discipline around durable behavior, controlled transitions, and quality-gated delivery.  
**Guarantees introduced/solidified:** deterministic service behavior expectations, improved operational safety, and stable groundwork for client-agent lifecycle hardening.

## Phase 3A
Phase 3A initiated the NASCA-oriented client-agent execution model under strict fail-closed constraints. It established typed contracts and controlled execution orchestration boundaries while keeping vendor execution untrusted/unproven.  
**Guarantees introduced/solidified:** strongly typed runtime decisions, architecture boundaries for job execution, and safe default behavior when runtime readiness is not met.

## Phase 3A.1
Phase 3A.1 expanded reliability in lifecycle handling and state-oriented orchestration, emphasizing predictable transitions and controlled retry/recovery semantics.  
**Guarantees introduced/solidified:** tighter execution-state discipline, clearer transition pathways, and stronger restart-aware behavior.

## Phase 3A.2
Phase 3A.2 reinforced storage and lifecycle durability for in-flight execution tracking, reducing ambiguity after interruptions and improving deterministic recovery expectations.  
**Guarantees introduced/solidified:** durable execution state, bounded transition behavior, and safer resumability patterns.

## Phase 3A.3
Phase 3A.3 concentrated on secure work isolation and controlled filesystem usage around execution artifacts.  
**Guarantees introduced/solidified:** opaque work directories, LocalAppData-contained storage practices, and stronger containment assumptions for staged execution data.

## Phase 3A.4
Phase 3A.4 hardened staging and artifact integrity behavior using atomic operations and integrity checks before downstream progression.  
**Guarantees introduced/solidified:** atomic staging semantics and hash verification for staged data integrity.

## Phase 3A.5
Phase 3A.5 completed substantial pre-vendor hardening for output/workflow handling, including recovery and containment behavior around startup and cleanup paths.  
**Guarantees introduced/solidified:** restart recovery posture, cleanup containment rules, and hardened secure handling around work lifecycle boundaries.

## Phase 3A.6.4
Phase 3A.6.4 implemented directory traversal resistance, reparse-point defenses, level-by-level BFS enumeration, depth/count/file-size limits, and fail-closed security-first entry classification.
**Guarantees introduced/solidified:** security-first entry classification, level-by-level BFS enumeration with `StringComparer.Ordinal` determinism, non-throwing fail-closed exception boundaries, and strict cancellation contract preservation.

## Phase 3A.6.5
Phase 3A.6.5 implemented NASCA output stability-window validation using `TimeProvider`-based timing. Every polling iteration executes security-validated BFS snapshotting (`SearchOption.TopDirectoryOnly`), enforcing continuous stability window restart semantics on file modifications while revalidating reparse points, containment, and size/depth/count limits. Resolves test-side deadlock root cause in cancellation test via safe `Task.WhenAny` orchestration.
**Guarantees introduced/solidified:** continuous stability-window verification, security-first BFS snapshot polling, bounded fake-time polling without real sleeps, fail-closed limit/reparse revalidation during polling, and verified test-only cancellation deadlock resolution. (Verification totals: 97 validator tests, 27 work directory tests, 338 ClientAgent tests, 480 Release solution tests, 0 build warnings/errors).

---

# Current Architecture

## Backend
ASP.NET Core backend remains the system-of-record service layer with existing API contracts and authentication model. API architecture is accepted and must be preserved.

## Client Agent
Windows per-user background process architecture is retained. Agent uses LocalAppData-scoped storage, DPAPI CurrentUser for local secret protection, and single-instance runtime guarding. Pairing/auth/refresh rotation behavior remains established and out of scope for redesign.

## Frontend
Existing frontend architecture is already established and explicitly out of scope for current phase work (Phase 3A.6).

## Execution pipeline
Current pipeline is contract-driven and fail-closed:
1. Durable execution record created/updated
2. Opaque work directory prepared
3. Input staged atomically and verified by hash
4. Job runner invoked through abstraction (production runner remains “not configured”)
5. Output validation runs through typed validator and policy checks
6. Lifecycle/state updated deterministically for completion/recovery/failure handling

## Work directory
Work directories are opaque (`work_<32hex>`) under a controlled root (LocalAppData-oriented root data directory). Subdirectories are structured (`input`, `output`, `state`, `quarantine`) with path-security enforcement and atomic manifest update patterns.

## Execution state
Durable SQLite-backed execution state tracks correlation, execution ID, attempt number, state enum, and sanitized reason codes. State transitions are controlled and validated to enforce deterministic lifecycle behavior and recovery signaling.

## Restart recovery
On startup/recovery flow, recoverable work/state is discovered through constrained rules. Architecture supports resuming safe work and marking recovery-required conditions without unsafe assumptions.

## Security model
Defense-in-depth model includes:
- strict path containment
- reparse-point/junction defenses
- sanitized reason-code logging discipline
- fail-closed checks on unsafe/unknown filesystem conditions
- no unsafe process-launch automation or Office interop dependencies in production paths

---

# Current Guarantees

The system currently enforces the following guarantees:

- **Fail closed:** unknown/unsafe conditions default to safe rejection behavior.
- **Strong typing:** runtime decisions and outcomes are represented by typed contracts/enums.
- **Deterministic behavior:** execution-state transitions and policy checks avoid ambiguous outcomes.
- **Restart safety:** durable state + recoverable work discovery supports recovery.
- **Atomic staging:** staged input operations use atomic write/move semantics.
- **Opaque work directories:** per-execution directories are non-semantic opaque identifiers.
- **Hash verification:** integrity verification (SHA-256) is enforced in staging/validation paths.
- **Path containment:** target paths must remain under approved roots.
- **Reparse protection:** reparse-point/junction detection protects filesystem traversal boundaries.
- **Cleanup containment:** cleanup/discovery constrained to valid opaque work roots and safe paths.
- **No Process.Start:** prohibited from production architecture.
- **No Excel COM:** prohibited.
- **No Office Interop:** prohibited.

---

# Current Known Unknowns

The following are intentionally unverified at this stage and must remain treated as unknowns:

- NASCA CLI details
- Vendor API surface
- Vendor IPC/COM model (if any)
- Output schema specifics
- Output file naming conventions
- Output file extensions
- Exit code semantics
- Completion semantics / readiness signaling specifics
- Licensing/runtime packaging constraints
- Vendor process model and lifecycle behavior in production environments

These unknowns are **intentional** until vendor compatibility is formally proven and accepted by explicit phase scope.

---

# Current Roadmap

## Current
- **Phase 3A.6.6** — Validation Descriptor Persistence

## Next
- **Phase 3A.7** — Post-Validation Stabilization

## After that
- **Frontend F1**
- **Frontend F2**
- **Frontend F3**
- **Frontend F4**
- **Publish Website**
- **Phase 3B**
- **Frontend F5**

---

# Current Risks

## Technical risks
- Output-validation edge-case gaps may still exist until full Phase 3A.6 acceptance tests are complete.
- Unknown vendor output semantics may later require contract extension (must be additive and architecture-preserving).
- Strict fail-closed checks can surface operational friction if undocumented edge inputs appear.

## Vendor risks
- Vendor compatibility is not proven.
- Runtime behavior, output contracts, and error semantics remain unknown.
- Integration assumptions made too early could break deterministic guarantees.

## Operational risks
- Recovery/cleanup policies require continuous verification against real-world workload patterns.
- Misconfiguration risks remain if deployment/environment hardening deviates from documented constraints.
- Documentation drift can lead to unsafe implementation shortcuts by future contributors/agents.

---

# AI Agent Instructions

- Do **not** redesign existing architecture.
- Do **not** introduce vendor-specific assumptions.
- Do **not** use `Process.Start` or shell-launch workarounds.
- Do **not** introduce Excel COM / Office Interop.
- Read relevant documentation before coding.
- Reuse existing abstractions and DI composition.
- Keep production fail-closed.
- Keep `FakeNascaJobRunner` test-only.
- Preserve backward compatibility and accepted API/frontend architecture boundaries.

---

# Documents To Read First

- `AGENTS.md`
- `ARCHITECTURE_DECISIONS.md`
- `WORK-DIRECTORY.md`
- `EXECUTION-STATE.md`
- `LOCAL-STORAGE.md`
- `SECURITY.md`
- `NASCA-INTEGRATION.md`
- `QUEUE.md`
- `OUTPUT-VALIDATION.md`
- `CLIENT-AGENT-NASCA-INTEGRATION-ASSESSMENT.md`
- `CLIENT-AGENT-NASCA-INTEGRATION-RESULT.md`

> Note: In this repository, many of these documents exist under `docs/client-agent/` and `DevKit/reports/` naming conventions; preserve canonical references used by the team.

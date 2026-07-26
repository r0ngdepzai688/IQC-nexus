# ARCHITECTURE_DECISIONS.md

Stable Architecture Decision Records (ADRs) for IQC Nexus Portal.  
Primary audience: AI coding agents and maintainers who must preserve accepted architecture and avoid accidental redesign.

---

## ADR-001 — Interactive per-user Client Agent

**Status:** Accepted (Stable)  
**Context:** The client-side workload must run with user-scoped context and user-available resources in enterprise Windows environments.  
**Decision:** The Client Agent is an interactive per-user Windows background process, not a system-wide machine service.  
**Consequences:** User identity and user profile boundaries are preserved; deployment and operations must honor per-user runtime assumptions.  
**Future Review:** Revisit only if explicit product/security direction requires machine-level execution.

---

## ADR-002 — DPAPI CurrentUser

**Status:** Accepted (Stable)  
**Context:** Local secret material requires encryption bound to user identity without introducing custom crypto key management complexity.  
**Decision:** Use Windows DPAPI with `CurrentUser` scope for local credential/token protection.  
**Consequences:** Secret material remains tied to the user profile and cannot be trivially reused across accounts.  
**Future Review:** Revisit only with explicit cross-platform crypto strategy and migration plan.

---

## ADR-003 — LocalAppData storage

**Status:** Accepted (Stable)  
**Context:** Agent state/artifacts require local persistence with user-profile scoping and predictable filesystem semantics.  
**Decision:** Persist agent-local data under LocalAppData-controlled roots.  
**Consequences:** Data location is user-scoped, operationally predictable, and aligned with Windows profile boundaries.  
**Future Review:** Revisit only if storage policy or platform targets are formally changed.

---

## ADR-004 — Single-instance Client Agent

**Status:** Accepted (Stable)  
**Context:** Concurrent agent instances can cause race conditions in queues, staging, state transitions, and cleanup.  
**Decision:** Enforce single-instance runtime semantics for the Client Agent per user context.  
**Consequences:** Reduced concurrency hazards and more deterministic lifecycle behavior.  
**Future Review:** Revisit only with explicit multi-instance concurrency architecture and safety proof.

---

## ADR-005 — Fail-closed readiness model

**Status:** Accepted (Stable)  
**Context:** Vendor runtime behavior is not yet proven; permissive defaults would introduce unsafe production behavior.  
**Decision:** Readiness and execution gates fail closed. If preconditions are not explicitly satisfied, execution is rejected safely.  
**Consequences:** Production safety is prioritized over permissive operation; unknowns do not become implicit approvals.  
**Future Review:** Revisit only after verified vendor integration and explicit readiness criteria expansion.

---

## ADR-006 — Strongly typed runtime decisions

**Status:** Accepted (Stable)  
**Context:** Stringly-typed runtime decision logic is fragile and error-prone in complex execution workflows.  
**Decision:** Runtime decisions use strongly typed enums/contracts rather than ad-hoc string signaling.  
**Consequences:** Improved determinism, safer refactoring, and clearer validation boundaries.  
**Future Review:** Continue as default; extend types additively.

---

## ADR-007 — Strongly typed execution state

**Status:** Accepted (Stable)  
**Context:** Execution lifecycle requires predictable state progression and valid transition enforcement.  
**Decision:** Execution state model is strongly typed with explicit transitions and controlled updates.  
**Consequences:** Prevents ambiguous lifecycle behavior and supports robust recovery semantics.  
**Future Review:** Expand states only via explicit ADR-compatible transition rules.

---

## ADR-008 — Restart recovery model

**Status:** Accepted (Stable)  
**Context:** Agent or host restarts are unavoidable; in-flight execution cannot rely on volatile memory.  
**Decision:** Use durable state + recoverable work discovery to resume/contain execution after restart.  
**Consequences:** Improved operational resilience and deterministic post-restart behavior.  
**Future Review:** Revisit only with explicit durability model changes.

---

## ADR-009 — Opaque work directory identifiers

**Status:** Accepted (Stable)  
**Context:** Semantic directory naming can leak intent and increase manipulation risk.  
**Decision:** Work directories use opaque identifiers (`work_<opaque-id>` style), not semantic names.  
**Consequences:** Reduced information leakage and safer containment assumptions for cleanup/recovery.  
**Future Review:** Maintain opacity requirement unless explicit security model change is approved.

---

## ADR-010 — Atomic staging

**Status:** Accepted (Stable)  
**Context:** Partial writes and interrupted copy operations can corrupt staged input artifacts.  
**Decision:** Input staging uses atomic write/move semantics.  
**Consequences:** Staging integrity is preserved across interruptions and race conditions.  
**Future Review:** Keep atomic semantics mandatory for all staging paths.

---

## ADR-011 — SHA-256 verification

**Status:** Accepted (Stable)  
**Context:** Artifact integrity must be validated before downstream processing.  
**Decision:** Use SHA-256 verification for staging/output integrity workflows where required by current design.  
**Consequences:** Detects corruption/tampering and supports deterministic integrity checks.  
**Future Review:** Revisit algorithm only with explicit security policy changes.

---

## ADR-012 — Path containment enforcement

**Status:** Accepted (Stable)  
**Context:** Filesystem traversal and root escape risks are critical in agent-managed paths.  
**Decision:** Enforce strict root containment for all managed filesystem targets.  
**Consequences:** Prevents path escape and constrains operations to approved roots.  
**Future Review:** Containment remains non-negotiable; only implementation details may evolve safely.

---

## ADR-013 — Reparse-point prohibition

**Status:** Accepted (Stable)  
**Context:** Reparse points/junctions can bypass containment assumptions and introduce unsafe filesystem indirection.  
**Decision:** Reparse points are prohibited in sensitive managed paths and ancestry checks.  
**Consequences:** Hardens against symlink/junction path redirection attacks.  
**Future Review:** Maintain fail-closed behavior for uncertain filesystem conditions.

---

## ADR-014 — Cleanup containment

**Status:** Accepted (Stable)  
**Context:** Cleanup is high risk if directory discovery/deletion is not tightly bounded.  
**Decision:** Cleanup only applies to valid contained opaque work directories under strict security checks.  
**Consequences:** Prevents accidental/unsafe deletion outside intended work roots.  
**Future Review:** Expand cleanup scope only with explicit safety proof and test coverage.

---

## ADR-015 — Vendor-neutral architecture

**Status:** Accepted (Stable)  
**Context:** Vendor execution model and contracts are intentionally unverified.  
**Decision:** Architecture remains vendor-neutral until compatibility is proven.  
**Consequences:** Integration assumptions are deferred; execution design remains abstraction-driven and safe by default.  
**Future Review:**  
**Explicit rule:** Unknown vendor behavior must never be invented.

---

## ADR-016 — No Process.Start

**Status:** Accepted (Stable)  
**Context:** Direct process launching introduces unverified execution semantics and operational/security risks in current phase scope.  
**Decision:** `Process.Start` is prohibited in production runtime architecture for NASCA execution paths.  
**Consequences:** Prevents premature unsafe runtime coupling to unknown vendor process behavior.  
**Future Review:**  
**Explicit rule:** Production runtime must never launch NASCA until vendor behavior has been verified.

---

## ADR-017 — No Excel COM

**Status:** Accepted (Stable)  
**Context:** COM automation and Office interop increase fragility, dependency complexity, and operational risk.  
**Decision:** Excel COM automation is excluded from architecture.  
**Consequences:** Avoids Office runtime dependency and automation instability vectors.  
**Future Review:**  
**Explicit rule:** `Microsoft.Office.Interop.Excel` is intentionally excluded.

---

## ADR-018 — FakeNascaJobRunner isolation

**Status:** Accepted (Stable)  
**Context:** Test doubles are required for deterministic simulation but must never alter production behavior.  
**Decision:** `FakeNascaJobRunner` is allowed only in test assembly scope.  
**Consequences:** Test simulation remains available without contaminating production composition/DI.  
**Future Review:**  
**Explicit rule:** Test assembly only. Never production.

---

## ADR-019 — Execution identity model

**Status:** Accepted (Stable)  
**Context:** Distributed/restart-safe execution requires explicit identity dimensions to avoid ambiguity and support idempotent orchestration.  
**Decision:** Execution identity model includes and preserves:
- `QueueItemId`
- `CorrelationId`
- `ExecutionId`
- `AttemptNumber`
- `WorkDirectoryId`  
**Consequences:** Enables deterministic tracking, retries, recovery, and safe state transitions.  
**Future Review:** Add fields only with backward-compatible migration and explicit state/contract impact review.

---

## ADR-020 — Current roadmap

**Status:** Accepted (Living Stable Record)  
**Context:** Future work must follow accepted sequencing and avoid unauthorized phase jumps.  
**Decision:** Roadmap alignment is mandatory:
- **Current accepted phase baseline:** through Phase 3A.5 Security Closure
- **Current implementation target:** Phase 3A.6 (Vendor-Neutral Output Validation)
- **Future milestones:** Phase 3A.7 → Frontend F1 → Frontend F2 → Frontend F3 → Frontend F4 → Publish Website → Phase 3B → Frontend F5  
**Consequences:** Work remains scoped, reviewable, and architecture-consistent.  
**Future Review:** Update only through explicit planning/approval artifacts.

---

# Architecture Principles

All future changes must preserve these principles:

- **Fail closed** — unknown/unsafe conditions reject safely.
- **Deterministic** — predictable outcomes and controlled transitions.
- **Strong typing** — contracts/enums over stringly-typed behavior.
- **Security first** — containment, integrity, and least-assumption defaults.
- **Minimal assumptions** — never invent unknown vendor/runtime behavior.
- **Restart safety** — durable state and recoverable execution model.
- **Idempotency** — safe repeat behavior across retries/restarts.
- **Vendor neutrality** — no premature coupling to unverified vendor behavior.
- **Containment** — strict filesystem and lifecycle bounds.
- **Testability** — deterministic, isolated, architecture-aligned tests.
- **Backward compatibility** — preserve accepted architecture and contracts unless explicitly changed.

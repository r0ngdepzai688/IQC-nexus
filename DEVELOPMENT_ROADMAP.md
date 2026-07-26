# DEVELOPMENT_ROADMAP.md

Authoritative development roadmap for IQC Nexus Portal.  
Primary audience: AI coding agents and technical maintainers.

---

# Project Timeline

This timeline describes project progression from foundational architecture through production release preparation.  
The roadmap is sequential and architecture-constrained: each phase builds on accepted guarantees from prior phases, and no future phase should invalidate completed architectural decisions.

---

## Phase 2D.2 — Foundation Hardening

**Status:** ✅ Completed  
**Summary:**  
Established foundational implementation discipline for the enterprise baseline, including reliable boundaries between components and early operational safety practices. This phase focused on creating a stable base for later client-agent and import hardening work without introducing unstable assumptions.

**Dependencies:**  
- Initial repository/platform setup
- Baseline backend/client architecture direction

**Acceptance Criteria:**  
- Foundational architecture conventions accepted
- Core layering and service boundaries established
- Initial reliability/safety checks in place

**Deliverables:**  
- Stabilized base structure for backend and agent evolution
- Initial quality-gate alignment inputs

**Approximate complexity:** Medium

---

## Phase 2E — Platform Readiness Consolidation

**Status:** ✅ Completed  
**Summary:**  
Consolidated operational and architecture readiness after foundation work. This phase reinforced consistency in implementation and quality-gate expectations to reduce risk before deeper client-agent execution hardening.

**Dependencies:**  
- Phase 2D.2 completion

**Acceptance Criteria:**  
- Architecture consistency validated
- Delivery/quality process alignment accepted
- Readiness baseline documented

**Deliverables:**  
- Consolidated platform readiness posture
- Improved traceability into next execution-focused phases

**Approximate complexity:** Medium

---

## Phase 3A — Client-Agent Execution Baseline

**Status:** ✅ Completed  
**Summary:**  
Introduced the structured NASCA-oriented execution architecture under strict fail-closed defaults. Runtime decisions and orchestration boundaries were made strongly typed to prevent ambiguous behavior and to preserve deterministic execution semantics.

**Dependencies:**  
- Phase 2E completion

**Acceptance Criteria:**  
- Typed runtime decision model established
- Execution abstraction boundaries accepted
- Fail-closed baseline enforced

**Deliverables:**  
- Initial client-agent execution contracts and orchestration direction
- Stable basis for subsequent hardening sub-phases

**Approximate complexity:** High

---

## Phase 3A.1 — Execution Lifecycle Strengthening

**Status:** ✅ Completed  
**Summary:**  
Expanded lifecycle controls and transition discipline for execution handling. Focused on reducing ambiguous transitions and improving controlled retry/recovery behavior under deterministic state assumptions.

**Dependencies:**  
- Phase 3A completion

**Acceptance Criteria:**  
- Lifecycle transitions are explicit and controlled
- Retry/recovery behavior is predictable
- Transition integrity is testable

**Deliverables:**  
- Hardened lifecycle transition model
- Supporting validation coverage

**Approximate complexity:** Medium-High

---

## Phase 3A.2 — Durable Execution State

**Status:** ✅ Completed  
**Summary:**  
Implemented and accepted durable execution state handling, allowing in-progress work to survive interruptions. State persistence and transition reliability were hardened to support restart-safe processing.

**Dependencies:**  
- Phase 3A.1 completion

**Acceptance Criteria:**  
- Durable execution records persist reliably
- Transition operations are deterministic and safe
- Recovery-required states are representable

**Deliverables:**  
- Durable execution-state implementation
- Recovery-aware state management capabilities

**Approximate complexity:** High

---

## Phase 3A.3 — Secure Work Directory Model

**Status:** ✅ Completed  
**Summary:**  
Established secure, opaque work directory architecture with controlled lifecycle structure. This phase defined constrained workspace organization for input/output/state/quarantine while preserving containment assumptions.

**Dependencies:**  
- Phase 3A.2 completion

**Acceptance Criteria:**  
- Opaque work directory identifiers enforced
- Work directory layout standardized and safe
- Security checks integrated into work-path handling

**Deliverables:**  
- Work directory manager architecture and conventions
- Containment-oriented work-root controls

**Approximate complexity:** High

---

## Phase 3A.4 — Atomic Staging and Integrity Verification

**Status:** ✅ Completed  
**Summary:**  
Hardened ingestion/staging with atomic semantics and cryptographic verification. Prevented partial/corrupt staging outcomes and ensured integrity checks before workflow continuation.

**Dependencies:**  
- Phase 3A.3 completion

**Acceptance Criteria:**  
- Atomic staging semantics enforced
- SHA-256 integrity verification in critical paths
- Mismatch handling safely quarantines/contains risk

**Deliverables:**  
- Atomic staging behavior
- Integrity verification guarantees

**Approximate complexity:** High

---

## Phase 3A.5 — Pre-NASCA Hardening Completion

**Status:** ✅ Completed  
**Summary:**  
Completed major pre-vendor hardening for execution and artifact handling. Included durability, containment, and operational guardrails required prior to any vendor compatibility assumptions.

**Dependencies:**  
- Phase 3A.4 completion

**Acceptance Criteria:**  
- Pre-vendor hardening controls accepted
- Recovery/containment behavior validated
- Production-safe default operation confirmed

**Deliverables:**  
- Hardened pre-integration execution posture
- Expanded quality evidence for production readiness

**Approximate complexity:** High

---

## Phase 3A.5 Security Closure — Security Hardening Finalization

**Status:** ✅ Completed  
**Summary:**  
Closed remaining security concerns for Phase 3A.5 by formalizing strict path-security behavior and enforcing fail-closed defenses against unsafe filesystem conditions and execution shortcuts.

**Dependencies:**  
- Phase 3A.5 completion

**Acceptance Criteria:**  
- Reparse-point defenses active
- Path containment checks consistently enforced
- Unsafe execution mechanisms excluded from production architecture

**Deliverables:**  
- Security-closure acceptance artifacts
- Finalized fail-closed security envelope for current baseline

**Approximate complexity:** Medium-High

---

## Phase 3A.6.5 — Output Stability-Window Validation

**Status:** ✅ Completed
**Summary:**
Implemented vendor-neutral NASCA output stability-window validation using `TimeProvider`-based timing and continuous stability-window restart semantics. Revalidates reparse points, containment, and size/depth/count limits during every polling iteration using security-first BFS enumeration (`SearchOption.TopDirectoryOnly`).

**Dependencies:**
- Phase 3A.6.4 completion

**Acceptance Criteria:**
- Security-validated BFS snapshot polling enforced on every poll
- Continuous stability window resets upon file modification / size / timestamp change
- Bounded fake-time polling without real sleeps or background leaks
- Fail-closed revalidation of limits and reparse points during polling
- Test-only cancellation deadlock resolved via `Task.WhenAny` orchestration

**Deliverables:**
- Hardened stability polling implementation in `NascaOutputValidator.cs`
- Matrix of deterministic unit tests in `NascaOutputValidatorTests.cs` (97 passed)

**Approximate complexity:** Medium-High

---

## Phase 3A.6.6 — Validation Descriptor Persistence

**Status:** 🟨 Next
**Summary:**
Persist and retain validated descriptor metadata required for deterministic downstream execution state and auditability.

**Dependencies:**
- Phase 3A.6.5 completion

**Acceptance Criteria:**
- Descriptor list construction consistency and deterministic ordering
- Hash descriptor integrity alignment with durable state
- Fail-closed metadata handling

**Deliverables:**
- Descriptor persistence handling and tests

**Approximate complexity:** Medium

---

## Phase 3A.7 — Post-Validation Stabilization

**Status:** ⏳ Planned  
**Summary:**  
Planned stabilization and closure phase after vendor-neutral output validation, expected to consolidate remaining non-frontend readiness work and tighten operational confidence before frontend progression.

**Dependencies:**  
- Phase 3A.6 completion

**Acceptance Criteria:**  
- 3A-series remaining scope accepted
- No regression in fail-closed and deterministic guarantees
- Documentation/quality gates updated

**Deliverables:**  
- Stabilized post-3A baseline
- Final 3A-track readiness confirmation

**Approximate complexity:** Medium-High

---

## Frontend F1 — Frontend Foundation Increment 1

**Status:** ⏳ Planned  
**Summary:**  
Begins frontend-track milestones after backend/agent execution-track readiness is sufficiently stable. Intended to advance UI delivery without violating established backend/agent contracts.

**Dependencies:**  
- Phase 3A.7 completion

**Acceptance Criteria:**  
- F1 scope delivered against existing frontend architecture
- Backward compatibility with accepted APIs/contracts
- Quality gates passed for F1 targets

**Deliverables:**  
- Frontend F1 feature increment

**Approximate complexity:** Medium

---

## Frontend F2 — Frontend Foundation Increment 2

**Status:** ⏳ Planned  
**Summary:**  
Builds on F1 with additional UI/product capabilities and workflow maturity while preserving architecture and contract stability.

**Dependencies:**  
- Frontend F1 completion

**Acceptance Criteria:**  
- F2 scope complete and tested
- No contract-breaking changes
- UX/quality targets for F2 met

**Deliverables:**  
- Frontend F2 feature increment

**Approximate complexity:** Medium

---

## Frontend F3 — Frontend Feature Expansion 3

**Status:** ⏳ Planned  
**Summary:**  
Extends frontend functional surface and interaction maturity, continuing iterative delivery under established design/system constraints.

**Dependencies:**  
- Frontend F2 completion

**Acceptance Criteria:**  
- F3 scope accepted
- Stability and regression checks pass
- Documentation updated where needed

**Deliverables:**  
- Frontend F3 feature increment

**Approximate complexity:** Medium-High

---

## Frontend F4 — Frontend Feature Expansion 4

**Status:** ⏳ Planned  
**Summary:**  
Final major frontend increment before publish readiness, focused on completing high-priority UX/workflow targets.

**Dependencies:**  
- Frontend F3 completion

**Acceptance Criteria:**  
- F4 scope complete
- Publish-readiness blockers identified/resolved
- Quality and consistency checks pass

**Deliverables:**  
- Frontend F4 feature increment
- Publish readiness inputs

**Approximate complexity:** Medium-High

---

## Publish Website — Release Publishing Milestone

**Status:** ⏳ Planned  
**Summary:**  
Publishes the website/frontend deliverable after F1–F4 completion and readiness checks. This milestone formalizes deployment/publishing outcomes for the frontend track.

**Dependencies:**  
- Frontend F4 completion

**Acceptance Criteria:**  
- Publishing pipeline completed successfully
- Deployment checks pass
- Release documentation finalized

**Deliverables:**  
- Published website release artifact(s)

**Approximate complexity:** Medium

---

## Phase 3B — Next Major Platform Phase

**Status:** ⏳ Planned  
**Summary:**  
Represents the next major architecture/program phase after publish milestone, expected to continue platform evolution under accepted constraints and formal planning.

**Dependencies:**  
- Publish Website completion

**Acceptance Criteria:**  
- 3B scope definition approved
- Architecture compatibility maintained
- Entry criteria for 3B satisfied

**Deliverables:**  
- Phase 3B implementation outcomes (as defined by approved scope)

**Approximate complexity:** High

---

## Frontend F5 — Post-3B Frontend Increment

**Status:** ⏳ Planned  
**Summary:**  
Future frontend increment scheduled after Phase 3B, intended for subsequent feature or refinement delivery aligned to post-3B priorities.

**Dependencies:**  
- Phase 3B completion

**Acceptance Criteria:**  
- F5 scope approved and delivered
- Compatibility/quality gates pass

**Deliverables:**  
- Frontend F5 feature increment

**Approximate complexity:** Medium

---

# Dependency Graph

```text
Phase 2D.2
  -> Phase 2E
    -> Phase 3A
      -> Phase 3A.1
        -> Phase 3A.2
          -> Phase 3A.3
            -> Phase 3A.4
              -> Phase 3A.5
                -> Phase 3A.5 Security Closure
                  -> Phase 3A.6 (Current, In Progress)
                    -> Phase 3A.7
                      -> Frontend F1
                        -> Frontend F2
                          -> Frontend F3
                            -> Frontend F4
                              -> Publish Website
                                -> Phase 3B
                                  -> Frontend F5
```

---

# Milestone Summary

- **Foundation Milestones (Completed):** Phase 2D.2, 2E  
- **Execution Hardening Milestones (Completed):** Phase 3A through 3A.5 Security Closure  
- **Current Milestone (Active):** Phase 3A.6 Vendor-Neutral Output Validation  
- **Near-Term Milestone (Planned):** Phase 3A.7 stabilization  
- **Product Delivery Milestones (Planned):** Frontend F1–F4 + Publish Website  
- **Longer-Term Milestones (Planned):** Phase 3B + Frontend F5

---

# Current Project Completion Estimate

Based on phase count in this roadmap:
- Total phases/milestones listed: 18
- Completed: 9
- In Progress: 1
- Planned: 8
- Blocked: 0

**Estimated completion (phase-count view): ~50% complete**  
(Completed + In Progress = 10 / 18 milestones in active progression.)

---

# Current Remaining Work

1. Complete Phase 3A.6 acceptance (vendor-neutral output validation hardening and evidence).
2. Execute and accept Phase 3A.7 stabilization.
3. Deliver frontend sequence F1 → F4.
4. Publish website milestone.
5. Execute Phase 3B.
6. Deliver Frontend F5.

---

# Next Recommended Phase

**Immediate next recommended focus:**  
Continue and complete **Phase 3A.6 (Vendor-Neutral Output Validation)** before initiating any planned downstream phases.

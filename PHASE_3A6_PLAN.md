# Phase

## Phase 3A.6

Vendor-Neutral Output Validation

---

# Objective

Phase 3A.6 establishes a strict, typed, fail-closed validation envelope for NASCA output artifacts **without** introducing vendor-specific assumptions.

This phase focuses on:

- **Filesystem validation**: output root boundaries, traversal resistance, reparse-point safety, size/count/depth constraints, deterministic file enumeration.
- **Security validation**: containment, fail-closed handling, sanitized reason codes, quarantine-required classifications.
- **Output ownership validation**: correlation/work-directory identity checks and execution ownership consistency.

This phase explicitly does **not** include:

- vendor-specific behavior assumptions,
- business-data parsing/semantic interpretation,
- NASCA compatibility certification/proof.

---

# Scope

## Included

- Typed validation contracts and outcomes for output validation.
- Validation options and policy bounds for output checks.
- Output-root ownership and work-directory identity validation.
- Filesystem safety checks (containment, traversal, reparse defense, depth/count/size limits).
- Stability-window validation for output readiness.
- Descriptor generation/validation metadata handling (hash/file descriptors) as validation artifacts.
- Execution-state integration for validation outcomes (typed, deterministic mapping).
- Test expansion for validation behavior and failure paths.
- Documentation updates for Phase 3A.6 behavior and boundaries.

## Explicitly Excluded

- Any vendor-specific NASCA runtime assumptions.
- Business payload parsing/mapping semantics.
- New execution pipeline model or runtime redesign.
- Frontend work.
- Phase 3B work.
- Relaxation of security/fail-closed constraints.
- Use of `Process.Start`, Excel COM, Office Interop.
- Production use of `FakeNascaJobRunner`.

---

# Architectural Constraints

The following architecture constraints are mandatory and unchanged:

- Fail-closed behavior on unknown/unsafe conditions.
- Strongly typed runtime decisions and outcomes.
- Restart safety via durable state and recoverable work model.
- Local user-scoped storage under LocalAppData model.
- DPAPI `CurrentUser` model remains unchanged.
- Opaque work-directory model (`work_<opaque-id>`) remains unchanged.
- No `Process.Start` in production NASCA path.
- No Excel COM / `Microsoft.Office.Interop.Excel`.
- No vendor assumptions before compatibility proof.
- `FakeNascaJobRunner` remains test-assembly only.

---

# Milestone Breakdown

## 3A.6.1 — Validation contracts

### Purpose
Establish the public contract surface for Phase 3A.6 with typed outcomes/models/interfaces only.

### Scope
- Validation request/result models.
- Typed outcomes and reason-code contract normalization.
- Validator interface/API contract stabilization.
- DI-facing abstraction review (interfaces only).

### Existing classes to reuse
- `INascaOutputValidator`
- `NascaOutputValidationOutcome`
- `NascaOutputValidationRequest`
- `NascaOutputValidationResult`
- `NascaOutputValidationOptions`
- `NascaValidatedFileDescriptor`

### New classes expected
- None required by default (additive types only if gap is proven).

### Existing files expected to change
- `backend/src/IqcQms.ClientAgent.Application/Nasca/INascaOutputValidator.cs` (if additive contract refinement is required)

### New files expected
- None expected.

### Dependency injection impact
- Interface shape confirmation only; no runtime behavior change expected.

### Documentation impact
- Contract-level notes in Phase docs if contract surface changes additively.

### Tests expected
- Contract compile/compatibility checks (existing tests adjusted only if additive contract changes occur).

### Risks
- Contract drift if behavioral concerns are mixed into this milestone.
- Breaking changes if non-additive edits are made.

### Explicitly out of scope
- Validator behavior logic.
- Filesystem or execution-state behavior changes.
- DI runtime wiring changes beyond interface compatibility.

### Estimated commits
- 2–4

---

## 3A.6.2 — Configuration and validation options

### Purpose
Define/normalize validation option policy boundaries for deterministic and safe behavior.

### Scope
- Option bounds validation (count, size, depth, timeout windows, polling constraints).
- Fail-closed option validation behavior.
- Consistent sanitized policy violation codes.

### Existing classes to reuse
- `NascaOutputValidationOptions`
- `NascaOutputValidationResult`
- `NascaOutputValidationOutcome`

### New classes expected
- None expected; optional additive constants/options helper if required.

### Existing files expected to change
- `backend/src/IqcQms.ClientAgent.Application/Nasca/INascaOutputValidator.cs`
- `backend/src/IqcQms.ClientAgent.Infrastructure/Nasca/NascaOutputValidator.cs`

### New files expected
- None expected.

### Dependency injection impact
- No new DI registrations expected.

### Documentation impact
- Validation policy options docs update.

### Tests expected
- Option-boundary tests and invalid-option fail-closed tests.

### Risks
- Overly permissive defaults.
- Timeout/polling relationships becoming non-deterministic.

### Explicitly out of scope
- Ownership/containment filesystem logic.
- Execution-state integration.

### Estimated commits
- 2–5

---

## 3A.6.3 — Output-root identity and containment

### Purpose
Ensure output root belongs to the expected work directory/correlation and cannot escape approved boundaries.

### Scope
- Work-directory identity matching.
- Correlation ownership verification.
- Output-root containment checks against assigned work root.

### Existing classes to reuse
- `INascaWorkDirectoryManager`
- `NascaWorkManifest`
- `INascaPathSecurityGuard`
- `INascaOutputValidator`

### New classes expected
- None expected.

### Existing files expected to change
- `backend/src/IqcQms.ClientAgent.Infrastructure/Nasca/NascaOutputValidator.cs`

### New files expected
- None expected.

### Dependency injection impact
- No new registrations expected.

### Documentation impact
- Ownership/containment validation notes.

### Tests expected
- Correlation mismatch tests.
- Root escape/containment failure tests.

### Risks
- False positives from strict ownership checks if identity assumptions are inconsistent.
- Accidental coupling to non-contractual path semantics.

### Explicitly out of scope
- Deep file traversal safety rules.
- Stability timing logic.
- State transitions.

### Estimated commits
- 2–4

---

## 3A.6.4 — Directory traversal and file safety

### Purpose
Enforce directory/file-level safety checks within validated output roots.

### Scope
- Traversal resistance.
- Reparse-point detection handling.
- Directory depth/count limits.
- File count/single-file size/total size limits.
- Unexpected directory policy handling.

### Existing classes to reuse
- `INascaPathSecurityGuard`
- `NascaOutputValidationOptions`
- `NascaOutputValidationOutcome`
- `NascaOutputValidator`

### New classes expected
- None expected.

### Existing files expected to change
- `backend/src/IqcQms.ClientAgent.Infrastructure/Nasca/NascaOutputValidator.cs`

### New files expected
- None expected.

### Dependency injection impact
- None expected.

### Documentation impact
- Filesystem security behavior documentation updates.

### Tests expected
- Reparse/traversal tests.
- Depth/count/size threshold tests.
- Unexpected directory policy tests.

### Risks
- Platform filesystem edge-case handling.
- Excessive strictness causing operational noise.

### Explicitly out of scope
- Stability-window timing.
- Execution-state transitions.

### Estimated commits
- 3–5

---

## 3A.6.5 — Stability validation

### Purpose
Ensure output is stable before acceptance to avoid partial/in-flight capture.

### Scope
- Snapshot polling.
- Stability-window confirmation.
- Timeout/cancel handling.
- Retryable vs non-retryable typed outcomes.

### Existing classes to reuse
- `NascaOutputValidationOptions`
- `NascaOutputValidationResult`
- `NascaOutputValidator`
- `TimeProvider`

### New classes expected
- None expected.

### Existing files expected to change
- `backend/src/IqcQms.ClientAgent.Infrastructure/Nasca/NascaOutputValidator.cs`

### New files expected
- None expected.

### Dependency injection impact
- None expected (existing constructor/time abstraction retained).

### Documentation impact
- Stability behavior notes in client-agent docs.

### Tests expected
- Stability success/failure tests.
- Timeout and cancellation tests.
- Deterministic time-sensitive tests.

### Risks
- Flaky tests if time control is weak.
- Polling/performance balance issues.

### Explicitly out of scope
- Descriptor persistence and state-store updates.

### Estimated commits
- 2–5

---

## 3A.6.6 — Validation descriptor persistence

### Purpose
Persist/retain validated descriptor metadata required for deterministic downstream state and auditability.

### Scope
- Descriptor list construction consistency.
- Deterministic ordering/indexing.
- Hash descriptor integrity alignment.

### Existing classes to reuse
- `NascaValidatedFileDescriptor`
- `NascaOutputValidationResult`
- Existing execution/work manifest models where applicable

### New classes expected
- Optional additive descriptor record model only if required by current store contracts.

### Existing files expected to change
- `backend/src/IqcQms.ClientAgent.Infrastructure/Nasca/NascaOutputValidator.cs`
- Potentially execution-state/work-manifest related files if persistence target requires additive fields

### New files expected
- None expected by default.

### Dependency injection impact
- None expected.

### Documentation impact
- Descriptor persistence/retention behavior notes.

### Tests expected
- Deterministic descriptor ordering/hash tests.
- Duplicate output handling tests.

### Risks
- Accidental schema coupling.
- Backward compatibility pressure if non-additive persistence changes occur.

### Explicitly out of scope
- State-transition policy changes.

### Estimated commits
- 2–5

---

## 3A.6.7 — Execution-state integration

### Purpose
Map validation outcomes into durable execution-state transitions deterministically.

### Scope
- Outcome-to-state transition mapping.
- Sanitized reason-code propagation.
- Recovery-required and retry path consistency.
- Idempotent state updates for repeated evaluation scenarios.

### Existing classes to reuse
- `INascaExecutionStateStore`
- `NascaExecutionStateRecord`
- `NascaExecutionState`
- Validation result/outcome contracts

### New classes expected
- None expected by default.

### Existing files expected to change
- Orchestration files consuming validator outcomes (ClientAgent/Application/Infrastructure depending current composition)
- `backend/src/IqcQms.ClientAgent.Infrastructure/Nasca/SqliteNascaExecutionStateStore.cs` (only if additive support needed)

### New files expected
- None expected.

### Dependency injection impact
- No new DI service expected; composition usage may be updated.

### Documentation impact
- Execution-state flow docs update for validation outcomes.

### Tests expected
- Outcome->state mapping tests.
- Recovery-required tests.
- Idempotent transition tests.

### Risks
- State transition drift from accepted model.
- Reason-code inconsistency across failure paths.

### Explicitly out of scope
- New pipeline architecture.
- Queue architecture redesign.

### Estimated commits
- 2–5

---

## 3A.6.8 — Tests

### Purpose
Close Phase 3A.6 validation with explicit automated coverage.

### Scope
- Unit/integration tests for all validation outcomes and boundaries.
- Security and filesystem edge-case validation.
- Deterministic timing and cancellation coverage.
- Regression tests for known failure classifications.

### Existing classes to reuse
- Existing test infrastructure in `backend/tests/IqcQms.ClientAgent.Tests`
- `FakeNascaJobRunner` (test-only)

### New classes expected
- New test classes only.

### Existing files expected to change
- Existing test files for validator/state integration where extension is preferable.

### New files expected
- Additional test files for milestone-specific coverage matrix.

### Dependency injection impact
- Test composition only; no production DI impact.

### Documentation impact
- Test strategy and phase evidence update references.

### Tests expected
- This milestone is itself test-focused.

### Risks
- Incomplete edge-case matrix.
- Non-deterministic tests if time/filesystem isolation not strict.

### Explicitly out of scope
- Production feature expansion beyond 3A.6 acceptance.

### Estimated commits
- 3–5

---

## 3A.6.9 — Documentation

### Purpose
Finalize documentation and acceptance traceability for Phase 3A.6.

### Scope
- Update client-agent docs for output validation behavior and boundaries.
- Update roadmap/status references if phase status changes.
- Cross-document consistency pass across planning/security/testing guidance.

### Existing classes to reuse
- N/A (documentation milestone).

### New classes expected
- None.

### Existing files expected to change
- Relevant docs under `docs/client-agent/`
- Root governance/status docs if phase progression is approved

### New files expected
- Optional phase evidence/report document if required by process.

### Dependency injection impact
- None.

### Documentation impact
- Primary output of milestone.

### Tests expected
- Documentation completeness and consistency review.

### Risks
- Documentation drift from implementation.
- Incomplete acceptance evidence chain.

### Explicitly out of scope
- New code behavior.

### Estimated commits
- 2–4

---

# Dependency Graph

Dependency order is strictly sequential where noted:

- **3A.6.1** must complete before all others (contract baseline).
- **3A.6.2** depends on 3A.6.1.
- **3A.6.3** depends on 3A.6.1 and 3A.6.2.
- **3A.6.4** depends on 3A.6.3.
- **3A.6.5** depends on 3A.6.2 and 3A.6.4.
- **3A.6.6** depends on 3A.6.4 and 3A.6.5.
- **3A.6.7** depends on 3A.6.3, 3A.6.5, 3A.6.6.
- **3A.6.8** depends on 3A.6.2–3A.6.7 coverage targets.
- **3A.6.9** depends on 3A.6.1–3A.6.8 completion evidence.

No milestone may begin if its dependencies are incomplete.

---

# Repository Impact

## Projects expected to change
- `backend/src/IqcQms.ClientAgent.Application`
- `backend/src/IqcQms.ClientAgent.Infrastructure`
- `backend/src/IqcQms.ClientAgent` (only if orchestration/DI composition touchpoints require additive updates)
- `backend/tests/IqcQms.ClientAgent.Tests`
- `docs/client-agent/*` and relevant root governance docs for closure

## Namespaces expected to change
- `IqcQms.ClientAgent.Application.Nasca`
- `IqcQms.ClientAgent.Infrastructure.Nasca`
- Test namespaces under `IqcQms.ClientAgent.Tests`

## DI registrations expected to change
- Prefer no new service categories.
- Potential additive registration alignment for existing interfaces only if required by contract/behavior split.
- Production runner remains `NascaJobRunnerNotConfigured`.
- `FakeNascaJobRunner` remains test-only composition.

## Tests expected to change
- `NascaOutputValidatorTests`
- `NascaExecutionStateTests`
- `NascaWorkDirectoryTests`
- `DeterministicPathReparseTests`
- `QueueRecovery`/recovery-related tests where validation-state interplay exists
- New Phase 3A.6-focused test files as needed

## Documentation expected to change
- `docs/client-agent/NASCA-RUNTIME-READINESS.md`
- `docs/client-agent/WORK-DIRECTORY.md`
- `docs/client-agent/EXECUTION-STATE.md`
- `docs/client-agent/INPUT-PATH-SECURITY.md`
- status/roadmap docs if phase completion is approved

---

# Acceptance Criteria Mapping

| Phase 3A.6 Acceptance Criterion | Mapped Milestones |
|---|---|
| Typed validation contracts established | 3A.6.1 |
| Validation options and policy bounds defined | 3A.6.2 |
| Output ownership/correlation/workdir identity enforced | 3A.6.3 |
| Root containment enforced | 3A.6.3, 3A.6.4 |
| Traversal/reparse/file safety checks enforced | 3A.6.4 |
| Output stability verified before acceptance | 3A.6.5 |
| Descriptor/hash metadata finalized for validated output | 3A.6.6 |
| Validation outcome integrated with durable execution state | 3A.6.7 |
| Automated test coverage for happy/error/edge/security paths | 3A.6.8 |
| Documentation and traceability closure | 3A.6.9 |

No acceptance criterion is left unmapped.

---

# Risk Register

## Technical risks
- Contract/implementation drift across milestones.
- Determinism risks in stability-window behavior.
- Backward compatibility risks if non-additive model changes occur.

## Architecture risks
- Scope creep into pipeline redesign.
- Unintended DI/runtime behavior shifts.
- Violating strict milestone boundaries.

## Vendor risks
- Accidental vendor assumption leakage into validation logic.
- Premature compatibility claims without evidence.

## Operational risks
- False-positive validation failures causing operational friction.
- Incomplete reason-code normalization affecting triage.

## Testing risks
- Insufficient edge-case coverage (reparse/traversal/stability).
- Flaky time/filesystem tests reducing confidence.
- Incomplete regression tests for failure classes.

---

# Verification Plan

## 3A.6.1
- Build verification: Application + dependent projects compile.
- Test verification: contract and compile-focused tests pass.
- Documentation verification: contract surface docs updated if additive changes.
- Publish verification: N/A.

## 3A.6.2
- Build verification: compile with options validation changes.
- Test verification: boundary/invalid-option tests pass.
- Documentation verification: option policy docs aligned.
- Publish verification: N/A.

## 3A.6.3
- Build verification: compile with identity/containment checks.
- Test verification: ownership/containment failure matrix passes.
- Documentation verification: ownership/containment docs aligned.
- Publish verification: N/A.

## 3A.6.4
- Build verification: compile with filesystem safety checks.
- Test verification: traversal/reparse/depth/count/size tests pass.
- Documentation verification: filesystem safety docs aligned.
- Publish verification: N/A.

## 3A.6.5
- Build verification: compile with stability logic.
- Test verification: stability/timeout/cancel deterministic tests pass.
- Documentation verification: stability behavior documented.
- Publish verification: N/A.

## 3A.6.6
- Build verification: compile with descriptor persistence wiring.
- Test verification: descriptor/hash determinism tests pass.
- Documentation verification: descriptor behavior documented.
- Publish verification: N/A.

## 3A.6.7
- Build verification: compile with state integration.
- Test verification: outcome->state mapping tests pass.
- Documentation verification: execution-state flow docs updated.
- Publish verification: N/A.

## 3A.6.8
- Build verification: full solution build passes.
- Test verification: phase coverage suites pass.
- Documentation verification: test evidence references updated.
- Publish verification: N/A.

## 3A.6.9
- Build verification: no build regression from doc-only updates.
- Test verification: N/A beyond consistency checks.
- Documentation verification: cross-doc consistency complete.
- Publish verification: internal docs publication workflow if required.

---

# Completion Checklist

- [ ] 3A.6.1 Contracts
- [ ] 3A.6.2 Configuration
- [ ] 3A.6.3 Output-root identity and containment
- [ ] 3A.6.4 Directory traversal and file safety
- [ ] 3A.6.5 Stability validation
- [ ] 3A.6.6 Validation descriptor persistence
- [ ] 3A.6.7 Execution-state integration
- [ ] 3A.6.8 Tests
- [ ] 3A.6.9 Documentation

---

# Inconsistency Notes

1. Requested reference file `AGENTS.md` was not found at repository root; `.agents/AGENTS.md` exists and was treated as project-rule source.
2. This plan preserves accepted architecture documents as authoritative; no silent architectural changes are introduced.

---

"This implementation plan is considered frozen.

Milestone boundaries, responsibilities, and acceptance mapping must not be changed without explicit approval."

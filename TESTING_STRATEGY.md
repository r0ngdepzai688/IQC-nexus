# TESTING_STRATEGY.md

Repository-wide testing strategy for **IQC Nexus Portal**.  
Primary audience: engineers and AI coding agents.

---

## 1) Purpose and Scope

This document defines how tests should be designed, prioritized, and enforced across backend, client-agent, and frontend workstreams.  
Goals:

- protect accepted architecture,
- prevent regressions,
- preserve security guarantees,
- keep behavior deterministic and reviewable.

This strategy is architecture-aligned and intended to guide both incremental development and release-readiness validation.

---

## 2) Unit Testing Philosophy

Unit tests are the first line of defense and should validate behavior in small, isolated scopes.

Principles:

- Test contracts and decision logic, not incidental implementation detail.
- Prefer narrow tests with explicit setup and assertions.
- Keep tests deterministic and side-effect minimal.
- Validate fail-closed behavior for uncertain/unsafe conditions.
- Cover both expected and boundary/error outcomes.

Expectations:

- New behavior requires unit tests.
- Changed behavior requires updated tests.
- No feature is “done” if core decision paths are untested.

---

## 3) Integration Testing

Integration tests verify component collaboration, persistence, runtime composition, and cross-boundary correctness.

Focus areas:

- backend endpoint behavior and side effects,
- client-agent orchestration across state/store/work-directory abstractions,
- contract compatibility between modules.

Guidelines:

- Use integration tests for seams where unit tests cannot validate end-to-end contracts.
- Include failure-path and recovery-path scenarios, not only happy paths.
- Keep test environments reproducible and isolated.

---

## 4) Architecture Tests

Architecture tests ensure accepted design constraints are preserved over time.

What to protect:

- layering and project-boundary expectations,
- composition-root discipline,
- no introduction of prohibited runtime mechanisms,
- no test-only components leaking into production composition.

Architecture tests should fail fast when structural rules are violated, especially for critical client-agent constraints.

---

## 5) Security Tests

Security tests are mandatory for changes touching trust boundaries.

Coverage priorities:

- fail-closed behavior,
- auth/authorization boundary preservation,
- token/secret safety behaviors,
- path containment and reparse-point defenses,
- sanitized logging expectations.

Security tests must include negative scenarios to verify safe rejection behavior.

---

## 6) Filesystem Tests

Filesystem tests are required for work-directory and input/output safety workflows.

Validate:

- containment enforcement under approved roots,
- traversal rejection,
- reparse/junction protection,
- cleanup containment and non-escape behavior,
- atomic staging assumptions,
- safe handling under missing/corrupt/unexpected filesystem states.

Use temporary isolated directories and deterministic setup/teardown patterns.

---

## 7) Mocking Strategy

Mock only external dependencies and unstable boundaries; keep business rules real where possible.

Preferred mocking targets:

- filesystem adapters/guards,
- time abstraction (`TimeProvider`),
- external service boundaries,
- process/runner abstractions,
- network dependencies.

Guidelines:

- Avoid over-mocking internal logic.
- Keep mocks explicit and behaviorally meaningful.
- Use test doubles in the correct scope (test-only components remain test-only).

---

## 8) TimeProvider Usage

Time-dependent logic must be testable through `TimeProvider` abstraction.

Rules:

- Production logic should avoid hard-coded direct time calls where behavior depends on time.
- Tests should control time flow to avoid flaky timing assertions.
- Stability-window/timeout/retry logic must be verified with deterministic time control when feasible.

---

## 9) Deterministic Testing

Determinism is a repository-level requirement.

Practices:

- eliminate randomness unless explicitly controlled,
- avoid clock/race dependent assertions without controlled abstractions,
- avoid external mutable shared state,
- ensure tests pass consistently across repeated runs.

A flaky test is considered a defect and should be fixed or quarantined with explicit justification.

---

## 10) OS-Specific Tests

Some behaviors are platform-specific (notably Windows client-agent features).

Policy:

- OS-specific tests are allowed and expected where architecture requires it.
- Such tests must clearly declare platform assumptions.
- Skip conditions must be explicit and justified, not silent.
- Cross-platform test runs should still provide meaningful signal for non-OS-bound components.

---

## 11) Skipped Tests Policy

Skipped tests are exceptions, not normal practice.

Requirements for skipped tests:

- explicit reason,
- owner/context,
- expected reactivation condition,
- no permanent silent skipping of critical security/correctness coverage.

Critical-path tests (security/correctness/architecture) should not remain skipped without active remediation tracking.

---

## 12) Coverage Expectations

Coverage is a quality signal, not a substitute for thoughtful test design.

Expectations:

- Maintain strong coverage of decision-heavy and security-sensitive paths.
- Prioritize meaningful path coverage over superficial line counts.
- Ensure all new/changed code has proportionate test coverage.
- Preserve regression coverage for previously fixed defects.

Coverage targets should be interpreted with risk context (security-critical paths demand stronger assurance).

---

## 13) CI Expectations

Continuous Integration must provide fast and trustworthy feedback.

Baseline CI expectations:

- build validation,
- automated test execution (unit + selected integration),
- failure on test regressions,
- reproducible run behavior.

For risk-sensitive changes, CI should include expanded checks (security/architecture-focused suites) before merge/release.

---

## 14) Regression Prevention

Regression prevention is an explicit objective, not incidental.

Required practices:

- add a regression test for every discovered bug fix where practical,
- maintain backward compatibility behavior tests for stable contracts,
- preserve architecture/security test suites when refactoring,
- verify no accidental drift in fail-closed behavior.

---

## 15) Definition of Done (Testing)

A change is not done until all applicable items below are satisfied:

1. Scope-appropriate tests are added/updated.
2. Security-sensitive changes include negative/fail-closed tests.
3. Time-dependent behavior is deterministic/testable (using `TimeProvider` as needed).
4. Platform-specific behavior is validated or explicitly/justifiably skipped.
5. Existing relevant tests continue to pass (no regression introduced).
6. Documentation is updated when behavior/architecture expectations change.
7. Residual risks and known limitations are explicitly reported in delivery summary.

---

## 16) Practical Test Depth Guidance for AI Agents

When asked to “test before completion,” select depth by task type:

- **Docs-only changes:** validate requirement completeness, consistency, and cross-document alignment.
- **Backend/API changes:** run endpoint-focused and edge-case tests for impacted contracts.
- **Client-agent/security/filesystem changes:** run unit + integration + security/path tests for affected boundaries.
- **Broad refactors:** run expanded suites to protect architecture and regression surfaces.

If testing depth is unclear, explicitly ask whether to run critical-path only or thorough coverage.

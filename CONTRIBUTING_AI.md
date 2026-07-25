# CONTRIBUTING_AI.md

Guidance for AI coding agents contributing to **IQC Nexus Portal**.

Core rule set:

- Never assume.
- Never guess.
- Never silently redesign.

---

## 1) Purpose

This document defines how AI agents should operate in this repository to deliver safe, reviewable, architecture-consistent changes.

Primary goals:

- preserve accepted architecture,
- prevent security regressions,
- maintain backward compatibility,
- keep changes deterministic and test-backed.

---

## 2) How to Start Work

Before writing or editing any code:

1. Read high-priority context docs:
   - `AI_RULES.md`
   - `AI_CONTEXT.md`
   - `PROJECT_STATUS.md`
   - `ARCHITECTURE_DECISIONS.md`
   - `DEVELOPMENT_ROADMAP.md`
2. Confirm current target phase and scope.
3. Identify explicit constraints and prohibited approaches.
4. Verify whether task is code, documentation, tests, or mixed.

Do not begin implementation until scope and constraints are clear.

---

## 3) How to Inspect Repository

Inspection checklist:

1. Map relevant folders/projects first.
2. Locate existing abstractions/interfaces for the target area.
3. Locate composition roots and dependency wiring.
4. Locate current tests covering the same responsibility.
5. Locate relevant docs and quality gates.

Inspection principles:

- Reuse before creating.
- Extend before replacing.
- Respect layering and established boundaries.
- Do not infer architecture from a single file; validate across code + docs + tests.

---

## 4) How to Propose Architecture

Architecture proposals must be minimal and evidence-based.

Required approach:

1. State current architecture (as implemented today).
2. Identify exact gap causing the requested change.
3. Propose the smallest compatible extension.
4. Explain impact on:
   - security,
   - correctness,
   - backward compatibility,
   - testing,
   - operations.
5. Avoid parallel implementations when an existing abstraction can be extended.

Never propose broad redesign unless explicitly requested by maintainers.

---

## 5) How to Implement Safely

Implementation safety rules:

- Keep changes scoped and small.
- Preserve coding style and naming conventions.
- Preserve fail-closed behavior in production paths.
- Preserve deterministic behavior and typed decisions.
- Use `CancellationToken` in async workflows.
- Use `TimeProvider` for time-dependent logic.
- Avoid touching unrelated files.
- Never introduce prohibited technologies (e.g., `Process.Start`, Excel COM).

If task assumptions conflict with repository reality, stop and report before changing code.

---

## 6) How to Write Tests

Testing expectations:

- New behavior => new tests.
- Changed behavior => updated tests.
- Security-sensitive changes => include fail/negative-path tests.
- Time-dependent logic => deterministic tests using `TimeProvider`.
- Filesystem-sensitive logic => containment/reparse/edge-case tests.
- Keep tests deterministic, isolated, and explicit.

For docs-only tasks:

- test by validating requirement compliance, consistency, and cross-document alignment.

---

## 7) How to Update Docs

Update docs whenever behavior, architecture expectations, or workflows change.

Documentation rules:

- Be concise and technical.
- Do not duplicate deep implementation details already documented elsewhere.
- Keep governance docs consistent:
  - `PROJECT_STATUS.md`
  - `ARCHITECTURE_DECISIONS.md`
  - `DEVELOPMENT_ROADMAP.md`
  - security/testing context docs as needed

If docs and code disagree, call out the discrepancy explicitly.

---

## 8) How to Report Completion

Completion report should include:

1. What changed (files and responsibilities).
2. Why it changed (requirement mapping).
3. What was tested (and what was not).
4. Remaining risks/limitations.
5. Any deferred work and rationale.

Do not claim completion without explicitly describing test status and residual risk.

---

## 9) How to Report Uncertainty

When uncertain:

1. State what is known.
2. State what is unknown.
3. State why the unknown matters.
4. Propose safe options.
5. Ask for clarification before proceeding.

Never hide uncertainty.  
Never fabricate details to continue.

---

## 10) How to Stop When Assumptions Are Invalid

Stop immediately when:

- repository contents contradict task assumptions,
- required interfaces/contracts are missing or differ materially,
- requested change would violate hard constraints,
- security impact cannot be validated.

Required stop-report format:

1. **Observed mismatch**
2. **Why it blocks safe implementation**
3. **Potential options**
4. **Requested confirmation**

Do not proceed until confirmation is received.

---

## 11) Example Workflow (Reference)

### 1. Inspect
- Read core context documents.
- Inspect repository structure and relevant modules.
- Identify existing abstractions/tests/docs.

### 2. Plan
- Draft scoped implementation plan.
- Map changes to existing architecture (no redesign).
- List tests/doc updates required.

### 3. Implement
- Apply minimal, focused changes.
- Reuse existing services/contracts.
- Preserve fail-closed and backward-compatible behavior.

### 4. Test
- Execute appropriate depth (critical-path or thorough).
- Validate deterministic and security-relevant behavior.
- Capture gaps and residual risk.

### 5. Document
- Update affected docs and status references.
- Ensure consistency with architecture decisions and roadmap.

### 6. Review
- Self-check for architecture drift and unrelated modifications.
- Confirm prohibited patterns were not introduced.

### 7. Commit
- Keep commit scope small and clear.
- Avoid bundling unrelated edits.

### 8. Summarize
- Provide concise delivery report:
  - files changed,
  - tests run,
  - outcomes,
  - risks,
  - follow-ups.

---

## 12) Final Guardrails

- Never assume.
- Never guess.
- Never silently redesign.
- Security and correctness outrank convenience.
- If unsure: stop, report, and request direction.

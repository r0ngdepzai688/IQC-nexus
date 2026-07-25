# SECURITY_MODEL.md

Security architecture summary for **IQC Nexus Portal**.  
This document provides a repository-level security model for engineers and AI coding agents, without repeating low-level implementation details already covered in subsystem docs.

---

## 1) Threat Model

The system defends against both accidental and malicious failure modes across backend, client agent, and local execution/storage surfaces.

Primary threat categories:

- **Identity and session abuse**
  - Stolen or replayed tokens
  - Unauthorized device/user association attempts
- **Local secret exposure**
  - Credential/token disclosure from local storage
  - Cross-user secret access attempts
- **Filesystem boundary violations**
  - Path traversal
  - Root escape
  - Reparse-point/junction redirection
- **Execution workflow corruption**
  - Partial/unsafe staging
  - Output tampering
  - Inconsistent recovery behavior after crash/restart
- **Operational integrity risks**
  - Duplicate submissions
  - Non-idempotent retries
  - Unsafe cleanup deleting unintended paths
- **Information disclosure**
  - Sensitive values in logs
  - Path/data leakage beyond approved observability boundaries

Security posture is fail-closed: uncertain or unsafe states are rejected rather than implicitly permitted.

---

## 2) Authentication

Authentication remains an established subsystem and is not to be redesigned without explicit instruction.

Repository-level security expectations:

- Existing authentication contracts are authoritative.
- Credential/token trust boundaries are explicit and enforced by existing backend and client-agent flows.
- Device and user identity handling must remain aligned with accepted pairing and token lifecycle patterns.
- Any new work must preserve current auth behavior and backward compatibility.

---

## 3) Authorization

Authorization follows existing API and agent contract boundaries:

- Access is scoped to authenticated principals and approved contexts.
- Existing endpoint and workflow permissions are authoritative.
- Security-sensitive operations must remain gated by explicit checks, not inferred assumptions.
- No phase work may weaken current authorization model.

---

## 4) DPAPI

Local secret protection relies on Windows DPAPI with **CurrentUser** scope.

Security implications:

- Secret material is user-context bound.
- Cross-user portability of protected values is intentionally constrained.
- Local confidentiality is anchored to Windows user profile protections.

This is a stable architecture decision and must be preserved unless explicitly re-architected.

---

## 5) Secrets

Secret-management principles across repository scope:

- Persist only what is required for operation.
- Encrypt local secret material using established user-scoped protection mechanisms.
- Avoid plaintext secret persistence.
- Keep secret exposure out of logs, diagnostics, and non-secure artifacts.
- Do not invent alternative secret stores without approval.

---

## 6) Token Handling

Token handling is governed by accepted auth architecture.

Repository-level requirements:

- Preserve existing token lifecycle and refresh-rotation behavior.
- Maintain anti-replay and concurrency-safe semantics already established.
- Never log raw token values.
- Treat token-bearing artifacts as sensitive at all times.
- Do not alter token contracts outside explicitly scoped security work.

---

## 7) AllowedInputRoots

Input-origin restrictions are a core security boundary.

Model expectations:

- Inputs are constrained to approved roots.
- Root approval is explicit, not implicit.
- Paths outside allowed roots are rejected.
- This boundary is part of overall ingestion and local filesystem safety posture.

AllowedInputRoots behavior must remain unchanged unless explicitly directed.

---

## 8) Filesystem Containment

Filesystem operations are containment-first:

- All managed paths must resolve under approved roots.
- Boundary validation is mandatory for staging, output processing, and cleanup operations.
- Any path that escapes approved scope must be rejected.
- Containment checks are part of the default safety model, not optional guards.

---

## 9) Reparse-Point Defense

Reparse points/junctions are treated as high-risk redirection vectors.

Security model requirements:

- Reject unsafe path chains that include reparse-point redirection in sensitive operations.
- Fail closed when filesystem certainty cannot be established.
- Preserve strict reparse-point prohibition where currently enforced.

---

## 10) Cleanup Containment

Cleanup routines are security-sensitive destructive operations and must be constrained.

Security expectations:

- Cleanup scope is limited to approved managed workspaces.
- Candidate validation must prevent deletion outside bounded roots.
- Nonconforming/unsafe paths are skipped or quarantined per existing policies.
- Cleanup must never become an implicit recursive delete across unvalidated locations.

---

## 11) Logging Policy

Logging is required for observability but constrained for confidentiality and safety.

Policy principles:

- Log sanitized reason codes and operationally relevant metadata.
- Do not log secrets, tokens, sensitive payload content, or unnecessary local path details.
- Avoid treating raw exceptions as business-state contracts.
- Keep logs useful for incident/recovery workflows without exposing protected data.

---

## 12) Hash Verification

Integrity verification is part of the trust model for staged and validated artifacts.

Model requirements:

- Use cryptographic hashing (SHA-256 in accepted architecture) for integrity-critical flow points.
- Detect mismatch/corruption before progressing workflow state.
- Treat integrity mismatch as security-relevant failure requiring safe containment behavior.

---

## 13) Restart Safety

Crash/restart scenarios are expected and included in security architecture.

Security-relevant guarantees:

- Durable state supports deterministic restart behavior.
- Recovery workflow avoids ambiguous in-memory assumptions.
- Interrupted execution is resumed/contained based on persisted trusted state, not guesswork.

---

## 14) Idempotency

Idempotency protects correctness and security in distributed/retry scenarios.

Repository-level expectations:

- Duplicate operations (including submission paths) must not produce inconsistent outcomes.
- Retry semantics must remain deterministic and bounded.
- Idempotency controls are part of anti-corruption and anti-duplication security posture.

---

## 15) Known Assumptions

Current security model assumes:

- Windows user-profile security properties for DPAPI CurrentUser use-cases.
- Existing auth/pairing/token architecture remains authoritative.
- Managed filesystem roots remain correctly configured and enforced.
- Fail-closed default behavior is preserved in future changes.
- Quality gates and tests remain part of release validation process.

---

## 16) Known Limitations

Intentional current limitations include:

- Vendor runtime behavior is not fully verified; architecture remains vendor-neutral.
- Some external integration semantics remain unknown until formal compatibility evidence is complete.
- Security posture favors conservative rejection, which can increase operational friction in ambiguous edge cases.
- Documentation references across repository may evolve; consistency must be actively maintained.

---

## 17) Future Security Work

Planned/expected security evolution areas:

- Complete Phase 3A.6 security-aligned output-validation acceptance.
- Continue hardening around vendor-neutral integration boundaries.
- Expand evidence-driven validation for unknown vendor semantics before any production runtime enablement.
- Maintain and evolve security tests for containment, idempotency, restart safety, and logging redaction.
- Keep architecture decisions and security docs synchronized as roadmap phases progress.

---

## Cross-Reference Guidance

For subsystem-specific details, use established source documents (examples):

- `docs/client-agent/SECURITY.md`
- `docs/client-agent/INPUT-PATH-SECURITY.md`
- `docs/client-agent/WORK-DIRECTORY.md`
- `docs/client-agent/EXECUTION-STATE.md`
- `docs/client-agent/NASCA-INTEGRATION.md`
- `docs/security/AUTHENTICATION-AND-AUTHORIZATION.md`
- `ARCHITECTURE_DECISIONS.md`
- `PROJECT_STATUS.md`

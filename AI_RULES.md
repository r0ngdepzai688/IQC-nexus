# AI_RULES.md

Repository-wide operating rules for AI coding agents in **IQC Nexus Portal**.

---

## 1) Core Mission

- Preserve accepted architecture.
- Extend safely and incrementally.
- Keep production behavior secure, deterministic, and backward compatible.
- Avoid speculative redesign or vendor assumptions.

---

## 2) Non-Negotiable Prohibitions

- Never redesign architecture unless explicitly instructed.
- Never invent vendor behavior.
- Never introduce `Process.Start`.
- Never introduce shell execution workarounds for vendor runtime launch.
- Never introduce Excel COM.
- Never introduce `Microsoft.Office.Interop.Excel`.
- Never replace existing abstractions with parallel implementations.
- Never rewrite unrelated code to “modernize” or “clean up” outside scope.
- Never introduce fake production behavior/data paths.
- Never silently alter security boundaries, auth flows, pairing, refresh rotation, queue model, or API contracts.

---

## 3) Required Engineering Behavior

- Always inspect repository structure before modifying.
- Always inspect relevant implementation before modifying.
- Always inspect relevant documentation before modifying.
- Always reuse existing abstractions and extension points where possible.
- Always preserve backward compatibility.
- Always keep production fail-closed.
- Always prefer deterministic behavior.
- Always thread `CancellationToken` through asynchronous operations.
- Always use `TimeProvider` for time-dependent logic.
- Always keep changes small and reviewable.
- Always report risks and known unknowns explicitly.
- Always avoid assumptions when uncertainty exists; stop and request clarification.

---

## 4) Modification Discipline

Before editing:
1. Identify exact scope and affected files.
2. Confirm architecture constraints for the target area.
3. Confirm whether related tests/docs must be updated.

During editing:
1. Make minimal, focused changes.
2. Preserve coding style and existing patterns.
3. Avoid introducing parallel services when an existing one can be extended.

After editing:
1. Update/add tests for changed behavior.
2. Update documentation impacted by the change.
3. Validate no unintended architectural drift.
4. Summarize what changed, why, and residual risks.

---

## 5) Testing Rules

- Always update tests when behavior changes.
- Prefer deterministic tests over timing-fragile behavior.
- Preserve security and fail-closed coverage.
- Keep test-only doubles isolated from production composition.
- For docs-only tasks, validate requirement compliance and cross-document consistency.

---

## 6) Documentation Rules

- Always update documentation for architecture-affecting changes.
- Keep docs concise, technical, and machine-usable.
- Ensure project status, architecture decisions, and roadmap remain aligned.
- Do not leave undocumented behavior changes.

---

## 7) Commit and Change Management

- Keep commits small and scoped.
- Do not combine unrelated work in one change.
- Preserve backward compatibility and accepted contracts.
- Explicitly call out operational, security, or vendor risks in summaries.

---

## 8) Security and Reliability Defaults

- Fail closed on uncertain or unsafe conditions.
- Maintain strict containment assumptions in filesystem operations.
- Preserve reparse-point defenses and path validation posture.
- Do not weaken sanitized logging standards.
- Do not trade security/correctness for convenience.

---

## 9) Decision Hierarchy

When tradeoffs are required, apply this order:

1. **Security**  
2. **Correctness**  
3. **Architecture**  
4. **Backward compatibility**  
5. **Performance**  
6. **Convenience**

# Risk Register

| Risk | Impact | Mitigation | Verification |
| --- | --- | --- | --- |
| Numeric Excel values interpreted as dates | Silent data corruption | Preserve raw numeric values; date interpretation only downstream | provider tests |
| Raw NASCA upload reaches server | Security boundary breach | No NASCA file provider; normalized-payload-only contract | route and scan tests |
| Preview bypass | Unreviewed commit | Persist/version preview acknowledgement | lifecycle tests |
| Partial or replayed commit | Inconsistent/duplicate records | EF transaction plus idempotency key | rollback/replay tests |
| Cross-user import access | Data disclosure or mutation | stable user ID ownership and explicit admin permission | integration tests |
| Cell values in diagnostics | Confidentiality breach | structured sanitized diagnostics and log tests | sink assertions/scans |
| Existing auth bypasses | Privilege escalation | fallback policy, secure endpoints, remove UI override | 401/403 matrix |
| Duplicate domain entities | Migration and behavior conflict | adapt existing Data Hub model before migration | model review |
| Decorative UI obscures operations | Poor usability | industrial tokens, compact hierarchy, state matrix | UI quality gate |
| Local confidential artifacts enter changes | Data leak | opaque boundary and pre-commit status scan | Git checks |


## Verification-pass residual risks

- Provider-neutral mapping, validation, audit, and API orchestration are contracts
  only; do not represent them as production-complete.
- Durable job ownership, preview attestations, and idempotency receipts still
  require an additive persistence design review.
- Legacy DataHub error responses and audit value handling require a separate
  security pass before broad provider-neutral exposure.
- Formula text is unavailable from the current safe XLSX reader; cached values
  are retained with an explicit diagnostic.
- Excluded local artifacts are untracked and ignored; status scans remain required
  before staging.

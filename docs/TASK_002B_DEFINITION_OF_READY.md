# Task 002B Definition of Ready

## Task

- Roadmap: Task 002 / E01-T02
- Slice: 002B — Replace PII-dependent fixtures with synthetic fixtures
- Status: NOT READY

## Objective

Replace fixtures and test data that depend on real personnel information with canonical synthetic data, without changing production behavior.

## Entry conditions

- [x] Task 001 complete.
- [x] Task 002A containment complete.
- [x] Root personnel artifacts are no longer tracked.
- [x] Consumer inventory complete.
- [x] Canonical synthetic schema established.
- [x] Edge-case matrix established.
- [x] Runtime consumers no longer depend on `users.json`.
- [ ] Data Owner or Security approval recorded.

## Canonical synthetic schema

- Employee IDs use the `SYN-` prefix.
- Email addresses use the reserved `example.invalid` domain.
- Personnel references use employee ID, not display name.
- Runtime fixtures contain no intentional duplicate or invalid records.
- Unicode, optional-field and account-status coverage is documented separately.

## Discovery scope

- `frontend/src/data/**`
- `frontend/src/**/*fixture*`
- `frontend/src/**/*mock*`
- `frontend/src/**/*.test.*`
- `backend/**/Seeders/**`
- `backend/**/*Fixture*`
- `backend/**/*Mock*`
- `backend/**/*Test*`
- References to personnel, employee, staff and `users.json`

## Scope note

The branch also contains pre-existing compilation fixes in Auth/RBAC typing, ThemeProvider, click-outside hook and New Models UI. These changes are retained because reverting them would restore a failing frontend build. They must be reviewed as explicit scope exceptions in the pull request.

## Definition of Done

- [x] Consumer inventory complete.
- [x] Runtime fixture no longer depends on known personnel data.
- [x] Synthetic fixture follows the canonical schema.
- [x] Automated checks reject known legacy IDs, names and email domains.
- [x] Runtime `users.json` dependency removed.
- [x] Data inventory updated.
- [x] Task 002C has not been started.
- [x] Frontend build passes on the final branch.
- [x] Backend build passes when a backend build script is available.
- [x] Browser smoke test completed.
- [ ] Data Owner or Security approver confirmation recorded.

## Repository-local implementation status

- A repository-level UTF-8 fixture is validated independently and by frontend/backend adapters.
- Backend startup validates the fixture before migration; validation failure prevents startup.
- Missing `IQC_SYNTHETIC_USER_SEED_PASSWORD` permits migration after validation but skips personnel mutation.
- No API contract, database schema, committed credential, real-artifact content or Git history was changed.
- Repository-owner, Data Owner and Security decisions in `TASK_002B_APPROVAL_CHECKLIST.md` remain unresolved.

## Readiness decision

NOT READY. Repository-local technical validation does not resolve the approval, secure-store, retention, access/encryption or Git-history gates. Browser smoke testing and required human approvals remain outstanding.

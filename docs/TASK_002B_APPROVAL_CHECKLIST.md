# Task 002B — Repository Owner Approval Checklist

## Status and use

- **Task status:** NOT READY.
- This checklist converts unresolved Task 002B decisions into explicit approval choices. It does not authorize implementation by itself.
- The repository owner should select one option per item, complete the approval statement, and obtain the additional Data Owner or Security approvals identified below.
- Names and dates are intentionally blank; the repository does not establish who holds those roles.

## Repository-owner technical decisions

### 1. Canonical fixture location and format

- **Current evidence:** frontend personnel data is a typed TypeScript array in `frontend/src/lib/mock-data/personnelMock.ts`; backend has a different JSON shape in `backend/src/IqcQms.Infrastructure/Data/Seeders/users_seed.json`; backend runtime currently reads `User_DB.xlsx` instead of that JSON.
- **A — Recommended:** one repository-level UTF-8 JSON fixture, for example `fixtures/personnel.synthetic.json`, containing the approved logical schema; consumer-specific adapters own shape conversion. **Risk:** introduces a new shared fixture path and requires validation tooling.
- **B:** retain TypeScript as canonical and generate backend data from it. **Risk:** couples backend preparation to the frontend/Node toolchain.
- **C:** retain separate frontend and backend fixtures with equivalence checks. **Risk:** duplication and drift remain possible.
- **Approval statement:** “I approve option ___ as the canonical synthetic personnel fixture location/format, subject to Data Owner and Security approval. Repository owner: __________. Date: __________.”

### 2. Frontend-to-backend adapter ownership

- **Current evidence:** `usersService.ts` already adapts `PersonnelRecord` for frontend consumers; `UserSeeder.SyncUsersFromExcelAsync` currently owns backend ingestion and entity defaults; no shared adapter exists.
- **A — Recommended:** each consumer owns its adapter: frontend mapping under the frontend data layer and backend mapping under backend seeding infrastructure, both validated against the shared fixture contract. **Risk:** two adapters must remain contract-tested.
- **B:** one generator produces both consumer formats. **Risk:** creates a cross-stack build/tooling dependency and a new generated-artifact lifecycle.
- **C:** backend exposes the sole runtime contract and frontend fetches it. **Risk:** changes runtime behavior and exceeds Task 002B scope.
- **Approval statement:** “I approve option ___ for adapter ownership and require contract validation before either consumer accepts fixture data. Repository owner: __________. Date: __________.”

### 3. Identifier normalization and case sensitivity

- **Current evidence:** proposed IDs use uppercase `SYN-`; frontend `Map` lookup is case-sensitive and only strips a leading `@`; backend dictionary lookup uses `Username`, but datastore collation/case behavior has not been established by repository evidence.
- **A — Recommended:** require canonical ASCII uppercase IDs matching `^SYN-[A-Z0-9-]+$`, trim surrounding whitespace at validation, compare IDs ordinal case-sensitively, and reject non-canonical case. **Risk:** previously tolerated lowercase input will fail explicitly.
- **B:** normalize IDs to uppercase before comparison. **Risk:** can collapse distinct malformed inputs and conceal producer errors.
- **C:** preserve input and use datastore-default comparison. **Risk:** behavior may differ across frontend, SQLite and future databases.
- **Approval statement:** “I approve option ___ for identifier normalization and comparison. Any datastore mismatch must fail validation rather than silently alter identity. Repository owner: __________. Date: __________.”

### 4. Null versus empty-string conversion

- **Current evidence:** the proposed canonical contract uses null for optional values; TypeScript uses an optional `preferredName`; many `User` string properties are non-nullable and default to empty strings; API projections contain empty-value display fallbacks.
- **A — Recommended:** canonical optional values use null; frontend converts to `undefined` only where required; backend converts null to empty string only at existing non-nullable persistence boundaries; required whitespace/empty values are rejected. **Risk:** adapters must distinguish semantic absence from compatibility conversion.
- **B:** use empty strings everywhere. **Risk:** loses the distinction between absent and intentionally empty values.
- **C:** use null everywhere. **Risk:** conflicts with existing non-nullable entity contracts and would require out-of-scope schema changes.
- **Approval statement:** “I approve option ___ for null/empty handling and prohibit silent fallback for missing required fixture values. Repository owner: __________. Date: __________.”

### 5. Duplicate handling and atomic seeding

- **Current evidence:** current Excel seeding silently skips duplicate employee IDs and updates existing users incrementally; the decision document proposes rejecting duplicate `employeeId` and non-null `knoxId`; no transaction/atomic validation behavior is established for the replacement.
- **A — Recommended:** validate the entire fixture before mutation; reject any duplicate or invalid record; seed in one transaction so failure writes nothing. **Risk:** one bad record blocks the whole seed, requiring correction before progress.
- **B:** skip invalid/duplicate records and seed the rest. **Risk:** partial, environment-dependent datasets and hidden errors.
- **C:** deterministic last-record-wins upsert. **Risk:** duplicate identity can overwrite data silently.
- **Approval statement:** “I approve option ___ for duplicate handling. For option A, fixture validation and seeding must be atomic and produce no partial writes. Repository owner: __________. Date: __________.”

### 6. Synthetic credential source and environment policy

- **Current evidence:** current seeding hashes a committed static default password; the proposed fixture excludes plaintext credentials; no approved secure-store owner or environment policy is recorded.
- **A — Recommended:** inject an ephemeral development/test secret from an approved local/CI secret mechanism, hash it at seed time, never log it, and disable synthetic credential seeding in production-like environments unless explicitly authorized. **Risk:** local/CI secret provisioning must be maintained.
- **B:** generate random credentials per seed and expose them through a controlled one-time channel. **Risk:** adds credential distribution and recovery complexity.
- **C:** commit a shared synthetic password or hash. **Risk:** reusable known credentials and accidental promotion to less controlled environments; not recommended.
- **Approval statement:** “I approve option ___ for synthetic credentials and confirm that production-like seeding is [disabled / explicitly authorized under control __________]. Repository owner: __________. Date: __________. Security approval reference: __________.”

### 7. Default system role

- **Current evidence:** `Administrator` and `User` are observed values; `User.SystemRole` defaults to `User`; current seeding assigns one administrator using a real identifier-specific condition.
- **A — Recommended:** default every synthetic record to `User`; any synthetic administrator must be explicit, minimal, opt-in and separately controlled. **Risk:** administrator-dependent tests require deliberate setup.
- **B:** include one administrator in every fixture. **Risk:** elevated access becomes ubiquitous across environments.
- **C:** omit the role and rely on entity defaults. **Risk:** behavior becomes implicit and adapter-dependent.
- **Approval statement:** “I approve option ___ for the default system role. No administrator assignment may depend on a real identifier. Repository owner: __________. Date: __________.”

### 8. Account-status and `isActive` consistency

- **Current evidence:** frontend observes `Active`, `Inactive`, `Pending` and `Locked`; frontend personnel fixtures currently use only `Active`/`Inactive`; backend stores both `AccountStatus` and `IsActive`, but no invariant between them is enforced.
- **A — Recommended:** `isActive` is true only when `accountStatus` is `Active`; it is false for `Inactive`, `Pending` and `Locked`; contradictory input is rejected. **Risk:** this establishes a new fixture invariant that must be approved and tested.
- **B:** map only `Inactive` to false and allow `Pending`/`Locked` to remain true. **Risk:** authentication eligibility becomes ambiguous.
- **C:** treat `isActive` and `accountStatus` as independent. **Risk:** contradictory security state remains possible.
- **Approval statement:** “I approve option ___ as the fixture consistency rule for `accountStatus` and `isActive`; contradictory records must be handled as specified by that option. Repository owner: __________. Date: __________. Security approval reference: __________.”

### 9. `RoleProfile` without an observed vocabulary

- **Current evidence:** `User.RoleProfile` and frontend auth state exist, but no enum, constant or repository-backed vocabulary containing `Standard` was found.
- **A — Recommended:** keep `roleProfile` optional/null in the canonical fixture and convert absence only at current compatibility boundaries; do not introduce `Standard`. **Risk:** consumers requiring a meaningful profile must define it later.
- **B:** approve `Standard` as a new synthetic fixture value. **Risk:** creates vocabulary without current business/domain evidence.
- **C:** require the repository owner to supply an approved vocabulary before implementation. **Risk:** blocks Task 002B longer but avoids unsupported semantics.
- **Approval statement:** “I approve option ___ for `RoleProfile`. If option B or C is selected, the approved vocabulary and authority are: __________. Repository owner: __________. Date: __________.”

## Organizational and release decisions

### 10. Data Owner

- **Current evidence:** repository documentation marks the Data Owner as unresolved; no person or accountable role is named.
- **A — Recommended:** designate a named accountable Data Owner who approves schema, replacement scope, retention and disposal before implementation/release approval. **Risk:** work remains blocked until designation and sign-off.
- **B:** designate an accountable organizational role first, then record the current role holder before approval. **Risk:** ambiguity remains until a person accepts accountability.
- **C:** defer designation. **Risk:** Task 002B remains NOT READY and cannot receive data-governance approval.
- **Approval statement:** “The accountable Data Owner for Task 002B is __________, acting as __________. They are authorized to approve synthetic replacement scope, retention and disposal. Repository owner: __________. Date: __________.”

### 11. Security approver

- **Current evidence:** Security approval is an explicit unresolved release gate; no approver is named.
- **A — Recommended:** designate a named Security approver for credentials, access/encryption and Git-history disposition. **Risk:** approval is blocked until review completes.
- **B:** route approval through an established security review board/process and record its decision reference. **Risk:** scheduling/process delay.
- **C:** defer Security approval. **Risk:** Task 002B remains NOT READY and release remains blocked.
- **Approval statement:** “Security approval authority is __________, acting through process/reference __________. This authority will approve credentials, access/encryption and Git-history disposition. Repository owner: __________. Date: __________.”

### 12. Secure-store owner

- **Current evidence:** root PII artifacts remain local pending disposition; no approved secure-store location or owner is recorded.
- **A — Recommended:** name one accountable secure-store owner and approved organizational storage location before any move/backup. **Risk:** transfer cannot proceed until access and integrity controls are verified.
- **B:** assign responsibility to the Data Owner with an explicitly approved store. **Risk:** concentrates operational and governance duties.
- **C:** retain local-only custody temporarily. **Risk:** unmanaged loss, access and retention exposure; release remains blocked.
- **Approval statement:** “The secure-store owner is __________ and the approved store/control reference is __________. Transfer verification and access ownership are accepted by __________. Date: __________.”

### 13. Retention and disposal policy

- **Current evidence:** PII artifacts are untracked but retained locally; duration and disposal procedure are unresolved.
- **A — Recommended:** Data Owner specifies a minimum necessary retention period, legal/business basis, secure deletion trigger, responsible operator and evidence of disposal. **Risk:** requires organizational policy input before action.
- **B:** transfer one approved archival copy, verify integrity, then securely remove working copies on an approved date. **Risk:** archive remains sensitive and requires continuing controls.
- **C:** retain indefinitely or defer. **Risk:** unnecessary exposure and unresolved compliance obligations; release remains blocked.
- **Approval statement:** “Approved retention basis/period: __________. Approved disposal trigger/method: __________. Responsible owner/operator: __________. Data Owner approval/date: __________.”

### 14. Encryption and access controls

- **Current evidence:** documentation requires least privilege and encryption, but no control baseline, approved store or owner is recorded.
- **A — Recommended:** require organization-approved encryption at rest and in transit, named least-privilege access group, access review cadence, audit logging and recovery controls. **Risk:** operational setup and recurring review overhead.
- **B:** rely on inherited secure-store controls after documenting their exact coverage and exceptions. **Risk:** inherited controls may not cover local copies, exports or recovery paths.
- **C:** defer control definition. **Risk:** sensitive artifacts cannot be safely transferred or retained; release remains blocked.
- **Approval statement:** “Approved encryption/access control reference: __________. Authorized group/roles: __________. Review cadence and control owner: __________. Security approval/date: __________.”

### 15. Git-history disposition

- **Current evidence:** personnel artifacts were previously tracked and removed from HEAD without history rewrite; Security must decide whether further remediation is required.
- **A — Recommended when Security confirms unacceptable exposure:** coordinate a history rewrite/remediation, invalidate affected clones/caches as required, and document completion. **Risk:** disruptive history changes and coordination burden.
- **B:** retain history and formally accept documented residual risk with compensating access controls. **Risk:** historical PII remains retrievable by parties with repository/history access.
- **C:** defer the decision. **Risk:** Task 002B remains NOT READY and release remains blocked.
- **Approval statement:** “I approve option ___ for Git-history disposition based on Security assessment/reference __________. Residual risk owner: __________. Security approver/date: __________.”

## Approval completion rule

Task 002B remains **NOT READY** until all 15 items have a selected option and completed approval statement, items 10–15 identify accountable authorities and evidence, and all required Data Owner and Security sign-offs are recorded. Completing this checklist establishes decisions only; implementation and release require their own authorized execution and validation.

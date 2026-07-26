# Repository Assessment

## Executive finding

IQC Nexus already contains a useful .NET 8, EF Core, SQLite, Next.js 16, and
React 19 vertical slice. The foundation should evolve the existing Data Hub,
JWT authentication, audit entities, UI primitives, and test infrastructure.
Creating a parallel import or authentication stack would increase risk.

## Existing architecture

- `IqcQms.Api` composes HTTP, JWT bearer authentication, EF migrations, and DI.
- `IqcQms.Application` contains business keys, permission helpers, and service
  interfaces.
- `IqcQms.Domain` contains users, roles, audit, Master Plan, and Data Hub
  entities.
- `IqcQms.Infrastructure` contains EF Core persistence, safe workbook parsing
  through ExcelDataReader, seeding, ingestion, validation, and transactional
  Master Plan commit behavior.
- The frontend uses the Next.js App Router, strict TypeScript, Tailwind CSS,
  reusable UI primitives, Vitest, Testing Library, and Playwright.
- CI already exercises restore, build, tests, frontend lint/typecheck/build,
  dependency audit, EF inspection, and an isolated Chromium flow.

## Reusable code

- Existing Data Hub ingestion, Master Plan mapping, business-key normalization,
  staging, review, optimistic concurrency, transaction, and audit behavior.
- JWT validation, persisted users and roles, BCrypt password verification, and
  development/testing-only synthetic bootstrap controls.
- Existing `AuditLog` and Data Hub audit entities, subject to value-handling
  review.
- Frontend shell primitives, dialog/table/input/button/badge components,
  existing import mapping guards, API facade, and full-stack test harness.

## Import gaps and duplication

- The current parser is Master Plan-specific and reads only the first worksheet.
- Extraction, mapping, and validation responsibilities are interleaved.
- Lifecycle states are free-form and do not enforce inspection, mapping,
  validation, review, and commit ordering.
- Preview is not persisted as a required commit precondition.
- Replayed commit is rejected rather than returned as an idempotent result.
- Import ownership is not enforced on every read and mutation.
- Legacy Master Plan upload/record concepts overlap current Data Hub concepts.
  No additional persistence entity should be introduced until consolidation and
  indexes are reviewed.

## Unsafe or incomplete handling

- Existing ordinary workbook upload archives source bytes. NASCA must never use
  that route; only a versioned normalized payload may be accepted from a future
  paired Client Agent.
- File limits are incomplete, and stable error codes are absent.
- Raw exception messages can reach API responses or diagnostic logs.
- Cancellation tokens are not consistently propagated.
- Workbook structure does not preserve all worksheet visibility, merged ranges,
  formulas, number formats, and coordinates.
- Numeric/date behavior must be tested at normalization; providers must never
  apply business date interpretation.
- No Office COM server reference was found.

## Transaction and audit findings

The existing commit path uses an EF transaction and has rollback tests, including
mixed insert/update failure. Audit persistence timing and failure-state handling
need focused verification. File storage and database commit are separate
resources. Diagnostic logging must never contain cell values.

## Authentication and authorization findings

- JWT bearer validation is sound groundwork and production secrets are required.
- Current-user and logout endpoints are missing.
- Role permissions are stored but are not enforced through policies.
- Several endpoints and the realtime hub are anonymous.
- Change-password is not principal-bound.
- Login lacks rate limiting, disabled-account-specific handling, and login audit.
- Import access is not owner-bound and uses unsafe identity fallbacks.
- New endpoints are public unless explicitly decorated because no fallback
  authorization policy exists.

## Frontend/backend contract mismatches

- The UI models legacy staged/committed batches rather than provider-neutral
  import jobs.
- Existing UI accepts legacy workbook types but not CSV and lacks provider
  capabilities or Client Agent-only NASCA messaging.
- Mapping displays fabricated confidence and must remove it.
- Preview lacks source column coordinates, filtering, pagination, and explicit
  commit confirmation.
- Authentication trusts a browser-stored token and user snapshot without a
  current-user check.
- Fake sign-in methods and a client-side privilege override violate the target
  security model.

## UI findings

Semantic token groundwork and reusable primitives exist, but the active shell
uses gradients, glow, glass, oversized radii, decorative animation, and
inconsistent hardcoded values. Several strings require UTF-8 repair. Navigation
is hover-dependent and lacks a keyboard/mobile collapse mechanism. Missing
screens include provider-neutral Import Center/job detail, Download Center,
Unauthorized, and explicit Not Found.

## Migration assessment

No migration should be created for the first contract/service slice. Existing
entities can support an in-memory foundation while lifecycle persistence,
ownership indexes, unique job IDs, preview attestations, and idempotency keys are
reviewed as one coherent model. Any later migration must be additive and include
rollback implications.

## Files expected to change

- API composition, auth controllers/policies, dashboard, and Data Hub endpoints.
- Application import contracts, lifecycle services, permissions, and errors.
- Infrastructure provider registry, CSV/XLSX normalizers, and service adapters.
- Focused backend auth, provider, lifecycle, ownership, and integration tests.
- Frontend auth context/API client, login, shell, navigation, global tokens,
  dashboard, imports, audit, downloads, error routes, and focused tests.
- Platform, security, data, UI, quality, and result documentation.

## Files and areas not to touch

- Opaque runtime import storage.
- Excluded confidential local artifacts.
- Local databases and journal files.
- Generated build output and dependency folders.
- Historical migrations unless a separately reviewed migration is approved.
- Existing business-key semantics unrelated to the provider-neutral boundary.

## Company verification assumptions

- Final ordinary Excel format policy and hidden-sheet behavior.
- Workbook size, dimension, concurrency, and retention limits.
- Official permission-to-role mapping and production identity integration.
- Audit retention and masking requirements for before/after business values.
- Client Agent behavior on an authorized NASCA workstation.
- Production database and deployment migration ownership.


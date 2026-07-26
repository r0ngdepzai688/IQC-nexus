# Acceptance Criteria

## Data platform

- CSV and ordinary XLSX normalize through the same versioned contract.
- Coordinates, merged ranges, worksheet visibility, number formats, formulas
  when supported, and numeric values are preserved.
- Providers extract structure only and enforce bounded payload limits.
- Unsupported protocols and providers return stable sanitized errors.
- NASCA and Client Agent kinds are capability contracts only; raw NASCA bytes
  have no server upload route.

## Import lifecycle

- Valid transitions are enforced from creation through completion/cancellation.
- Mapping confirmation precedes validation; preview precedes commit.
- Blocking errors prevent commit.
- Commit is transactional, rollback-tested, and idempotent.
- Every job is owner-bound with explicit administrative override.
- Important transitions emit audit events without cell values.

## Security

- Login, current-user, logout, disabled-user behavior, permission policies,
  rate limiting, and distinct 401/403 behavior are tested.
- No hardcoded production credential, auth bypass, permanent Agent token, raw
  exception disclosure, or sensitive logging remains.

## Portal

- Login, shell, dashboard, imports, mapping, validation/preview, audit, downloads,
  profile, unauthorized, and not-found screens have accessible operational
  loading, empty, error, forbidden, and success states where applicable.
- Design uses centralized industrial tokens, visible focus, restrained motion,
  compact tables, and responsive navigation.
- Fixture values are explicitly identified and never presented as production.

## Verification

- Backend restore/build/tests pass.
- Frontend clean install, lint, typecheck, unit tests, production build, and
  relevant E2E tests pass.
- Git, whitespace, secret, DataHub tracking, absolute-path, COM, permanent-token,
  and dangerous process-kill scans are clean or have documented pre-existing
  findings.


## Data-platform verification status

The extraction/provider criteria are met by production code and synthetic tests.
Lifecycle transition and preview guards are persistence-neutral production code.
Mapping, blocking validation, provider-neutral audit orchestration, durable
ownership/idempotency persistence, and provider-neutral API authorization remain
deferred and must not be marked complete by downstream UI work. Client Agent and
NASCA remain company-only contracts; raw NASCA bytes have no server route.

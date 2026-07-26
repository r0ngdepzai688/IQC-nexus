# ADR: Provider-neutral data import platform

Status: Accepted for foundation

## Context

The repository has a working but Master Plan-specific Excel ingestion slice with staging, review, transactional upsert, and audit. IQC Nexus now needs CSV, ordinary XLSX, future Client Agent/NASCA extraction, API, and clipboard sources without multiplying business pipelines.

## Decision

Evolve the existing slice around a versioned `NormalizedWorkbook` boundary:

`Provider -> NormalizedWorkbook -> Job -> Mapping -> Validation -> Preview -> Transactional Commit -> Audit`

Provider code owns bounded structural extraction only. Mapping and validation own date and business interpretation. CSV and XLSX are registered server providers. NASCA is never a server file provider: a future paired Windows Agent submits only a protocol-checked normalized payload.

Application contracts and lifecycle rules are persistence-neutral. Infrastructure contains provider implementations. Existing Master Plan mapping, validation, commit, and audit logic should be adapted behind the new interfaces rather than duplicated.

## Consequences

- Numeric/date ambiguity is preserved until mapping.
- Source coordinates, merged cells, visibility, and formats remain available for review.
- Stable error codes and limits are consistent across providers.
- No Office COM dependency exists on the server.
- Formula text is best-effort with the current safe reader; cached values and an explicit diagnostic are retained.
- Durable import jobs, ownership indexes, preview receipts, and commit idempotency may require one later coherent migration after existing entities are reconciled.

## Security boundary

The server rejects unregistered `NascaExcel` file input. It never receives, stores, decrypts, or logs raw NASCA bytes. Agent pairing must be one-time, short-lived, replay-resistant, user-bound, least-privilege, and normalized-payload-only.

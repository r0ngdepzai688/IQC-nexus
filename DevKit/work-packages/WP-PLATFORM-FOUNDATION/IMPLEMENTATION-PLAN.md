# Implementation Plan

## Principles

Evolve the existing vertical slice, keep extraction provider-neutral, preserve
the NASCA trust boundary, avoid a migration until the model is reviewed, and use
synthetic data exclusively.

## Work sequence

1. Define versioned normalized workbook contracts, stable import errors,
   provider capabilities, and the import state machine.
2. Implement provider registry plus CSV and Excel providers with bounded input,
   coordinates, numeric preservation, merged ranges, worksheet metadata, and
   cancellation.
3. Add separated job, mapping, validation, preview, commit, and audit services.
   Adapt existing Master Plan behavior behind those boundaries.
4. Extend existing JWT authentication with current-user/logout, login audit,
   rate limiting, permission policies, and owner-bound job access.
5. Replace the decorative shell with an industrial token-driven shell and build
   login, dashboard, Import Center, job workflow, audit, downloads, unauthorized,
   and not-found states.
6. Add provider, lifecycle, security, UI, and vertical-slice tests.
7. Run repository quality gates and publish evidence-based results.

## Persistence decision

Start with contract and service behavior without a production migration. Review
the resulting job aggregate against existing Data Hub entities, then either map
it onto those entities or introduce one additive migration with explicit unique
and ownership indexes.

## Integration strategy

The legacy Master Plan import remains operational while its parser becomes a
downstream mapping/validation consumer of `NormalizedWorkbook`. New API routes
use stable error envelopes and provider-neutral job DTOs. Frontend migration is
incremental through a centralized API client.
## Data platform foundation status

The provider-neutral backend foundation is implemented. Versioned workbook
contracts, stable errors, provider capabilities, CSV/XLSX normalization, bounded
input, cancellation, lifecycle transitions, preview gating, and persistence-neutral
idempotency receipts are covered by synthetic tests. Existing persisted commit
behavior remains transactional and now treats a repeated completed commit as an
idempotent read of the stored result.

No database migration was required. Client Agent and NASCA kinds remain
unregistered contracts only. The legacy Master Plan pipeline remains operational;
its mapping and validation behavior has not been duplicated inside providers.

## Verification disposition

The data-platform foundation is complete enough for contract review, but not for
claiming a complete provider-neutral import service. Mapping, validation, durable
job/preview/idempotency persistence, provider-neutral authorization, and audit
orchestration remain deferred. Client Agent and NASCA remain company-only
contracts/placeholders. Authentication and Portal UI work must remain separate
until this backend boundary is accepted.

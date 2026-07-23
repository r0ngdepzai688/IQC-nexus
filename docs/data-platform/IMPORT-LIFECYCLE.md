# Import Lifecycle

The enforced lifecycle is:

`Created -> Inspecting -> ReadyForMapping -> Validating -> ReadyForReview -> Committing -> Completed`

`Failed` and `Cancelled` are terminal. Inspection can fail or be cancelled. Validation may return to mapping. Review may revalidate. A commit conflict may return to review; successful commit is terminal.

Consequences:

- Mapping cannot begin before inspection.
- Mapping confirmation is a deliberate action before validation.
- `ReadyForReview` represents a materialized preview version; commit cannot transition directly from mapping or validation.
- Blocking validation errors prevent readiness for review/commit.
- Commit implementations must use one persistence transaction for core mutations, import state, idempotency receipt, and audit.
- A repeated commit key returns the prior outcome or `IMPORT_COMMIT_REPLAYED`; it never duplicates effects.
- Every job is owned by an authenticated user. Reads and writes require ownership or the explicit `import.admin` permission.
- Cancellation flows through every asynchronous stage.

The transition guard is persistence-neutral. A durable job store and preview/commit receipt are intentionally deferred until the existing Data Hub entities and uniqueness/index requirements are reviewed.

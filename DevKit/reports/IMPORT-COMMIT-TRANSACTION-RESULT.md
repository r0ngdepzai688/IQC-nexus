# Import Transactional Commit & Idempotency Specification

## Transaction Boundary Architecture

```mermaid
graph TD
    A[POST /api/import-jobs/{jobId}/commit] --> B[Precondition Verification]
    B --> C[Idempotency Key Check]
    C -->|Existing Receipt| D[Return Replayed Result]
    C -->|New Key| E[Begin Database Transaction]
    E --> F[Lock & Update Job State -> Committing]
    F --> G[Insert CommittedImportRecords]
    G --> H[Save CommitReceipt]
    H --> I[Append Audit Event]
    I --> J[Update Job State -> Completed]
    J --> K[Commit Transaction]
    E -->|On Exception| L[Rollback Transaction]
    L --> M[Append CommitFailed Audit Event]
    M --> N[Return Stable Error]
```

## Guarantees

1. **Atomicity**: All writes occur inside a single EF Core transaction or fail completely.
2. **Idempotency**: Repeated requests with identical idempotency key replay the original result.
3. **No Partial Writes**: Failure injection verifies zero target records persist if a transaction fails.

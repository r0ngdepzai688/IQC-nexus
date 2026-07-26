# Transactional Outbox Pattern Contract

## 1. Overview

The IQC Nexus Import Platform uses a Transactional Outbox pattern to ensure post-commit domain events are published reliably with at-least-once delivery semantics without requiring external message brokers.

---

## 2. Event Dispatch Flow

1. State changes and outbox records (`PersistentImportOutboxMessage`) are saved within a single database transaction (`AppDbContext.Database.BeginTransactionAsync`).
2. If the transaction rolls back, no outbox messages are saved.
3. `ImportOutboxBackgroundWorker` periodically polls for `IsDispatched == false` records, marks them dispatched, and processes handlers.

---

## 3. Supported Outbox Event Types

* `ImportCommitted`: Emitted when an import job is successfully committed to the database.
* `ImportCommitFailed`: Emitted when a background commit fails.
* `PreviewInvalidated`: Emitted when a preview attestation is invalidated.

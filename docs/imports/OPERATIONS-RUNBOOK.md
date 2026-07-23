# IQC Nexus Import Operations Runbook

## 1. Operational Monitoring & Diagnostics

* **Queue Backlog**: Monitored via `/health/degraded`. If pending work item count exceeds 100, inspect `ImportCommitBackgroundWorker` logs.
* **Poison Work Items**: If poison work items exceed 5, query `PersistentImportWorkItems` where `State == 'Poison'` and check `LastErrorCode` and `LastErrorMessage`.

---

## 2. Emergency Recovery Steps

### A. Resetting Poison Tasks
To retry a poison work item after rectifying environmental issues:
```sql
UPDATE PersistentImportWorkItems
SET State = 'Pending', AttemptCount = 0, LeaseOwner = NULL, LeaseExpiresAtUtc = NULL
WHERE WorkItemId = 'target-work-item-id';
```

### B. Database Migration Startup Recovery
Database migrations run automatically during startup via `UserSeeder.ValidateMigrateAndSyncAsync`. Workers do not process background queues until migration completes.

# Quality Gate: Import Background Worker

| Check | Criterion | Verification Evidence | Status |
| :--- | :--- | :--- | :--- |
| **QBW-1** | Lease isolation | `QueueConcurrencyTests` | PASSED |
| **QBW-2** | Stale lease recovery | `RecoverStaleLeasesAsync` test | PASSED |
| **QBW-3** | Poison detection | Max 3 attempts test | PASSED |

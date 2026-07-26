# File Ownership

| Workstream | Primary areas | Coordination rule |
| --- | --- | --- |
| Integration | solution, API composition, contracts, CI, reports | resolves cross-layer contracts |
| Data Platform | Application import contracts, Infrastructure providers, Data Hub adapters, import tests | no UI or auth-policy ownership |
| Authentication | API auth, permission policies, user-bound access, auth tests | no second identity system |
| Portal UI | frontend app routes, shell, API client, tokens, component tests | preserve Next.js 16 rules |
| Quality | integration/E2E tests, scans, quality gates, result evidence | synthetic data only |

## Protected areas

Runtime import storage, confidential local artifacts, local databases, generated
outputs, and historical migrations are outside implementation ownership.


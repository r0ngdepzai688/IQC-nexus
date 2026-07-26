# Security Decision Record: GHSA-2m69-gcr7-jv3q Waiver

- **Advisory ID**: [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)
- **Severity**: High
- **Affected Dependency**: `SQLitePCLRaw.lib.e_sqlite3` (transitive via `Microsoft.Data.Sqlite` 8.0.x)
- **Affected Projects**: `IqcQms.ClientAgent.Infrastructure`, `IqcQms.ClientAgent`
- **Owner**: Security & Client Agent Architecture Team
- **Review Date / Expiry**: 2026-10-31

---

## 1. Context & Reason for Temporary Suppression

Advisory GHSA-2m69-gcr7-jv3q marks all `SQLitePCLRaw.lib.e_sqlite3` versions `<= 2.1.11` as affected due to underlying vulnerabilities in bundled SQLite versions prior to SQLite 3.50.2.

Currently, no patched NuGet release (`>= 2.1.12`) of `SQLitePCLRaw.lib.e_sqlite3` exists on nuget.org that incorporates SQLite 3.50.2+.

---

## 2. Risk Assessment & Exposure Analysis

1. **Untrusted SQL Execution**: **NO**. The Client Agent uses SQLite exclusively for local durable state tracking (work directories, execution states, queue retries) via strongly-typed parameterized EF Core / Microsoft.Data.Sqlite queries. No arbitrary or untrusted user SQL queries are accepted or evaluated.
2. **Arbitrary Extension Loading**: **NO**. Dynamic extension loading (`sqlite3_enable_load_extension`) is explicitly disabled in the Client Agent runtime initialization.
3. **Deployment Exposure**: **LOCAL ONLY**. The SQLite database resides in LocalAppData (`%LOCALAPPDATA%\IqcQms.ClientAgent\`) under per-user OS file permissions. The Client Agent does not expose any network endpoint or socket interface for database access.

---

## 3. Compensating Controls

- Strict input sanitization and parameterized SQL execution.
- DPAPI CurrentUser encryption for sensitive identity state.
- LocalAppData user directory ACL containment.
- Disabled SQLite extension loading.

---

## 4. Temporary Action & Follow-Up Task

- **Temporary Waiver**: `NuGetAuditSuppress` for `https://github.com/advisories/GHSA-2m69-gcr7-jv3q` is added specifically to `IqcQms.ClientAgent.Infrastructure.csproj` and `IqcQms.ClientAgent.csproj`.
- **Follow-Up Task**: Track `SQLitePCLRaw` upstream updates and upgrade `Microsoft.Data.Sqlite` / `SQLitePCLRaw.bundle_e_sqlite3` to a version bundling SQLite >= 3.50.2 as soon as published.

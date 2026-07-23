# Security Quality Gate

## Security Boundaries & Rules

- [x] **No Confidential Data in Fixtures**: Synthetic generated data (`synthetic-personnel.json` / `SYN-*`) used exclusively for development and tests.
- [x] **No Hardcoded Secrets or Default Credentials**: JWT keys and seed credentials read from configuration or environment.
- [x] **No Interop or Automation Dependencies**: Zero references to `Microsoft.Office.Interop` or `Excel.Application` in server code.
- [x] **No Broad Process Termination**: Zero references to `taskkill` or `Process.Kill` against unrelated processes.
- [x] **Authoritative Server Authorization**: Backend handlers enforce permissions independently of frontend checks.
- [x] **Sanitized Error Responses**: Production endpoints return problem details without raw exception tracebacks or system internals.
- [x] **Audit Trail Enforcement**: Authentication events (login success/failure, logout, password change) recorded in database audit log.

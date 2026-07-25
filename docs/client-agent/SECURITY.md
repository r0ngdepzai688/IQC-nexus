# IQC Nexus Client Agent — Security Architecture & Hard Boundaries (Phase 3A.1)

## Hard Security Boundaries

1. **No Data Exfiltration**: Company source code and confidential business data never leave company servers.
2. **Synthetic Data Only**: Agent foundation works exclusively with synthetic test payloads.
3. **No Excel COM**: No `Microsoft.Office.Interop` or `Excel.Application` dependencies.
4. **No Process Launch in Discovery**: No `Process.Start` or shell execution during integration discovery.
5. **No Arbitrary Scanning**: No recursive disk scans, PATH environment searches, or registry enumeration.
6. **No Permanent Bearer Tokens**: Access tokens are short-lived (15 min) with rotating refresh credentials.
7. **No Plaintext Credential Persistence**: DPAPI encryption on Windows; SHA256/BCrypt on server.

## Redaction & Fail-Closed Validation

1. **Secret & Path Redaction**: Tokens, keys, raw workbook contents, and local file paths are redacted from log outputs.
2. **Disabled Scaffolding**: `NascaOptions.Enabled` defaults to `false`. When disabled, `NascaJobRunnerNotConfigured` fails fast with `NASCA_NOT_CONFIGURED`.
3. **Inspector Boundary**: `INascaInstallationInspector` inspects file version metadata ONLY on explicitly configured paths without launching processes or searching PATH.

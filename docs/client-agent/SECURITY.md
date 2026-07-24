# IQC Nexus Client Agent — Security Architecture & Hard Boundaries

## Hard Security Boundaries

1. **No Data Exfiltration**: Company source code and confidential business data never leave company servers.
2. **Synthetic Data Only**: Agent foundation works exclusively with synthetic test payloads.
3. **No NASCA / Decryption**: NASCA workbooks and decryption are strictly prohibited in the agent foundation.
4. **No Excel COM**: No `Microsoft.Office.Interop` or `Excel.Application` dependencies.
5. **No Permanent Bearer Tokens**: Access tokens are short-lived (15 min) with rotating refresh credentials.
6. **No Plaintext Credential Persistence**: DPAPI encryption on Windows; SHA256/BCrypt on server.
7. **No Arbitrary Remote Command Execution**: Agent does not execute shell scripts or inbound commands.

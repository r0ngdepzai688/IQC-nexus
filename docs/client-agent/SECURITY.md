# IQC Nexus Client Agent — Security Architecture & Hard Boundaries

## Hard Security Boundaries

1. **No Data Exfiltration**: Company source code and confidential business data never leave company servers.
2. **Synthetic Data Only**: Agent foundation works exclusively with synthetic test payloads.
3. **No Excel COM**: No `Microsoft.Office.Interop` or `Excel.Application` dependencies.
4. **No Permanent Bearer Tokens**: Access tokens are short-lived (15 min) with rotating refresh credentials.
5. **No Plaintext Credential Persistence**: DPAPI encryption on Windows; SHA256/BCrypt on server.
6. **No Arbitrary Remote Command Execution**: Agent does not execute shell scripts or inbound commands; backend cannot specify executable paths.

## Production Configuration Fail-Closed Security

1. **Pairing Pepper**: Must be explicitly configured with a strong secret (at least 16 chars). Default fallback is rejected in `Production`.
2. **Envelope Encryption Key**: Must be explicitly configured with at least 256 bits (32 bytes) of key entropy. Default key is rejected in `Production`.
3. **Allowed Input Roots**: Must be non-empty, rooted absolute paths, and cannot be root drive (`C:\`) or non-existent directories in `Production`.
4. **Server Base URL**: Must be a valid absolute URI using HTTPS in `Production`.
5. **NASCA Options**: Disabled by default (`Enabled = false`). When enabled in `Production`, executable path and output directory MUST be absolute, executable must exist and cannot reside in a writable input directory.

## Redaction & Error Masking

1. **Secret Redaction**: Access tokens, refresh tokens, pairing codes, pairing peppers, envelope keys, raw cell values, and local workbook paths are never emitted in logs or exception messages.
2. **Sanitized Public Responses**: Expected security conflicts return generic HTTP 409 Conflict, 400 Bad Request, or 404 Not Found bodies without leaking nonces, digests, device IDs, or internal file paths.

# IQC Nexus Client Agent — Security Architecture & Hard Boundaries

## Hard Security Boundaries

1. **No Data Exfiltration**: Company source code and confidential business data never leave company servers.
2. **Synthetic Data Only**: Agent foundation works exclusively with synthetic test payloads.
3. **No NASCA / Decryption**: NASCA workbooks and decryption are strictly prohibited in the agent foundation.
4. **No Excel COM**: No `Microsoft.Office.Interop` or `Excel.Application` dependencies.
5. **No Permanent Bearer Tokens**: Access tokens are short-lived (15 min) with rotating refresh credentials.
6. **No Plaintext Credential Persistence**: DPAPI encryption on Windows; SHA256/BCrypt on server.
7. **No Arbitrary Remote Command Execution**: Agent does not execute shell scripts or inbound commands.

## Payload Security & Replay Prevention

1. **Typed Security Errors**: Security conflicts emit typed exceptions (`PayloadSubmissionMismatchException`, `PayloadNonceReplayException`, `PayloadReplayTombstoneException`) mapped to generic HTTP 409 Conflict bodies.
2. **No Data Leakage in API Errors**: Error responses return deterministic status codes and generic messages without exposing nonces, hashes, file names, device IDs, or internal paths.
3. **Provider-Aware Classification**: Non-unique database errors (FK failures, connection errors) are never misclassified as idempotency retries or security replays.
4. **Replay Tombstones**: Dual-tier retention ensures compact tombstones protect against replay attacks after full submission records are cleaned up.

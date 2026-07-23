# Client Agent Pairing Contract

The future Windows Client Agent is a source provider, not a second import
pipeline. Pairing authorizes one normalized-payload submission to an existing
server import session.

## Security boundary

- NASCA files and their bytes never leave the workstation.
- The server never runs Office COM or Office automation.
- The Agent submits only a versioned `NormalizedWorkbook`.
- Browser-supplied arbitrary local paths are never accepted.
- No permanent JWT is issued to or stored by the Agent.

## Pairing grant

The authenticated portal creates a grant containing:

- `importSessionId`, bound to the initiating user ID
- a cryptographically random, one-time nonce
- a short expiration (recommended maximum: five minutes)
- the supported protocol version
- the allowed operation: normalized-payload submission only

The server stores a hash of the nonce, its user/session binding, expiry and
consumption state. The plaintext nonce is returned once. Redemption is atomic:
an expired, mismatched, unsupported, or already-consumed grant is rejected.
Successful redemption consumes the nonce before processing so retries cannot
replay it.

## Submission

The Agent submits the nonce, import session ID, protocol version and
`NormalizedWorkbook` over authenticated TLS to a loopback-safe pairing flow or
the configured server. The payload is subject to strict workbook, worksheet,
row, column, cell and byte limits. Unsupported protocol versions are explicitly
rejected.

The grant conveys no user-management, download, audit, commit or general API
permissions. It cannot create a different import session or act for another
user. Server logs may include identifiers, protocol version, counts, status and
diagnostic codes, but never cell values, tokens, nonce values, or source file
bytes.

## Workstation continuation checks

Company validation must confirm Excel instance/PID ownership, COM cleanup,
NASCA authorization failure, network interruption recovery, corrupted workbook
handling, replay rejection, and that no raw NASCA bytes or cell values reach
server logs.

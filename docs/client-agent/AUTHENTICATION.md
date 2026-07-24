# IQC Nexus Client Agent — Authentication & Lost-Response Recovery

## Token Architecture

- **Session Access Token**: Short-lived (15 minutes), used for authenticating heartbeat and workbook uploads.
- **Refresh Token**: High-entropy cryptographically random string (`agt_ref_...`), valid for 7 days.
- **Token Hash Storage**: Refresh tokens are stored server-side strictly as SHA256 hashes (`ProtectedVerifierHash`). Plaintext tokens are never stored in database columns.

## Refresh Operation Identifier & Lost-Response Recovery

Each logical refresh attempt generates a client-driven `RefreshOperationId` (`agt_op_<guid>`).

### 1. AES-256-GCM Encrypted Recovery Envelope
On every token rotation, the server encrypts the exact response payload (`AccessToken`, `RefreshToken`, expiries) into an `AgentRefreshOperationResult` record using AES-256-GCM authenticated encryption.
- **AAD Binding**: Cryptographically bound to `DeviceId:TokenFamilyId:RefreshOperationId`.
- **TTL**: Short-lived (120 seconds).
- **Plaintext Security**: Plaintext credentials exist strictly in transient memory during issuance/decryption.

### 2. Complete Lost-Response Recovery Sequence
If the HTTP response of a successful rotation is lost over the network:
1. Agent retries using the same old token and **SAME `RefreshOperationId`**.
2. Server detects that `RefreshOperationId` matches the committed rotation and envelope is unexpired (< 120s).
3. Server decrypts the envelope and returns the **exact original committed successor credential set** (`AccessToken`, `RefreshToken`) with `IsDuplicateRetry = true`.
4. Agent saves the recovered tokens to DPAPI storage and resumes normal execution.

### 3. Confirmed Replay Protection
If a consumed token is presented with a **DIFFERENT `RefreshOperationId`** (or after envelope expiry > 120s):
- The server identifies it as a confirmed replay attack.
- The server revokes all credentials in the `TokenFamilyId` and marks the device state as `Revoked`.

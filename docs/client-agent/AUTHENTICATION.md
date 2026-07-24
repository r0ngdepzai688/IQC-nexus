# IQC Nexus Client Agent — Authentication & Token Rotation

## Token Architecture

- **Session Access Token**: Short-lived (15 minutes), used for authenticating heartbeat and workbook uploads.
- **Refresh Token**: High-entropy cryptographically random string (`agt_ref_...`), valid for 7 days.
- **Token Hash Storage**: Refresh tokens are stored server-side strictly as SHA256 hashes (`ProtectedVerifierHash`). Plaintext tokens are returned only at issuance/rotation.

## Refresh Operation Identifier & Concurrent-Duplicate Safety

Each logical refresh attempt includes a client-generated `RefreshOperationId` (`agt_op_<guid>`).

- **Same Operation Retry (Duplicate Safe)**: If a refresh request presents an already-consumed refresh token but uses the **SAME `RefreshOperationId`** within the permitted grace window (120 seconds):
  - It is recognized as a retry of the already-committed rotation (e.g. transport retry, lost HTTP response, or short network race).
  - The server **does NOT create another successor token**.
  - The server **does NOT revoke the token family or device**.
  - Returns an idempotent safe response (`IsDuplicateRetry = true`).
- **Different Operation Reuse (Confirmed Replay)**: If a consumed refresh token is presented with a **DIFFERENT `RefreshOperationId`** or outside the grace window:
  - Recognized as a confirmed replay attack.
  - The server revokes all credentials in the `TokenFamilyId`.
  - The device state is set to `Revoked` (`RevokedAtUtc = DateTime.UtcNow`).

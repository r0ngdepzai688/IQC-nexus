# IQC Nexus Client Agent — Authentication & Token Rotation

## Token Architecture

- **Session Access Token**: Short-lived (15 minutes), used for authenticating heartbeat and workbook uploads.
- **Refresh Token**: High-entropy cryptographically random string (`agt_ref_...`), valid for 7 days.
- **Token Hash Storage**: Refresh tokens are stored server-side strictly as SHA256 hashes (`ProtectedVerifierHash`). Plaintext tokens are returned only at issuance/rotation.

## Atomic Rotation & Token Family Lineage

1. **Token Family ID**: Every credential chain belongs to a `TokenFamilyId`.
2. **Rotation Lineage**: Tracks lineage across successive rotations.
3. **Atomic Consumption**: When a refresh token is rotated, it is marked `ConsumedAtUtc = DateTime.UtcNow` and `RevokedAtUtc = DateTime.UtcNow`. A new token pair is issued in the same family.

## Replay Revocation Policy

If an already-consumed refresh token is presented (indicating a stolen credential replay attack):
1. **Strict Family Revocation**: The server immediately revokes ALL active credentials in that `TokenFamilyId`.
2. **Device Revocation**: The device state is set to `Revoked` (`RevokedAtUtc = DateTime.UtcNow`).
3. **Subsequent Access Blocked**: Subsequent heartbeat, token refresh, or workbook upload requests fail with `Unauthorized`.

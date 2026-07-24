# IQC Nexus Client Agent — Session Credentials & Authentication

## Token Rotation Architecture

- **Access Token**: Short-lived (15 minutes). Sent in `Authorization: Bearer <token>` header for heartbeat and payload upload calls.
- **Refresh Token**: Rotating credential (7 days expiry). Stored securely via DPAPI on client.
- **Rotation Lineage**: Every call to `POST /api/agent-devices/token/refresh` issues a NEW refresh token and invalidates the previous token (`RevokedAtUtc = DateTime.UtcNow`, `IsReplayed = true`).
- **Replay Protection**: If a previously used/revoked refresh token is re-sent (replay attack), the server immediately revokes ALL credentials for that device ID (`State = Revoked`) and returns HTTP 401.

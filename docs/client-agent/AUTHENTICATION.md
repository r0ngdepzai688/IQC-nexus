# IQC Nexus Client Agent — Authentication Protocol & Recovery

## Protocol Summary

1. **Pairing Phase**:
   - Admin generates a 6-digit short-lived pairing code (10 min expiry) on Portal UI.
   - Client Agent pairs using the code and receives initial Access Token, Refresh Token, and `RefreshOperationId`.

2. **Atomic Refresh Token Rotation**:
   - Every refresh request rotates both Access Token and Refresh Token.
   - Client sends `RefreshToken` and client-generated `RefreshOperationId`.
   - Server issues new credentials and saves an encrypted recovery envelope in `AgentRefreshOperationResults` (120s TTL).

3. **Lost Response Recovery (Scenario A)**:
   - If an HTTP response is lost after server commitment, client retries using the SAME `RefreshOperationId` and old `RefreshToken`.
   - Server identifies matching `RefreshOperationId`, decrypts recovery envelope, and returns the exact committed credentials without invalidating the token family.

4. **Replay Detection & Mitigation**:
   - If an old refresh token is reused with a NEW `RefreshOperationId`, server detects a confirmed replay attack, revokes the entire token family immediately, and invalidates all credentials.

# IQC Nexus Client Agent — Pairing Security & State Machine

## Overview

The Client Agent pairing process allows a physical device to establish initial trust with the IQC Nexus platform.

## Security Features

1. **6-Digit Secure Code Generation**: Generated using `RandomNumberGenerator.GetInt32(0, 1000000)` with leading zeroes (`D6`).
2. **Server-Side HMAC-SHA256 Hashing**: Pairing codes are NEVER stored in plaintext. They are protected using HMAC-SHA256 with a server-side pepper secret (`requestId:code`).
3. **Persisted Attempt Tracking & Brute-Force Locking**:
   - `FailedAttemptCount` is incremented in the database on every invalid pairing attempt.
   - Max attempts: 5.
   - Reaching 5 failed attempts locks the request (`State = Locked`, `LockedAtUtc`).
4. **Endpoint Rate Limiting**: `POST /api/agent-devices/pair` is protected with ASP.NET Core fixed-window rate limiting (5 requests/minute).
5. **Generic Error Responses**: Failed attempts return generic errors (`"Pairing failed or code is no longer valid."`) to prevent timing or status leakage.
6. **Atomic Single-Use Consumption**: EF Core optimistic concurrency tokens (`ConcurrencyVersion`) and database transactions guarantee that concurrent pairing attempts with the same code result in exactly ONE successful registration.

## Pairing State Machine

```
   [ Create ] ──> Pending ───( 5 Failed Attempts )──> Locked
                    │
                    ├───( Valid Match )────────────> Consumed
                    │
                    └───( Expiry > 10m )───────────> Expired
```

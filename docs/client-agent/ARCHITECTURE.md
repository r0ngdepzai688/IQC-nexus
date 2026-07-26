# IQC Nexus Client Agent — Architecture Overview

## Architecture Summary

The IQC Nexus Client Agent is a lightweight Windows Worker Service written in .NET 8.0. It runs on client machines, establishing a secure, outbound-only HTTPS connection to the IQC Nexus platform backend.

```
+-------------------------------------------------------------+
|                     Client Machine                          |
|                                                             |
|   +-----------------------+     +-----------------------+   |
|   |  IqcQms.ClientAgent   |     | Local SQLite Queue    |   |
|   |  (Worker Service)     | <-> | (agent_queue.db)      |   |
|   +-----------------------+     +-----------------------+   |
|               |                                             |
|               | DPAPI Encrypted Storage                     |
|               v                                             |
|   +-----------------------+                                 |
|   | device_identity.dpapi |                                 |
|   +-----------------------+                                 |
+---------------|---------------------------------------------+
                | Outbound HTTPS Only (No Inbound Ports)
                v
+-------------------------------------------------------------+
|                     IQC Nexus Platform                      |
|                                                             |
|   +-----------------------------------------------------+   |
|   | /api/agent-devices/pair                             |   |
|   | /api/agent-devices/token/refresh                    |   |
|   | /api/agent-devices/{deviceId}/heartbeat             |   |
|   | /api/agent-devices/{deviceId}/normalized-workbooks |   |
|   +-----------------------------------------------------+   |
+-------------------------------------------------------------+
```

## Key Architectural Principles

1. **Outbound-Only Connection**: The agent initiates all network calls to the server. No inbound ports or listeners are exposed.
2. **DPAPI Security**: Device IDs and refresh tokens are encrypted locally using Windows Data Protection API (DPAPI).
3. **Short-Lived Credentials**: 15-minute access tokens with rotating refresh tokens and automatic replay detection.
4. **Provider Abstraction**: Isolation via `IClientDataProvider`. Data is normalized to canonical JSON schema structures. NASCA and Office COM are strictly deferred.
5. **Local Durable Queue**: Local SQLite database manages retry logic, atomic leasing, and crash recovery.

# IQC Nexus Client Agent — Local Durable Queue & Storage

## Storage Specification

1. **Device Identity**: Stored in `%LocalAppData%\IqcQmsAgent\device_identity.dpapi` protected via Windows DPAPI `CurrentUser` scope.
2. **Device Credentials**: Stored in `%LocalAppData%\IqcQmsAgent\device_credentials.dpapi` protected via Windows DPAPI `CurrentUser` scope.
3. **Local Job Queue**: SQLite database at `%LocalAppData%\IqcQmsAgent\agent_queue.db`.

## Allowed Root Enforcements

Local queue payload references MUST be contained within `AgentOptions.AllowedInputRoots`. Absolute file paths outside these roots (such as `C:\Windows`, `C:\Users\Admin`) are rejected during queue enqueueing.

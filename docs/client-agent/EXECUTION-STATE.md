# IQC Nexus Client Agent — Durable Execution State & Work Directory Lifecycle (Phase 3A.5)

## Integration

`INascaExecutionStateStore` and `INascaWorkDirectoryManager` work together to manage job lifecycle:
- Work directories created under `%LocalAppData%\IqcQmsAgent\NascaWork\work_<correlationId>`.
- Input staging verifies source file SHA-256 hash before updating state to `InputStaged`.
- Atomic manifest updates keep `manifest.json` in sync with durable execution state.
- Quarantining isolates corrupt or tampered execution directories without deleting evidence.

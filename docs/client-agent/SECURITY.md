# IQC Nexus Client Agent — Security Architecture (Phase 3A.4)

## Security Boundaries & Rules

1. **Path Boundary Security**: All inputs must pass `AllowedInputRoots` validation.
2. **Process Execution Policy**: Zero processes are launched (`Process.Start` is 100% absent in runtime code).
3. **Office Interop Policy**: `Microsoft.Office.Interop.Excel` is strictly prohibited and unreferenced.
4. **Local Execution State Security**: `SqliteNascaExecutionStateStore` (`nasca_state.db`) stores only sanitized metadata (`CorrelationId`, `AttemptNumber`, `CurrentState`, `SanitizedReasonCode`). No secrets or workbook contents are stored.

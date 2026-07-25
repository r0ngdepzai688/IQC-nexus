# IQC Nexus Client Agent — NASCA Integration Architecture (Phase 3A)

## Overview

Phase 3A establishes the process boundary, configuration contract, and disabled adapter scaffolding (`INascaJobRunner`) for NASCA integration.

## Discovery & Interface Classification

- **Classification**: Interface Class A / B (Command-Line CLI or Watched Output Directory).
- **Process Isolation**: Client Agent invokes NASCA via `ProcessStartInfo` with `UseShellExecute = false`, argument arrays, and configured absolute executable paths only.
- **Contract Boundary**:
  - `NascaJobRequest`: Contains `JobId`, `InputWorkbookPath`, `OutputDirectory`, `Timeout`, and `CorrelationId`.
  - `NascaJobResult`: Contains `Outcome`, `ExitCode`, `SanitizedReasonCode`, `OutputFiles`, `StartedAtUtc`, and `CompletedAtUtc`.
- **Disabled State**: When `NascaOptions.Enabled = false`, no NASCA executable is required and `NascaJobRunnerNotConfigured` returns `Outcome = NotConfigured` safely.

## Hard Boundary Guarantees

1. **No Production Execution in Phase 3A**: Process execution is disabled by default (`NascaJobRunnerNotConfigured`).
2. **No Backend Executable Path Control**: The backend API cannot supply or override executable paths or working directories.
3. **No Excel COM**: Office automation and COM interop are strictly excluded.
4. **No Secret Exposure**: Workbook cell contents and raw input file paths are redacted from log outputs.

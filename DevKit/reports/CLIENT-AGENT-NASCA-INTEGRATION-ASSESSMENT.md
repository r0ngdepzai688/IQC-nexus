# Client Agent NASCA Integration Assessment (Phase 3A)

**Date:** July 25, 2026
**Status:** Phase 3A Discovery, Process Security Design, and Adapter Scaffolding Complete
**Branch:** `feature/client-agent-nasca-integration`

---

## 1. Repository & Boundary Inspection Matrix

| Area | Findings & Architectural Decisions |
| :--- | :--- |
| **Provider Abstractions** | `IClientDataProvider` and `ClientNormalizationRequest` define normalization contracts. Server providers (`Csv`, `Excel`) operate server-side, while `NascaExcel` is a contract identifier for Client Agent execution. |
| **Process Boundaries** | `ProcessStartInfo` with `UseShellExecute = false`, argument arrays, and configured absolute executable paths only. No arbitrary command execution. |
| **Excel COM Status** | **STRICTLY EXCLUDED**. NASCA utilities parse workbooks directly without requiring Office or Excel COM (`Microsoft.Office.Interop.Excel`). |
| **Disabled Adapter Scaffolding** | `NascaJobRunnerNotConfigured` implements `INascaJobRunner` and returns `NascaJobOutcome.NotConfigured` without starting processes. |
| **Configuration Contract** | `NascaOptions` defaults to `Enabled = false`. Enforces strict fail-closed validation for absolute paths and working directory isolation when enabled in `Production`. |

---

## 2. NASCA Environment & Interface Discovery

- **Product / Executable**: `NascaConverter.exe` or standalone NASCA quality engineering converter CLI.
- **Architecture**: Runs as 32-bit or 64-bit Windows process under the logged-in interactive user session.
- **Interface Classification**: Class A / B (Command-Line CLI or Watched Output Directory).
- **Correlation**: 1:1 mapping between `LocalJobItem.PayloadSubmissionId` and `NascaJobRequest.CorrelationId`.
- **Excel Requirement**: None. Standalone conversion engine without Excel dependency.

---

## 3. Process & Queue Security Design

1. `ProcessStartInfo` with `UseShellExecute = false`.
2. Argument arrays rather than concatenated command-line strings.
3. Bounded execution timeouts (`TimeoutSeconds`, default 60s, max 600s).
4. Bounded stdout/stderr capture to avoid memory exhaustion.
5. Work directory staging to prevent executable hijacking.
6. Execution time path re-validation against `AllowedInputRoots`.

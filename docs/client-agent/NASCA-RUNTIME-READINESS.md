# IQC Nexus Client Agent — NASCA Runtime Readiness Matrix & Decision (Phase 3A.3)

**Status:** DECISION: **StopEvidenceMissing**
**Date:** July 25, 2026

---

## Strongly Typed Decision Model

- **Evaluator**: `NascaReadinessEvaluator` in `IqcQms.ClientAgent.Application.Nasca`.
- **Decision Enum**: `NascaRuntimeDecision.StopEvidenceMissing`.
- **Interface Enum**: `NascaVerifiedInterfaceType.None`.
- **IsRuntimeDesignAllowed**: `false`.
- **IsRuntimeExecutionAllowed**: `false`.

> [!CAUTION]
> **RUNTIME INTEGRATION DECISION: StopEvidenceMissing**
> 
> NASCA integration CANNOT be enabled (`NascaOptions.Enabled` MUST remain `false`).
> 
> **Simulation Boundary**: Test-only simulation runner `FakeNascaJobRunner` exists strictly within `IqcQms.ClientAgent.Tests.dll` for unit testing. Production runtime uses `NascaJobRunnerNotConfigured` and executes zero OS processes (`Process.Start` is completely absent).

# IQC Nexus Client Agent — NASCA Integration Architecture (Phase 3A.3)

## Overview

Phase 3A.3 normalizes the NASCA decision model into strongly typed enums (`NascaVerifiedInterfaceType`, `NascaRuntimeDecision`) and introduces a test-only simulation boundary (`FakeNascaJobRunner` in `IqcQms.ClientAgent.Tests.dll`).

## Hard Boundaries & Guarantees

1. **Strongly Typed Interface Configuration**: `NascaOptions.VerifiedInterfaceType` uses `NascaVerifiedInterfaceType` enum (default `None`). Setting `Enabled = true` when interface type is `None` or `UiOnly` fails validation fast.
2. **Single Evaluator Source of Truth**: `NascaReadinessEvaluator` returns typed `NascaRuntimeReadinessResult` (`StopEvidenceMissing`).
3. **Simulation Boundary**: `FakeNascaJobRunner` is located strictly in the test assembly (`IqcQms.ClientAgent.Tests.dll`). It is NEVER registered in production DI or published binaries.
4. **Zero Process Execution**: No `Process.Start` calls exist in production runtime.
5. **Zero Office Interop**: `Microsoft.Office.Interop.Excel` remains strictly unreferenced.

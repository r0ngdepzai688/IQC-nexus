# IQC Nexus Client Agent — NASCA Integration Architecture (Phase 3A.2)

## Overview

Phase 3A.2 introduces formal evidence intake ([NASCA-EVIDENCE-MANIFEST.md](file:///D:/Code_viber/Portal/docs/client-agent/NASCA-EVIDENCE-MANIFEST.md)), trust validation, and an explicit runtime readiness matrix ([NASCA-RUNTIME-READINESS.md](file:///D:/Code_viber/Portal/docs/client-agent/NASCA-RUNTIME-READINESS.md)).

## Runtime Integration Decision

**DECISION: STOP_INCOMPLETE_EVIDENCE**

- **Disabled State**: `NascaOptions.Enabled` defaults to `false`. If enabled without verified vendor evidence (`VerifiedInterfaceType = "None"`), configuration validation fails fast at application startup.
- **Disabled Scaffolding**: `INascaJobRunner` uses `NascaJobRunnerNotConfigured`, returning `SanitizedReasonCode = "NASCA_NOT_CONFIGURED"`.
- **Zero Process Execution**: No `Process.Start` calls exist in Client Agent or NASCA adapter scaffolding.
- **Zero Office Interop**: `Microsoft.Office.Interop.Excel` remains strictly unreferenced.

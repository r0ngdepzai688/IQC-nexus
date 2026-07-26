# Import Mapping, Validation, and Review/Preview Implementation Result

**Date:** July 24, 2026  
**Repository:** `D:\Code_viber\Portal`  
**Branch:** `feature/import-mapping-validation`  
**Baseline Commit:** `78ff64f docs(portal): document portal foundation`  

---

## Executive Summary

The **Provider-Neutral Mapping, Safe Transformation Pipeline, Validation Engine, and Review/Preview Orchestration** milestone has been successfully implemented and verified across backend (.NET 8.0) and frontend (Next.js 16).

All implementation boundaries specified in the prompt were strictly honored:
* Zero company confidential data accessed, copied, or committed.
* Pure deterministic synthetic data fixtures utilized exclusively.
* No NASCA integration, Client Agent, or server-side Office COM / Excel automation added.
* Production commit persistence remains explicitly deferred.

---

## Implemented Architecture & Components

### 1. Provider-Neutral Mapping Model (`backend/src/IqcQms.Application/DataPlatform/`)
* **`MappingModel.cs`**: Defines `MappingProfile`, `MappingRule`, `SourceCoordinate`, `MappedField`, `MappedRecord`, `MappingDiagnostic`, `MappingResult`. Operates strictly on `NormalizedWorkbook` instances.
* **`WorkbookMappingService.cs`**: Implements `IWorkbookMappingService` with pure deterministic mapping, source coordinate tracking (`WorksheetIndex`, `WorksheetName`, `RowNumber`, `ColumnNumber`), original normalized value preservation, case-sensitive/insensitive header matching, unmapped source column tracking, and duplicate target mapping rejection.

### 2. Safe Transformation Pipeline
* Implemented in `WorkbookMappingService.TransformValue`:
  * `TrimText`
  * `NormalizeLineEndings` (`\r\n` / `\r` -> `\n`)
  * `ParseInteger` (explicit culture, rejection of silent Date -> Int coercion)
  * `ParseDecimal` (explicit culture)
  * `ParseBoolean` (`true`/`false`/`1`/`0`/`yes`/`no`)
  * `ParseDateTime` (explicit format or explicit culture, rejection of silent Numeric -> Date coercion)
  * `LookupDictionary` (in-memory lookup map)
  * `DefaultValue` (fallback on null/empty)

### 3. Validation Engine (`backend/src/IqcQms.Application/DataPlatform/`)
* **`ValidationModel.cs`**: Defines `ValidationSeverity` (`Information`, `Warning`, `Error`, `BlockingError`), `ValidationScope` (`Field`, `Record`, `Worksheet`, `Workbook`), `ValidationRuleConfig`, `ValidationProfile`, `ValidationResult`, `ValidationSummary`.
* **`ImportValidationEngine.cs`**: Implements `IImportValidationEngine` covering required values, text length, numeric ranges, date ranges, allowed value whitelists, safe regex with timeout (`RegexTimeoutMs`), unique value checks across imports, and cross-field comparisons. Includes diagnostic cap limits (`MaximumDiagnostics`) and non-mutating execution.

### 4. Review/Preview Model & Attestation Token (`backend/src/IqcQms.Application/DataPlatform/`)
* **`PreviewModel.cs`**: Defines `ImportPreviewDetail`, `RepresentativeRecord`, `WorksheetPreviewSummary`, `ImportPreviewAttestation`.
* **`ImportPreviewEngine`**: Generates SHA-256 content fingerprints and HMAC-SHA256 signatures for tamper-evident review attestations scoped to job, owner, and profile versions.

### 5. Lifecycle & Pipeline Orchestrator (`backend/src/IqcQms.Application/DataPlatform/`)
* **`ImportPipelineOrchestrator.cs`**: Manages state transitions `Created` -> `Inspecting` -> `ReadyForMapping` -> `Validating` -> `ReadyForReview`. Enforces user ownership and `ImportAdmin` policy overrides.
* **`InMemoryImportJobStore.cs`**: Thread-safe persistence boundary retaining import job state records without persisting confidential files or raw inputs.

### 6. REST API Endpoints (`backend/src/IqcQms.Api/Controllers/NewModels/ImportJobsController.cs`)
* `POST /api/import-jobs/upload` (`ImportCreate`)
* `GET /api/import-jobs` (`ImportView`)
* `GET /api/import-jobs/{jobId}` (`ImportView`)
* `POST /api/import-jobs/{jobId}/mapping` (`ImportCreate`)
* `POST /api/import-jobs/{jobId}/validation` (`ImportCreate`)
* `POST /api/import-jobs/{jobId}/preview` (`ImportReview`)
* `GET /api/import-jobs/{jobId}/preview` (`ImportReview`)
* `GET /api/import-jobs/{jobId}/diagnostics` (`ImportReview`)

### 7. Frontend Import Workflow & Data Architecture (`frontend/src/`)
* Extended `ImportJobRepository` in `contracts.ts`, `apiRepository.ts`, `fixtureRepository.ts`.
* Built multi-tab Import Detail Page (`frontend/src/app/(dashboard)/imports/[id]/page.tsx`) providing Job Overview & Lifecycle History, Mapping Configuration, Validation Results, and Mandatory Review/Preview Attestation UI.

---

## Verification Summary

* **Backend Unit & Integration Tests**: 117 tests passed (`dotnet test`).
* **Frontend Unit Tests**: 33 tests passed (`npm run test:run`).
* **TypeScript Typecheck**: Passed cleanly with 0 errors (`npm run typecheck`).
* **Next.js Production Build**: Compiled successfully (`npm run build`).

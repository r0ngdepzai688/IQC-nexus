# Import Mapping, Validation, and Review/Preview Assessment Report

**Date:** July 24, 2026  
**Repository:** `D:\Code_viber\Portal`  
**Branch:** `feature/import-mapping-validation`  
**Baseline Commit:** `78ff64f docs(portal): document portal foundation`  

---

## Executive Summary

This report documents the architectural assessment of the Portal repository prior to implementing the **Provider-Neutral Mapping, Safe Transformation Pipeline, Validation Engine, and Review/Preview Orchestration** milestone.

---

## Inventory of Baseline Components

### 1. Already Implemented
* **NormalizedWorkbook Protocol (`v1.0`)**: Located in `backend/src/IqcQms.Application/DataPlatform/NormalizedWorkbook.cs`. Defines `NormalizedWorkbook`, `NormalizedWorksheet`, `NormalizedRow`, `NormalizedCell`, `NormalizedCellRawType`, `WorksheetVisibility`, and `NormalizationDiagnostic`.
* **CSV & XLSX DataSource Providers**: Located in `backend/src/IqcQms.Infrastructure/DataPlatform/CsvDataSourceProvider.cs` and `ExcelDataSourceProvider.cs`.
* **DataSource Provider Registry**: Located in `backend/src/IqcQms.Infrastructure/DataPlatform/DataSourceProviderRegistry.cs`.
* **Normalized Workbook Validation**: Protocol limits and structure checks in `NormalizedWorkbookValidation.cs`.
* **Import Job Lifecycle Foundation**: `ImportJob.cs` and `ImportJobTransitionGuard.cs` managing `Created`, `Validating`, `ReadyForReview`, `Committing`, `Completed`, and `Cancelled` states.
* **Authentication & Authorization Policies**: `PlatformPermissions.ImportCreate`, `ImportView`, `ImportReview`, `ImportCommit`, `ImportAdmin` registered in `PlatformPermissions.cs`.
* **Portal Application Shell**: Navigation layout and permission gating in Next.js frontend.
* **Import Center Contract UI**: Contract-level table in `frontend/src/app/(dashboard)/imports/page.tsx` backed by `ImportJobRepository` abstraction (`ApiImportJobRepository` and `FixtureImportJobRepository`).

### 2. Partial
* **DataHub Ingestion Service & Controller**: Legacy `DataHubController.cs` and `DataHubIngestionService.cs` providing monolithic upload and staging for legacy Master Plan file structures.

### 3. Contract-Only
* **Import Platform Contracts**: `IImportMappingService`, `IImportValidationService`, `IImportPreviewService`, `IImportCommitService` in `ImportPlatformContracts.cs` are high-level interface stubs without domain services, transformation pipelines, rule engines, or preview fingerprinting.

### 4. Missing (Milestone Focus)
* **Provider-Neutral Mapping Service & Domain Model**:
  * Immutable `MappingProfile`, `MappingRule`, `MappingResult`, `MappedRecord`, `MappedField`, `MappingDiagnostic`.
  * Source coordinate & original value preservation.
  * Duplicate target detection and target validation.
  * Header matching (case-sensitive/insensitive).
* **Safe Transformation Pipeline**:
  * Pure, deterministic transformations (trim, explicit numeric/date parsing with explicit culture, default values, dictionary lookups).
  * Diagnostic generation on failed parsing; cancellation and limits support.
* **Validation Engine**:
  * Validation rules (`ValidationRule`, `ValidationProfile`, `ValidationResult`, `ValidationDiagnostic`, `ValidationSummary`).
  * Severities: `Information`, `Warning`, `Error`, `BlockingError`.
  * Scopes: `Field`, `Record`, `Worksheet`, `Workbook`.
  * Safe regex evaluation with timeout, uniqueness checks, cross-field predicates.
  * Maximum diagnostic bounds, non-mutating execution, deterministic ordering.
* **Review/Preview Model & Attestation Token**:
  * Summary calculations (records, warnings, errors, blocking errors).
  * Representative record sampling with original vs transformed values.
  * Server-generated, tamper-evident preview fingerprint/attestation token.
  * Owner and version scoping.
* **Import Job Lifecycle Integration**:
  * Integrated pipeline transitioning jobs from `Created` -> `Mapped` -> `Validating` -> `ReadyForReview`.
* **REST API Endpoints**:
  * Endpoints for mapping configuration, validation execution, preview generation, and paginated diagnostics.
* **Frontend Workflow & Components**:
  * Mapping configuration UI, Validation report UI, Mandatory Preview UI, and extended `ImportJobRepository` methods.

### 5. Explicitly Deferred
* Production DB commit persistence for final business entities.
* Database migration for final imported domain models.
* NASCA Excel integration / Client Agent integration.
* Server-side or browser-side Office COM / Excel automation.
* Business-specific confidential domain mappings.

---

## Baseline Test Verification

* **Backend (.NET 8.0)**: `dotnet test` passed 106 tests across `ApiIntegrationTests`, `DataHubChecks`, `ApiAuthChecks`, and `SeederSafetyChecks`.
* **Frontend (Next.js / Vitest / TypeScript)**:
  * `npm run test:run` passed 29 vitest tests.
  * `npm run typecheck` passed with 0 errors.

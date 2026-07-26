# Import Durable Persistence & Transactional Commit Assessment Report

**Date:** July 24, 2026  
**Repository:** `D:\Code_viber\Portal`  
**Branch:** `feature/import-durable-persistence`  
**Baseline Tag:** `import-review-foundation-complete`  
**Baseline Commit:** `2a56c6d docs(import): reconcile import workflow contracts and milestone hardening report`  

---

## Executive Summary

This report documents the architectural assessment of the Portal repository prior to implementing **Durable Import Persistence, Preview Invalidation, Idempotent Transactional Commit, Audit Trail, and Rollback Verification**.

---

## Inventory of Architectural Components

### 1. Implemented
* **NormalizedWorkbook Protocol (`v1.0`)**: Provider-neutral in-memory normalized workbook protocol.
* **Provider Registry & Providers**: CSV and XLSX data source providers.
* **Workbook Mapping Engine**: Immutable `MappingProfile`, `MappingRule`, `MappingResult`, `MappedField`, `MappedRecord` with safe deterministic transformations.
* **Import Validation Engine**: `ValidationRuleConfig`, `ValidationProfile`, `ValidationResult`, `ValidationSummary`, supporting range, text length, whitelist, safe regex timeouts, uniqueness, and cross-field rules.
* **Preview Attestation Service**: HMAC-SHA256 fixed-time attestation generation and verification.
* **Authorization Policies**: `PlatformPermissions.ImportCreate`, `ImportView`, `ImportReview`, `ImportCommit`, `ImportAdmin`.

### 2. In-Memory Only
* **Import Job Store**: `InMemoryImportJobStore` implementing `IImportJobStore`. State is process-local and resets upon process restart.
* **Idempotency & Concurrency**: Memory-only locking and version checks on `InMemoryImportJobStore`.

### 3. Missing (Milestone Focus)
* **EF Core Durable Import Job Persistence**:
  * `ImportJobEntity`: Persistent import job record storing state, provider metadata, profile versions, fingerprints, attestation details, and optimistic concurrency tokens.
  * `ImportMappedRecordEntity` / `ImportCanonicalPayloadEntity`: Bounded, deterministic canonical snapshot of mapped records required for commit execution.
  * `CommittedImportRecordEntity`: Generic synthetic target entity (`ItemCode`, `Quantity`, `InspectionDate`, `Result`) for commit verification.
  * `ImportAuditEventEntity`: Append-only audit trail table for tracking job lifecycle events.
  * `EfImportJobStore`: EF Core implementation of `IImportJobStore`.
* **Durable Preview Invalidation Engine**:
  * Automatic preview invalidation upon changes to normalized source, provider, mapping configuration, or validation rules.
* **Transactional Commit Engine**:
  * `IImportCommitService`: Single atomic EF Core database transaction (`AppDbContext.Database.BeginTransactionAsync`).
  * Enforces `import.commit` policy, valid unexpired attestation token, matching content fingerprint, no blocking errors, and optimistic concurrency version.
  * Rollback verification & failure injection mechanism.
* **Durable Idempotency**:
  * `ImportCommitReceiptEntity`: Durably stores commit receipts for idempotent replay of duplicate commit requests.
* **API Endpoints**:
  * `POST /api/import-jobs/{jobId}/commit`
  * `GET /api/import-jobs/{jobId}/commit`
  * `GET /api/import-jobs/{jobId}/audit`
* **Frontend Commit Workflow**:
  * Final commit review UI, attestation display, eligibility checks, confirmation modal, idempotent replay UI, and audit timeline.

### 4. Explicitly Deferred
* NASCA Excel integration / Client Agent integration.
* Server-side or browser-side Office COM / Excel automation.
* Production document conversion / company-specific proprietary schemas.

---

## Baseline Verification Results

* **Backend (.NET 8.0)**: **121 / 121 Passed** across `DataHubChecks`, `ApiIntegrationTests`, `ApiAuthChecks`, and `SeederSafetyChecks`.
* **Frontend (Next.js 16 / TypeScript)**:
  * `npm run typecheck`: **0 Errors**.
  * `npm run test:run`: **33 / 33 Passed**.

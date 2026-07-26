# Import Production Readiness & Operational Hardening Assessment Report

**Date:** July 24, 2026  
**Repository:** `D:\Code_viber\Portal`  
**Branch:** `feature/import-production-readiness`  
**Baseline Tag:** `import-durable-persistence-complete`  
**Baseline Commit:** `531f726 docs(import): document durable persistence transactional commit and audit contracts`  

---

## Executive Summary

This report documents the architectural assessment of the Portal repository prior to implementing **Operational Hardening: Background Execution, Durable Work Queue, Transactional Outbox, Structured Logging, Metrics, Health Checks, Input Abuse Protection, Performance Baselines, and Operational Runbooks**.

---

## Inventory of Architectural Components

### 1. Production-Ready
* **Durable Entities & Persistence**: `PersistentImportJob`, `PersistentImportMappedPayload`, `CommittedImportRecord`, `PersistentImportCommitReceipt`, `PersistentImportAuditEvent` in EF Core (`AppDbContext`).
* **Transactional Commit Engine**: `ImportCommitService` executing atomic database transactions.
* **Preview Invalidation**: `PreviewInvalidationEngine` detecting content, profile, and expiration staleness.
* **Authorization Policies**: Role-based permissions (`ImportCreate`, `ImportView`, `ImportReview`, `ImportCommit`, `ImportAdmin`).

### 2. Synchronous / In-Memory Only
* **Commit Execution**: Currently synchronous via `POST /api/import-jobs/{jobId}/commit`. Needs conversion to async background queueing (`Queued` $\rightarrow$ `Committing` $\rightarrow$ `Completed`) with atomic lease acquisition and backoff retries.

### 3. Missing (Milestone Scope)
* **Durable Work Queue**: `PersistentImportWorkItem` entity & `EfImportWorkQueue` for EF-backed async task processing.
* **Background Worker**: Hosted service (`ImportCommitBackgroundWorker`) executing queued commit tasks with lease recovery and poison job protection.
* **Transactional Outbox**: `PersistentImportOutboxMessage` entity & `ImportOutboxDispatcher` for post-commit event publishing.
* **Structured Logging & Metrics**: `ImportMetrics` using `System.Diagnostics.Metrics.Meter` for metrics emission without high-cardinality label pollution.
* **Health Checks**: ASP.NET Core `AddHealthChecks()` with Liveness (`/health/live`), Readiness (`/health/ready`), and Degraded Operational backlog checks (`/health/degraded`).
* **Diagnostics Pagination**: Server-side filterable pagination (`page`, `pageSize`, `severity`, `field`) bounded to 1,000 diagnostics per query.
* **Parser Abuse Hardening**: Protection against CSV formula injection (`=`, `+`, `-`, `@`), malformed UTF-8, excessively long lines, corrupted XLSX, ZIP bombs, and macro workbooks.
* **Performance Baselines**: Opt-in benchmark suite in `backend/tests/IqcQms.DataHubChecks/PerformanceBaselineTests.cs` covering 1k, 10k, and 100k synthetic record datasets.
* **Operational Documentation**: Operations runbook, observability guide, and health check specifications.

### 4. Explicitly Deferred
* External message broker (RabbitMQ/Kafka) — EF Core outbox and queue are repository-native and sufficient.
* NASCA Excel integration / Client Agent integration.
* Server-side or browser-side Office COM / Excel automation.

---

## Baseline Verification Results

* **Backend (.NET 8.0)**: **126 / 126 Passed** across `DataHubChecks`, `ApiIntegrationTests`, `ApiAuthChecks`, and `SeederSafetyChecks`.
* **Frontend (Next.js 16 / TypeScript)**:
  * `npm run typecheck`: **0 Errors**.
  * `npm run test:run`: **33 / 33 Passed**.

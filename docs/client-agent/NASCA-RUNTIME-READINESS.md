# IQC Nexus Client Agent — NASCA Runtime Readiness Matrix & Decision (Phase 3A.2)

**Status:** DECISION: **STOP_INCOMPLETE_EVIDENCE**
**Date:** July 25, 2026

---

## 1. Runtime Readiness Matrix

| # | Criterion | Evaluated Status | Details / Evidence Requirement |
| :- | :--- | :--- | :--- |
| 1 | **Product Identity Known** | **Fail** | Missing official vendor documentation for product name |
| 2 | **Executable Identity Known** | **Fail** | Missing operator-supplied binary metadata inspection |
| 3 | **Publisher Policy Known** | **Fail** | Missing vendor publisher verification |
| 4 | **Version Policy Known** | **Fail** | Missing allowed product version list |
| 5 | **Architecture Known** | **Fail** | Missing target OS bitness confirmation |
| 6 | **Interface Documented** | **Fail** | Missing official vendor CLI / interface manual |
| 7 | **Input Format Documented** | **Fail** | Missing input workbook specification |
| 8 | **Output Format Documented** | **Fail** | Missing output file specification |
| 9 | **Output Correlation Deterministic** | **Fail** | Missing correlation ID mapping rule |
| 10 | **Exit Codes Documented** | **Fail** | Missing exit-code table |
| 11 | **Timeout Behavior Documented** | **Fail** | Missing process hang timeout guidelines |
| 12 | **Cancellation Behavior Understood** | **Fail** | Missing process tree cleanup rule |
| 13 | **Concurrency Bounded** | **Fail** | Missing maximum concurrent instance limit |
| 14 | **Interactive-Session Requirement Supported** | **Fail** | Missing per-user session test report |
| 15 | **Licensing Permits Automation** | **Fail** | Missing vendor EULA automation authorization |
| 16 | **Excel Dependency Acceptable** | **Fail** | Missing clean environment test without Office installed |
| 17 | **No UI Automation Required** | **Pass** | UI automation is strictly prohibited by architecture |

---

## 2. Definitive Runtime Decision

> [!CAUTION]
> **DECISION: STOP_INCOMPLETE_EVIDENCE**
> 
> NASCA integration CANNOT be enabled (`NascaOptions.Enabled` MUST remain `false`).
> 
> **Rationale**: 16 out of 17 mandatory readiness checklist criteria have status **Fail** due to incomplete vendor documentation and missing operator evidence. No OS processes shall be launched (`Process.Start` is completely absent), and `NascaJobRunnerNotConfigured` will fail fast on any execution request.

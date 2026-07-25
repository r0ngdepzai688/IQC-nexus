# IQC Nexus Client Agent — NASCA Interface Classification & Evidence Record (Phase 3A.2 Revision)

**Status:** DECISION: **STOP_INCOMPLETE_EVIDENCE**
**Date:** July 25, 2026

---

## Interface Classification & Readiness

| Interface Class | Vendor Documentation Status | Readiness Status | Action |
| :--- | :--- | :--- | :--- |
| **Class A — Command-Line CLI** | **Unverified / Missing** | **STOP** | Preferred candidate if CLI switches are documented by vendor. |
| **Class B — Watched Folder** | **Unverified / Missing** | **STOP** | Secondary candidate if file drop is documented. |
| **Class C — API / SDK / COM / IPC** | **Unverified / Missing** | **STOP** | Unverified. |
| **Class D — UI Automation** | **REJECTED BY RULE** | **STOP** | **STRICTLY BANNED**. |

---

## Decision Logic

1. No interface has vendor documentation ingested into the repository or operator manifest (`STOP_INCOMPLETE_EVIDENCE`).
2. `INascaJobRunner` remains bound to `NascaJobRunnerNotConfigured` returning `NascaJobOutcome.NotConfigured`.
3. Zero processes shall be launched (`Process.Start` is absent).

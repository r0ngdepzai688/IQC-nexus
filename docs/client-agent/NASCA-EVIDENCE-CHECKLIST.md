# IQC Nexus Client Agent — NASCA Operator & Vendor Evidence Checklist (Phase 3A.1)

**Status:** MANDATORY BEFORE PRODUCTION NASCA INTEGRATION ACTIVATION
**Date:** July 25, 2026

## Objective

To replace unverified architectural proposals with authoritative, operator-confirmed or vendor-documented evidence before enabling NASCA automation or process execution.

> [!IMPORTANT]
> Do NOT collect, transmit, or store license keys, proprietary customer workbooks, or confidential business data during evidence collection.

---

## Operator & Vendor Required Evidence Items

| # | Item Description | Status | Evidence Source / Reference |
| :- | :--- | :--- | :--- |
| 1 | **Exact Product Name** | **Unknown** | Pending official vendor integration manual or operator screenshot |
| 2 | **Vendor / Publisher Name** | **Unknown** | Pending file version metadata or Authenticode signature |
| 3 | **Exact Product Version** | **Unknown** | Pending operator installation audit |
| 4 | **Executable Filename & Relative Path** | **Unknown** | Pending operator verification |
| 5 | **Canonical Installation Path** | **Unknown** | Pending operator environment inspection |
| 6 | **Bitness & System Architecture** | **Unknown** | Pending x86 vs x64 verification |
| 7 | **Official Integration Manual / User Guide** | **Unknown** | Pending vendor documentation upload |
| 8 | **Command-Line Help Output (`--help` / `/?`)** | **Unknown** | Pending operator capture |
| 9 | **Sample Input Specification** | **Unknown** | Synthetic workbook specification only |
| 10 | **Sample Output Specification** | **Unknown** | Pending vendor format specification |
| 11 | **Process Exit-Code Table** | **Unknown** | Pending vendor documentation |
| 12 | **Timeout & Process Hang Behavior** | **Unknown** | Pending operator stress test report |
| 13 | **Licensing Terms for Unattended Automation** | **Unknown** | Pending vendor EULA review |
| 14 | **Interactive Session Dependency** | **Unknown** | Pending Windows service vs GUI session test |
| 15 | **Microsoft Office / Excel Dependency** | **Unknown** | Pending clean machine test without Office installed |
| 16 | **Maximum Supported Concurrency** | **Unknown** | Pending vendor concurrency guideline |
| 17 | **Output Completion Handoff Signal** | **Unknown** | Pending file lock / marker verification |

---

## Rules of Evidence

1. **Generic Web Search Rejection**: Information obtained from generic web searches for similarly named software is strictly REJECTED.
2. **Path / Filename Assumptions**: Executable names (e.g. `NascaConverter.exe`) and command arguments (e.g. `--input`, `--format json`) MUST NOT be treated as confirmed until verified by vendor evidence.
3. **Fail-Closed Execution**: Until items 1 through 17 are verified and approved, `NascaOptions.Enabled` MUST remain `false`, and `NascaJobRunnerNotConfigured` MUST reject requests safely.

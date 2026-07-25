# IQC Nexus Client Agent — NASCA Operator & Vendor Evidence Checklist (Phase 3A.2 Audit)

**Status:** INCOMPLETE — RUNTIME DECISION: **STOP_INCOMPLETE_EVIDENCE**
**Date:** July 25, 2026

## Objective

To replace unverified architectural proposals with authoritative, operator-confirmed or vendor-documented evidence before enabling NASCA automation or process execution.

---

## Operator & Vendor Required Evidence Items Status

| # | Item Description | Status | Evidence Source / Reference |
| :- | :--- | :--- | :--- |
| 1 | **Exact Product Name** | **Missing** | Item `EVD-001` in [NASCA-EVIDENCE-MANIFEST.md](file:///D:/Code_viber/Portal/docs/client-agent/NASCA-EVIDENCE-MANIFEST.md) |
| 2 | **Vendor / Publisher Name** | **Missing** | Item `EVD-001` |
| 3 | **Exact Product Version** | **Missing** | Item `EVD-001` |
| 4 | **Executable Filename & Relative Path** | **Missing** | Item `EVD-002` |
| 5 | **Canonical Installation Path** | **Missing** | Item `EVD-003` |
| 6 | **Bitness & System Architecture** | **Missing** | Item `EVD-002` |
| 7 | **Official Integration Manual / User Guide** | **Missing** | Item `EVD-001` |
| 8 | **Command-Line Help Output (`--help` / `/?`)** | **Missing** | Item `EVD-004` |
| 9 | **Sample Input Specification** | **Missing** | Item `EVD-001` |
| 10 | **Sample Output Specification** | **Missing** | Item `EVD-001` |
| 11 | **Process Exit-Code Table** | **Missing** | Item `EVD-001` |
| 12 | **Timeout & Process Hang Behavior** | **Missing** | Item `EVD-001` |
| 13 | **Licensing Terms for Unattended Automation** | **Missing** | Item `EVD-001` |
| 14 | **Interactive Session Dependency** | **Missing** | Item `EVD-003` |
| 15 | **Microsoft Office / Excel Dependency** | **Missing** | Item `EVD-001` |
| 16 | **Maximum Supported Concurrency** | **Missing** | Item `EVD-001` |
| 17 | **Output Completion Handoff Signal** | **Missing** | Item `EVD-001` |

---

## Current Decision

**STOP_INCOMPLETE_EVIDENCE**: Until all 17 items are supplied and verified, `NascaOptions.Enabled` MUST remain `false`.

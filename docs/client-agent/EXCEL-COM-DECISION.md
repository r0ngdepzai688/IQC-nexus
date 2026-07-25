# IQC Nexus Client Agent — Architectural Decision Record: Excel COM Exclusion (Phase 3A.1 Revision)

**Status:** APPROVED & MANDATORY
**Date:** July 25, 2026

## Decision Summary

1. **Excel COM Automation (`Microsoft.Office.Interop.Excel`)**: **STRICTLY PROHIBITED AND REJECTED**.
   - No Office interop assemblies shall be referenced, imported, or invoked by the Client Agent.
   - The Client Agent will NEVER create `Excel.Application` COM objects or simulate user desktop interactions.

2. **NASCA Internal Excel Dependency Status**: **UNKNOWN (PENDING VENDOR EVIDENCE)**.
   - Whether the proprietary NASCA binary itself requires Microsoft Excel to be installed on the host machine is currently **Unknown** and pending official vendor evidence.
   - **Crucial Distinction**: Even if NASCA requires Microsoft Excel to be installed on the host operating system for its internal file conversion engine, the Client Agent MUST still interact with NASCA exclusively through a safe file-based or CLI process boundary (`INascaJobRunner`). The Client Agent itself will NEVER invoke Excel COM directly.

3. **Stop Condition**:
   - If vendor verification reveals that NASCA cannot be operated via CLI or file exchange and explicitly requires the Client Agent to invoke direct Excel COM automation, integration MUST STOP IMMEDIATELY and report to technical leadership.

## Operational Risk Analysis

- **Orphaned Processes**: Excel COM interop frequently leaves background `EXCEL.EXE` processes stuck in memory after exceptions or crashes.
- **Session Locking**: COM automation requires an interactive desktop message pump, failing in unattended or background service environments.
- **Licensing & Stability**: Microsoft explicitly advises against unattended server/service COM automation of Office applications.

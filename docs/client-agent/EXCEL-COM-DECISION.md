# IQC Nexus Client Agent — Architectural Decision Record: Excel COM Exclusion

**Status:** APPROVED & MANDATORY
**Date:** July 25, 2026

## Context & Problem Statement

Legacy enterprise automation systems often rely on Microsoft Excel COM Automation (`Microsoft.Office.Interop.Excel` / `Excel.Application` interop assemblies) to parse, macro-execute, or convert proprietary Excel workbooks.

In the IQC Nexus Client Agent foundation, we must evaluate whether Excel COM automation is required or permitted for NASCA integration workflows.

## Decision Analysis & Key Questions

1. **Does NASCA require Excel to be installed?**
   - **No**. NASCA command-line utilities and file conversion engines parse binary workbook formats directly without requiring a local Office / Excel installation.

2. **Does NASCA itself automate Excel internally?**
   - **No**. NASCA operates standalone on raw workbook files or XML/JSON structures.

3. **Does the Client Agent need to control Excel directly?**
   - **No**. The Client Agent consumes synthetic normalized payloads (`NormalizedWorkbook`) or invokes standalone CLI tools via a safe process boundary (`INascaJobRunner`).

4. **Is a file-based interface sufficient?**
   - **Yes**. Command-line flags (`--input`, `--output-dir`) and file-drop directories provide complete process and file isolation.

5. **Is Open XML processing sufficient before or after NASCA?**
   - **Yes**. Standard .NET stream processing, JSON normalization, and Open XML libraries parse structured data safely without Office automation.

6. **Would Excel COM require same-user interactive desktop execution?**
   - **Yes**, and COM requires interactive desktop state, window message pumps, and complex DCOM permissions.

7. **What are the risks of orphaned Excel.exe processes?**
   - Unhandled exceptions or crashes during COM interop leave orphaned background `EXCEL.EXE` processes, leaking memory, locking file handles, causing registry corruption, and requiring manual process termination.

8. **What cleanup and retry guarantees would be required for COM?**
   - Complex COM garbage collection (`Marshal.ReleaseComObject`), Win32 process enumeration, and force-kill logic.

## Final Decision

**Excel COM automation is STRICTLY PROHIBITED and REJECTED in IQC Nexus.**

- No `Microsoft.Office.Interop.Excel` assemblies shall be referenced or imported.
- No `Excel.Application` COM objects shall be created.
- All NASCA integration MUST use a safe command-line or file-based adapter boundary (`INascaJobRunner`).

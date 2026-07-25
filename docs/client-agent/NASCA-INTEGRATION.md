# IQC Nexus Client Agent — NASCA Integration Architecture (Phase 3A.5)

## Overview

Phase 3A.5 implements the complete secure work-directory lifecycle (`INascaWorkDirectoryManager`) for NASCA-bound execution tasks.

## Guarantees

1. **Work Directory Isolation**: Every job receives an isolated work directory (`%LocalAppData%\IqcQmsAgent\NascaWork\work_<correlationId>`).
2. **Atomic Staging**: Input workbook staging is atomic with SHA-256 hash validation. Original input file is untouched.
3. **Quarantine & Retention**: Suspension and quarantine of corrupt work directories prevent evidence loss. Active directories are protected from cleanup.
4. **Zero Process Launch**: `Process.Start` remains 100% absent in runtime code.
5. **Zero Office Interop**: `Microsoft.Office.Interop.Excel` remains strictly unreferenced.

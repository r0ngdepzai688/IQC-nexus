# IQC Nexus Client Agent — NASCA Integration Architecture (Phase 3A.5 Security Closure)

## Overview

Phase 3A.5 Security Closure reinforces the secure work-directory lifecycle (`INascaWorkDirectoryManager`) with opaque folder identifiers, reparse-point defense (`INascaPathSecurityGuard`), TOCTOU mitigation, and cleanup/discovery containment.

## Guarantees

1. **Opaque Workspace Identifiers**: Folder names follow `work_<32_hex_chars>` and do not leak user paths or correlation identifiers.
2. **Reparse-Point Defense**: Symlinks, junctions, and reparse points fail closed across all work tree operations.
3. **Atomic Staging**: Input workbook staging is atomic with SHA-256 hash validation. Original input file is untouched.
4. **Quarantine & Retention**: Suspension and quarantine of corrupt work directories prevent evidence loss. Active directories are protected from cleanup.
5. **Zero Process Launch**: `Process.Start` remains 100% absent in runtime code.
6. **Zero Office Interop**: `Microsoft.Office.Interop.Excel` remains strictly unreferenced.

# Client Agent Pre-NASCA Hardening Assessment

**Date:** July 24, 2026
**Status:** Phase 2C Read-Only Inspection Complete
**Branch:** `feature/client-agent-pre-nasca-hardening`
**Scope:** Assessment of Allowed Input Roots, Windows Reparse-Point & Path Boundary Hardening

---

## Phase 2C Inspection Matrix (Allowed Input Roots & Path Security)

| # | Inspection Item | Classification | Inspection Findings & Analysis |
| :- | :--- | :--- | :--- |
| 1 | **Lexical Canonicalization** | **IMPLEMENTED BUT NEEDS MORE TESTS** | Uses `Path.GetFullPath`, but lacks trailing separator normalization, DOS device prefix stripping (`\\.\`), or alternate data stream check. |
| 2 | **Root-Boundary Comparison** | **SECURITY RISK** | Uses `fullPath.StartsWith(root, OrdinalIgnoreCase)` without a trailing separator check. Vulnerable to sibling directory prefix collisions (e.g. `C:\Allowed2\file.xlsx` accepted for root `C:\Allowed`). |
| 3 | **Reparse-Point Traversal** | **SECURITY RISK** | Windows NTFS junctions, directory symlinks, and volume mount points are not resolved component-by-component to their physical target. `Path.GetFullPath` returns the link path rather than target destination. |
| 4 | **Final-File Validation** | **MISSING** | Lacks validation ensuring candidate path is a regular file (not directory, named pipe, device, or alternate data stream `file.xlsx:stream`). |
| 5 | **Path Race Exposure (TOCTOU)** | **SECURITY RISK** | Path is validated at enqueue time, but `Worker.cs` does not revalidate the path immediately before processing/opening. |
| 6 | **UNC / Mapped-Drive Behavior** | **IMPLEMENTED BUT NEEDS MORE TESTS** | Standard UNC paths (`\\server\share`) resolve, but device namespaces (`\\.\`, `\\?\GLOBALROOT`) are not explicitly rejected. |
| 7 | **Alternate Data Streams** | **MISSING** | `file.xlsx:stream` syntax is not inspected or rejected. |
| 8 | **Non-Existing Path Behavior** | **VERIFIED** | Non-existing files fail fast during normalization attempt. |

---

## Selected Policy & Design for Phase 2C

1. **Dedicated Path Security Abstraction**: Implement `IAllowedInputPathValidator` and `AllowedInputPathValidator`.
2. **Reparse-Point Policy**: Component-by-component path resolution from root to target. Reject candidate if any link or junction resolves outside configured allowed roots.
3. **Separator-Aware Boundary Check**: `candidate == root || candidate.StartsWith(root + Path.DirectorySeparatorChar)`.
4. **Enqueue & Processing Time Dual Validation**: Revalidate path in `Worker.cs` immediately before processing.
5. **File Namespace Hardening**: Reject device namespaces (`\\.\`), alternate data streams (`:`), and non-regular files.

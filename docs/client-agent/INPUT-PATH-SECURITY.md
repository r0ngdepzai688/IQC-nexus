# IQC Nexus Client Agent — Allowed Input Roots & Path Boundary Hardening

## Overview

The IQC Nexus Client Agent enforces strict physical target resolution and boundary validation for all candidate input file paths before accepting them into the processing pipeline.

## Why `Path.GetFullPath` Alone Is Insufficient

Standard lexical canonicalization (e.g. `Path.GetFullPath(candidate).StartsWith(root)`) is vulnerable to:
1. **Sibling Prefix Collisions**: Root `C:\Allowed` matches candidate `C:\Allowed2\file.xlsx` when using simple string prefix checks without directory separator awareness.
2. **NTFS Junctions and Symbolic Links**: `Path.GetFullPath` does not resolve the physical target destination of directory junctions, symlinks, or volume mount points. A link at `C:\Allowed\Link` pointing to `C:\Windows` would pass standard lexical prefix checks.
3. **Alternate Data Streams**: `C:\Allowed\file.xlsx:stream` can bypass simple extension checks.
4. **Device Namespaces**: `\\.\` or `\\?\GLOBALROOT` namespace paths bypass standard drive/UNC security controls.

## Path Security Architecture

### 1. `IAllowedInputPathValidator`
All candidate file paths are validated through `IAllowedInputPathValidator` ([AllowedInputPathValidator.cs](file:///D:/Code_viber/Portal/backend/src/IqcQms.ClientAgent.Infrastructure/Storage/AllowedInputPathValidator.cs)):
- **Component-by-Component Resolution**: Recursively resolves every path component from the drive root to the target file.
- **Reparse Point Target Verification**: Resolves directory junctions and symbolic links to their physical target destinations. Rejects any link resolving outside configured `AllowedInputRoots`.
- **Separator-Aware Boundary Check**: `candidate == root || candidate.StartsWith(root + Path.DirectorySeparatorChar)`.

### 2. Dual Validation (Enqueue-Time & Processing-Time)
To reduce Time-of-Check to Time-of-Use (TOCTOU) race windows:
1. **Enqueue-Time Validation**: Evaluated in `SqliteLocalAgentQueue.EnqueueJobAsync` before a job is enqueued.
2. **Processing-Time Revalidation**: Evaluated in `Worker.ProcessNextLocalJobAsync` immediately before opening and normalizing the file.

### 3. Explicit Namespace & File Type Restrictions
- **Device Namespaces**: `\\.\` and `\\?\` paths are rejected (`DeviceNamespaceNotAllowed`).
- **Alternate Data Streams**: Colons after drive prefix are rejected (`AlternateDataStreamNotAllowed`).
- **Directories**: Directory candidates are rejected (`NotRegularFile`).
- **Relative Paths**: Non-fully-qualified paths are rejected (`RelativePathNotAllowed`).

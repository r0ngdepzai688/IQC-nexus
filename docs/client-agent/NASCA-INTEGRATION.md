# IQC Nexus Client Agent — NASCA Integration Architecture (Phase 3A.1)

## Overview

Phase 3A.1 refines the NASCA integration architecture to enforce strict evidence-based verification. All Phase 3A proposed assumptions (such as executable names, command-line arguments, and output file formats) are explicitly classified as **Proposed / Unknown** until confirmed by operator evidence.

## Evidence-Based Classification Summary

- **Adapter Boundary Interface (`INascaJobRunner`)**: **Verified / Complete** (Defined in `NascaJobContracts.cs`).
- **Disabled Scaffolding (`NascaJobRunnerNotConfigured`)**: **Verified / Complete** (Fails safely when `NascaOptions.Enabled = false`).
- **Read-Only Metadata Inspector (`INascaInstallationInspector`)**: **Verified / Complete** (Inspects file version metadata of explicitly configured paths without process execution).
- **Executable Filename (`NascaConverter.exe`)**: **Proposed / Unverified** (Pending operator evidence).
- **CLI Arguments (`--input`, `--output-dir`, `--format json`)**: **Proposed / Unverified** (Pending vendor documentation).
- **Watched Output Directory**: **Proposed / Unverified** (Pending vendor documentation).
- **Standalone Engine vs Office Dependency**: **Unknown** (Pending vendor documentation).

## Hard Security Boundaries

1. **No Process Launch in Discovery Phases**: `Process.Start` is NEVER invoked.
2. **No Arbitrary Disk or Registry Scanning**: Inspector checks ONLY the explicitly configured absolute path.
3. **No Excel COM Automation**: Office interop assemblies are strictly prohibited.
4. **Disabled by Default**: `NascaOptions.Enabled` defaults to `false`.

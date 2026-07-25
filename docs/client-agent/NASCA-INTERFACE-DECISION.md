# IQC Nexus Client Agent — NASCA Interface Classification & Evidence Record (Phase 3A.1)

**Status:** PROPOSED ARCHITECTURE — PENDING OPERATOR EVIDENCE
**Date:** July 25, 2026

---

## Interface Classification Matrix

| Interface Class | Description | Vendor Status | Decision & Action |
| :--- | :--- | :--- | :--- |
| **Class A — Command-Line CLI** | Executable with argument flags | **Proposed / Unverified** | Preferred candidate once vendor arguments are verified by operator. |
| **Class B — Watched Folder / File Exchange** | File drop directory with completion markers | **Proposed / Unverified** | Secondary candidate if CLI interface is unavailable. |
| **Class C — API / SDK / COM / IPC** | Native API DLL or COM server | **Unknown** | Unverified. |
| **Class D — UI Automation** | Screen clicking, SendKeys, UI Automation | **REJECTED** | **STRICTLY PROHIBITED**. If UI automation is the only available interface, integration STOPS immediately. |

---

## Candidate Interface Evaluation

### Class A — CLI (Proposed)
- **Status**: Proposed assumption from Phase 3A awaiting vendor evidence.
- **Contract**: `INascaJobRunner.RunJobAsync(NascaJobRequest request)`.
- **Process Security**: `ProcessStartInfo` with `UseShellExecute = false`, argument list array, bounded timeouts.
- **Pending Verification**: Exact command-line flags (`--input`, `--output`, etc.) and exit-code meanings.

### Class B — Watched Folder (Proposed)
- **Status**: Proposed secondary alternative.
- **Contract**: File staging in `InputDirectory` with atomic rename/lock checks.
- **Pending Verification**: File completion signaling mechanism (e.g. `.done` marker vs file lock).

### Class D — UI Automation (Rejected)
- **Status**: STRICTLY PROHIBITED.
- **Rules**: Keyboard/mouse automation, SendKeys, and UI Automation framework calls are strictly banned. If NASCA supports UI-only interaction, integration must STOP and report to project leads.

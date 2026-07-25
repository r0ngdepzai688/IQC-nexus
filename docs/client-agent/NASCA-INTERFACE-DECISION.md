# IQC Nexus Client Agent — NASCA Interface Classification & Decision (Phase 3A.3)

**Status:** Strongly Typed Enum Classification — Decision: `StopEvidenceMissing`
**Date:** July 25, 2026

---

## Strongly Typed Enum Interface Matrix (`NascaVerifiedInterfaceType`)

| Enum Value | Description | Production Status | Action |
| :--- | :--- | :--- | :--- |
| **`None`** | Default unverified state | **Active Baseline** | Application startup fails fast if `Enabled = true`. |
| **`Cli`** | Documented command-line flags | **Architectural Category** | Candidate if vendor CLI flags are verified. |
| **`WatchedFolder`** | Documented file drop directory | **Architectural Category** | Candidate if watched folder is verified. |
| **`Api`** | Native API / DLL | **Architectural Category** | Unverified. |
| **`Ipc`** | Inter-process communication | **Architectural Category** | Unverified. |
| **`ComServer`** | Registered COM server | **Architectural Category** | Unverified. |
| **`UiOnly`** | UI Automation / SendKeys | **STRICTLY PROHIBITED** | Always triggers validation failure and `StopUiOnly`. |

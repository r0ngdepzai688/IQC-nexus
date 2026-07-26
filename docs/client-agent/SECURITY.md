# IQC Nexus Client Agent — Security Architecture (Phase 3A.5 Security Closure)

## Security Boundaries & Rules

1. **Path Boundary Security**: All inputs must pass `AllowedInputRoots` validation.
2. **Process Execution Policy**: Zero processes are launched (`Process.Start` is 100% absent in runtime code).
3. **Office Interop Policy**: `Microsoft.Office.Interop.Excel` is strictly prohibited and unreferenced.
4. **Local Work Directory Security**: `NascaWorkDirectoryManager` uses opaque folder names (`work_<32_hex_chars>`), atomic file staging, hash verification, root boundary containment, reparse-point defense (`INascaPathSecurityGuard`), and quarantines suspicious or corrupt directories.
5. **Log Privacy Policy**: Local file system paths are redacted in log messages.

# IQC Nexus Client Agent — NASCA Operations Runbook (Phase 3A.1)

## Configuration Contract

```json
{
  "NascaOptions": {
    "Enabled": false,
    "ExecutablePath": "C:\\Program Files\\NASCA\\Nasca.exe",
    "WorkingDirectory": "C:\\Program Files\\NASCA\\",
    "InputDirectory": "C:\\IqcQmsInputs\\",
    "OutputDirectory": "C:\\IqcQmsOutputs\\",
    "TimeoutSeconds": 60,
    "MaximumConcurrentJobs": 1,
    "ExpectedProductName": "",
    "ExpectedPublisher": "",
    "AllowedProductVersions": [],
    "RequireAuthenticodeSignature": false
  }
}
```

## Operator Verification Steps

Before setting `Enabled: true` in production:
1. Complete all items in [NASCA-EVIDENCE-CHECKLIST.md](file:///D:/Code_viber/Portal/docs/client-agent/NASCA-EVIDENCE-CHECKLIST.md).
2. Configure `ExecutablePath` and `OutputDirectory` as absolute paths.
3. Confirm `ExecutablePath` does not reside inside a writable input directory.
4. Verify `INascaInstallationInspector` returns `SanitizedReasonCode = "METADATA_INSPECTED"` for target binary.

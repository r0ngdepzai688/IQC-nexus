# IQC Nexus Client Agent — NASCA Operations Runbook

## Configuration Guidelines

`NascaOptions` in appsettings:

```json
{
  "NascaOptions": {
    "Enabled": false,
    "ExecutablePath": "C:\\Program Files\\NASCA\\NascaConverter.exe",
    "WorkingDirectory": "C:\\Program Files\\NASCA\\",
    "InputDirectory": "C:\\IqcQmsInputs\\",
    "OutputDirectory": "C:\\IqcQmsOutputs\\",
    "TimeoutSeconds": 60,
    "MaximumConcurrentJobs": 1
  }
}
```

## Production Security Rules

1. `Enabled` defaults to `false`.
2. In `Production`, `ExecutablePath` and `OutputDirectory` MUST be valid absolute paths on the host.
3. `ExecutablePath` CANNOT be located inside a writable input directory.
4. `TimeoutSeconds` must be positive and bounded (1 to 600 seconds).
5. `MaximumConcurrentJobs` must be positive and bounded (1 to 10).

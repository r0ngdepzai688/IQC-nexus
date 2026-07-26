# IQC Nexus Client Agent — Normalized Payload Upload Contract

## Request Structure

Endpoints accept `POST /api/agent-devices/{deviceId}/normalized-workbooks`.

```json
{
  "canonicalSchemaVersion": "1.0",
  "deviceId": "dev_12345",
  "serverImportJobId": "guid",
  "providerId": "SyntheticProvider",
  "providerVersion": "1.0.0",
  "sourceFingerprint": "sha256_hash",
  "normalizedWorkbook": {
    "workbookName": "Synthetic_MasterPlan.xlsx",
    "sheets": [
      {
        "sheetName": "MasterPlan",
        "rows": [
          {
            "rowIndex": 1,
            "cells": [
              { "columnName": "Model", "columnIndex": 1, "value": "SYNTH-01", "dataType": "String" }
            ]
          }
        ]
      }
    ]
  },
  "recordCount": 1,
  "diagnosticsSummary": "Synthetic Normalization Complete"
}
```

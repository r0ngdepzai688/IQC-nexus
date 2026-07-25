# IQC Nexus Client Agent — NASCA Evidence Intake Manifest (Phase 3A.2)

**Manifest ID:** `manifest_phase3a2_baseline_001`
**Evaluated At:** 2026-07-25T10:31:00Z
**Current Decision:** `STOP_INCOMPLETE_EVIDENCE`

---

## Registered Evidence Intake Items

| Evidence ID | Classification | Verification Status | Supplied By | Content Hash / Summary | Approved |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `EVD-001` | `VendorDocumentation` | `Missing` | None | Missing official vendor manual | No |
| `EVD-002` | `VendorSignedBinaryMetadata` | `Missing` | None | Missing target binary inspection | No |
| `EVD-003` | `OperatorConfirmed` | `Missing` | None | Missing operator installation audit | No |
| `EVD-004` | `ApprovedCommandHelpOutput` | `Missing` | None | Missing command help capture | No |

---

## Evidence Governance Rules

1. **Secret & Key Prohibition**: No license keys, customer workbooks, or confidential outputs are stored in this manifest.
2. **Redistribution Restrictions**: Vendor documentation and binary metadata hashes are stored as sanitized summaries.
3. **Immutability**: Items added to this manifest must include provenance (`SuppliedBy`, `EvidenceDate`, `SourceClassification`).

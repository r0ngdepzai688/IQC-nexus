# Master Plan Data Contract

## 1. Purpose
This document defines the strict data contract for importing R&D Master Plan Excel files into the IQC Nexus platform. It establishes the rules for column discovery, data type mapping, validation, cleaning, and upsert logic to ensure data integrity before modifying the core database.

## 2. Source Folder
Files are manually uploaded or dropped into the following staging directory:
By default, runtime files are stored below `DataHub/NewModels/MasterPlan/` under the API content root. Deployments may override each path through the `DataHub` configuration section; machine-specific absolute paths are not committed.

## 3. Supported File Types
- `.xlsx`
- `.xlsm`
- `.xls`

## 4. Header Row Detection Rule
The header row is not guaranteed to be row 1. The system scans the first 20 rows of the active sheet. The row containing the highest number of recognized Master Plan column aliases (e.g., "Project name", "SKU", "PVR Target") is selected as the definitive header row. Data extraction begins on the immediately following row.

## 5. Canonical Fields
The universal fields recognized by the internal Master Plan processor:
`ProjectName`, `Basic`, `Area`, `Grade`, `SKU`, `QtyLPR`, `QtyLSR`, `PVRTarget`, `PRATarget`, `SRATarget`, `HWPIC`, `ImportedStatus`, `Remark`

## 6. Required Hard Columns
These columns MUST exist in the file. If missing, the entire file is rejected.
- `ProjectName`
- `SKU`
- `PVRTarget`

## 7. Business Required Columns
These columns should exist. If missing, the system will raise a missing column warning, but processing may continue (data rows missing these values will be flagged in the Review Queue).
- `Area`
- `Grade`
- `HWPIC` (formerly IQCPIC)
- `PRATarget`
- `SRATarget`

## 8. Optional Columns
These columns enhance the data but are not strictly necessary.
- `Basic`
- `QtyLPR`
- `QtyLSR`
- `ImportedStatus`
- `Remark`

## 9. Alias Mapping
During header detection, columns are identified by normalizing strings (trim spaces, ignore case, remove line breaks) and matching against known aliases:
- **ProjectName**: Project Name, Project, Model, Model Name
- **Basic**: Basic, Base, Basic Model
- **Area**: Area, Product Area, Category, Area Buyer
- **Grade**: Grade, Rank
- **SKU**: SKU, SKU Code, Model Code
- **QtyLPR**: Q'ty LPR, Qty LPR, LPR Qty, Quantity LPR, LPR/LCV
- **QtyLSR**: Q'ty LSR, Qty LSR, LSR Qty, Quantity LSR, LSR
- **PVRTarget**: PVR Target, PVR, PVR Date, PVR Target Pre PRA
- **PRATarget**: PRA Target, PRA, PRA Date
- **SRATarget**: SRA Target, SRA, SRA Date
- **HWPIC**: IQC PIC, HW 검증 (IQC), HW 검증, HW 검증 RQE, PIC, Owner, Responsible Person
- **ImportedStatus**: Status, Trạng thái, Plan Status
- **Remark**: Remark, Remarks, Note, Notes, Comment, Other

## 10. Data Type for Each Field
- `ProjectName`: String (Max 100)
- `SKU`: String (Max 50)
- `Basic`: String (Max 100)
- `Area`: String (Max 50)
- `Grade`: String (Max 10)
- `QtyLPR`: Integer
- `QtyLSR`: Integer
- `PVRTarget`: DateTime
- `PRATarget`: DateTime
- `SRATarget`: DateTime
- `HWPIC`: String (Max 100)
- `ImportedStatus`: String (Max 50)
- `Remark`: String (Max 500)

## 11. Date Format Rules
- **Native Excel Dates**: Read directly as `DateTime` or OADate doubles.
- **Korean String Format**: Specifically intercepts formats like `MM월 DD일` (e.g., `05월 28일`) and translates them to standard DateTime using the current year.
- **ISO Strings**: Standard `YYYY-MM-DD` strings are parsed using invariant culture.

## 12. Number Format Rules
- Parsed using standard invariant culture integer parsing.
- Missing or non-numeric values (e.g., "TBD", "-") in quantity fields are mapped to `NULL`.
- "K" suffixes (e.g. `1.25K`) are ignored in raw int parsing unless explicitly cleaned.

## 13. Duplicate Key
At the file level: **SHA256 File Hash**. 
Files matching an exact hash of a previously imported batch are flagged as `ExactDuplicate` and rejected. 

## 14. Business Key
At the row level: **SKU**, compared case-insensitively. Duplicate SKUs within one file are blocking validation errors. An SKU already present in core data requires review and is not overwritten by import. A previously unseen SKU is eligible for insert.

## 15. Validation Rules
- **ProjectName**: Cannot be empty.
- **SKU**: Cannot be empty.
- **PVRTarget**: Cannot be empty.

## 16. Cleaning Rules
- **String Trimming**: Leading and trailing whitespaces are removed from all string fields.
- **Korean Name Parsing**: Line breaks in names (e.g., "Hoang Duc Nhan\n안준기") are cleaned or retained depending on UI display requirements.

## 17. Mapping Rules
- Any unmapped (extra) columns are ignored during ingestion.
- Missing optional columns default to empty string (`""`) or `NULL`.

## 18. Core Validation Rules
- Verify that `Area` matches known business categories (e.g., Mobile, Tablet, Wearable).
- Verify that `Grade` matches known values (e.g., A, B, S, D+).
- The `HWPIC` must be resolvable to a real employee ID or flagged for manual review if unknown.

## 19. Upsert Rules
- **INSERT**: If SKU is not found in `MasterPlans`, create a new record. Status defaults to "Active", ActionStatus defaults to "Future".
- **UPDATE**: If SKU exists, update QtyLPR, QtyLSR, Target Dates, and HWPIC. Do NOT overwrite existing ActionStatus unless explicitly triggered by business rules.

## 20. Error Handling
- Rows with critical errors (Missing SKU, Missing ProjectName, Invalid Dates) are marked as `RowStatus = "Error"` in `Staging_MasterPlan` and cannot be committed.
- Rows with warnings (e.g., missing Area or HWPIC) are marked as `RowStatus = "ReviewRequired"` and block commit until resolved.
- Commit is all-or-nothing: any validation, review, or blocked row prevents persistence to core tables.
- The UI disables commit when there are no valid rows or when any error/review row remains.

## 23. Current runtime behavior and known blockers
- Uploads are limited to non-empty `.xlsx`, `.xls`, or `.xlsm` files up to 50 MB. Missing and duplicate canonical headers reject the batch before staging rows are accepted.
- Row errors identify the source row and field in `ValidationErrors`; warnings remain distinct as review items.
- Core inserts, milestones, audit records, and batch status are committed in one database transaction. Existing Master Plan rows are never silently overwritten.
- The archive filesystem and database cannot share one transaction. A failed database operation can leave an unreferenced raw/report file for operational cleanup.
- Data Hub and committed Master Plan endpoints require the repository JWT bearer authentication. The frontend login now obtains that JWT from the existing `/api/auth/login` endpoint and sends it with Data Hub/Master Plan requests.
- Header inspection proposes exact/case-insensitive canonical matches. Explicit mappings are validated server-side; duplicate canonical fields, missing required fields, ambiguous suggestions, and unknown canonical fields are rejected.
- Review summaries expose row, SKU, field, current value, severity, and actionable messages. Existing-SKU rows remain blocked by default; an authenticated user may explicitly skip them or cancel the entire batch. Skip is audited and never overwrites core data.

## 21. Example Valid Row
| ProjectName | SKU | PVRTarget | PRATarget | HWPIC | QtyLPR |
|-------------|-----|-----------|-----------|-------|--------|
| Galaxy S26 | SM-S941B | 2026-08-01 | 2026-08-15 | Kim.HW | 150 |

## 22. Example Invalid Rows
| ProjectName | SKU | PVRTarget | Reason |
|-------------|-----|-----------|--------|
| *Empty* | SM-S941B | 2026-08-01 | **Error:** ProjectName is missing |
| Galaxy S26 | *Empty* | 2026-08-01 | **Error:** SKU is missing |
| Galaxy Z Fold | SM-F966B | *Empty* | **Error:** PVRTarget is missing |
| Galaxy Buds | SM-R600 | 99월 99일 | **Error:** PVRTarget is an invalid date string |

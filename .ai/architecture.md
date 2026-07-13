# IQC Nexus - Architecture & Logic Design

## 1. Role-Based Access Control (RBAC) & View Lenses

The application serves multiple levels of users, from factory workers to top-level executives. The system utilizes "Lenses" (Role-based Views) to ensure users only see and interact with what is relevant and safe for their role.

### 1.1 Data Lens (Backend Filtering)
- **Worker (Công nhân)**: Receives only tasks where `assignee === user.empId`. The Dashboard acts as a personal workspace focusing on immediate execution (Today's tasks, Overdue tasks).
- **Manager (Quản lý/Part Leader)**: Receives tasks created by them, tasks assigned to their subordinates, and team-level metrics.
- **Executive (Giám đốc)**: Receives aggregated metrics across all modules. Primarily interacts with Analytics and Gantt views for bird-eye project management.

### 1.2 Action Lens (Frontend Permissions)
- **Worker**: 
  - Allowed: Move tasks (`To Do` -> `In Progress` -> `Review`), Upload evidence files, Add progress comments.
  - Denied: Create tasks, Edit titles/descriptions, Set deadlines, Move tasks to `Done` (Approve), Delete tasks.
- **Manager**: 
  - Allowed: Create tasks, Edit tasks, Approve tasks (Move from `Review` to `Done`), Reject tasks (Move back to `In Progress`), Reassign tasks.

### 1.3 UX Lens (Simplified Mode)
- For non-tech-savvy workers (as identified by role), the UI should automatically switch to a "Simplified Mode".
- Characteristics: Primarily Vietnamese language, larger touch targets, removal of complex charts (Timeline/Gantt/Analytics hidden), and focus solely on the Kanban/List execution flow.

## 2. Task Execution Workflow (Directives)

When a Manager issues a directive (Task) to a Worker:
1. **Creation**: Manager sets Title, Description, Deadline, and assigns it using the `@name` Autocomplete (which captures the real `empId`). Task enters `To Do`.
2. **Execution**: Worker drags to `In Progress`. Worker can add comments to the Progress Updates timeline.
3. **Evidence Submission**: Worker uploads required files (e.g., inspection photos, reports).
4. **Review**: Worker drags task to `Review` column.
5. **Approval**: Manager reviews the evidence. If correct, Manager drags to `Done`. If incorrect, Manager leaves a comment and drags back to `In Progress`.

## 3. File Upload Architecture
1. **Frontend**: Dropzone in `TaskDetailModal`.
2. **API**: `POST /api/upload` (multipart/form-data) to the backend.
3. **Storage**: Backend streams file to S3 bucket or secure static volume.
4. **Response**: Backend returns a secure URL.
5. **Binding**: Frontend appends the secure URL to the Task's `evidenceFiles` array and updates the Task record in the Database.

## 4. Audit Trail
Every state change (column movement), comment, or file upload MUST be timestamped and tagged with the user's `empId` to ensure a strict chain of custody and accountability.

## 5. Master Plan Data Hub Architecture (New Models)
### 5.1 Pipeline (Process & Commit)
The system uses a 2-phase pipeline for Excel imports:
1. **Process Phase (No Writes to Core)**: Upload -> SHA256 Duplication Check -> ImportBatch Creation -> Raw Archive -> Excel Parsing (ExcelDataReader) -> StagingMasterPlan -> Validation Engine (V1-V13) -> Core Validation (Insert/Update detection) -> Preview.
2. **Commit Phase**: Upsert to `MasterPlanRecords` -> `DataHubAuditLog` entry per field change -> Move to Processed folder.

### 5.2 Core Entities
- **New Models Module**: `MasterPlanRecord`, `MasterPlanUpload`, `ProjectWorkspace`.
- **Data Hub Module**: `ImportBatch`, `RawFile`, `StagingMasterPlan`, `ValidationError`, `ReviewQueueItem`, `ImportLog`, `DataHubAuditLog`, `MappingDictionary`, `ImportSource`, `ImportNotification`.

### 5.3 Validation & Alias Engine
- **ExcelDataReader** is used for header detection (top 20 rows) and parsing.
- **Alias Mapping** resolves header names like "project name", "model", etc., to canonical names.
- **Validation Engine (V1-V13)** evaluates missing fields, formats, lengths, and date order logic. Generates statuses: `ValidationError`, `ReviewRequired`, or `ReadyToInsert/Update`.

### 5.4 UI Architecture (Frontend)
- **Unified Master Plan (`/new-model`)**: Split view with Left panel for search/selection, Right panel for Master Plan view vs Project Workspace (KPI Dashboard, Stepper). Implements Glassmorphism, specific design token paddings/radius, and strict Light/Dark mode.
- **Import Wizard (`/new-model/import-master-plan`)**: 4-step state machine (Select -> Processing -> Preview -> Committed). Includes summary cards, preview table with action badges, and commit confirmation.

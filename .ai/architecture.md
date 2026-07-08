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

# Current State

- **Platform:** IQC Nexus
- **Phase:** Design System, Permission Refactoring, and **New Models Module Redesign**
- **Latest Updates (Home Environment - Antigravity AI):**
  - **New Models Module UI Redesign:** Completely overhauled the `new-models/pipeline` module to an Enterprise SaaS workspace (Linear/Stripe style). Implemented the "Unified Master Plan" view with KPI Dashboard cards, Compact Filter Toolbar, Auto-sorting mechanism, relative date logic, and a sliding Right-Side Detail Drawer.
  - **Master Plan Backend API:** Built the C# `.NET 8` Backend API. Created `MasterPlanRecords`, `MasterPlanUploads`, and `ProjectWorkspaces` in SQLite via EF Core. Implemented `MasterPlanService` utilizing `ExcelDataReader` to parse R&D Excel files and perform automated Delta Checks (detecting changes in PVR dates and statuses).
  - **Database Migration:** Successfully ran `dotnet ef migrations add AddNewModels` and `dotnet ef database update`.
  - **DataHub Ingestion Pipeline:** Implemented E2E phase 1 manual upload, staging, business rules validation (Mapping dictionaries, User validation, Duplicate checks), resolution API, and commit to `MasterPlans` core tables.

# Dual-Environment Workflow (Home vs Office)
Due to data security, the project operates in two distinct modes:
1. **Home Environment (Antigravity AI):**
   - **Focus:** UI/UX Design, Architecture, Structural Refactoring, Permission system, Strict Linting.
   - **Data:** Uses Mock Data (simulated via MOCK arrays or environment flags).
2. **Office Environment (Cline AI + Real Data):**
   - **Focus:** API Integration, Business Logic, Data Binding, Authentication.
   - **Data:** Real confidential company data.

**Key Rule:** Both environments must strictly adhere to shared TypeScript interfaces to ensure the UI does not break when switching from Mock to Real data.

# Next Steps (Phase 3: Data Hub & Master Plan Pipeline)
- [ ] **Validation Errors Tab**: Fetch and display row-level errors for selected batch (`GET /api/datahub/validation-errors/{batchId}`).
- [ ] **Review Queue UI**: Resolve actions (approve/reject/map/skip with notes).
- [ ] **Mapping Dictionary UI**: Add/edit mappings (`GET/POST /api/datahub/mapping-dictionary`).
- [x] **PVR Target Algorithm**: Implement backend logic for `Urgent`/`Ready`/`Future` states based on dates.
- [x] **Master Plan Page API Integration**: Replace mock data with real API fetch.
- [ ] **Batch Detail View**: Expandable staging/logs/audit panels.
- [ ] **Error Report Export**: Excel/CSV download for validation failures.
- [ ] **Audit Log Viewer**: Field-level diff display (`GET /api/datahub/audit-logs/{batchId}`).
- [ ] **Delta Check Summary**: File-level diff for `MasterPlanUpload.DeltaSummary`.

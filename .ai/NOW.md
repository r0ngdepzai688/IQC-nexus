# Current State

- **Platform:** IQC Nexus
- **Phase:** Design System, Permission Refactoring, and **New Models Module Redesign**
- **Latest Updates (Home Environment - Antigravity AI):**
  - **New Models Module UI Redesign:** Completely overhauled the `new-model` module. Implemented the "Master Plan" view and detailed "Project Workspace" with 4 tabs (Overview, Risk & DFx, BOM Tracking, MPPR).
  - **Risk & DFx Tab Implementation:** Designed empty and data states for the Risk tab with options to upload from Excel or pick from a library.
  - **BOM & Part Tracking:** Created a detailed data table. Updated `UserBadge` to display full names and handle "avatar-only" mode for compact layouts. Integrated click-to-chat capabilities for all assigned personnel.
  - **Permission Architecture Redesign:** Eliminated the non-existent `Team Leader` role. Elevated `IQC Group Leader` to the top operational role across the system.

# Dual-Environment Workflow (Home vs Office)
Due to data security, the project operates in two distinct modes:
1. **Home Environment (Antigravity AI):**
   - **Focus:** UI/UX Design, Architecture, Structural Refactoring, Permission system, Strict Linting.
   - **Data:** Uses Mock Data (simulated via MOCK arrays or environment flags).
2. **Office Environment (Cline AI + Real Data):**
   - **Focus:** API Integration, Business Logic, Data Binding, Authentication.
   - **Data:** Real confidential company data.

**Key Rule:** Both environments must strictly adhere to shared TypeScript interfaces to ensure the UI does not break when switching from Mock to Real data.

# Next Steps
- [x] Implement New Models Module UI Redesign (Master Plan & Project Workspace).
- [x] Implement Risk & DFx UI and Data Table tracking.
- [ ] Implement BOM Parser: Backend algorithm to parse Excel BOM files.
- [ ] Connect the Dashboard UI components to use the newly defined data contracts and Mock Data.
- [ ] Establish real data fetching service utilizing the `NEXT_PUBLIC_USE_MOCK_DATA` toggle.

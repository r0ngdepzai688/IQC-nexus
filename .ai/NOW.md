# Current State

- **Platform:** IQC Nexus
- **Phase:** Dual-Environment Workflow Setup
- **Latest Updates:**
  - Implemented High-Density Enterprise UI across the system.
  - Unified 
ew-model page background, banner, and sidebar with the overview page (Glassmorphism & Ambient Pastel Backgrounds).
  - Cleaned up Header layout, removing Cartoonish avatars and replacing with Enterprise text ID.
  - Created .clinerules to enforce strict UI/UX boundaries for external AI assistants (Cline).
  - Established Dual-Environment Git Workflow (Home vs Office).

# Dual-Environment Workflow (Home vs Office)
Due to data security, the project operates in two distinct modes:
1. **Home Environment (Antigravity AI):**
   - **Focus:** UI/UX Design, Architecture, Structural Refactoring, creating new visual components.
   - **Data:** Uses Mock Data (simulated via MOCK arrays or environment flags).
2. **Office Environment (Cline AI + Real Data):**
   - **Focus:** API Integration, Business Logic, Data Binding, Authentication.
   - **Data:** Real confidential company data.

**Key Rule:** Both environments must strictly adhere to shared TypeScript interfaces (@/lib/types or similar) to ensure the UI does not break when switching from Mock to Real data.

# Next Steps
- Implement environment variable toggles for Mock vs Real data fetching.
- Build strictly typed data contracts (Interfaces) before injecting real data.

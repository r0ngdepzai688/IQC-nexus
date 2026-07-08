# Architectural & Design Decisions

## 1. Permission Architecture (IQC Nexus)
**Context:** The initial prototype included a `Team Leader` role which does not exist in the actual IQC organizational structure.
**Decision:** Eliminated the `Team Leader` role entirely. Established `IQC Group Leader` as the highest operational role within the system, overseeing all Org-level Capacity, KPIs, and Supplier Performance. 
**Impact:** `AuthContext.tsx`, `Header.tsx` (Role Lens), and Dashboard conditional logic were updated to enforce this new hierarchy.

## 2. UI/UX Design System
**Context:** Need a premium, high-density Enterprise SaaS appearance (2026 standard) while avoiding old ERP interfaces.
**Decision:** Implemented a unified Glassmorphism and Ambient Pastel background aesthetic. Replaced cartoonish avatars with professional text IDs. Used Shadcn/UI and Lucide React strictly.
**Impact:** A clean, modern, unified UI experience across `overview`, `new-model`, `hr`, and `tasks` modules. 

## 3. Strict React Lifecycles & Linting
**Context:** The UI suffered from `react-hooks/set-state-in-effect` hydration bugs and usage of strict `any` types.
**Decision:** Enforced 100% Zero-Error Linting. Used `setTimeout` for asynchronous state updates inside `useEffect` to avoid Next.js cascading render conflicts. Built explicit TypeScript interfaces (e.g., `UserData`) to ensure type safety.
**Impact:** Stabilized the core system, ensuring no runtime crashes during data binding in the Office environment.

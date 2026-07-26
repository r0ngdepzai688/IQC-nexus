# IQC Nexus Authentication + Permission Foundation Result Report

## Executive Summary
The platform authentication and authorization foundation has been established across backend (.NET 8 Web API) and frontend (Next.js 16 App Router). Authorization boundaries are authoritative on the server and enforced via JWT Bearer tokens and permission policies.

## Architectural Implementation

### 1. Canonical User Model
Normalized authenticated user domain representation across DTO and frontend client:
- `id`: int / number
- `username`: string
- `fullName`: string
- `employeeId`: string (mapped from Username or KnoxId)
- `position`: string
- `systemRole`: string ("Administrator" | "User")
- `roles`: string[]
- `permissions`: string[]

### 2. Single-Source Role-Permission Mapping (`RolePermissions.cs`)
- Backend `RolePermissions.Parse` handles JSON arrays (`["dashboard.view", ...]`) as well as legacy delimited strings (`dashboard.view;import.view`).
- Case-insensitive, deterministic ordering.
- Supported permissions: `dashboard.view`, `import.view`, `import.create`, `import.review`, `import.commit`, `import.admin`, `download.view`, `download.manage`, `audit.view`, `user.manage`, `role.manage`.

### 3. Backend Authorization Policy Enforcer (`PermissionAuthorization.cs`)
- Custom `PermissionRequirement` and `PermissionAuthorizationHandler` registered in `Program.cs`.
- Enforces strict policy-based authorization (`[Authorize(Policy = PlatformPermissions.ImportCreate)]`).
- Administrator system role bypasses specific permission requirements authoritatively on the server.
- Returns explicit `401 Unauthorized` for unauthenticated callers and `403 Forbidden` for authenticated callers lacking policy permissions.

### 4. Client Adapter & State Management (`client.ts` & `AuthContext.tsx`)
- `authClient` normalizes API responses exactly once upon receipt.
- `AuthContext` provides `useAuth()`, `usePermission(permission)`, and `useAnyPermission(permissions)`.
- `RequirePermission` / `PermissionGate` component wraps UI sections for usability gating.

### 5. Synthetic Seeding
- Seeding activates strictly in `Development` or `Testing` environments when `IQC_SYNTHETIC_USER_SEED_PASSWORD` is supplied.
- Password hashes use BCrypt; passwords are never logged.

## Test Verification
- Backend tests (`IqcQms.ApiAuthChecks`, `IqcQms.ApiIntegrationTests`): 106 passed.
- Frontend tests (`vitest`): 29 passed across 7 suites.

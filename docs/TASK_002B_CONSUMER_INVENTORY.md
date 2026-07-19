# Task 002B Consumer Inventory

## Canonical personnel source

- `fixtures/personnel.synthetic.json`
  - Repository-level UTF-8 synthetic fixture only.
  - IDs use uppercase `SYN-` values and emails use `example.invalid`.
  - Includes Vietnamese and Korean Unicode records.
  - Includes active/inactive/pending/locked records and optional/null fields.
  - Contains no password, credential hash or administrator record.

## Frontend adapter and runtime consumers

- `frontend/src/lib/mock-data/personnelMock.ts`
  - Validates the shared fixture and maps it to the existing `PersonnelRecord` contract.
- `frontend/src/lib/data/usersService.ts`
  - Provides immutable copies through `getAllUsers`.
  - Resolves users by exact-case synthetic employee ID through `getUserById`.
- `frontend/src/components/FloatingChat.tsx`
  - Reads personnel through `usersService`.
  - No direct `users.json` dependency.
- `frontend/src/components/ui/user-badge.tsx`
  - Resolves display names by synthetic employee ID.
- `frontend/src/components/ui/user-search-input.tsx`
  - Searches and returns synthetic employee IDs.
- `frontend/src/components/new-models/ActiveWorkspacesBoard.tsx`
  - Passes canonical `ownerId` to `UserBadge`.
- `frontend/src/app/(dashboard)/new-models/pipeline/page.tsx`
  - Creates workspaces using the canonical PIC ID and resolves `ownerName`.
- `frontend/src/lib/mock-data/newModelsMock.ts`
  - Stores PIC and owner references as synthetic personnel IDs.

## Backend adapter and runtime consumers

- `backend/src/IqcQms.Infrastructure/Data/Seeders/UserSeeder.cs`
  - Validates all records before migration/mutation orchestration and seeds atomically when enabled.
  - Uses ordinal case-sensitive IDs, null boundary conversion, duplicate rejection, `User` role and the synthetic status/active invariant.
- `backend/src/IqcQms.Api/Program.cs`
  - Loads the packaged fixture before migration and no longer locates `User_DB.xlsx`.
  - Missing external Development/Testing credential skips personnel mutation after validation.
- `backend/src/IqcQms.Api/IqcQms.Api.csproj`
  - Copies the canonical fixture to build and publish output.
- Existing entity/API consumers remain unchanged: `User`, `Role`, `AppDbContext`, `AuthController`, `UsersController`, `PermissionEngine`, Data Hub user resolution and task/chat identifier fields.
- Existing EF migrations and model snapshot remain unchanged.

## Auth fixture

- `frontend/src/lib/contexts/AuthContext.tsx`
  - Uses `SYN-ADMIN-001` and `admin@example.invalid`.
  - This is a development default identity, not production identity data or the canonical personnel list.

## Removed and retained dependencies

- `frontend/src/data/users.json`
  - No runtime imports are permitted.
- `backend/src/IqcQms.Infrastructure/Data/Seeders/users_seed.json`
  - Remains untouched and is not a runtime source.
- `extract_users.py`
  - Remains an untouched non-runtime legacy utility.
- Ignored root `User_DB.xlsx`, `users.json`, `user_preview.json` and `user_preview_utf8.json` remain untouched.

## Validation coverage

- `scripts/personnel-fixture-validator.mjs` validates the canonical fixture.
- `scripts/personnel-fixture-validator.test.mjs` covers Unicode, duplicates, invalid ID case, status contradictions and invalid email domains.
- Frontend and backend adapters independently reject invalid fixture content.

## Verification commands

```powershell
git grep -n "users.json" -- frontend/src
git grep -n -E "EMP[0-9]{4}|example\.test|10545998" -- frontend/src docs
git grep -n -E "Hoang Duc Nhien|Nguyen Van Kien|안병기|박군수" -- frontend/src docs
node scripts/personnel-fixture-validator.mjs
node --test scripts/personnel-fixture-validator.test.mjs
```

Task 002B remains **NOT READY** pending the approvals in `TASK_002B_APPROVAL_CHECKLIST.md`.

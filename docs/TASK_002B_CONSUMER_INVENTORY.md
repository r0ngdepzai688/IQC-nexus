# Task 002B Consumer Inventory

## Canonical personnel source

- `frontend/src/lib/mock-data/personnelMock.ts`
  - Synthetic fixture only.
  - IDs use `SYN-`.
  - Emails use `example.invalid`.
  - Includes Vietnamese and Korean Unicode records.
  - Includes active/inactive records and optional `preferredName`.

## Runtime consumers

- `frontend/src/lib/data/usersService.ts`
  - Provides immutable copies through `getAllUsers`.
  - Resolves users by synthetic employee ID through `getUserById`.
- `frontend/src/components/FloatingChat.tsx`
  - Reads personnel through `usersService`.
  - No direct `users.json` dependency.
- `frontend/src/components/ui/user-badge.tsx`
  - Resolves display names by synthetic employee ID.
- `frontend/src/components/new-models/ActiveWorkspacesBoard.tsx`
  - Passes canonical `ownerId` to `UserBadge`.
- `frontend/src/app/(dashboard)/new-models/pipeline/page.tsx`
  - Creates workspaces using the canonical PIC ID and resolves `ownerName`.
- `frontend/src/lib/mock-data/newModelsMock.ts`
  - Stores PIC and owner references as synthetic personnel IDs.

## Auth fixture

- `frontend/src/lib/contexts/AuthContext.tsx`
  - Uses `SYN-ADMIN-001` and `admin@example.invalid`.
  - This is a development default identity, not production identity data.

## Removed dependency

- `frontend/src/data/users.json`
  - No runtime imports are permitted.

## Verification commands

```powershell
git grep -n "users.json" -- frontend/src
git grep -n -E "EMP[0-9]{4}|example\.test|10545998" -- frontend/src docs
git grep -n -E "Hoang Duc Nhien|Nguyen Van Kien|ì•ˆë³‘ê¸°|ë°•êµ°ìˆ˜" -- frontend/src docs
```
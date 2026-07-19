# Contributor checks

The repository root `global.json` selects a compatible .NET 8 SDK. Run backend commands from the repository root so the same SDK policy is applied locally and in automation.

```powershell
dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
dotnet test backend/IqcQms.sln --no-build
npm --prefix frontend run lint
npm --prefix frontend run typecheck
npm --prefix frontend run build
```

The backend tests use temporary or in-memory SQLite databases and do not require credentials, external services, or network access after package restore.

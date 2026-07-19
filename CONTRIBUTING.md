# Contributor checks

The repository root `global.json` selects a compatible .NET 8 SDK. Run backend commands from the repository root so the same SDK policy is applied locally and in automation.

```powershell
dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
dotnet test backend/IqcQms.sln --no-build
dotnet tool restore
dotnet tool run dotnet-ef dbcontext list --project backend/src/IqcQms.Infrastructure --startup-project backend/src/IqcQms.Api --no-build
dotnet tool run dotnet-ef migrations list --project backend/src/IqcQms.Infrastructure --startup-project backend/src/IqcQms.Api --no-build
npm --prefix frontend run lint
npm --prefix frontend run typecheck
npm --prefix frontend run build
```

The backend tests use temporary or in-memory SQLite databases and do not require credentials, external services, or network access after package restore.

Entity Framework CLI commands use the repository-local .NET 8 tool manifest. Listing commands are read-only; migration creation and database updates must be run deliberately and are not part of the standard checks.

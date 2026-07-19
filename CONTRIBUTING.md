# Contributor checks

The repository root `global.json` selects a compatible .NET 8 SDK. Run backend commands from the repository root so the same SDK policy is applied locally and in automation.

```powershell
dotnet restore backend/IqcQms.sln
dotnet build backend/IqcQms.sln --no-restore
dotnet test backend/IqcQms.sln --no-build
dotnet test backend/tests/IqcQms.ApiIntegrationTests --filter Category=Integration
dotnet test backend/tests/IqcQms.ApiIntegrationTests --filter Category=Contract
dotnet tool restore
dotnet tool run dotnet-ef dbcontext list --project backend/src/IqcQms.Infrastructure --startup-project backend/src/IqcQms.Api --no-build
dotnet tool run dotnet-ef migrations list --project backend/src/IqcQms.Infrastructure --startup-project backend/src/IqcQms.Api --no-build
npm --prefix frontend run lint
npm --prefix frontend run typecheck
npm --prefix frontend run test:run
npm --prefix frontend run build
npm --prefix frontend audit --audit-level=moderate
```

Use `npm --prefix frontend test` for frontend test watch mode during local development. Use `test:run` for a single deterministic run and in CI.

For the Chromium E2E smoke suite, restore the backend with the command above, install the browser once with `npm --prefix frontend exec playwright install chromium`, then run `npm --prefix frontend run test:e2e`. Playwright starts the frontend and an isolated SQLite-backed backend, generates process-only synthetic credentials, and recreates `frontend/.playwright-runtime` without using the normal developer database. Retry traces and reports are written beneath `frontend/test-results` and `frontend/playwright-report`; CI uploads them only after failure.

The backend integration tests use a unique migrated SQLite database and generated process-local credentials under the operating-system temporary directory. Each factory removes its database and Data Hub files on disposal; tests never use the developer `localstorage.db`. Use the filtered command above to run only integration tests.

The focused API contract tests protect stable HTTP status, media type, JSON shape, authentication, frontend-consumed Data Hub fields, and selected OpenAPI structure. They deliberately avoid full response and OpenAPI snapshots. For an intentional breaking API change, update the backend endpoint and frontend consumer together, then change only the affected structural assertions and document the compatibility impact in the pull request.

Entity Framework CLI commands use the repository-local .NET 8 tool manifest. Listing commands are read-only; migration creation and database updates must be run deliberately and are not part of the standard checks.

## Continuous integration

Pull requests and pushes to `main` run the same backend, frontend, dependency-audit, read-only EF, and isolated Chromium smoke checks in `.github/workflows/ci.yml`. CI does not deploy or update a developer database.

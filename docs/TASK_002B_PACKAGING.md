# Task 002B Packaging Constraint

No Dockerfile, CI workflow or hosting manifest is present in this repository. The currently supported build path is a complete repository checkout containing `frontend/`, `backend/` and `fixtures/` as siblings.

- Frontend imports `fixtures/personnel.synthetic.json` at build time. `next.config.ts` resolves the Turbopack root relative to the configuration file, so the command working directory does not determine fixture resolution. The deployed Next.js runtime uses the bundled data and does not read the source JSON from disk.
- Backend `IqcQms.Api.csproj` copies the fixture to `fixtures/personnel.synthetic.json` in build and publish output. Backend startup reads that packaged copy.
- An isolated `frontend/` or `backend/` build context that omits the repository-level `fixtures/` directory is unsupported and must not be introduced without explicitly copying the fixture into that context.

Validation from repository root:

```powershell
node scripts/personnel-fixture-validator.mjs
npm.cmd --prefix frontend run build
dotnet build backend/IqcQms.sln --no-restore
Test-Path backend/src/IqcQms.Api/bin/Debug/net8.0/fixtures/personnel.synthetic.json
```

The expected final command result is `True`. Task 002B remains **NOT READY** regardless of packaging validation.

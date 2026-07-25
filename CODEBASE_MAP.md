# CODEBASE_MAP.md

Repository responsibility map for **IQC Nexus Portal**.  
This document describes ownership and composition boundaries (not implementation internals).

---

## Repository Root (`/`)

### Purpose
Top-level coordination layer for backend, frontend, docs, DevKit, scripts, and architecture-control files used by engineers and AI agents.

### Major projects
- `backend/` (.NET solution + tests)
- `frontend/` (web frontend workspace)
- `docs/` (architecture/process/operational references)
- `DevKit/` (quality gates and assessment reports)

### Major namespaces
N/A at root level (composition by subfolders).

### Major entry points
- `backend/IqcQms.sln`
- Root governance docs: `PROJECT_STATUS.md`, `ARCHITECTURE_DECISIONS.md`, `DEVELOPMENT_ROADMAP.md`, `AI_CONTEXT.md`, `AI_RULES.md`

### Dependencies
Root-level orchestration only; dependencies are managed within subprojects.

### Public interfaces
Repository-level documents and standards consumed by teams/agents.

### Testing projects
Contained under `backend/tests/` and frontend test directories.

### Documentation
Primary docs under `docs/` and `DevKit/`.

### Build artifacts
May appear in subprojects (`bin/`, `obj/`, `out/`, test output folders).

### DI composition roots
Defined inside executable projects (not at repository root).

---

## `backend/`

### Purpose
Holds the .NET backend ecosystem: API, domain/application/infrastructure layers, client agent host, client-agent contracts/infrastructure, and automated tests.

### Major projects
- `src/IqcQms.Api` — backend API host
- `src/IqcQms.Application` — backend application layer
- `src/IqcQms.Domain` — backend domain model
- `src/IqcQms.Infrastructure` — backend infrastructure
- `src/IqcQms.ClientAgent` — Windows Client Agent host process
- `src/IqcQms.ClientAgent.Application` — client-agent contracts and application abstractions
- `src/IqcQms.ClientAgent.Contracts` — shared contracts
- `src/IqcQms.ClientAgent.Infrastructure` — client-agent infrastructure implementations
- `tests/*` — solution test projects

### Major namespaces
- `IqcQms.Api.*`
- `IqcQms.Application.*`
- `IqcQms.Domain.*`
- `IqcQms.Infrastructure.*`
- `IqcQms.ClientAgent.*`
- `IqcQms.ClientAgent.Application.*`
- `IqcQms.ClientAgent.Infrastructure.*`
- `IqcQms.ClientAgent.Contracts.*`

### Major entry points
- API host startup entry in `IqcQms.Api` (Program bootstrap)
- Client Agent host startup in `IqcQms.ClientAgent` (`Program.cs`, hosted worker)

### Dependencies
- Layered project dependencies across domain/application/infrastructure
- Client-agent host depends on application contracts + infrastructure
- Data/storage/logging/security libraries (NuGet-managed per project)

### Public interfaces
- API endpoints/contracts exposed by `IqcQms.Api`
- Client-agent interfaces exposed by `IqcQms.ClientAgent.Application` (e.g., execution/work/state/security abstractions)
- Shared DTO/contracts via `IqcQms.ClientAgent.Contracts`

### Testing projects
- `tests/IqcQms.ApiAuthChecks`
- `tests/IqcQms.ApiIntegrationTests`
- `tests/IqcQms.ClientAgent.Tests`
- `tests/IqcQms.DataHubChecks`
- `tests/IqcQms.SeederSafetyChecks`

### Documentation
Backend behavior is documented primarily in `docs/`, especially:
- `docs/client-agent/*`
- `docs/security/*`
- `docs/imports/*`
- architecture and decision logs in `docs/`

### Build artifacts
- `backend/out/`
- per-project `bin/` and `obj/` directories

### DI composition roots
- `src/IqcQms.Api` startup composition
- `src/IqcQms.ClientAgent` host composition

---

## `frontend/`

### Purpose
Existing frontend application workspace including app source, component libraries, UI tests, and frontend build tooling.

### Major projects
- Frontend app source (`src/`)
- End-to-end tests (`e2e/`)
- Frontend config/tooling (`package.json`, Next/Vite/eslint/playwright configs)

### Major namespaces
TypeScript/React module structure under `frontend/src/` (app/components/lib/test).

### Major entry points
- Frontend runtime entry via framework app directory (`src/app`)
- Test entry via playwright/vitest configuration files

### Dependencies
Managed through npm (`package.json`, lockfile).

### Public interfaces
- UI routes/components and frontend integration interfaces with backend APIs.

### Testing projects
- `frontend/e2e/`
- `frontend/src/test/`
- generated results under `frontend/test-results/`

### Documentation
- `frontend/README.md`
- UI/system design docs under `docs/ui/`

### Build artifacts
- `frontend/out/`
- framework/tool-generated temporary output

### DI composition roots
Frontend composition via framework conventions (app bootstrapping in `src/app` and config files).

---

## `docs/`

### Purpose
Primary source of architecture intent, project status, security, execution order, and subsystem operating guidance.

### Major projects
- `docs/client-agent/` — client-agent architecture, security, runtime/readiness, local storage, queue, work directory
- `docs/security/` — auth and security contracts
- `docs/imports/` and `docs/data-platform/` — import/data lifecycle references
- `docs/architecture/` — ADR-level architecture records

### Major namespaces
N/A (documentation content).

### Major entry points
Key governance/operational docs:
- `docs/PROJECT_STATUS.md`
- `docs/EXECUTION_ORDER.md`
- `docs/DECISION_LOG.md`
- `docs/client-agent/ARCHITECTURE.md`

### Dependencies
Depends on implementation state and accepted decisions; consumed by engineering and AI agents.

### Public interfaces
Documentation contracts and decision records used for implementation alignment.

### Testing projects
N/A (docs validated by process/review).

### Documentation
Self-hosting documentation tree.

### Build artifacts
None expected.

### DI composition roots
N/A.

---

## `DevKit/`

### Purpose
Quality-governance and assessment workspace for milestone verification, quality gates, integration checklists, and result reports.

### Major projects
- `DevKit/quality/` — quality gates and test matrix/checklists
- `DevKit/reports/` — assessments/results by area/milestone
- `DevKit/work-packages/` — structured work package references

### Major namespaces
N/A (governance artifacts).

### Major entry points
- Quality gate docs (client-agent/import/security/UI)
- milestone assessment/result docs

### Dependencies
Consumes outputs from implementation/testing/documentation activities.

### Public interfaces
Quality criteria and acceptance evidence consumed by teams/reviewers/agents.

### Testing projects
No direct runnable test project; governs test expectations and acceptance framing.

### Documentation
All files are governance documentation.

### Build artifacts
None expected.

### DI composition roots
N/A.

---

## `scripts/`

### Purpose
Operational and validation scripts supporting checks, smoke runs, and fixture validation.

### Major projects
- PowerShell operational scripts
- Node-based validation scripts

### Major namespaces
N/A (script-level modules).

### Major entry points
- `scripts/datahub-authorized-smoke.ps1`
- `scripts/personnel-fixture-validator.mjs`
- `scripts/personnel-fixture-validator.test.mjs`

### Dependencies
Shell/runtime-specific dependencies (PowerShell, Node.js).

### Public interfaces
Script commands and validation outputs.

### Testing projects
Script-level tests where present (e.g., validator tests).

### Documentation
Script behavior documented indirectly via docs/DevKit references.

### Build artifacts
None expected (except transient runtime outputs).

### DI composition roots
N/A.

---

## `fixtures/`

### Purpose
Synthetic/non-production data fixtures for validation/testing workflows.

### Major projects
- Example synthetic data files (e.g., personnel synthetic fixture)

### Major namespaces
N/A.

### Major entry points
Fixture files consumed by tests/scripts.

### Dependencies
Used by test and validation tooling.

### Public interfaces
Fixture schemas/content expectations for tests and utilities.

### Testing projects
Referenced by backend/frontend/script tests as needed.

### Documentation
Context referenced in docs and script/test files.

### Build artifacts
None expected.

### DI composition roots
N/A.

---

## `memory-bank/`

### Purpose
Session/context continuity artifacts that capture brief, progress, and system-level notes.

### Major projects
- `project_brief.md`
- `system_architecture.md`
- `progress.md`

### Major namespaces
N/A.

### Major entry points
Context files consumed by agents/workflows requiring continuity.

### Dependencies
Depends on project/documentation state.

### Public interfaces
Human/agent-readable contextual summaries.

### Testing projects
N/A.

### Documentation
Acts as supplemental context docs.

### Build artifacts
None expected.

### DI composition roots
N/A.

---

## Top-Level Governance / Control Files

### Purpose
Repository-level AI and architecture controls to maintain consistent decision-making.

### Major projects
- `PROJECT_STATUS.md`
- `ARCHITECTURE_DECISIONS.md`
- `DEVELOPMENT_ROADMAP.md`
- `AI_CONTEXT.md`
- `AI_RULES.md`

### Major namespaces
N/A.

### Major entry points
These files are first-read artifacts for AI agents before code changes.

### Dependencies
Derived from accepted architecture, roadmap, and docs.

### Public interfaces
Operating contracts for contributor and AI behavior.

### Testing projects
N/A (review/process validation).

### Documentation
These are governance docs by definition.

### Build artifacts
None.

### DI composition roots
N/A.

---

## Quick Composition Summary (DI Roots)

Primary composition roots currently live in executable hosts:

1. `backend/src/IqcQms.Api` (API composition root)
2. `backend/src/IqcQms.ClientAgent` (Client Agent composition root)

All service registration and runtime graph construction should be treated as controlled architecture surfaces.

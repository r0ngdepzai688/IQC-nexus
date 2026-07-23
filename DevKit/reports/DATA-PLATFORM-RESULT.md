# Data Platform Result

## Outcome

The backend import foundation now has a provider-neutral normalized workbook
boundary, working CSV and ordinary XLSX providers, explicit import-job lifecycle
rules, stable public error codes, and focused synthetic tests. No database
migration was created.

## Delivered behavior

- CSV and XLSX normalize into the same `NormalizedWorkbook` contract.
- Providers preserve source worksheet, row, and column coordinates and do not
  infer headers, mappings, validation rules, or business meaning.
- Excel extraction preserves numeric values as numeric, including date-formatted
  serials without implicit `DateTime` interpretation.
- Multiple worksheets, visibility, merged ranges, empty rows, cached formula
  values where available, and number formats are retained.
- Payload, worksheet, row, column, and total-cell limits use stable errors.
- Cancellation is propagated through bounded input and provider processing.
- Unsupported Client Agent and NASCA kinds remain unregistered capability
  placeholders; no raw NASCA server upload route was introduced.
- The persistence-neutral import-job aggregate enforces explicit transitions,
  requires a current preview before commit, and returns idempotent replay
  receipts.
- The existing persisted Data Hub commit remains transactional and now returns
  an already committed batch without duplicating mutations.
- Provider failure messages and provider diagnostics are sanitized; provider cell
  values are not logged. Legacy DataHub audit value handling remains a residual
  security review item.

## Verification evidence

Focused provider, lifecycle, rollback, and commit replay tests use generated
synthetic CSV and XLSX bytes only. Backend restore and build completed successfully
with zero warnings and zero errors; all 102 backend tests passed. Whitespace, Git
boundary, COM-reference, and raw-NASCA-route checks were clean.

## Deferred work

Durable provider-neutral job, preview, and idempotency persistence remains
deferred until the aggregate is reviewed against existing Data Hub entities and
index requirements. Mapping, validation, UI integration, and Client Agent
pairing remain separate downstream work.

## Implementation matrix

| Capability | Implemented | Tested | File/Project | Remaining work |
| --- | --- | --- | --- | --- |
| NormalizedWorkbook contracts | Implemented production code | Yes | Application DataPlatform/NormalizedWorkbook.cs; DataHubChecks | None for extraction boundary |
| Contract protocol version | Implemented production code | Yes | Application contract + validation | Version negotiation beyond 1.0 |
| Contract validation | Implemented at registry boundary | Yes | NormalizedWorkbookValidation.cs, DataSourceProviderRegistry.cs | Validate any future non-registry entrypoints |
| CSV provider | Implemented production code | Yes | Infrastructure CsvDataSourceProvider.cs | None in foundation scope |
| XLSX provider | Implemented production code | Yes | Infrastructure ExcelDataSourceProvider.cs | Formula-text support depends on safe reader capability |
| Provider registry | Implemented production code | Yes | Infrastructure DataSourceProviderRegistry.cs | Register future providers only through capability review |
| Provider capability metadata | Implemented production code | Partial | Application contracts; provider implementations | Add direct capability-contract assertions |
| Source row/column coordinates | Implemented production code | Yes | Both providers; synthetic tests | None |
| Merged ranges | Implemented production code | Yes | XLSX provider; synthetic workbook tests | None |
| Hidden worksheet metadata | Implemented production code | Yes | XLSX provider; synthetic workbook tests | Very-hidden fixture coverage can be expanded |
| Numeric type preservation | Implemented production code | Yes | XLSX cell normalization; synthetic numeric/date-format test | None |
| Formula behavior | Partially implemented | Yes | XLSX cached-value path and diagnostic | Formula text remains unavailable in the safe reader |
| Number-format preservation | Implemented production code | Yes | XLSX provider; synthetic style fixture | None |
| Workbook safety limits | Implemented production code | Yes | Bounded input and provider limits | Add streaming/operational load tests |
| Import Job lifecycle | Implemented persistence-neutral aggregate | Yes | Application ImportJob.cs, transition guard | Durable job store and API orchestration |
| Mapping boundary | Contract only | No production implementation | IImportMappingService, ImportFieldMapping | Implement mapping confirmation service |
| Validation boundary | Contract only | No production implementation | IImportValidationService, summaries | Implement blocking validation service |
| Preview requirement | Implemented persistence-neutral guard | Yes | ImportJob.MarkPreviewReady/BeginCommit | Persist/version preview attestations |
| Transactional commit | Implemented in existing DataHub pipeline | Yes | DataHubIngestionService.CommitBatchAsync | Adapt provider-neutral commit service |
| Idempotent commit | Partial production behavior | Yes | Existing committed-batch replay + in-memory receipt | Durable idempotency key/receipt persistence |
| User ownership | Partial production behavior | Yes for legacy DataHub routes | ImportJob.OwnerUserId, DataHub controller ownership checks | Owner-bound provider-neutral job store |
| Authorization | Partial production behavior | Existing auth/integration tests | Existing API policies and DataHub route guards | Apply policies to provider-neutral APIs |
| Cancellation | Implemented for providers and aggregate | Yes | Provider input/normalizers and ImportJob | Propagate through future orchestration services |
| Stable error codes | Implemented production boundary | Yes | ImportPlatformException, registry/providers/lifecycle | Standardize all API envelopes |
| Sanitized API errors | Partial | Provider tests; not universal API coverage | Provider exceptions and legacy controllers | Remove remaining raw legacy exception responses |
| Audit events | Partial / legacy pipeline | Existing DataHub audit tests and source | DataHubAuditLog, IImportAuditService contract | Implement provider-neutral audit service without cell values |
| ClientAgentExcel / NascaExcel | Contract/placeholders only | Yes unsupported-provider tests | Provider kind enum and registry | Company-only paired agent work; no raw server upload |
| Database migration | Deferred | N/A | No new migration created | Review aggregate against DataHub model first |
| Portal UI/authentication | Deferred for this pass | N/A | Outside data-platform scope | Start only after this backend contract review |
## Confidential-artifact reconciliation

Git metadata checks classify every checked excluded path as untracked and ignored;
none are tracked. No excluded artifact was opened, read, parsed, staged, or
committed. The stale tracked-file warning was removed from the boundary note.

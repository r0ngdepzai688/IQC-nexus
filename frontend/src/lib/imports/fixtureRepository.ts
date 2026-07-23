import {
  ImportAuditEvent,
  ImportCommitResult,
  ImportJob,
  ImportJobRepository,
  ImportPreviewDetail,
  ImportQuery,
  MappingProfileConfig,
  MappingResultSummary,
  PaginatedAuditResult,
  ValidationResultSummary,
  ValidationRuleConfigRequest,
} from "./contracts";

const SYNTHETIC_JOBS: ImportJob[] = [
  {
    id: "IQC-SYN-1039",
    source: "CSV provider",
    fileName: "synthetic_master_plan_batch1.csv",
    creator: "alex.engineer",
    status: "Failed",
    state: "Failed",
    errors: 3,
    warnings: 1,
    blockingErrors: 1,
    createdAt: "2026-07-24T00:15:00Z",
  },
  {
    id: "JOB-2026-001",
    source: "CSV provider",
    fileName: "synthetic_master_plan_batch1.csv",
    creator: "alex.engineer",
    status: "Pending review",
    state: "ReadyForReview",
    errors: 0,
    warnings: 2,
    blockingErrors: 0,
    createdAt: "2026-07-24T00:15:00Z",
    previewVersion: "preview-v1.0",
  },
  {
    id: "JOB-2026-002",
    source: "Excel provider",
    fileName: "synthetic_iqc_inspection_report.xlsx",
    creator: "sarah.qa",
    status: "Validating",
    state: "Validating",
    errors: 1,
    warnings: 3,
    blockingErrors: 0,
    createdAt: "2026-07-23T22:30:00Z",
  },
  {
    id: "JOB-2026-003",
    source: "CSV provider",
    fileName: "synthetic_supplier_lot_data.csv",
    creator: "david.iqc",
    status: "Completed",
    state: "Completed",
    errors: 0,
    warnings: 0,
    blockingErrors: 0,
    createdAt: "2026-07-23T18:00:00Z",
    previewVersion: "preview-v1.0",
  },
];

export class FixtureImportJobRepository implements ImportJobRepository {
  async list(query: ImportQuery, _signal?: AbortSignal): Promise<ImportJob[]> {
    const search = query.search.toLowerCase();
    return SYNTHETIC_JOBS.filter(
      (job) =>
        (query.status === "All" || job.status === query.status) &&
        `${job.id} ${job.fileName} ${job.creator}`.toLowerCase().includes(search)
    );
  }

  async getJob(id: string, _signal?: AbortSignal): Promise<ImportJob> {
    const found = SYNTHETIC_JOBS.find((j) => j.id === id);
    if (found) return found;
    return {
      id,
      source: "Synthetic Fixture Provider",
      fileName: "synthetic_import_fixture.csv",
      creator: "alex.engineer",
      status: "Ready for mapping",
      state: "ReadyForMapping",
      errors: 0,
      warnings: 0,
      blockingErrors: 0,
      createdAt: new Date().toISOString(),
    };
  }

  async applyMapping(
    _id: string,
    profile: MappingProfileConfig,
    _signal?: AbortSignal
  ): Promise<MappingResultSummary> {
    return {
      profileId: profile.profileId || "synthetic-profile",
      profileVersion: profile.version || "1.0",
      totalRowsProcessed: 42,
      mappedRecordCount: 42,
      hasBlockingErrors: false,
      unmappedSourceColumns: ["IgnoredCol1", "InternalNotes"],
      ignoredSourceColumns: profile.ignoredSourceColumns || [],
    };
  }

  async runValidation(
    _id: string,
    rules: ValidationRuleConfigRequest[],
    _signal?: AbortSignal
  ): Promise<ValidationResultSummary> {
    return {
      validationProfileId: "val-profile-v1",
      validationProfileVersion: "1.0",
      summary: {
        totalRecordsEvaluated: 42,
        validRecords: 40,
        invalidRecords: 2,
        informationCount: 1,
        warningCount: 2,
        errorCount: 0,
        blockingErrorCount: 0,
      },
      diagnosticsSample: [
        {
          ruleId: rules[0]?.ruleId || "r1",
          code: "VAL_NUMERIC_OUT_OF_RANGE",
          message: "Quantity (0) is below minimum recommended threshold (1).",
          severity: 1,
          targetField: "Quantity",
          coordinate: { worksheetIndex: 1, worksheetName: "Sheet1", rowNumber: 14, columnNumber: 3 },
        },
      ],
    };
  }

  async generatePreview(id: string, _signal?: AbortSignal): Promise<ImportPreviewDetail> {
    return this.getPreview(id);
  }

  async getPreview(id: string, _signal?: AbortSignal): Promise<ImportPreviewDetail> {
    const job = await this.getJob(id);
    return {
      jobId: job.id,
      ownerUserId: job.creator,
      providerKind: job.source,
      sourceDisplayName: job.fileName,
      mappingProfileId: "synthetic-map-v1",
      mappingProfileVersion: "1.0",
      validationProfileId: "synthetic-val-v1",
      validationProfileVersion: "1.0",
      generatedAt: new Date().toISOString(),
      totalRecordsProcessed: 42,
      sampleRecordsCount: 2,
      totalWarningsCount: job.warnings,
      totalErrorsCount: job.errors,
      totalBlockingErrorsCount: job.blockingErrors,
      representativeRecords: [
        {
          recordIndex: 1,
          rowCoordinate: { worksheetIndex: 1, worksheetName: "Main", rowNumber: 2, columnNumber: 1 },
          fields: [
            { targetField: "PartNo", sourceColumnName: "Part Number", originalNormalizedValue: "  PN-1001  ", mappedValue: "PN-1001", hasTransformationError: false },
            { targetField: "Quantity", sourceColumnName: "Qty", originalNormalizedValue: "50", mappedValue: 50, hasTransformationError: false },
          ],
          diagnostics: [],
        },
        {
          recordIndex: 2,
          rowCoordinate: { worksheetIndex: 1, worksheetName: "Main", rowNumber: 3, columnNumber: 1 },
          fields: [
            { targetField: "PartNo", sourceColumnName: "Part Number", originalNormalizedValue: "  PN-1002  ", mappedValue: "PN-1002", hasTransformationError: false },
            { targetField: "Quantity", sourceColumnName: "Qty", originalNormalizedValue: "0", mappedValue: 0, hasTransformationError: false },
          ],
          diagnostics: [
            { ruleId: "val-qty", code: "VAL_NUMERIC_OUT_OF_RANGE", message: "Quantity is 0", severity: 1, targetField: "Quantity", coordinate: { worksheetIndex: 1, worksheetName: "Main", rowNumber: 3, columnNumber: 2 } }
          ],
        },
      ],
      diagnosticsSample: [],
      attestation: {
        jobId: job.id,
        ownerUserId: job.creator,
        contentFingerprint: "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        mappingProfileVersion: "1.0",
        validationProfileVersion: "1.0",
        generatedAt: new Date().toISOString(),
        expiresAt: new Date(Date.now() + 7200000).toISOString(),
        signature: "synth_attestation_sig_abc123",
      },
      canCommit: true,
    };
  }

  async commitImportJob(
    id: string,
    idempotencyKey: string,
    _expectedVersion: number,
    _signal?: AbortSignal
  ): Promise<ImportCommitResult> {
    return {
      jobId: id,
      idempotencyKey,
      insertedCount: 42,
      updatedCount: 0,
      skippedCount: 0,
      committedAt: new Date().toISOString(),
      replayed: false,
    };
  }

  async getAuditEvents(
    id: string,
    page: number = 1,
    pageSize: number = 50,
    _signal?: AbortSignal
  ): Promise<PaginatedAuditResult> {
    const events: ImportAuditEvent[] = [
      {
        id: 1,
        eventId: "evt-001",
        jobId: id,
        eventType: "Created",
        actorUserId: "alex.engineer",
        fromState: null,
        toState: "Created",
        code: "JOB_CREATED",
        message: "Import job created from CSV provider.",
        occurredAt: "2026-07-24T00:10:00Z",
      },
      {
        id: 2,
        eventId: "evt-002",
        jobId: id,
        eventType: "Mapped",
        actorUserId: "alex.engineer",
        fromState: "ReadyForMapping",
        toState: "Validating",
        code: "MAPPING_APPLIED",
        message: "Applied mapping profile v1.0 (42 records mapped).",
        occurredAt: "2026-07-24T00:12:00Z",
      },
      {
        id: 3,
        eventId: "evt-003",
        jobId: id,
        eventType: "PreviewGenerated",
        actorUserId: "alex.engineer",
        fromState: "Validating",
        toState: "ReadyForReview",
        code: "PREVIEW_GENERATED",
        message: "Generated preview attestation.",
        occurredAt: "2026-07-24T00:14:00Z",
      },
    ];

    return {
      totalCount: events.length,
      page,
      pageSize,
      totalPages: 1,
      items: events,
    };
  }
}
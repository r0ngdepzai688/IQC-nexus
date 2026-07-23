export type ImportJobStatus = "Pending review" | "Failed" | "Completed" | "Validating" | "Ready for mapping";

export type ImportJobState =
  | "Created"
  | "Inspecting"
  | "ReadyForMapping"
  | "Validating"
  | "ReadyForReview"
  | "Committing"
  | "Completed"
  | "Failed"
  | "Cancelled";

export interface ImportJob {
  id: string;
  source: string;
  fileName: string;
  creator: string;
  status: ImportJobStatus;
  state: ImportJobState;
  errors: number;
  warnings: number;
  blockingErrors: number;
  createdAt: string;
  previewVersion?: string;
  version?: number;
}

export interface ImportQuery {
  search: string;
  status: string;
  sort: "newest" | "oldest" | "status";
}

export interface MappingRuleConfig {
  sourceColumnIdentifier: string;
  targetField: string;
  isRequired: boolean;
  transformationType: number; // 0=None, 1=TrimText, 2=NormalizeLineEndings, 3=ParseInteger, 4=ParseDecimal, 5=ParseBoolean, 6=ParseDateTime, 7=LookupDictionary
  defaultValue?: string;
  cultureName?: string;
}

export interface MappingProfileConfig {
  profileId?: string;
  version?: string;
  name?: string;
  worksheetName?: string;
  headerRowNumber: number;
  caseInsensitiveHeaderMatching: boolean;
  rules: MappingRuleConfig[];
  ignoredSourceColumns?: string[];
}

export interface MappingResultSummary {
  profileId: string;
  profileVersion: string;
  totalRowsProcessed: number;
  mappedRecordCount: number;
  hasBlockingErrors: boolean;
  unmappedSourceColumns: string[];
  ignoredSourceColumns: string[];
}

export interface ValidationRuleConfigRequest {
  ruleId?: string;
  name?: string;
  kind: number; // 0=RequiredValue, 1=TextLength, 2=NumericRange, 3=DateRange, 4=AllowedValues, 5=RegexPattern, 6=UniqueValue, 7=CrossFieldComparison
  severity: number; // 0=Information, 1=Warning, 2=Error, 3=BlockingError
  targetField?: string;
  minNumeric?: number;
  maxNumeric?: number;
}

export interface ValidationDiagnosticDto {
  ruleId: string;
  code: string;
  message: string;
  severity: number;
  scope?: number;
  targetField?: string;
  coordinate?: {
    worksheetIndex: number;
    worksheetName: string;
    rowNumber: number;
    columnNumber: number;
  };
}

export interface ValidationResultSummary {
  validationProfileId: string;
  validationProfileVersion: string;
  summary: {
    totalRecordsEvaluated: number;
    validRecords: number;
    invalidRecords: number;
    informationCount: number;
    warningCount: number;
    errorCount: number;
    blockingErrorCount: number;
  };
  diagnosticsSample?: ValidationDiagnosticDto[];
}

export interface RepresentativeRecordDto {
  recordIndex: number;
  rowCoordinate: {
    worksheetIndex: number;
    worksheetName: string;
    rowNumber: number;
    columnNumber: number;
  };
  fields: Array<{
    targetField: string;
    sourceColumnName: string;
    originalNormalizedValue?: string;
    mappedValue?: any;
    hasTransformationError: boolean;
  }>;
  diagnostics: ValidationDiagnosticDto[];
}

export interface ImportPreviewAttestationDto {
  jobId: string;
  ownerUserId: string;
  contentFingerprint: string;
  mappingProfileVersion: string;
  validationProfileVersion: string;
  generatedAt: string;
  expiresAt: string;
  signature: string;
}

export interface ImportPreviewDetail {
  jobId: string;
  ownerUserId: string;
  providerKind: string;
  sourceDisplayName: string;
  mappingProfileId: string;
  mappingProfileVersion: string;
  validationProfileId: string;
  validationProfileVersion: string;
  generatedAt: string;
  totalRecordsProcessed: number;
  sampleRecordsCount: number;
  totalWarningsCount: number;
  totalErrorsCount: number;
  totalBlockingErrorsCount: number;
  representativeRecords: RepresentativeRecordDto[];
  diagnosticsSample: ValidationDiagnosticDto[];
  attestation: ImportPreviewAttestationDto;
  canCommit: boolean;
}

export interface ImportCommitResult {
  jobId: string;
  idempotencyKey: string;
  insertedCount: number;
  updatedCount: number;
  skippedCount: number;
  committedAt: string;
  replayed: boolean;
}

export interface ImportAuditEvent {
  id: number;
  eventId: string;
  jobId: string;
  eventType: string;
  actorUserId: string;
  fromState?: string | null;
  toState?: string | null;
  code: string;
  message: string;
  sanitizedMetadataJson?: string;
  occurredAt: string;
}

export interface PaginatedAuditResult {
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  items: ImportAuditEvent[];
}

export interface ImportJobRepository {
  list(query: ImportQuery, signal?: AbortSignal): Promise<ImportJob[]>;
  getJob(id: string, signal?: AbortSignal): Promise<ImportJob>;
  applyMapping(id: string, profile: MappingProfileConfig, signal?: AbortSignal): Promise<MappingResultSummary>;
  runValidation(id: string, rules: ValidationRuleConfigRequest[], signal?: AbortSignal): Promise<ValidationResultSummary>;
  generatePreview(id: string, signal?: AbortSignal): Promise<ImportPreviewDetail>;
  getPreview(id: string, signal?: AbortSignal): Promise<ImportPreviewDetail>;
  commitImportJob(id: string, idempotencyKey: string, expectedVersion: number, signal?: AbortSignal): Promise<ImportCommitResult>;
  getAuditEvents(id: string, page?: number, pageSize?: number, signal?: AbortSignal): Promise<PaginatedAuditResult>;
}
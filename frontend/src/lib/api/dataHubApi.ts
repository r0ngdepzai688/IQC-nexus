import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';
const authHeaders = () => {
  if (typeof window === 'undefined') return {};
  const token = window.localStorage.getItem('token');
  return token ? { Authorization: `Bearer ${token}` } : {};
};

export class DataHubApiError extends Error {
  constructor(message: string, public readonly status: number) {
    super(message);
  }
}

export const parseApiError = (data: unknown, fallback: string) => {
  if (typeof data === 'string' && data.trim()) return data;
  if (data && typeof data === 'object') {
    if ('detail' in data && typeof data.detail === 'string' && data.detail.trim()) return data.detail;
    if ('message' in data && typeof data.message === 'string' && data.message.trim()) return data.message;
    if ('title' in data && typeof data.title === 'string' && data.title.trim()) return data.title;
    if ('errors' in data && data.errors && typeof data.errors === 'object') {
      const messages = Object.values(data.errors).flatMap(value => Array.isArray(value) ? value : []).filter(value => typeof value === 'string');
      if (messages.length) return messages.join(' ');
    }
  }
  return fallback;
};

const apiCall = async <T>(request: () => Promise<{ data: T }>, fallback: string): Promise<T> => {
  try {
    return (await request()).data;
  } catch (reason) {
    if (axios.isAxiosError(reason)) {
      throw new DataHubApiError(parseApiError(reason.response?.data, fallback), reason.response?.status ?? 0);
    }
    throw reason;
  }
};

export interface ManualFile {
  fileName: string;
  sizeBytes: number;
  lastModifiedUtc: string;
  checksum: string;
  duplicateStatus: string;
}

export interface ImportBatch {
  batchId: string;
  module: string;
  uploadedBy: string;
  uploadedAt: string;
  status: string;
  totalRows: number;
  validRows: number;
  errorRows: number;
  reviewRequiredRows: number;
  skippedRows: number;
  createdRecords: number;
  updatedRecords: number;
  noChangeRecords: number;
  durationMs: number;
}

export interface StagingMasterPlan {
  id: number;
  batchId: string;
  rawRowNumber: number;
  projectName: string;
  basic: string;
  area: string;
  grade: string;
  cat: string;
  basicKey: string;
  catKey: string;
  sku: string;
  qtyLpr: number | null;
  qtyLsr: number | null;
  pvrTargetDate: string | null;
  praTargetDate: string | null;
  sraTargetDate: string | null;
  hwPic: string;
  importedStatus: string;
  remark: string;
  rowStatus: string;
  validationMessage: string;
  coreValidationMessage: string;
}
export interface HeaderColumn { columnIndex: number; header: string; suggestedCanonical: string | null; ambiguous: boolean; parentHeader?: string; childHeader?: string; effectiveHeaderPath?: string; sampleValues?: string[]; detectedDataType?: string; confidence?: number; reason?: string; learnedSuggestion?: boolean }
export interface HeaderInspection { headerRow: number; headerDepth?: number; dataStartRow?: number; workbookFingerprint?: string; columns: HeaderColumn[]; canonicalFields: string[]; requiredFields: string[] }
export interface HeaderMapping { columnIndex: number; canonicalField: string; normalizedHeaderPath?: string; confirmLearning?: boolean; workbookFingerprint?: string; detectedDataType?: string }
export type ReviewResolutionAction = 'Override' | 'Ignore' | 'CreateMissing' | 'Update' | 'Skip';
export interface ImportReviewRow { rowNumber: number; sku: string; basic: string; cat: string; field: string; currentValue: string; oldValue: string; newValue: string; severity: string; message: string; status: string; conflictType: string; reviewItemId: number | null; supportedActions: ReviewResolutionAction[] }
export interface ImportReviewSummary { batchId: string; fileName: string; validRows: number; warningRows: number; errorRows: number; existingSkuConflicts: number; existingBusinessKeyConflicts: number; readyToUpdateRows: number; noChangeRows: number; skippedRows: number; rows: ImportReviewRow[] }
export type ExistingSkuResolution = 'Skip' | 'Cancel';
export type ExistingBusinessKeyResolution = 'Update' | 'Skip' | 'Cancel';
export type WarningResolution = 'Accept' | 'Skip';
export interface ResolutionResponse { success: boolean }
export type CommitResponse = ImportBatch;

export const getManualFiles = async (): Promise<ManualFile[]> => {
  return apiCall(() => axios.get(`${API_BASE_URL}/DataHub/manual-files`, { headers: authHeaders() }), 'Unable to load manual files.');
};

export const processManualUpload = async (fileName: string, module: string = 'NewModels'): Promise<ImportBatch> => {
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/process-manual?fileName=${encodeURIComponent(fileName)}&module=${module}`, undefined, { headers: authHeaders() }), 'Unable to process the server file.');
};

export const getHistory = async (module: string = 'NewModels'): Promise<ImportBatch[]> => {
  return apiCall(() => axios.get(`${API_BASE_URL}/DataHub/history?module=${module}`, { headers: authHeaders() }), 'Unable to load import history.');
};

export const getBatchPreview = async (batchId: string): Promise<ImportBatch> => {
  return apiCall(() => axios.get(`${API_BASE_URL}/DataHub/preview/${batchId}`, { headers: authHeaders() }), 'Unable to refresh the batch preview.');
};

export const getStagingRecords = async (batchId: string): Promise<StagingMasterPlan[]> => {
  return apiCall(() => axios.get(`${API_BASE_URL}/DataHub/batch/${batchId}/staging`, { headers: authHeaders() }), 'Unable to load staging rows.');
};

export const inspectMasterPlanHeaders = async (file: File): Promise<HeaderInspection> => {
  const form = new FormData();
  form.append('file', file);
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/inspect-headers`, form, { headers: authHeaders() }), 'Unable to inspect workbook headers.');
};

export const uploadMasterPlan = async (file: File, module = 'NewModels', mappings?: HeaderMapping[]): Promise<ImportBatch> => {
  const form = new FormData();
  form.append('file', file);
  form.append('module', module);
  if (mappings) form.append('headerMapping', JSON.stringify(mappings));
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/upload`, form, { headers: authHeaders() }), 'Upload failed.');
};

export const getReviewSummary = async (batchId: string): Promise<ImportReviewSummary> => {
  return apiCall(() => axios.get(`${API_BASE_URL}/DataHub/review/${encodeURIComponent(batchId)}`, { headers: authHeaders() }), 'Unable to load review details.');
};

export const resolveExistingSku = async (batchId: string, resolution: ExistingSkuResolution): Promise<ImportBatch> => {
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/resolve-existing/${encodeURIComponent(batchId)}`, { resolution }, { headers: authHeaders() }), 'Unable to resolve existing SKUs.');
};

export const resolveExistingBusinessKey = async (batchId: string, resolution: ExistingBusinessKeyResolution, rowNumber?: number): Promise<ImportBatch> => {
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/resolve-existing-business-key/${encodeURIComponent(batchId)}`, { resolution, rowNumber }, { headers: authHeaders() }), 'Unable to resolve existing business keys.');
};

export const resolveWarningRow = async (batchId: string, rowNumber: number, resolution: WarningResolution): Promise<ResolutionResponse> => {
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/resolve-warning/${encodeURIComponent(batchId)}/${rowNumber}`, { resolution }, { headers: authHeaders() }), 'Unable to resolve the warning row.');
};

export const resolveReviewItem = async (reviewItemId: number, action: ReviewResolutionAction, note?: string): Promise<ResolutionResponse> => {
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/resolve-review/${reviewItemId}`, { action, note }, { headers: authHeaders() }), 'Unable to resolve the business review item.');
};

export const commitBatch = async (batchId: string): Promise<CommitResponse> => {
  return apiCall(() => axios.post(`${API_BASE_URL}/DataHub/commit/${encodeURIComponent(batchId)}`, undefined, { headers: authHeaders() }), 'Commit failed.');
};

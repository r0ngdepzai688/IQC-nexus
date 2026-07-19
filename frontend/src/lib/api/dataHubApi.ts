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

const errorMessage = (data: unknown, fallback: string) => {
  if (typeof data === 'string' && data.trim()) return data;
  if (data && typeof data === 'object' && 'detail' in data && typeof data.detail === 'string') return data.detail;
  return fallback;
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
export interface HeaderColumn { columnIndex: number; header: string; suggestedCanonical: string | null; ambiguous: boolean }
export interface HeaderInspection { headerRow: number; columns: HeaderColumn[]; canonicalFields: string[]; requiredFields: string[] }
export interface HeaderMapping { columnIndex: number; canonicalField: string }
export interface ImportReviewRow { rowNumber: number; sku: string; field: string; currentValue: string; severity: string; message: string; status: string }
export interface ImportReviewSummary { batchId: string; fileName: string; validRows: number; warningRows: number; errorRows: number; existingSkuConflicts: number; skippedRows: number; rows: ImportReviewRow[] }

export const getManualFiles = async (): Promise<ManualFile[]> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/manual-files`, { headers: authHeaders() });
  return response.data;
};

export const processManualUpload = async (fileName: string, module: string = 'NewModels'): Promise<ImportBatch> => {
  const response = await axios.post(`${API_BASE_URL}/DataHub/process-manual?fileName=${encodeURIComponent(fileName)}&module=${module}`, undefined, { headers: authHeaders() });
  return response.data;
};

export const getHistory = async (module: string = 'NewModels'): Promise<ImportBatch[]> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/history?module=${module}`, { headers: authHeaders() });
  return response.data;
};

export const getBatchPreview = async (batchId: string): Promise<ImportBatch> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/preview/${batchId}`, { headers: authHeaders() });
  return response.data;
};

export const getStagingRecords = async (batchId: string): Promise<StagingMasterPlan[]> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/batch/${batchId}/staging`, { headers: authHeaders() });
  return response.data;
};

export const inspectMasterPlanHeaders = async (file: File): Promise<HeaderInspection> => {
  const form = new FormData();
  form.append('file', file);
  const response = await axios.post(`${API_BASE_URL}/DataHub/inspect-headers`, form, { headers: authHeaders() });
  return response.data;
};

export const uploadMasterPlan = async (file: File, module = 'NewModels', mappings?: HeaderMapping[]): Promise<ImportBatch> => {
  const form = new FormData();
  form.append('file', file);
  form.append('module', module);
  if (mappings) form.append('headerMapping', JSON.stringify(mappings));
  try {
    const response = await axios.post(`${API_BASE_URL}/DataHub/upload`, form, { headers: authHeaders() });
    return response.data;
  } catch (reason) {
    if (axios.isAxiosError(reason)) throw new DataHubApiError(errorMessage(reason.response?.data, 'Upload failed.'), reason.response?.status ?? 0);
    throw reason;
  }
};

export const getReviewSummary = async (batchId: string): Promise<ImportReviewSummary> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/review/${encodeURIComponent(batchId)}`, { headers: authHeaders() });
  return response.data;
};

export const resolveExistingSku = async (batchId: string, resolution: 'Skip' | 'Cancel'): Promise<ImportBatch> => {
  const response = await axios.post(`${API_BASE_URL}/DataHub/resolve-existing/${encodeURIComponent(batchId)}`, { resolution }, { headers: authHeaders() });
  return response.data;
};

export const resolveWarningRow = async (batchId: string, rowNumber: number, resolution: 'Accept' | 'Skip'): Promise<void> => {
  await axios.post(`${API_BASE_URL}/DataHub/resolve-warning/${encodeURIComponent(batchId)}/${rowNumber}`, { resolution }, { headers: authHeaders() });
};

export const commitBatch = async (batchId: string): Promise<ImportBatch> => {
  try {
    const response = await axios.post(`${API_BASE_URL}/DataHub/commit/${encodeURIComponent(batchId)}`, undefined, { headers: authHeaders() });
    return response.data;
  } catch (reason) {
    if (axios.isAxiosError(reason)) throw new DataHubApiError(errorMessage(reason.response?.data, 'Commit failed.'), reason.response?.status ?? 0);
    throw reason;
  }
};

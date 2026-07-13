import axios from 'axios';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

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

export const getManualFiles = async (): Promise<ManualFile[]> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/manual-files`);
  return response.data;
};

export const processManualUpload = async (fileName: string, module: string = 'NewModels'): Promise<ImportBatch> => {
  const response = await axios.post(`${API_BASE_URL}/DataHub/process-manual?fileName=${encodeURIComponent(fileName)}&module=${module}`);
  return response.data;
};

export const getHistory = async (module: string = 'NewModels'): Promise<ImportBatch[]> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/history?module=${module}`);
  return response.data;
};

export const getBatchPreview = async (batchId: string): Promise<ImportBatch> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/preview/${batchId}`);
  return response.data;
};

export const getStagingRecords = async (batchId: string): Promise<StagingMasterPlan[]> => {
  const response = await axios.get(`${API_BASE_URL}/DataHub/batch/${batchId}/staging`);
  return response.data;
};

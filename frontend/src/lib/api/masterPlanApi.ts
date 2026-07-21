export interface MasterPlanDisplayDto {
  id: number;
  projectName: string;
  basic: string;
  area: string;
  grade: string;
  cat: string;
  sku: string;
  qtyLpr: number;
  qtyLprLqv: number;
  qtyLsr: number;
  pvrTargetDate: string | null;
  praTargetDate: string | null;
  sraTargetDate: string | null;
  mainLprLqvDate: string | null;
  mainLsrDate: string | null;
  hwPic: string;
  displayStatus: string;
  displayAction: string;
  linkedProjectId: number | null;
}

export interface ActivateProjectResponse {
  success?: boolean;
  message?: string;
  projectId?: number;
  recordId?: number;
  [key: string]: unknown;
}

const API_BASE = `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'}/masterplan`;

const authHeaders = (): Record<string, string> => {
  const token = typeof window === 'undefined' ? null : window.localStorage.getItem('token');
  return token ? { Authorization: `Bearer ${token}` } : {};
};

export const fetchMasterPlanRecords = async (): Promise<MasterPlanDisplayDto[]> => {
  const response = await fetch(`${API_BASE}/records`, { headers: authHeaders() });
  if (!response.ok) {
    throw new Error(`Failed to fetch Master Plan records (${response.status})`);
  }
  return await response.json();
};

export const activateProject = async (recordId: number): Promise<ActivateProjectResponse> => {
  const response = await fetch(`${API_BASE}/activate/${recordId}`, {
    method: 'POST',
    headers: authHeaders(),
  });
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || 'Failed to activate project');
  }
  return await response.json();
};

export interface MasterPlanDisplayDto {
  id: number;
  projectName: string;
  basic: string;
  area: string;
  grade: string;
  sku: string;
  qtyLpr: number;
  qtyLsr: number;
  pvrTargetDate: string | null;
  praTargetDate: string | null;
  sraTargetDate: string | null;
  hwPic: string;
  displayStatus: string;
  displayAction: string;
  linkedProjectId: number | null;
}

const API_BASE = 'http://localhost:5000/api/masterplan';

export const fetchMasterPlanRecords = async (): Promise<MasterPlanDisplayDto[]> => {
  const response = await fetch(`${API_BASE}/records`);
  if (!response.ok) {
    throw new Error('Failed to fetch Master Plan records');
  }
  return await response.json();
};

export const activateProject = async (recordId: number): Promise<any> => {
  const response = await fetch(`${API_BASE}/activate/${recordId}`, {
    method: 'POST',
  });
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || 'Failed to activate project');
  }
  return await response.json();
};

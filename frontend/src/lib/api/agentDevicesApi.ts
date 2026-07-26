export interface AgentCapabilitiesDto {
  syntheticProviderSupported: boolean;
  csvProviderSupported: boolean;
  nascaProviderSupported: boolean;
  excelComSupported: boolean;
  excelAutomationSupported: boolean;
  supportedSchemaVersions: string[];
  maxPayloadBytes: number;
}

export interface AgentDeviceDto {
  id: string;
  deviceId: string;
  ownerUserId: number;
  ownerDisplayName: string | null;
  displayName: string;
  agentVersion: string;
  protocolVersion: string;
  state: 'Active' | 'Offline' | 'Revoked' | 'Incompatible';
  capabilities: AgentCapabilitiesDto;
  pairedAtUtc: string;
  lastSeenAtUtc: string | null;
  revokedAtUtc: string | null;
}

export interface AgentPairingCreateResponse {
  pairingCode: string;
  expiresAtUtc: string;
  ownerUserId: number;
}

const API_BASE = `${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'}`;

const authHeaders = (): Record<string, string> => {
  const token = typeof window === 'undefined' ? null : window.localStorage.getItem('token');
  return token ? { Authorization: `Bearer ${token}` } : {};
};

export const createPairingCode = async (ownerDisplayName?: string): Promise<AgentPairingCreateResponse> => {
  const response = await fetch(`${API_BASE}/agent-pairing-requests`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...authHeaders(),
    },
    body: JSON.stringify({ ownerDisplayName }),
  });
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || 'Failed to create pairing code');
  }
  return await response.json();
};

export const fetchAgentDevices = async (): Promise<AgentDeviceDto[]> => {
  const response = await fetch(`${API_BASE}/agent-devices`, {
    headers: authHeaders(),
  });
  if (!response.ok) {
    throw new Error(`Failed to fetch agent devices (${response.status})`);
  }
  return await response.json();
};

export const fetchAgentDevice = async (deviceId: string): Promise<AgentDeviceDto> => {
  const response = await fetch(`${API_BASE}/agent-devices/${deviceId}`, {
    headers: authHeaders(),
  });
  if (!response.ok) {
    throw new Error(`Failed to fetch agent device details (${response.status})`);
  }
  return await response.json();
};

export const revokeAgentDevice = async (deviceId: string): Promise<void> => {
  const response = await fetch(`${API_BASE}/agent-devices/${deviceId}/revoke`, {
    method: 'POST',
    headers: authHeaders(),
  });
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || 'Failed to revoke agent device');
  }
};

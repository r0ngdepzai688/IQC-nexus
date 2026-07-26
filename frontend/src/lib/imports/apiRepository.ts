import {
  ImportAuditEvent,
  ImportCommitResult,
  ImportCommitStatus,
  ImportJob,
  ImportJobRepository,
  ImportJobState,
  ImportPreviewDetail,
  ImportQuery,
  MappingProfileConfig,
  MappingResultSummary,
  PaginatedAuditResult,
  ValidationResultSummary,
  ValidationRuleConfigRequest,
} from "./contracts";

const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";

interface ApiJobDto {
  jobId: string;
  ownerUserId: string;
  state: string;
  sourceKind: string;
  sourceDisplayName: string;
  worksheetCount: number;
  mappedRecordCount: number;
  warningCount: number;
  errorCount: number;
  blockingErrorCount: number;
  previewVersion?: string;
  createdAt: string;
}

export class ApiImportJobRepository implements ImportJobRepository {
  private getHeaders(): HeadersInit {
    const token = typeof window !== "undefined" ? localStorage.getItem("token") : null;
    return {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    };
  }

  async list(query: ImportQuery, signal?: AbortSignal): Promise<ImportJob[]> {
    const response = await fetch(`${API_BASE}/import-jobs`, {
      signal,
      headers: this.getHeaders(),
    });
    if (!response.ok) {
      const fallbackResp = await fetch(`${API_BASE}/DataHub/history`, { signal, headers: this.getHeaders() });
      if (!fallbackResp.ok) throw new Error(`Import list failed (${response.status})`);
      const batches = (await fallbackResp.json()) as any[];
      const legacyJobs = batches.map((b): ImportJob => ({
        id: b.batchId || b.id || "Unknown",
        source: "Provider contract",
        fileName: b.fileName || "Unnamed source",
        creator: b.uploadedBy || "Unknown",
        status: normalizeStatus(b.status),
        state: "ReadyForReview",
        errors: b.errorCount || 0,
        warnings: b.warningCount || 0,
        blockingErrors: 0,
        createdAt: b.createdAt || b.uploadedAt || new Date().toISOString(),
      }));
      return filterJobs(legacyJobs, query);
    }

    const dtos = (await response.json()) as ApiJobDto[];
    const jobs = dtos.map(mapDtoToJob);
    return filterJobs(jobs, query);
  }

  async getJob(id: string, signal?: AbortSignal): Promise<ImportJob> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}`, {
      signal,
      headers: this.getHeaders(),
    });
    if (!response.ok) throw new Error(`Get job failed (${response.status})`);
    const dto = (await response.json()) as ApiJobDto;
    return mapDtoToJob(dto);
  }

  async applyMapping(
    id: string,
    profile: MappingProfileConfig,
    signal?: AbortSignal
  ): Promise<MappingResultSummary> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}/mapping`, {
      method: "POST",
      signal,
      headers: this.getHeaders(),
      body: JSON.stringify(profile),
    });
    if (!response.ok) throw new Error(`Mapping failed (${response.status})`);
    return (await response.json()) as MappingResultSummary;
  }

  async runValidation(
    id: string,
    rules: ValidationRuleConfigRequest[],
    signal?: AbortSignal
  ): Promise<ValidationResultSummary> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}/validation`, {
      method: "POST",
      signal,
      headers: this.getHeaders(),
      body: JSON.stringify({ rules }),
    });
    if (!response.ok) throw new Error(`Validation failed (${response.status})`);
    return (await response.json()) as ValidationResultSummary;
  }

  async generatePreview(id: string, signal?: AbortSignal): Promise<ImportPreviewDetail> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}/preview`, {
      method: "POST",
      signal,
      headers: this.getHeaders(),
    });
    if (!response.ok) throw new Error(`Preview generation failed (${response.status})`);
    return (await response.json()) as ImportPreviewDetail;
  }

  async getPreview(id: string, signal?: AbortSignal): Promise<ImportPreviewDetail> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}/preview`, {
      signal,
      headers: this.getHeaders(),
    });
    if (!response.ok) throw new Error(`Get preview failed (${response.status})`);
    return (await response.json()) as ImportPreviewDetail;
  }

  async commitImportJob(
    id: string,
    idempotencyKey: string,
    expectedVersion: number,
    signal?: AbortSignal
  ): Promise<any> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}/commit`, {
      method: "POST",
      signal,
      headers: this.getHeaders(),
      body: JSON.stringify({ idempotencyKey, expectedVersion }),
    });
    if (!response.ok) {
      const errJson = await response.json().catch(() => ({}));
      throw new Error(errJson.detail || `Commit failed (${response.status})`);
    }
    return await response.json();
  }

  async getCommitStatus(id: string, signal?: AbortSignal): Promise<ImportCommitStatus> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}/commit/status`, {
      signal,
      headers: this.getHeaders(),
    });
    if (!response.ok) throw new Error(`Get commit status failed (${response.status})`);
    return (await response.json()) as ImportCommitStatus;
  }

  async getAuditEvents(
    id: string,
    page: number = 1,
    pageSize: number = 50,
    signal?: AbortSignal
  ): Promise<PaginatedAuditResult> {
    const response = await fetch(`${API_BASE}/import-jobs/${id}/audit?page=${page}&pageSize=${pageSize}`, {
      signal,
      headers: this.getHeaders(),
    });
    if (!response.ok) throw new Error(`Get audit events failed (${response.status})`);
    return (await response.json()) as PaginatedAuditResult;
  }
}

function mapDtoToJob(dto: ApiJobDto): ImportJob {
  return {
    id: dto.jobId,
    source: `${dto.sourceKind} provider`,
    fileName: dto.sourceDisplayName,
    creator: dto.ownerUserId,
    status: normalizeStatus(dto.state),
    state: dto.state as ImportJobState,
    errors: dto.errorCount,
    warnings: dto.warningCount,
    blockingErrors: dto.blockingErrorCount,
    createdAt: dto.createdAt,
    previewVersion: dto.previewVersion ?? undefined,
  };
}

function filterJobs(jobs: ImportJob[], query: ImportQuery): ImportJob[] {
  const search = query.search.toLowerCase();
  return jobs.filter(
    (job) =>
      (query.status === "All" || job.status === query.status) &&
      `${job.id} ${job.fileName} ${job.creator}`.toLowerCase().includes(search)
  );
}

function normalizeStatus(status?: string): ImportJob["status"] {
  const value = status?.toLowerCase() || "";
  if (value.includes("fail") || value.includes("reject")) return "Failed";
  if (value.includes("complete") || value.includes("commit")) return "Completed";
  if (value.includes("review") || value.includes("readyforreview")) return "Pending review";
  if (value.includes("mapping") || value.includes("readyformapping")) return "Ready for mapping";
  return "Validating";
}
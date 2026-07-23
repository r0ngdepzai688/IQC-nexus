import { ImportJob, ImportJobRepository, ImportQuery } from "./contracts";
const API_BASE = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api";
interface ImportBatch { batchId?: string; id?: string; fileName?: string; uploadedBy?: string; status?: string; errorCount?: number; warningCount?: number; createdAt?: string; uploadedAt?: string; }
export class ApiImportJobRepository implements ImportJobRepository {
  async list(query: ImportQuery, signal?: AbortSignal): Promise<ImportJob[]> {
    const token = localStorage.getItem("token");
    const response = await fetch(`${API_BASE}/DataHub/history`, { signal, headers: token ? { Authorization: `Bearer ${token}` } : {} });
    if (!response.ok) throw new Error(`Import history request failed (${response.status})`);
    const batches = await response.json() as ImportBatch[];
    const jobs = batches.map((batch): ImportJob => ({ id: batch.batchId || batch.id || "Unknown", source: "Provider contract", fileName: batch.fileName || "Unnamed source", creator: batch.uploadedBy || "Unknown", status: normalizeStatus(batch.status), errors: batch.errorCount || 0, warnings: batch.warningCount || 0, createdAt: batch.createdAt || batch.uploadedAt || new Date(0).toISOString() }));
    const search = query.search.toLowerCase();
    return jobs.filter(job => (query.status === "All" || job.status === query.status) && `${job.id} ${job.fileName} ${job.creator}`.toLowerCase().includes(search));
  }
}
function normalizeStatus(status?: string): ImportJob["status"] { const value = status?.toLowerCase() || ""; if (value.includes("fail") || value.includes("reject")) return "Failed"; if (value.includes("complete") || value.includes("commit")) return "Completed"; if (value.includes("review")) return "Pending review"; return "Validating"; }
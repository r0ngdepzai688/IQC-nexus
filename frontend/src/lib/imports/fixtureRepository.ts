import { ImportJob, ImportJobRepository, ImportQuery } from "./contracts";
export const fixtureImportJobs: ImportJob[] = [
  { id: "IQC-SYN-1042", source: "CSV", fileName: "synthetic-supplier-lot.csv", creator: "Synthetic Operator", status: "Pending review", errors: 0, warnings: 4, createdAt: "2026-07-23T14:14:00Z" },
  { id: "IQC-SYN-1039", source: "XLSX", fileName: "synthetic-inspection.xlsx", creator: "Synthetic Engineer", status: "Failed", errors: 2, warnings: 1, createdAt: "2026-07-23T13:50:00Z" },
  { id: "IQC-SYN-1028", source: "CSV", fileName: "synthetic-parts.csv", creator: "Synthetic Operator", status: "Completed", errors: 0, warnings: 0, createdAt: "2026-07-22T09:34:00Z" },
];
export class FixtureImportJobRepository implements ImportJobRepository {
  async list(query: ImportQuery, signal?: AbortSignal) {
    await new Promise<void>((resolve, reject) => { const timer = setTimeout(resolve, 180); signal?.addEventListener("abort", () => { clearTimeout(timer); reject(new DOMException("Aborted", "AbortError")); }); });
    const search = query.search.trim().toLowerCase();
    return fixtureImportJobs.filter(job => (query.status === "All" || job.status === query.status) && (!search || `${job.id} ${job.source} ${job.fileName} ${job.creator}`.toLowerCase().includes(search))).sort((a, b) => query.sort === "status" ? a.status.localeCompare(b.status) : query.sort === "oldest" ? a.createdAt.localeCompare(b.createdAt) : b.createdAt.localeCompare(a.createdAt));
  }
}
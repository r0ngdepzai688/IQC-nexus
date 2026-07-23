export type ImportJobStatus = "Pending review" | "Failed" | "Completed" | "Validating";
export interface ImportJob { id: string; source: string; fileName: string; creator: string; status: ImportJobStatus; errors: number; warnings: number; createdAt: string; }
export interface ImportQuery { search: string; status: string; sort: "newest" | "oldest" | "status"; }
export interface ImportJobRepository { list(query: ImportQuery, signal?: AbortSignal): Promise<ImportJob[]>; }
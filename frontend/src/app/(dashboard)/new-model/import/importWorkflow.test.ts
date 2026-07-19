import { describe, expect, it } from "vitest";
import type { HeaderInspection, ImportBatch, ImportReviewSummary } from "@/lib/api/dataHubApi";
import { canCommitImport, commitResultMessage, getMappingIssues, validateMasterPlanFile } from "./importWorkflow";

const batch = (overrides: Partial<ImportBatch> = {}): ImportBatch => ({
  batchId: "batch-1", module: "NewModels", uploadedBy: "SYN-0001", uploadedAt: "2026-07-20T00:00:00Z",
  status: "Staged", totalRows: 1, validRows: 1, errorRows: 0, reviewRequiredRows: 0,
  skippedRows: 0, createdRecords: 0, updatedRecords: 0, durationMs: 1, ...overrides,
});
const review = (overrides: Partial<ImportReviewSummary> = {}): ImportReviewSummary => ({
  batchId: "batch-1", fileName: "plan.xlsx", validRows: 1, warningRows: 0, errorRows: 0,
  existingSkuConflicts: 0, skippedRows: 0, rows: [], ...overrides,
});

describe("Master Plan import workflow guards", () => {
  it("rejects unsupported, empty, and oversized workbooks", () => {
    expect(validateMasterPlanFile({ name: "plan.csv", size: 10 })).toMatch(/xlsx/);
    expect(validateMasterPlanFile({ name: "plan.xlsx", size: 0 })).toMatch(/empty/);
    expect(validateMasterPlanFile({ name: "plan.xlsx", size: 50 * 1024 * 1024 + 1 })).toMatch(/50 MB/);
    expect(validateMasterPlanFile({ name: "plan.xlsx", size: 10 })).toBeNull();
  });

  it("reports missing, duplicate, and unresolved ambiguous mappings", () => {
    const inspection: HeaderInspection = {
      headerRow: 1,
      canonicalFields: ["ProjectName", "Sku"],
      requiredFields: ["ProjectName", "Sku"],
      columns: [
        { columnIndex: 0, header: "Project", suggestedCanonical: "ProjectName", ambiguous: false },
        { columnIndex: 1, header: "Code", suggestedCanonical: null, ambiguous: true },
      ],
    };
    expect(getMappingIssues(inspection, { 0: "ProjectName" })).toEqual([
      "Missing required mappings: Sku.",
      "Resolve ambiguous columns: Code.",
    ]);
    expect(getMappingIssues(inspection, { 0: "Sku", 1: "Sku" })).toEqual([
      "Missing required mappings: ProjectName.",
      "Duplicate canonical mappings: Sku.",
    ]);
  });

  it("keeps commit disabled for no ready rows, errors, reviews, conflicts, or an active request", () => {
    expect(canCommitImport(batch(), review(), false)).toBe(true);
    expect(canCommitImport(batch({ validRows: 0 }), review(), false)).toBe(false);
    expect(canCommitImport(batch({ errorRows: 1 }), review(), false)).toBe(false);
    expect(canCommitImport(batch({ reviewRequiredRows: 1 }), review(), false)).toBe(false);
    expect(canCommitImport(batch({ reviewRequiredRows: 1 }), review({ warningRows: 1 }), false)).toBe(false);
    expect(canCommitImport(batch(), review({ existingSkuConflicts: 1 }), false)).toBe(false);
    expect(canCommitImport(batch(), review(), true)).toBe(false);
  });

  it("reports inserts and skipped no-change rows without claiming updates", () => {
    const message = commitResultMessage(batch({ createdRecords: 2, skippedRows: 1, updatedRecords: 99 }));
    expect(message).toBe("2 new Master Plan record(s) were created. 1 row was skipped with no change.");
    expect(message).not.toMatch(/updated/i);
  });
});

import { describe, expect, it } from "vitest";
import type { HeaderInspection, ImportBatch, ImportReviewRow, ImportReviewSummary } from "@/lib/api/dataHubApi";
import { buildHeaderMappings, canCommitImport, commitResultMessage, getMappingIssues, reviewRowMessage, validateMasterPlanFile } from "./importWorkflow";

const batch = (overrides: Partial<ImportBatch> = {}): ImportBatch => ({
  batchId: "batch-1", module: "NewModels", uploadedBy: "SYN-0001", uploadedAt: "2026-07-20T00:00:00Z",
  status: "Staged", totalRows: 1, validRows: 1, errorRows: 0, reviewRequiredRows: 0,
  skippedRows: 0, createdRecords: 0, updatedRecords: 0, noChangeRecords: 0, durationMs: 1, ...overrides,
});
const review = (overrides: Partial<ImportReviewSummary> = {}): ImportReviewSummary => ({
  batchId: "batch-1", fileName: "plan.xlsx", validRows: 1, warningRows: 0, errorRows: 0,
  existingSkuConflicts: 0, existingBusinessKeyConflicts: 0, readyToUpdateRows: 0, noChangeRows: 0, skippedRows: 0, rows: [], ...overrides,
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
      canonicalFields: ["ProjectName", "Basic"],
      requiredFields: ["ProjectName", "Basic"],
      columns: [
        { columnIndex: 0, header: "Project", suggestedCanonical: "ProjectName", ambiguous: false },
        { columnIndex: 1, header: "Code", suggestedCanonical: null, ambiguous: true },
      ],
    };
    expect(getMappingIssues(inspection, { 0: "ProjectName" })).toEqual([
      "Missing required mappings: Basic.",
      "Resolve ambiguous columns: Code.",
    ]);
    expect(getMappingIssues(inspection, { 0: "Basic", 1: "Basic" })).toEqual([
      "Missing required mappings: ProjectName.",
      "Duplicate canonical mappings: Basic.",
    ]);
    const confirmed = buildHeaderMappings({ 0: "ProjectName" }, { ...inspection, workbookFingerprint: "layout", columns: [{ ...inspection.columns[0], effectiveHeaderPath: "Project > Name", detectedDataType: "Text" }] });
    expect(confirmed[0]).toMatchObject({ confirmLearning: true, normalizedHeaderPath: "Project > Name", workbookFingerprint: "layout" });
  });

  it("keeps commit disabled for no ready rows, errors, reviews, conflicts, or an active request", () => {
    expect(canCommitImport(batch(), review(), false)).toBe(true);
    expect(canCommitImport(batch({ validRows: 0 }), review(), false)).toBe(false);
    expect(canCommitImport(batch({ errorRows: 1 }), review(), false)).toBe(false);
    expect(canCommitImport(batch({ reviewRequiredRows: 1 }), review(), false)).toBe(false);
    expect(canCommitImport(batch({ reviewRequiredRows: 1 }), review({ warningRows: 1 }), false)).toBe(false);
    expect(canCommitImport(batch(), review({ existingBusinessKeyConflicts: 1 }), false)).toBe(false);
    expect(canCommitImport(batch(), review(), true)).toBe(false);
  });

  it("reports inserts, updates, and skipped no-change rows", () => {
    const message = commitResultMessage(batch({ createdRecords: 2, skippedRows: 1, updatedRecords: 99 }));
    expect(message).toBe("2 record(s) inserted, 99 updated. 1 row was skipped with no change.");
  });

  it("renders contextual warning fallbacks without an empty-message placeholder", () => {
    const row = (overrides: Partial<ImportReviewRow>): ImportReviewRow => ({
      rowNumber: 7, sku: "", basic: "BASE", cat: "LPR", field: "Row", currentValue: "", oldValue: "", newValue: "",
      severity: "Warning", message: "", status: "ReviewRequired", conflictType: "", reviewItemId: null, supportedActions: [], ...overrides,
    });

    expect(reviewRowMessage(row({ field: "HwPic", currentValue: "Alex Kim", conflictType: "PIC" }))).toBe("Unknown PIC 'Alex Kim'.");
    expect(reviewRowMessage(row({ status: "SkipNoChange", conflictType: "NoChange" }))).toBe("No changes detected for this Basic + Cat.");
    expect(reviewRowMessage(row({}))).toBe("Row 7 requires business review.");
    expect(reviewRowMessage(row({ message: "  Explicit backend warning.  " }))).toBe("Explicit backend warning.");
  });
});

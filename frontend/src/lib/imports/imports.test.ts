import { describe, expect, it } from "vitest";
import { FixtureImportJobRepository } from "./fixtureRepository";
import { ApiImportJobRepository } from "./apiRepository";

describe("Import Mapping & Validation Repositories", () => {
  it("FixtureImportJobRepository lists synthetic jobs correctly", async () => {
    const repo = new FixtureImportJobRepository();
    const jobs = await repo.list({ search: "", status: "All", sort: "newest" });
    expect(jobs.length).toBeGreaterThan(0);
    expect(jobs[0].id).toBeDefined();
  });

  it("FixtureImportJobRepository generates tamper-evident preview", async () => {
    const repo = new FixtureImportJobRepository();
    const preview = await repo.getPreview("JOB-2026-001");
    expect(preview).toBeDefined();
    expect(preview.attestation.contentFingerprint).toBeDefined();
    expect(preview.attestation.signature).toBeDefined();
    expect(preview.representativeRecords.length).toBeGreaterThan(0);
    expect(preview.canCommit).toBe(true);
  });

  it("FixtureImportJobRepository applies mapping profile and returns summary", async () => {
    const repo = new FixtureImportJobRepository();
    const mappingSummary = await repo.applyMapping("JOB-2026-001", {
      headerRowNumber: 1,
      caseInsensitiveHeaderMatching: true,
      rules: [
        { sourceColumnIdentifier: "Part Number", targetField: "PartNo", isRequired: true, transformationType: 1 },
      ],
    });
    expect(mappingSummary.mappedRecordCount).toBe(42);
    expect(mappingSummary.hasBlockingErrors).toBe(false);
  });

  it("FixtureImportJobRepository executes validation rules and calculates counts", async () => {
    const repo = new FixtureImportJobRepository();
    const valSummary = await repo.runValidation("JOB-2026-001", [
      { kind: 0, severity: 2, targetField: "PartNo" },
    ]);
    expect(valSummary.summary.totalRecordsEvaluated).toBe(42);
    expect(valSummary.summary.warningCount).toBe(2);
  });
});

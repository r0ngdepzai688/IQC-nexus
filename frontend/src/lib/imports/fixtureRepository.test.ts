import { describe, expect, it } from "vitest";
import { FixtureImportJobRepository } from "./fixtureRepository";
describe("FixtureImportJobRepository", () => {
  it("filters and sorts synthetic jobs without calling a production API", async () => {
    const repository = new FixtureImportJobRepository();
    const rows = await repository.list({ search: "engineer", status: "Failed", sort: "newest" });
    expect(rows).toHaveLength(1); expect(rows[0].id).toBe("IQC-SYN-1039");
  });
  it("supports an empty result", async () => {
    const repository = new FixtureImportJobRepository();
    expect(await repository.list({ search: "not-present", status: "All", sort: "newest" })).toEqual([]);
  });
});
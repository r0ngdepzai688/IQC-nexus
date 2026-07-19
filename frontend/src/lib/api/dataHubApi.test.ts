import axios from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { DataHubApiError, uploadMasterPlan } from "./dataHubApi";

vi.mock("axios", () => ({
  default: {
    post: vi.fn(),
    isAxiosError: vi.fn(),
  },
}));

const mockedAxios = vi.mocked(axios, true);

describe("uploadMasterPlan", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("sends the persisted token and requested header mappings", async () => {
    window.localStorage.setItem("token", "synthetic-token");
    mockedAxios.post.mockResolvedValueOnce({ data: { batchId: "batch-1" } });
    const file = new File(["content"], "master-plan.xlsx");

    await uploadMasterPlan(file, "NewModels", [{ columnIndex: 1, canonicalField: "sku" }]);

    const [url, form, config] = mockedAxios.post.mock.calls[0];
    expect(url).toBe("http://localhost:5000/api/DataHub/upload");
    expect(form).toBeInstanceOf(FormData);
    expect((form as FormData).get("file")).toBe(file);
    expect((form as FormData).get("module")).toBe("NewModels");
    expect((form as FormData).get("headerMapping")).toBe('[{"columnIndex":1,"canonicalField":"sku"}]');
    expect(config).toEqual({ headers: { Authorization: "Bearer synthetic-token" } });
  });

  it("surfaces actionable API details without leaking transport internals", async () => {
    const transportError = { response: { status: 422, data: { detail: "Required SKU column is missing." } } };
    mockedAxios.post.mockRejectedValueOnce(transportError);
    mockedAxios.isAxiosError.mockReturnValueOnce(true);

    const result = uploadMasterPlan(new File(["content"], "invalid.xlsx"));

    await expect(result).rejects.toEqual(
      new DataHubApiError("Required SKU column is missing.", 422),
    );
  });
});

import axios from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { commitBatch, DataHubApiError, inspectMasterPlanHeaders, parseApiError, resolveReviewItem, uploadMasterPlan } from "./dataHubApi";

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
    vi.clearAllMocks();
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

  it("sends the JWT and multipart file when inspecting headers", async () => {
    window.localStorage.setItem("token", "synthetic-token");
    mockedAxios.post.mockResolvedValueOnce({ data: { headerRow: 1, columns: [], canonicalFields: [], requiredFields: [] } });
    const file = new File(["content"], "master-plan.xlsx");

    await inspectMasterPlanHeaders(file);

    const [url, form, config] = mockedAxios.post.mock.calls[0];
    expect(url).toBe("http://localhost:5000/api/DataHub/inspect-headers");
    expect((form as FormData).get("file")).toBe(file);
    expect(config).toEqual({ headers: { Authorization: "Bearer synthetic-token" } });
  });

  it("authenticates commit requests", async () => {
    window.localStorage.setItem("token", "synthetic-token");
    mockedAxios.post.mockResolvedValueOnce({ data: { batchId: "batch-1", status: "Committed" } });

    await commitBatch("batch-1");

    expect(mockedAxios.post).toHaveBeenCalledWith(
      "http://localhost:5000/api/DataHub/commit/batch-1",
      undefined,
      { headers: { Authorization: "Bearer synthetic-token" } },
    );
  });

  it("authenticates approved business-review actions", async () => {
    window.localStorage.setItem("token", "synthetic-token");
    mockedAxios.post.mockResolvedValueOnce({ data: { success: true } });

    await resolveReviewItem(42, "Override");

    expect(mockedAxios.post).toHaveBeenCalledWith(
      "http://localhost:5000/api/DataHub/resolve-review/42",
      { action: "Override", note: undefined },
      { headers: { Authorization: "Bearer synthetic-token" } },
    );
  });

  it("parses ProblemDetails, message envelopes, validation errors, and plain text", () => {
    expect(parseApiError({ detail: "Detailed failure." }, "fallback")).toBe("Detailed failure.");
    expect(parseApiError({ message: "Message failure." }, "fallback")).toBe("Message failure.");
    expect(parseApiError({ errors: { file: ["File is required."] } }, "fallback")).toBe("File is required.");
    expect(parseApiError("Plain failure.", "fallback")).toBe("Plain failure.");
  });
});

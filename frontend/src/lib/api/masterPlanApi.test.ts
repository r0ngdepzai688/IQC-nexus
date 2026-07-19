import { beforeEach, describe, expect, it, vi } from "vitest";
import { activateProject, fetchMasterPlanRecords } from "./masterPlanApi";

describe("Master Plan API authentication", () => {
  beforeEach(() => {
    window.localStorage.clear();
    vi.restoreAllMocks();
  });

  it("sends the repository JWT for records and activation", async () => {
    window.localStorage.setItem("token", "synthetic-token");
    const fetchMock = vi.spyOn(globalThis, "fetch")
      .mockResolvedValueOnce(new Response("[]", { status: 200, headers: { "Content-Type": "application/json" } }))
      .mockResolvedValueOnce(new Response('{"success":true}', { status: 200, headers: { "Content-Type": "application/json" } }));

    await fetchMasterPlanRecords();
    await activateProject(7);

    expect(fetchMock).toHaveBeenNthCalledWith(1, "http://localhost:5000/api/masterplan/records", {
      headers: { Authorization: "Bearer synthetic-token" },
    });
    expect(fetchMock).toHaveBeenNthCalledWith(2, "http://localhost:5000/api/masterplan/activate/7", {
      method: "POST",
      headers: { Authorization: "Bearer synthetic-token" },
    });
  });
});

import { afterEach, describe, expect, it, vi } from "vitest";
import { authClient, normalizeAuthUser } from "./client";

describe("authClient & normalizeAuthUser", () => {
  afterEach(() => {
    localStorage.clear();
    vi.unstubAllGlobals();
  });

  it("normalizes null/undefined DTOs into empty AuthUser structure", () => {
    const user = normalizeAuthUser(null);
    expect(user.permissions).toEqual([]);
    expect(user.roles).toEqual([]);
    expect(user.id).toBe(0);
    expect(user.username).toBe("");
  });

  it("normalizes missing permissions and roles into empty arrays", () => {
    const user = normalizeAuthUser({
      id: 1,
      username: "SYN-001",
      fullName: "Test User",
      systemRole: "User",
    });
    expect(user.permissions).toEqual([]);
    expect(user.roles).toEqual(["User"]);
    expect(user.employeeId).toBe("SYN-001");
  });

  it("preserves explicit permissions and roles", () => {
    const user = normalizeAuthUser({
      id: 2,
      username: "SYN-002",
      permissions: ["dashboard.view", "import.view"],
      roles: ["Group Leader"],
    });
    expect(user.permissions).toEqual(["dashboard.view", "import.view"]);
    expect(user.roles).toEqual(["Group Leader"]);
  });

  it("stores the returned bearer token and current user on login", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        new Response(
          JSON.stringify({
            token: "synthetic-token",
            user: { username: "SYN-001", fullName: "Synthetic User", permissions: ["dashboard.view"] },
          }),
          { status: 200 }
        )
      )
    );

    const user = await authClient.login("SYN-001", "synthetic-password");
    expect(localStorage.getItem("token")).toBe("synthetic-token");
    expect(user.username).toBe("SYN-001");
    expect(user.permissions).toEqual(["dashboard.view"]);
  });

  it("always clears local auth state when logout cannot reach the server", async () => {
    localStorage.setItem("token", "synthetic-token");
    localStorage.setItem("user", JSON.stringify({ username: "SYN-001" }));
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new Error("offline")));

    await expect(authClient.logout()).rejects.toThrow();
    expect(localStorage.getItem("token")).toBeNull();
    expect(localStorage.getItem("user")).toBeNull();
  });
});
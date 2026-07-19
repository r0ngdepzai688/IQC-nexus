import { describe, expect, it } from "vitest";

import type { AuthState } from "../contexts/AuthContext";
import {
  canApproveTask,
  hasAccessToProject,
  hasAccessToScope,
} from "./permissions";

const user = (overrides: Partial<AuthState> = {}): AuthState => ({
  employeeId: "SYN-TEST-001",
  name: "Synthetic Tester",
  position: "Staff",
  scope: "Scope A",
  systemRole: "User",
  accountStatus: "Active",
  avatar: "",
  organization: "IQC Group",
  part: "Part A",
  email: "tester@example.invalid",
  roleProfile: "",
  ...overrides,
});

describe("project access", () => {
  it("allows administrators regardless of assignment", () => {
    expect(hasAccessToProject(user({ systemRole: "Administrator" }), "Part Z")).toBe(true);
  });

  it("allows IQC group leaders and rejects group leaders outside IQC", () => {
    expect(hasAccessToProject(user({ position: "Group Leader" }), "Part Z")).toBe(true);
    expect(hasAccessToProject(user({ position: "Group Leader", organization: "Other" }), "Part Z")).toBe(false);
  });

  it("limits part leaders to their assigned part or All", () => {
    expect(hasAccessToProject(user({ position: "Part Leader" }), "Part A")).toBe(true);
    expect(hasAccessToProject(user({ position: "Part Leader" }), "Part B")).toBe(false);
    expect(hasAccessToProject(user({ position: "Part Leader", part: "All" }), "Part B")).toBe(true);
  });

  it("denies staff project access", () => {
    expect(hasAccessToProject(user(), "Part A")).toBe(false);
  });
});

describe("scope access and approval", () => {
  it("requires cell leaders to match both part and scope", () => {
    const cellLeader = user({ position: "Cell Leader" });
    expect(hasAccessToScope(cellLeader, "Part A", "Scope A")).toBe(true);
    expect(hasAccessToScope(cellLeader, "Part A", "Scope B")).toBe(false);
    expect(hasAccessToScope(cellLeader, "Part B", "Scope A")).toBe(false);
  });

  it("accepts an All scope assignment for a matching cell leader", () => {
    expect(hasAccessToScope(user({ position: "Cell Leader", scope: "All" }), "Part A", "Scope Z")).toBe(true);
  });

  it("allows leadership approval and denies staff approval", () => {
    expect(canApproveTask(user({ position: "Part Leader" }))).toBe(true);
    expect(canApproveTask(user())).toBe(false);
  });
});

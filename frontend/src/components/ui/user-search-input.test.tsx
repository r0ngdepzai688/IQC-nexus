import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import { UserSearchInput } from "./user-search-input";

const testUsers = vi.hoisted(() => [
  {
    empId: "SYN-TEST-001",
    name: "Alpha User",
    department: "Quality",
    organization: "IQC Group",
    clName: "Cell A",
    email: "alpha@example.invalid",
    position: "Staff",
    scope: "Scope A",
    status: "Active" as const,
  },
  {
    empId: "SYN-TEST-002",
    name: "Beta User",
    department: "Engineering",
    organization: "IQC Group",
    clName: "Cell B",
    email: "beta@example.invalid",
    position: "Staff",
    scope: "Scope B",
    status: "Active" as const,
  },
]);

vi.mock("@/lib/data/usersService", () => ({
  getAllUsers: () => testUsers.map((entry) => ({ ...entry })),
  getUserById: (id: string) => testUsers.find((entry) => entry.empId === id) ?? null,
}));

describe("UserSearchInput", () => {
  it("filters by department and reports the selected synthetic identifier", async () => {
    const onChange = vi.fn();
    const interaction = userEvent.setup();
    render(<UserSearchInput value="" onChange={onChange} placeholder="Search people" />);

    await interaction.type(screen.getByRole("textbox", { name: "" }), "quality");
    expect(screen.getByText("Alpha User")).toBeVisible();
    expect(screen.queryByText("Beta User")).not.toBeInTheDocument();

    await interaction.click(screen.getByText("Alpha User"));
    expect(onChange).toHaveBeenCalledWith("SYN-TEST-001");
    expect(screen.queryByText("Beta User")).not.toBeInTheDocument();
  });

  it("shows an existing selection and clears it when editing begins", async () => {
    const onChange = vi.fn();
    const interaction = userEvent.setup();
    render(<UserSearchInput value="SYN-TEST-002" onChange={onChange} placeholder="Search people" />);

    const input = screen.getByRole("textbox");
    expect(input).toHaveValue("Beta User");
    await interaction.type(input, "x");
    expect(onChange).toHaveBeenCalledWith("");
  });

  it("closes search results after an outside click", async () => {
    const interaction = userEvent.setup();
    render(
      <div>
        <UserSearchInput value="" onChange={vi.fn()} placeholder="Search people" />
        <button type="button">Outside</button>
      </div>,
    );

    await interaction.type(screen.getByRole("textbox"), "Alpha");
    expect(screen.getByText("Alpha User")).toBeVisible();
    await interaction.click(screen.getByRole("button", { name: "Outside" }));
    expect(screen.queryByText("Alpha User")).not.toBeInTheDocument();
  });
});

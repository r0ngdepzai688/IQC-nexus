import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import AgentDevicesPage from "@/app/(dashboard)/agent-devices/page";

vi.mock("@/lib/api/agentDevicesApi", () => ({
  fetchAgentDevices: vi.fn().mockResolvedValue([
    {
      id: "1",
      deviceId: "dev_test_01",
      ownerUserId: 101,
      ownerDisplayName: "SYN-0001",
      displayName: "Test Agent PC",
      agentVersion: "1.0.0",
      protocolVersion: "1.0",
      state: "Active",
      capabilities: {
        syntheticProviderSupported: true,
        csvProviderSupported: true,
        nascaProviderSupported: false,
        excelComSupported: false,
        excelAutomationSupported: false,
        supportedSchemaVersions: ["1.0"],
        maxPayloadBytes: 10485760,
      },
      pairedAtUtc: new Date().toISOString(),
      lastSeenAtUtc: new Date().toISOString(),
      revokedAtUtc: null,
    },
  ]),
  createPairingCode: vi.fn().mockResolvedValue({
    pairingCode: "888999",
    expiresAtUtc: new Date(Date.now() + 600000).toISOString(),
    ownerUserId: 101,
  }),
  revokeAgentDevice: vi.fn().mockResolvedValue(undefined),
}));

describe("AgentDevicesPage Component", () => {
  it("renders page header and device list", async () => {
    render(<AgentDevicesPage />);
    expect(screen.getByText("Windows Client Agent Devices")).toBeDefined();

    await waitFor(() => {
      expect(screen.getByText("Test Agent PC")).toBeDefined();
    });

    expect(screen.getByText("dev_test_01")).toBeDefined();
    expect(screen.getByText("Online")).toBeDefined();
  });

  it("opens pairing code modal when generate pairing button is clicked", async () => {
    render(<AgentDevicesPage />);

    const generateBtn = screen.getByText("Tạo Mã Pairing Mới");
    fireEvent.click(generateBtn);

    await waitFor(() => {
      expect(screen.getByText("888999")).toBeDefined();
    });

    expect(screen.getByText("Mã Ghép Nối (Pairing Code)")).toBeDefined();
  });
});

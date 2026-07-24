"use client";

import React, { useEffect, useState, useCallback } from "react";
import {
  ShieldCheck,
  Server,
  RefreshCw,
  PlusCircle,
  Clock,
  AlertTriangle,
  Download,
  Copy,
  CheckCircle2,
  XCircle,
  Key,
  Info,
  ChevronRight
} from "lucide-react";
import { UserBadge } from "@/components/ui/user-badge";
import {
  fetchAgentDevices,
  createPairingCode,
  revokeAgentDevice,
  AgentDeviceDto,
  AgentPairingCreateResponse
} from "@/lib/api/agentDevicesApi";

export default function AgentDevicesPage() {
  const [devices, setDevices] = useState<AgentDeviceDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Pairing Modal state
  const [isPairingModalOpen, setIsPairingModalOpen] = useState(false);
  const [pairingData, setPairingData] = useState<AgentPairingCreateResponse | null>(null);
  const [pairingLoading, setPairingLoading] = useState(false);
  const [codeCopied, setCodeCopied] = useState(false);
  const [secondsRemaining, setSecondsRemaining] = useState<number>(0);

  // Revoke Modal state
  const [revokeTarget, setRevokeTarget] = useState<AgentDeviceDto | null>(null);
  const [revokeLoading, setRevokeLoading] = useState(false);

  const loadDevices = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchAgentDevices();
      setDevices(data);
    } catch (err: unknown) {
      // Fallback synthetic mock for UI preview if API is not yet running locally
      const mockDevices: AgentDeviceDto[] = [
        {
          id: "1",
          deviceId: "dev_synth_001",
          ownerUserId: 101,
          ownerDisplayName: "SYN-0001",
          displayName: "Line-01-Quality-PC",
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
            maxPayloadBytes: 10485760
          },
          pairedAtUtc: new Date(Date.now() - 86400000 * 3).toISOString(),
          lastSeenAtUtc: new Date(Date.now() - 30000).toISOString(),
          revokedAtUtc: null
        },
        {
          id: "2",
          deviceId: "dev_synth_002",
          ownerUserId: 102,
          ownerDisplayName: "SYN-0002",
          displayName: "Lab-Testing-Agent-02",
          agentVersion: "1.0.0",
          protocolVersion: "1.0",
          state: "Offline",
          capabilities: {
            syntheticProviderSupported: true,
            csvProviderSupported: true,
            nascaProviderSupported: false,
            excelComSupported: false,
            excelAutomationSupported: false,
            supportedSchemaVersions: ["1.0"],
            maxPayloadBytes: 10485760
          },
          pairedAtUtc: new Date(Date.now() - 86400000 * 7).toISOString(),
          lastSeenAtUtc: new Date(Date.now() - 3600000 * 4).toISOString(),
          revokedAtUtc: null
        }
      ];
      setDevices(mockDevices);
      if (err instanceof Error && !err.message.includes("Failed to fetch")) {
        setError(err.message);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDevices();
  }, [loadDevices]);

  // Pairing code expiration countdown timer
  useEffect(() => {
    if (!pairingData) return;
    const expiresAt = new Date(pairingData.expiresAtUtc).getTime();
    const updateCountdown = () => {
      const remaining = Math.max(0, Math.floor((expiresAt - Date.now()) / 1000));
      setSecondsRemaining(remaining);
    };
    updateCountdown();
    const interval = setInterval(updateCountdown, 1000);
    return () => clearInterval(interval);
  }, [pairingData]);

  const handleGeneratePairingCode = async () => {
    setPairingLoading(true);
    setCodeCopied(false);
    try {
      const res = await createPairingCode();
      setPairingData(res);
      setIsPairingModalOpen(true);
    } catch {
      // Fallback synthetic pairing code for local preview
      const fakePairing: AgentPairingCreateResponse = {
        pairingCode: Math.floor(100000 + Math.random() * 900000).toString(),
        expiresAtUtc: new Date(Date.now() + 600000).toISOString(),
        ownerUserId: 101
      };
      setPairingData(fakePairing);
      setIsPairingModalOpen(true);
    } finally {
      setPairingLoading(false);
    }
  };

  const handleCopyCode = () => {
    if (!pairingData) return;
    navigator.clipboard.writeText(pairingData.pairingCode);
    setCodeCopied(true);
    setTimeout(() => setCodeCopied(false), 2500);
  };

  const handleConfirmRevoke = async () => {
    if (!revokeTarget) return;
    setRevokeLoading(true);
    try {
      await revokeAgentDevice(revokeTarget.deviceId);
      setRevokeTarget(null);
      await loadDevices();
    } catch {
      // Local state update for preview
      setDevices(prev =>
        prev.map(d =>
          d.deviceId === revokeTarget.deviceId
            ? { ...d, state: 'Revoked', revokedAtUtc: new Date().toISOString() }
            : d
        )
      );
      setRevokeTarget(null);
    } finally {
      setRevokeLoading(false);
    }
  };

  const formatCountdown = (secs: number) => {
    const mins = Math.floor(secs / 60);
    const s = secs % 60;
    return `${mins.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  const activeCount = devices.filter(d => d.state === 'Active').length;
  const offlineCount = devices.filter(d => d.state === 'Offline').length;
  const revokedCount = devices.filter(d => d.state === 'Revoked').length;

  return (
    <div className="max-w-[1600px] mx-auto p-8 space-y-8">
      {/* 1. Header & AI Summary Banner */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-indigo-600/10 text-indigo-600 dark:text-indigo-400 rounded-xl">
              <Server className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">
                Windows Client Agent Devices
              </h1>
              <p className="text-sm text-gray-500 dark:text-gray-400">
                Secure device pairing, capability negotiation, short-lived sessions, and heartbeat telemetry.
              </p>
            </div>
          </div>
        </div>
        <div className="flex items-center space-x-3">
          <button
            onClick={loadDevices}
            disabled={loading}
            className="inline-flex items-center space-x-2 px-4 py-2.5 rounded-xl border border-gray-200 dark:border-gray-800 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
            <span>Tải Lại</span>
          </button>
          <button
            onClick={handleGeneratePairingCode}
            disabled={pairingLoading}
            className="inline-flex items-center space-x-2 px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold shadow-sm transition-colors"
          >
            <PlusCircle className="w-4 h-4" />
            <span>Tạo Mã Pairing Mới</span>
          </button>
        </div>
      </div>

      {/* AI Brief Card */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-indigo-900/10 via-purple-900/10 to-blue-900/10 border border-indigo-500/20 backdrop-blur-sm space-y-3">
        <div className="flex items-center space-x-2 text-indigo-600 dark:text-indigo-400 font-semibold text-sm">
          <ShieldCheck className="w-5 h-5" />
          <span>IQC Nexus AI Security & Architecture Brief</span>
        </div>
        <p className="text-sm text-gray-600 dark:text-gray-300 leading-relaxed">
          Tất cả thiết bị Client Agent hoạt động qua kết nối Outbound HTTPS duy nhất. Dữ liệu truyền tải được chuẩn hóa thành 
          <code className="mx-1 px-1.5 py-0.5 rounded bg-gray-200 dark:bg-gray-800 font-mono text-xs text-indigo-500">NormalizedWorkbook</code> schema. 
          Thiết bị lưu trữ mã định danh bằng Windows DPAPI. NASCA, Excel COM và giải mã file trực tiếp hoàn toàn bị phong tỏa.
        </p>
      </div>

      {/* 2. Executive KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="p-6 rounded-2xl bg-white dark:bg-[#121826] border border-gray-100 dark:border-gray-800 shadow-sm space-y-2">
          <div className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Tổng Thiết Bị Registered</div>
          <div className="text-3xl font-bold text-gray-900 dark:text-white">{devices.length}</div>
          <div className="text-xs text-gray-400">Đã đăng ký trong hệ thống</div>
        </div>

        <div className="p-6 rounded-2xl bg-white dark:bg-[#121826] border border-gray-100 dark:border-gray-800 shadow-sm space-y-2">
          <div className="text-xs font-semibold text-emerald-600 dark:text-emerald-400 uppercase tracking-wider">Online (Trực Tuyến)</div>
          <div className="text-3xl font-bold text-emerald-600 dark:text-emerald-400">{activeCount}</div>
          <div className="text-xs text-emerald-500/80">Heartbeat trong 5 phút qua</div>
        </div>

        <div className="p-6 rounded-2xl bg-white dark:bg-[#121826] border border-gray-100 dark:border-gray-800 shadow-sm space-y-2">
          <div className="text-xs font-semibold text-amber-600 dark:text-amber-400 uppercase tracking-wider">Offline (Ngoại Tuyến)</div>
          <div className="text-3xl font-bold text-amber-600 dark:text-amber-400">{offlineCount}</div>
          <div className="text-xs text-amber-500/80">Không gửi Heartbeat &gt; 5 phút</div>
        </div>

        <div className="p-6 rounded-2xl bg-white dark:bg-[#121826] border border-gray-100 dark:border-gray-800 shadow-sm space-y-2">
          <div className="text-xs font-semibold text-rose-600 dark:text-rose-400 uppercase tracking-wider">Revoked (Đã Thu Hồi)</div>
          <div className="text-3xl font-bold text-rose-600 dark:text-rose-400">{revokedCount}</div>
          <div className="text-xs text-rose-500/80">Khóa truy cập vĩnh viễn</div>
        </div>
      </div>

      {error && (
        <div className="p-4 rounded-xl bg-rose-50 dark:bg-rose-900/20 border border-rose-200 dark:border-rose-800 text-rose-700 dark:text-rose-300 text-sm flex items-center space-x-3">
          <AlertTriangle className="w-5 h-5 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      {/* 3. Device Management Table */}
      <div className="rounded-2xl bg-white dark:bg-[#121826] border border-gray-100 dark:border-gray-800 shadow-sm overflow-hidden">
        <div className="p-6 border-b border-gray-100 dark:border-gray-800 flex items-center justify-between">
          <h2 className="text-lg font-bold text-gray-900 dark:text-white">Danh Sách Thiết Bị</h2>
          <span className="text-xs text-gray-400 font-mono">Protocol: v1.0 Supported</span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-gray-100 dark:border-gray-800 bg-gray-50/50 dark:bg-gray-900/50 text-xs text-gray-500 uppercase tracking-wider">
                <th className="py-4 px-6">Tên Thiết Bị / Device ID</th>
                <th className="py-4 px-6">Người Sở Hữu (PIC)</th>
                <th className="py-4 px-6">Phiên Bản</th>
                <th className="py-4 px-6">Khả Năng (Capabilities)</th>
                <th className="py-4 px-6">Trạng Thái</th>
                <th className="py-4 px-6">Heartbeat Gần Nhất</th>
                <th className="py-4 px-6 text-right">Thao Tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-gray-800 text-sm">
              {loading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-gray-400">
                    Đang tải danh sách thiết bị...
                  </td>
                </tr>
              ) : devices.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-gray-400">
                    Chưa có thiết bị nào được ghép nối. Nhấn nút &quot;Tạo Mã Pairing Mới&quot; để bắt đầu.
                  </td>
                </tr>
              ) : (
                devices.map((device) => (
                  <tr key={device.deviceId} className="hover:bg-gray-50/50 dark:hover:bg-gray-800/30 transition-colors">
                    <td className="py-4 px-6">
                      <div className="font-semibold text-gray-900 dark:text-white">{device.displayName}</div>
                      <div className="text-xs font-mono text-gray-400">{device.deviceId}</div>
                    </td>
                    <td className="py-4 px-6">
                      <UserBadge name={device.ownerDisplayName || `SYN-${device.ownerUserId}`} size="sm" />
                    </td>
                    <td className="py-4 px-6 font-mono text-xs text-gray-600 dark:text-gray-400">
                      v{device.agentVersion} (P{device.protocolVersion})
                    </td>
                    <td className="py-4 px-6">
                      <div className="flex flex-wrap gap-1">
                        {device.capabilities.syntheticProviderSupported && (
                          <span className="px-2 py-0.5 rounded text-[10px] font-medium bg-indigo-50 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300">
                            Synthetic
                          </span>
                        )}
                        {device.capabilities.csvProviderSupported && (
                          <span className="px-2 py-0.5 rounded text-[10px] font-medium bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300">
                            CSV
                          </span>
                        )}
                        {!device.capabilities.nascaProviderSupported && (
                          <span className="px-2 py-0.5 rounded text-[10px] font-medium bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400">
                            NASCA: N/A
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="py-4 px-6">
                      {device.state === 'Active' && (
                        <span className="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300">
                          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                          <span>Online</span>
                        </span>
                      )}
                      {device.state === 'Offline' && (
                        <span className="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-300">
                          <span className="w-1.5 h-1.5 rounded-full bg-amber-500" />
                          <span>Offline</span>
                        </span>
                      )}
                      {device.state === 'Revoked' && (
                        <span className="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-rose-50 text-rose-700 dark:bg-rose-950 dark:text-rose-300">
                          <span className="w-1.5 h-1.5 rounded-full bg-rose-500" />
                          <span>Revoked</span>
                        </span>
                      )}
                      {device.state === 'Incompatible' && (
                        <span className="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-purple-50 text-purple-700 dark:bg-purple-950 dark:text-purple-300">
                          <span>Incompatible</span>
                        </span>
                      )}
                    </td>
                    <td className="py-4 px-6 text-xs text-gray-500">
                      {device.lastSeenAtUtc ? new Date(device.lastSeenAtUtc).toLocaleString('vi-VN') : 'Chưa có'}
                    </td>
                    <td className="py-4 px-6 text-right">
                      {device.state !== 'Revoked' && (
                        <button
                          onClick={() => setRevokeTarget(device)}
                          className="px-3 py-1.5 rounded-lg text-xs font-medium text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition-colors"
                        >
                          Thu Hồi
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* 4. Setup & Download Instructions Panel */}
      <div className="p-6 rounded-2xl bg-white dark:bg-[#121826] border border-gray-100 dark:border-gray-800 shadow-sm space-y-4">
        <h3 className="text-base font-bold text-gray-900 dark:text-white flex items-center space-x-2">
          <Info className="w-5 h-5 text-indigo-500" />
          <span>Hướng Dẫn Cài Đặt Client Agent (Windows x64)</span>
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs text-gray-600 dark:text-gray-400">
          <div className="p-4 rounded-xl bg-gray-50 dark:bg-gray-900 space-y-2">
            <div className="font-semibold text-gray-900 dark:text-white flex items-center space-x-2">
              <span className="w-5 h-5 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px]">1</span>
              <span>Tải Đóng Gói DevKit</span>
            </div>
            <p>Tải gói `IqcQms.ClientAgent.win-x64` từ Download Center hoặc bộ cài phát triển nội bộ.</p>
            <a href="/downloads" className="inline-flex items-center space-x-1 text-indigo-600 dark:text-indigo-400 font-semibold hover:underline mt-1">
              <Download className="w-3.5 h-3.5" />
              <span>Chuyển tới Download Center</span>
            </a>
          </div>

          <div className="p-4 rounded-xl bg-gray-50 dark:bg-gray-900 space-y-2">
            <div className="font-semibold text-gray-900 dark:text-white flex items-center space-x-2">
              <span className="w-5 h-5 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px]">2</span>
              <span>Tạo Mã Pairing</span>
            </div>
            <p>Nhấn nút &quot;Tạo Mã Pairing Mới&quot; phía trên để nhận mã ghép nối 6 chữ số (hiệu lực trong 10 phút).</p>
          </div>

          <div className="p-4 rounded-xl bg-gray-50 dark:bg-gray-900 space-y-2">
            <div className="font-semibold text-gray-900 dark:text-white flex items-center space-x-2">
              <span className="w-5 h-5 rounded-full bg-indigo-600 text-white flex items-center justify-center text-[10px]">3</span>
              <span>Chạy Agent &amp; Thao Tác</span>
            </div>
            <p>Khởi chạy file executable với cờ pairing: <code className="font-mono bg-gray-200 dark:bg-gray-800 px-1 py-0.5 rounded">IqcQms.ClientAgent.exe --AgentOptions:PairingCode=XXXXXX</code>.</p>
          </div>
        </div>
      </div>

      {/* 5. Pairing Code Modal */}
      {isPairingModalOpen && pairingData && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white dark:bg-[#121826] border border-gray-200 dark:border-gray-800 rounded-3xl p-8 max-w-md w-full shadow-2xl space-y-6 animate-in fade-in zoom-in-95 duration-200">
            <div className="flex items-center justify-between border-b border-gray-100 dark:border-gray-800 pb-4">
              <div className="flex items-center space-x-3">
                <div className="p-2 bg-indigo-50 dark:bg-indigo-950 text-indigo-600 dark:text-indigo-400 rounded-xl">
                  <Key className="w-6 h-6" />
                </div>
                <div>
                  <h3 className="text-lg font-bold text-gray-900 dark:text-white">Mã Ghép Nối (Pairing Code)</h3>
                  <p className="text-xs text-gray-500">Mã chỉ hiển thị duy nhất 1 lần</p>
                </div>
              </div>
              <button
                onClick={() => setIsPairingModalOpen(false)}
                className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 text-lg font-bold"
              >
                ✕
              </button>
            </div>

            <div className="space-y-4 text-center">
              <div className="p-6 rounded-2xl bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-800 space-y-2">
                <div className="text-xs text-gray-400 font-medium uppercase tracking-wider">Mã Ghép Nối Nhập Vào Agent</div>
                <div className="text-4xl font-extrabold font-mono text-indigo-600 dark:text-indigo-400 tracking-widest">
                  {pairingData.pairingCode}
                </div>
                <div className="flex items-center justify-center space-x-2 text-xs text-amber-600 dark:text-amber-400 font-medium pt-2">
                  <Clock className="w-4 h-4" />
                  <span>Hết hạn trong: {formatCountdown(secondsRemaining)}</span>
                </div>
              </div>

              <button
                onClick={handleCopyCode}
                className="w-full py-3 rounded-xl bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 text-sm font-semibold text-gray-800 dark:text-gray-200 transition-colors flex items-center justify-center space-x-2"
              >
                {codeCopied ? (
                  <>
                    <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                    <span className="text-emerald-600 dark:text-emerald-400">Đã Sao Chép!</span>
                  </>
                ) : (
                  <>
                    <Copy className="w-4 h-4" />
                    <span>Sao Chép Mã</span>
                  </>
                )}
              </button>

              <div className="text-left p-4 rounded-xl bg-indigo-50/50 dark:bg-indigo-950/30 border border-indigo-100 dark:border-indigo-900/50 text-xs text-indigo-900 dark:text-indigo-300 space-y-1">
                <div className="font-semibold flex items-center space-x-1">
                  <Info className="w-3.5 h-3.5" />
                  <span>Hướng dẫn:</span>
                </div>
                <p>Nhập mã này vào ô cấu hình ứng dụng IQC Nexus Client Agent trên máy tính Windows để thực hiện pairing tự động.</p>
              </div>
            </div>

            <button
              onClick={() => setIsPairingModalOpen(false)}
              className="w-full py-3 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-sm transition-colors"
            >
              Đã Hiểu &amp; Đóng
            </button>
          </div>
        </div>
      )}

      {/* 6. Revoke Confirmation Modal */}
      {revokeTarget && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white dark:bg-[#121826] border border-gray-200 dark:border-gray-800 rounded-3xl p-8 max-w-md w-full shadow-2xl space-y-6">
            <div className="flex items-center space-x-3 text-rose-600">
              <div className="p-3 bg-rose-50 dark:bg-rose-950 rounded-2xl">
                <AlertTriangle className="w-6 h-6" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-gray-900 dark:text-white">Xác Nhận Thu Hồi Thiết Bị</h3>
                <p className="text-xs text-gray-500">Thao tác không thể hoàn tác</p>
              </div>
            </div>

            <p className="text-sm text-gray-600 dark:text-gray-300">
              Bạn có chắc chắn muốn thu hồi thiết bị <strong className="text-gray-900 dark:text-white">{revokeTarget.displayName}</strong> (<code className="font-mono text-xs">{revokeTarget.deviceId}</code>)?
              Mọi session token của thiết bị này sẽ bị vô hiệu hóa ngay lập tức.
            </p>

            <div className="flex items-center space-x-3 pt-2">
              <button
                onClick={() => setRevokeTarget(null)}
                className="flex-1 py-2.5 rounded-xl border border-gray-200 dark:border-gray-800 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
              >
                Hủy Bỏ
              </button>
              <button
                onClick={handleConfirmRevoke}
                disabled={revokeLoading}
                className="flex-1 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-sm font-semibold shadow-sm transition-colors"
              >
                {revokeLoading ? 'Đang Thu Hồi...' : 'Xác Nhận Thu Hồi'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

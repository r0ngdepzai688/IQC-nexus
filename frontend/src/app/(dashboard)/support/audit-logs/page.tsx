"use client";

import { useState } from "react";
import { useAuth } from "@/lib/contexts/AuthContext";
import { 
  ShieldAlert, Search, Filter, ShieldCheck, Clock, FileText, ArrowRight, Download, Eye
} from "lucide-react";

// Mock Data
const MOCK_LOGS = [
  { id: "LOG-001", timestamp: "2024-07-05 09:12:45", admin: "System Admin (10545998)", target: "Mai Thị Oanh (11546567)", action: "Locked Account", oldVal: "Active", newVal: "Locked", ip: "192.168.1.105" },
  { id: "LOG-002", timestamp: "2024-07-04 15:30:22", admin: "System Admin (10545998)", target: "Lê Văn Huy (12532453)", action: "Changed Position", oldVal: "Cell Leader", newVal: "Staff", ip: "192.168.1.105" },
  { id: "LOG-003", timestamp: "2024-07-04 15:28:10", admin: "System Admin (10545998)", target: "Bùi Thị Thúy (10525728)", action: "Preview Dashboard", oldVal: "N/A", newVal: "Impersonated Staff", ip: "192.168.1.105" },
  { id: "LOG-004", timestamp: "2024-07-03 10:15:00", admin: "System Admin (10545998)", target: "Lê Thị Huệ (11556731)", action: "Created User", oldVal: "N/A", newVal: "New Account", ip: "10.0.5.21" },
];

export default function AuditLogsPage() {
  const { user } = useAuth();
  const [search, setSearch] = useState("");

  if (user.systemRole !== "Administrator") {
    return (
      <div className="min-h-[80vh] flex flex-col items-center justify-center p-10 bg-[#F4F6F8] dark:bg-[#000000] rounded-3xl">
        <div className="w-24 h-24 bg-rose-100 dark:bg-rose-900/20 rounded-full flex items-center justify-center mb-6">
          <ShieldAlert className="w-12 h-12 text-rose-600 dark:text-rose-500" />
        </div>
        <h1 className="text-3xl font-black text-gray-900 dark:text-white mb-3">Access Denied</h1>
        <p className="text-gray-500 max-w-md text-center font-medium">
          You do not have the required system permissions to view Audit Logs.
        </p>
      </div>
    );
  }

  const filteredLogs = MOCK_LOGS.filter(log => 
    log.target.toLowerCase().includes(search.toLowerCase()) || 
    log.action.toLowerCase().includes(search.toLowerCase()) ||
    log.admin.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="min-h-full bg-[#F4F6F8] dark:bg-[#000000] p-6 lg:p-10 pb-20 relative">
      <div className="flex flex-col md:flex-row justify-between items-start md:items-center mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-black text-gray-900 dark:text-white flex items-center">
            <FileText className="w-8 h-8 mr-3 text-[#1428A0] dark:text-blue-500" /> Security Audit Logs
          </h1>
          <p className="text-gray-500 font-medium mt-1">Immutable record of all administrative actions in the system.</p>
        </div>
        <button className="bg-white dark:bg-[#1A1A1A] border border-gray-200 dark:border-white/10 hover:bg-gray-50 dark:hover:bg-white/5 px-4 py-2.5 rounded-xl text-sm font-bold flex items-center transition-colors">
          <Download className="w-4 h-4 mr-2" /> Export to CSV
        </button>
      </div>

      <div className="bg-white dark:bg-[#121212] rounded-3xl border border-gray-200 dark:border-white/10 shadow-sm overflow-hidden flex flex-col min-h-[500px]">
        <div className="p-4 border-b border-gray-200 dark:border-white/10 flex flex-col md:flex-row justify-between gap-4">
          <div className="relative w-full md:w-96">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input 
              type="text" 
              placeholder="Search by Admin, Target, or Action..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-4 py-2.5 bg-gray-50 dark:bg-white/5 border border-gray-200 dark:border-white/10 rounded-xl text-sm font-medium focus:outline-none focus:ring-2 focus:ring-[#1428A0]"
            />
          </div>
          <button className="flex items-center px-4 py-2.5 bg-gray-50 dark:bg-white/5 border border-gray-200 dark:border-white/10 rounded-xl text-sm font-bold text-gray-700 dark:text-gray-300 hover:bg-gray-100 transition-colors">
            <Filter className="w-4 h-4 mr-2" /> Filter Range
          </button>
        </div>

        <div className="overflow-x-auto flex-1">
          <table className="w-full text-left border-collapse whitespace-nowrap">
            <thead>
              <tr className="bg-gray-50/50 dark:bg-white/5 border-b border-gray-200 dark:border-white/10 text-xs font-bold text-gray-500 uppercase tracking-wider">
                <th className="px-6 py-4">Timestamp</th>
                <th className="px-6 py-4">Administrator</th>
                <th className="px-6 py-4">Action Type</th>
                <th className="px-6 py-4">Target User</th>
                <th className="px-6 py-4">Changes</th>
                <th className="px-6 py-4">IP Address</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 dark:divide-white/5">
              {filteredLogs.map((log, i) => (
                <tr key={i} className="hover:bg-blue-50/50 dark:hover:bg-white/5 transition-colors">
                  <td className="px-6 py-4">
                    <div className="flex items-center text-sm font-bold text-gray-900 dark:text-white">
                      <Clock className="w-4 h-4 mr-2 text-gray-400" /> {log.timestamp}
                    </div>
                  </td>
                  <td className="px-6 py-4">
                    <span className="text-sm font-bold text-purple-600 dark:text-purple-400">{log.admin}</span>
                  </td>
                  <td className="px-6 py-4">
                    <span className="inline-flex items-center px-2.5 py-1 rounded-md text-[10px] font-bold bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-400 uppercase tracking-widest border border-blue-200 dark:border-blue-800">
                      {log.action}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-sm font-bold text-gray-900 dark:text-white">
                    {log.target}
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center text-xs font-medium space-x-2">
                      <span className="px-2 py-1 bg-gray-100 dark:bg-white/5 rounded text-gray-500 line-through">{log.oldVal}</span>
                      <ArrowRight className="w-3 h-3 text-gray-400" />
                      <span className="px-2 py-1 bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 rounded font-bold">{log.newVal}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 text-xs font-mono text-gray-500">
                    {log.ip}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

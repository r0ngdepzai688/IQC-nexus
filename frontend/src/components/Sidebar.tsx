"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Users,
  Wrench,
  PackageSearch,
  FileBadge,
  Settings,
  LogOut,
  ShieldCheck
} from "lucide-react";

const navigation = [
  { name: "Tổng quan", href: "/", icon: LayoutDashboard },
  { name: "Nhân sự IQC", href: "/hr", icon: Users },
  { name: "Thiết bị & Hiệu chuẩn", href: "/equipment", icon: Wrench },
  { name: "New Model (NPI)", href: "/new-model", icon: PackageSearch },
  { name: "Tiêu chuẩn & Form", href: "/standards", icon: FileBadge },
  { name: "Cài đặt hệ thống", href: "/settings", icon: Settings },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <div className="flex flex-col w-64 h-screen bg-white dark:bg-[#09090b] border-r border-gray-100/80 dark:border-gray-800/80 transition-colors duration-300">
      {/* Logo Area */}
      <div className="flex items-center px-6 h-16 mt-2 mb-4">
        <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-[#1428A0] text-white shadow-md shadow-[#1428A0]/20 mr-3">
          <ShieldCheck className="w-5 h-5" />
        </div>
        <h1 className="text-xl font-bold tracking-tight text-gray-900 dark:text-gray-100">IQC Portal</h1>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        <div className="px-3 mb-2 text-xs font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wider">
          Menu Chính
        </div>
        {navigation.map((item) => {
          const isActive = pathname === item.href;
          return (
            <Link
              key={item.name}
              href={item.href}
              className={`flex items-center justify-between px-3 py-2.5 text-sm font-medium rounded-lg transition-all duration-200 group ${
                isActive
                  ? "bg-blue-50/50 dark:bg-[#1428A0]/10 text-[#1428A0] dark:text-blue-400"
                  : "text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-900 hover:text-gray-900 dark:hover:text-gray-200"
              }`}
            >
              <div className="flex items-center">
                <item.icon
                  className={`w-5 h-5 mr-3 transition-colors ${
                    isActive ? "text-[#1428A0] dark:text-blue-400" : "text-gray-400 dark:text-gray-500 group-hover:text-gray-600 dark:group-hover:text-gray-300"
                  }`}
                  strokeWidth={isActive ? 2.5 : 2}
                />
                {item.name}
              </div>
              {isActive && <div className="w-1.5 h-1.5 rounded-full bg-[#1428A0] dark:bg-blue-400" />}
            </Link>
          );
        })}
      </nav>

      {/* User Area / Logout */}
      <div className="p-4 border-t border-gray-100/80 dark:border-gray-800/80 m-3 bg-gray-50/50 dark:bg-gray-900/50 rounded-xl">
        <button className="flex items-center justify-between w-full px-2 py-2 text-sm font-medium text-gray-600 dark:text-gray-400 rounded-lg hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-500/10 transition-all duration-200">
          <div className="flex items-center">
            <LogOut className="w-4 h-4 mr-3" />
            Đăng xuất
          </div>
        </button>
      </div>
    </div>
  );
}

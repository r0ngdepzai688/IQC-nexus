"use client";

import { useState } from "react";
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
  ShieldCheck,
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
  const [isHovered, setIsHovered] = useState(false);

  return (
    <div className="w-20 flex-shrink-0 h-full hidden sm:block relative z-40">
      
      {/* Avant-Garde Glass Sidebar */}
      <div 
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        className={`absolute top-0 left-0 h-full bg-white/70 dark:bg-[#111111]/70 backdrop-blur-2xl border border-white/80 dark:border-white/10 rounded-[2rem] shadow-[0_8px_32px_rgba(0,0,0,0.04)] dark:shadow-none transition-all duration-500 ease-[cubic-bezier(0.4,0,0.2,1)] flex flex-col overflow-hidden ${
          isHovered ? "w-[260px]" : "w-20"
        }`}
      >
        {/* Logo Area */}
        <div className="flex items-center h-20 mt-2 mb-4 px-5">
          <div className="flex items-center justify-center rounded-[1rem] bg-gradient-to-tr from-[#1428A0] to-blue-500 text-white shadow-lg shadow-blue-500/20 flex-shrink-0 w-10 h-10 transition-transform duration-300 hover:scale-110 hover:rotate-3">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <div 
            className={`flex flex-col justify-center ml-4 transition-opacity duration-300 ${
              isHovered ? "opacity-100" : "opacity-0"
            }`}
          >
            <h1 className="text-2xl font-black tracking-tighter text-transparent bg-clip-text bg-gradient-to-br from-gray-900 to-gray-600 dark:from-white dark:to-gray-400 whitespace-nowrap leading-none">
              IQC QMS
            </h1>
            <span className="text-[9px] font-bold tracking-widest text-[#1428A0] dark:text-blue-400 uppercase mt-1 whitespace-nowrap">
              Quality Management System
            </span>
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto space-y-2 py-2 px-3">
          <div 
            className={`mb-4 px-3 text-[10px] font-bold text-gray-400 dark:text-gray-500 uppercase tracking-[0.2em] whitespace-nowrap transition-opacity duration-300 ${
              isHovered ? "opacity-100" : "opacity-0"
            }`}
          >
            Menu Chính
          </div>
          
          {navigation.map((item) => {
            const isActive = pathname === item.href;
            return (
              <Link
                key={item.name}
                href={item.href}
                className={`relative flex items-center h-12 px-2.5 text-sm font-semibold rounded-2xl transition-all duration-300 group ${
                  isActive
                    ? "bg-white/80 dark:bg-white/10 text-[#1428A0] dark:text-white shadow-sm ring-1 ring-black/5 dark:ring-white/10"
                    : "text-gray-500 dark:text-gray-400 hover:bg-white/50 dark:hover:bg-white/5 hover:text-gray-900 dark:hover:text-gray-200"
                }`}
                title={!isHovered ? item.name : undefined}
              >
                <div className="flex items-center">
                  <item.icon
                    className={`w-5 h-5 flex-shrink-0 ml-1 transition-transform duration-300 group-hover:scale-110 ${
                      isActive ? "text-[#1428A0] dark:text-white" : "text-gray-400 dark:text-gray-500 group-hover:text-gray-700 dark:group-hover:text-gray-300"
                    }`}
                    strokeWidth={isActive ? 2.5 : 2}
                  />
                  
                  <span 
                    className={`whitespace-nowrap ml-4 transition-all duration-400 ${
                      isHovered ? "opacity-100 translate-x-0" : "opacity-0 -translate-x-4"
                    }`}
                  >
                    {item.name}
                  </span>
                </div>

                {isActive && isHovered && (
                  <div className="absolute right-3 w-1.5 h-1.5 rounded-full bg-[#1428A0] dark:bg-white animate-pulse" />
                )}
              </Link>
            );
          })}
        </nav>

        {/* User Area / Logout */}
        <div className="p-3 mt-auto">
          <div 
            className={`text-[10px] font-medium text-gray-400/60 dark:text-gray-500 mb-4 text-center transition-opacity duration-300 ${
              isHovered ? "opacity-100" : "opacity-0 hidden"
            }`}
          >
            © Bản quyền thuộc về hai.duong
          </div>
          <button 
            className={`flex items-center h-12 px-2.5 w-full text-sm font-semibold text-gray-500 dark:text-gray-400 rounded-2xl hover:text-rose-600 dark:hover:text-rose-400 hover:bg-rose-50/50 dark:hover:bg-rose-500/10 transition-all duration-300 group`}
            title={!isHovered ? "Đăng xuất" : undefined}
          >
            <div className="flex items-center">
              <LogOut className="w-5 h-5 flex-shrink-0 ml-1 group-hover:scale-110 transition-transform duration-300" />
              <span 
                className={`whitespace-nowrap ml-4 transition-all duration-400 ${
                  isHovered ? "opacity-100 translate-x-0" : "opacity-0 -translate-x-4"
                }`}
              >
                Đăng xuất
              </span>
            </div>
          </button>
        </div>
      </div>
    </div>
  );
}

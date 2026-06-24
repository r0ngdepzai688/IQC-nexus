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
    // Fixed placeholder to maintain layout space
    <div className="w-20 flex-shrink-0 h-screen hidden sm:block relative z-40">
      
      {/* The actual Sidebar that expands on hover */}
      <div 
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        className={`absolute top-0 left-0 h-screen bg-white dark:bg-[#09090b] border-r border-gray-100/80 dark:border-gray-800/80 transition-all duration-300 ease-[cubic-bezier(0.4,0,0.2,1)] shadow-[4px_0_24px_rgba(0,0,0,0.02)] dark:shadow-none flex flex-col overflow-hidden ${
          isHovered ? "w-64" : "w-20"
        }`}
      >
        {/* Logo Area */}
        <div className="flex items-center h-16 mt-2 mb-4 px-5">
          <div className="flex items-center justify-center rounded-lg bg-[#1428A0] text-white shadow-md shadow-[#1428A0]/20 flex-shrink-0 w-10 h-10 transition-transform hover:scale-105">
            <ShieldCheck className="w-5 h-5" />
          </div>
          <h1 
            className={`text-xl font-bold tracking-tight text-gray-900 dark:text-gray-100 whitespace-nowrap ml-4 transition-opacity duration-300 ${
              isHovered ? "opacity-100" : "opacity-0"
            }`}
          >
            IQC Portal
          </h1>
        </div>

        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto space-y-1.5 py-4 px-3">
          <div 
            className={`mb-3 px-2 text-[10px] font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest whitespace-nowrap transition-opacity duration-300 ${
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
                className={`relative flex items-center h-12 px-2.5 text-sm font-medium rounded-xl transition-colors duration-200 group ${
                  isActive
                    ? "bg-blue-50/80 dark:bg-[#1428A0]/15 text-[#1428A0] dark:text-blue-400"
                    : "text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800/50 hover:text-gray-900 dark:hover:text-gray-200"
                }`}
                title={!isHovered ? item.name : undefined} // Native tooltip when collapsed
              >
                <div className="flex items-center">
                  <item.icon
                    className={`w-5 h-5 flex-shrink-0 ml-1 ${
                      isActive ? "text-[#1428A0] dark:text-blue-400" : "text-gray-400 dark:text-gray-500 group-hover:text-gray-600 dark:group-hover:text-gray-300"
                    }`}
                    strokeWidth={isActive ? 2.5 : 2}
                  />
                  
                  <span 
                    className={`whitespace-nowrap ml-4 transition-all duration-300 ${
                      isHovered ? "opacity-100 translate-x-0" : "opacity-0 -translate-x-2"
                    }`}
                  >
                    {item.name}
                  </span>
                </div>

                {isActive && isHovered && (
                  <div className="absolute right-3 w-1.5 h-1.5 rounded-full bg-[#1428A0] dark:bg-blue-400 animate-in zoom-in duration-300" />
                )}
              </Link>
            );
          })}
        </nav>

        {/* User Area / Logout */}
        <div className="p-3 border-t border-gray-100/80 dark:border-gray-800/80 mt-auto">
          <div 
            className={`text-[10px] text-gray-400 dark:text-gray-500 mb-3 text-center transition-opacity duration-300 ${
              isHovered ? "opacity-100" : "opacity-0 hidden"
            }`}
          >
            © Bản quyền thuộc về hai.duong
          </div>
          <button 
            className={`flex items-center h-12 px-2.5 w-full text-sm font-medium text-gray-600 dark:text-gray-400 rounded-xl hover:text-red-600 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors duration-200 group`}
            title={!isHovered ? "Đăng xuất" : undefined}
          >
            <div className="flex items-center">
              <LogOut className="w-5 h-5 flex-shrink-0 ml-1 group-hover:scale-110 transition-transform duration-300" />
              <span 
                className={`whitespace-nowrap ml-4 transition-all duration-300 ${
                  isHovered ? "opacity-100 translate-x-0" : "opacity-0 -translate-x-2"
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

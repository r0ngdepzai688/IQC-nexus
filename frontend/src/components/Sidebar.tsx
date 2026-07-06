"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  FileText,
  ListPlus,
  History,
  Settings2,
  ChevronRight,
  LucideIcon
} from "lucide-react";

// Mock sub-menus for demonstration
const subMenus: Record<string, { name: string; href: string; icon: LucideIcon }[]> = {
  "/standards": [
    { name: "Standard Management", href: "/standards", icon: FileText },
    { name: "Dynamic Form Design", href: "/standards/forms", icon: ListPlus },
    { name: "Version History", href: "/standards/history", icon: History },
    { name: "Standard Settings", href: "/standards/settings", icon: Settings2 },
  ]
};

export function Sidebar() {
  const pathname = usePathname();
  const [isHovered, setIsHovered] = useState(false);

  // Determine active main module
  const mainModuleKey = Object.keys(subMenus).find(key => pathname.startsWith(key));
  const activeSubMenus = mainModuleKey ? subMenus[mainModuleKey] : null;

  if (!activeSubMenus) {
    return null; // Don't render sidebar if no sub-menus for this module
  }

  return (
    <div className="w-16 flex-shrink-0 h-full hidden sm:block relative z-40">
      
      {/* Avant-Garde Glass Sidebar for Sub-navigation */}
      <div 
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
        className={`absolute top-0 left-0 h-full bg-white/70 dark:bg-[#111111]/70 backdrop-blur-2xl border border-white/80 dark:border-white/10 rounded-[2rem] shadow-[0_8px_32px_rgba(0,0,0,0.04)] dark:shadow-none transition-all duration-500 ease-[cubic-bezier(0.4,0,0.2,1)] flex flex-col overflow-hidden ${
          isHovered ? "w-[240px]" : "w-16"
        }`}
      >
        {/* Navigation */}
        <nav className="flex-1 overflow-y-auto space-y-2 py-6 px-2">
          <div 
            className={`mb-4 px-3 text-[10px] font-bold text-gray-400 dark:text-gray-500 uppercase tracking-[0.2em] whitespace-nowrap transition-opacity duration-300 ${
              isHovered ? "opacity-100" : "opacity-0"
            }`}
          >
            Menu Phụ
          </div>
          
          {activeSubMenus.map((item) => {
            const isActive = pathname === item.href;
            return (
              <Link
                key={item.name}
                href={item.href}
                className={`relative flex items-center h-12 px-2 text-sm font-semibold rounded-2xl transition-all duration-300 group ${
                  isActive
                    ? "bg-white/80 dark:bg-white/10 text-[#1428A0] dark:text-white shadow-sm ring-1 ring-black/5 dark:ring-white/10"
                    : "text-gray-500 dark:text-gray-400 hover:bg-white/50 dark:hover:bg-white/5 hover:text-gray-900 dark:hover:text-gray-200"
                }`}
                title={!isHovered ? item.name : undefined}
              >
                <div className="flex items-center w-full">
                  <item.icon
                    className={`w-5 h-5 flex-shrink-0 ml-1.5 transition-transform duration-300 group-hover:scale-110 ${
                      isActive ? "text-[#1428A0] dark:text-white" : "text-gray-400 dark:text-gray-500 group-hover:text-gray-700 dark:group-hover:text-gray-300"
                    }`}
                    strokeWidth={isActive ? 2.5 : 2}
                  />
                  
                  <span 
                    className={`whitespace-nowrap ml-4 transition-all duration-400 flex-1 ${
                      isHovered ? "opacity-100 translate-x-0" : "opacity-0 -translate-x-4"
                    }`}
                  >
                    {item.name}
                  </span>

                  {isHovered && !isActive && (
                    <ChevronRight className="w-4 h-4 opacity-0 -translate-x-2 transition-all duration-300 group-hover:opacity-100 group-hover:translate-x-0 text-gray-400" />
                  )}
                </div>

                {isActive && isHovered && (
                  <div className="absolute right-3 w-1.5 h-1.5 rounded-full bg-[#1428A0] dark:bg-white animate-pulse" />
                )}
              </Link>
            );
          })}
        </nav>

        {/* Creator Signature Footer */}
        <div 
          className={`px-5 pb-2 mt-auto pt-8 transition-all duration-400 overflow-hidden ${
            isHovered ? "opacity-100 max-h-40" : "opacity-0 max-h-0"
          }`}
        >
          <div className="text-[11px] text-gray-400/60 dark:text-gray-500/50 font-medium select-none border-t border-gray-200/50 dark:border-white/5 pt-4">
            <span className="italic">&quot;Your concept. Engineered for<br/>exceptional experiences.&quot;</span><br/>
            <span className="mt-1 block opacity-70">
              &copy; 2026 IQC Nexus.<br/>Designed &amp; developed by hai.duong.
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

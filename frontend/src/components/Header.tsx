"use client";

import { Search, Bell, Command, Sun, Moon } from "lucide-react";
import { useTheme } from "next-themes";
import { useEffect, useState } from "react";

export function Header() {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    
    // Keyboard shortcut for dark mode (Ctrl+J or Cmd+J)
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'j') {
        e.preventDefault();
        setTheme(theme === 'dark' ? 'light' : 'dark');
      }
    };
    
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [theme, setTheme]);

  return (
    <header className="flex items-center justify-between h-16 px-8 bg-white/60 dark:bg-[#09090b]/60 backdrop-blur-xl border-b border-gray-100/80 dark:border-gray-800/80 sticky top-0 z-30 transition-colors duration-300">
      
      {/* Left side: Search / Command Palette Trigger */}
      <div className="flex items-center flex-1">
        <button className="flex items-center w-full max-w-md px-4 py-2 bg-gray-50 dark:bg-gray-900 hover:bg-gray-100 dark:hover:bg-gray-800 border border-gray-200/60 dark:border-gray-700/60 hover:border-gray-300 dark:hover:border-gray-600 rounded-xl transition-all text-sm text-gray-500 dark:text-gray-400 group">
          <Search className="w-4 h-4 mr-2 text-gray-400 group-hover:text-gray-500 transition-colors" />
          <span className="flex-1 text-left">Tìm kiếm tiêu chuẩn, mã linh kiện, NPI...</span>
          <div className="flex items-center space-x-1 opacity-70">
            <kbd className="hidden sm:inline-flex items-center h-5 px-1.5 text-[10px] font-medium bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded text-gray-500 dark:text-gray-400">
              <Command className="w-3 h-3 mr-0.5" /> K
            </kbd>
          </div>
        </button>
      </div>

      {/* Right side: Actions & Profile */}
      <div className="flex items-center space-x-5">
        {mounted && (
          <button 
            onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
            className="flex items-center p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors rounded-full hover:bg-gray-50 dark:hover:bg-gray-800 group"
            title="Chuyển đổi giao diện Sáng/Tối (Ctrl+J)"
          >
            {theme === 'dark' ? <Sun className="w-5 h-5" /> : <Moon className="w-5 h-5" />}
            <kbd className="hidden sm:inline-flex items-center ml-2 h-5 px-1.5 text-[10px] font-medium bg-transparent border border-gray-200 dark:border-gray-700 rounded text-gray-400 dark:text-gray-500 opacity-0 group-hover:opacity-100 transition-opacity">
              Ctrl+J
            </kbd>
          </button>
        )}

        <button className="relative p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition-colors rounded-full hover:bg-gray-50 dark:hover:bg-gray-800">
          <Bell className="w-5 h-5" />
          <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-[#1428A0] dark:bg-blue-500 rounded-full ring-2 ring-white dark:ring-[#09090b] animate-pulse"></span>
        </button>
        
        <div className="h-6 w-px bg-gray-200 dark:bg-gray-800"></div>

        <button className="flex items-center space-x-3 hover:opacity-80 transition-opacity">
          <div className="flex flex-col items-end">
            <span className="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-none">Nguyễn Hải Đường</span>
            <span className="text-[11px] font-medium text-[#1428A0] dark:text-blue-400 mt-1 bg-blue-50 dark:bg-[#1428A0]/20 px-2 py-0.5 rounded-full">Trưởng phòng IQC</span>
          </div>
          <div className="w-9 h-9 rounded-full bg-gradient-to-tr from-[#1428A0] to-blue-400 dark:from-blue-600 dark:to-blue-400 flex items-center justify-center text-white font-bold text-sm shadow-sm ring-2 ring-white dark:ring-[#09090b]">
            NHD
          </div>
        </button>
      </div>
    </header>
  );
}

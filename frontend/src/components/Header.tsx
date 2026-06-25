"use client";

import { Search, Bell, Command, Sun, Moon } from "lucide-react";
import { useTheme } from "next-themes";
import { useEffect, useState } from "react";

export function Header() {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    
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
    <header className="flex items-center justify-between h-16 px-4 sm:px-6 bg-white/70 dark:bg-[#111111]/70 backdrop-blur-2xl border border-white/80 dark:border-white/10 rounded-full shadow-[0_8px_32px_rgba(0,0,0,0.03)] dark:shadow-none z-30 transition-colors duration-500">
      
      {/* Left side: Search / Command Palette Trigger */}
      <div className="flex items-center flex-1">
        <button className="flex items-center w-full max-w-md px-4 py-2 bg-black/5 dark:bg-white/5 hover:bg-black/10 dark:hover:bg-white/10 border border-transparent hover:border-black/5 dark:hover:border-white/10 rounded-full transition-all duration-300 text-sm text-gray-500 dark:text-gray-400 group focus:outline-none focus:ring-2 focus:ring-[#1428A0]/30">
          <Search className="w-4 h-4 mr-3 text-gray-400 group-hover:text-[#1428A0] dark:group-hover:text-blue-400 transition-colors" />
          <span className="flex-1 text-left font-medium">Tìm kiếm thông minh...</span>
          <div className="flex items-center space-x-1 opacity-70 group-hover:opacity-100 transition-opacity">
            <kbd className="hidden sm:inline-flex items-center h-5 px-2 text-[10px] font-bold bg-white dark:bg-black border border-gray-200 dark:border-gray-800 rounded-full text-gray-500 dark:text-gray-300 shadow-sm">
              <Command className="w-3 h-3 mr-0.5" /> K
            </kbd>
          </div>
        </button>
      </div>

      {/* Right side: Actions & Profile */}
      <div className="flex items-center space-x-4">
        {mounted && (
          <button 
            onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
            className="flex items-center p-2.5 text-gray-400 hover:text-gray-900 dark:hover:text-white transition-all duration-300 rounded-full hover:bg-black/5 dark:hover:bg-white/10 group"
            title="Chuyển đổi Sáng/Tối (Ctrl+J)"
          >
            {theme === 'dark' ? <Sun className="w-4 h-4 group-hover:rotate-45 transition-transform" /> : <Moon className="w-4 h-4 group-hover:-rotate-12 transition-transform" />}
          </button>
        )}

        <button className="relative p-2.5 text-gray-400 hover:text-gray-900 dark:hover:text-white transition-all duration-300 rounded-full hover:bg-black/5 dark:hover:bg-white/10 group">
          <Bell className="w-4 h-4 group-hover:animate-bounce" />
          <span className="absolute top-2 right-2 w-2 h-2 bg-rose-500 rounded-full ring-2 ring-white dark:ring-[#111111] animate-pulse"></span>
        </button>
        
        <div className="h-8 w-px bg-gray-200 dark:bg-white/10 mx-1"></div>

        <button className="flex items-center space-x-3 hover:opacity-80 transition-opacity pl-1">
          <div className="hidden md:flex flex-col items-end">
            <span className="text-sm font-bold text-gray-900 dark:text-white leading-none">Nguyễn Hải Đường</span>
            <span className="text-[10px] font-bold tracking-wider uppercase text-[#1428A0] dark:text-blue-400 mt-1">Trưởng phòng IQC</span>
          </div>
          <div className="w-10 h-10 rounded-full bg-gradient-to-tr from-[#1428A0] to-blue-500 flex items-center justify-center text-white font-black text-sm shadow-lg shadow-blue-500/30 ring-2 ring-white dark:ring-[#111111] transition-transform hover:scale-105">
            NHD
          </div>
        </button>
      </div>
    </header>
  );
}

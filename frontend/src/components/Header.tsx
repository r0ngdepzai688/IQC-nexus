"use client";

import { 
  Search, Bell, Command, Sun, Moon, ShieldCheck, LayoutDashboard, Users, 
  Wrench, PackageSearch, FileBadge, Settings, ChevronDown, HelpCircle, 
  MessageSquare, ShieldAlert, LogOut, UserCog, Box, Sparkles, BookOpen, CheckCircle, Award
} from "lucide-react";
import { useTheme } from "next-themes";
import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth, Role, DashboardProfile, SystemRole } from "@/lib/contexts/AuthContext";
import { motion, AnimatePresence } from "framer-motion";

export function Header() {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);
  const pathname = usePathname();
  const router = useRouter();
  
  const { user, loginAs } = useAuth();
  const [showRoleSwitcher, setShowRoleSwitcher] = useState(false);

  // Dynamic Navigation builder
  const getNavigation = () => {
    const supportSubItems = [
      { name: "FAQ", href: "/support/faq", icon: HelpCircle },
      { name: "Q&A", href: "/support/qa", icon: MessageSquare },
      { name: "Chat with Admin", href: "#chat-with-admin", icon: ShieldAlert },
    ];

    if (user.systemRole === 'Administrator') {
      supportSubItems.push({ name: "User Management", href: "/support/users", icon: UserCog });
      supportSubItems.push({ name: "Audit Logs", href: "/support/audit-logs", icon: FileBadge });
    }

    return [
      { 
        name: "Workforce", 
        href: "/hr", 
        icon: Users,
        subItems: [
          { name: "Organization", href: "/hr", icon: LayoutDashboard },
          { name: "Training", href: "/hr/training", icon: BookOpen },
          { name: "Certificate", href: "/hr/certificate", icon: Award },
          { name: "Test", href: "/hr/test", icon: CheckCircle }
        ]
      },
      { name: "Inspections", href: "/equipment", icon: Wrench },
      { name: "New Models", href: "/new-model", icon: PackageSearch },
      { name: "Compliance", href: "/standards", icon: FileBadge },
      { 
        name: "Support", 
        href: "/settings", 
        icon: Settings,
        subItems: supportSubItems
      },
    ];
  };

  const mainNavigation = getNavigation();

  useEffect(() => {
    setMounted(true);
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        // Handle search focus here
        document.getElementById('global-search-input')?.focus();
      }
      if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'j') {
        e.preventDefault();
        setTheme(theme === 'dark' ? 'light' : 'dark');
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [theme, setTheme]);

  return (
    <>
      <header className="w-full flex items-center justify-between h-14 sm:h-16 px-4 sm:px-8 bg-white/80 dark:bg-[#050505]/80 backdrop-blur-xl border-b border-gray-200 dark:border-white/10 z-50 sticky top-0 transition-colors duration-500">
        
        {/* LEFT ZONE: Brand */}
        <div className="flex items-center flex-1 min-w-0">
          <Link href="/overview" className="flex items-center group relative overflow-hidden flex-shrink-0 mr-8">
            <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-[#1428A0] to-blue-500 flex items-center justify-center shadow-md shadow-blue-500/20 ring-1 ring-white/10 transition-transform duration-300 group-hover:scale-105">
              <Box className="w-4 h-4 text-white" strokeWidth={2.5} />
            </div>
            <div className="flex flex-col ml-3 hidden sm:flex justify-center h-full">
              <h1 className="text-lg font-black tracking-tight text-gray-900 dark:text-white leading-none">
                IQC Nexus
              </h1>
              <span className="text-[10px] font-bold text-gray-500 dark:text-gray-400 mt-0.5 leading-none">
                Enterprise Quality Platform
              </span>
            </div>
          </Link>
        </div>

        {/* CENTER ZONE: Navigation */}
        <div className="hidden lg:flex items-center justify-center space-x-1 flex-1">
          <nav className="flex items-center relative space-x-1">
            {mainNavigation.map((item) => {
              const isActive = pathname === item.href || (pathname.startsWith(item.href) && item.href !== '/');
              const isSupport = item.name === "Support";
              
              return (
                <div key={item.name} className="relative group">
                  <Link
                    href={item.href}
                    className={`relative flex items-center h-10 px-3.5 text-sm font-semibold transition-colors duration-300 rounded-lg hover:bg-gray-100 dark:hover:bg-white/5 ${
                      isActive
                        ? "text-gray-900 dark:text-white"
                        : isSupport 
                          ? "text-gray-400 dark:text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
                          : "text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white"
                    }`}
                  >
                    {item.name}
                    {item.subItems && <ChevronDown className="w-3.5 h-3.5 ml-1.5 opacity-50 group-hover:opacity-100 group-hover:rotate-180 transition-all duration-300" />}
                    
                    {/* Animated Underline for Active State */}
                    {isActive && (
                      <motion.div 
                        layoutId="navbar-underline"
                        className="absolute bottom-0 left-3 right-3 h-[2px] bg-[#1428A0] dark:bg-blue-400 rounded-t-full"
                        initial={false}
                        transition={{ type: "spring", stiffness: 300, damping: 30 }}
                      />
                    )}
                  </Link>

                  {/* Dropdown Menu */}
                  {item.subItems && (
                    <div className="absolute top-full left-1/2 -translate-x-1/2 pt-2 opacity-0 translate-y-1 pointer-events-none group-hover:opacity-100 group-hover:translate-y-0 group-hover:pointer-events-auto transition-all duration-300 z-[100]">
                      <div className="w-56 bg-white dark:bg-[#151515] border border-gray-100 dark:border-white/10 rounded-xl shadow-xl overflow-hidden p-1 flex flex-col backdrop-blur-xl">
                        {item.subItems.map((subItem) => (
                          <Link
                            key={subItem.name}
                            href={subItem.href}
                            className={`flex items-center px-3 py-2.5 text-sm font-medium rounded-lg transition-colors ${subItem.name === 'User Management' ? 'text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-500/10' : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white hover:bg-gray-50 dark:hover:bg-white/5'}`}
                            onClick={(e) => {
                              if(subItem.href === '#chat-with-admin') {
                                e.preventDefault();
                                window.dispatchEvent(new Event('open-admin-chat'));
                              }
                            }}
                          >
                            <subItem.icon className="w-4 h-4 mr-3 opacity-70" />
                            {subItem.name}
                          </Link>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              );
            })}
          </nav>
        </div>

        {/* RIGHT ZONE: Search, Tools, Profile */}
        <div className="flex items-center justify-end space-x-1 sm:space-x-2 flex-1">
          
          {/* Universal Search */}
          <div className="hidden xl:flex relative group mr-2">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 group-focus-within:text-[#1428A0] dark:group-focus-within:text-blue-400 transition-colors" />
            <input 
              id="global-search-input"
              type="text" 
              placeholder="Search projects, BOMs, users, suppliers..." 
              className="w-72 h-9 pl-9 pr-12 bg-gray-100/50 dark:bg-white/5 border border-transparent hover:border-gray-200 dark:hover:border-white/10 focus:border-[#1428A0]/30 dark:focus:border-blue-500/30 focus:bg-white dark:focus:bg-[#1A1A1A] rounded-lg text-sm font-medium text-gray-900 dark:text-white outline-none transition-all duration-300 placeholder-gray-400 dark:placeholder-gray-500 focus:shadow-[0_0_0_3px_rgba(20,40,160,0.1)] dark:focus:shadow-[0_0_0_3px_rgba(59,130,246,0.1)]"
            />
            <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center">
              <kbd className="inline-flex items-center h-5 px-1.5 text-[10px] font-bold bg-white dark:bg-[#2A2A2A] border border-gray-200 dark:border-white/10 rounded-md text-gray-400 shadow-sm">
                <Command className="w-3 h-3 mr-0.5" /> K
              </kbd>
            </div>
          </div>

          {/* AI Assistant */}
          <button className="hidden sm:flex relative p-2 text-blue-500 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors duration-300 rounded-lg group">
            <Sparkles className="w-[18px] h-[18px] group-hover:scale-110 transition-transform" />
          </button>

          {/* Chat */}
          <button className="relative p-2 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/5 transition-colors duration-300 rounded-lg group">
            <MessageSquare className="w-[18px] h-[18px]" />
          </button>

          {/* Notifications */}
          <button className="relative p-2 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/5 transition-colors duration-300 rounded-lg group">
            <Bell className="w-[18px] h-[18px] group-hover:animate-[wiggle_0.5s_ease-in-out_infinite]" />
            <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-rose-500 rounded-full ring-2 ring-white dark:ring-[#050505]"></span>
          </button>
          
          {/* Theme Toggle */}
          {mounted && (
            <button 
              onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
              className="p-2 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/5 transition-colors duration-300 rounded-lg group"
              title="Toggle Theme"
            >
              {theme === 'dark' ? <Sun className="w-[18px] h-[18px] group-hover:rotate-45 transition-transform" /> : <Moon className="w-[18px] h-[18px] group-hover:-rotate-12 transition-transform" />}
            </button>
          )}

          <div className="h-5 w-px bg-gray-200 dark:bg-white/10 mx-2 hidden sm:block"></div>

          {/* User Profile */}
          <div className="relative group ml-1">
            <button className="flex items-center space-x-2 p-1 pl-2 pr-3 bg-transparent hover:bg-gray-100 dark:hover:bg-white/5 rounded-full border border-transparent hover:border-gray-200 dark:hover:border-white/10 transition-all active:scale-95">
              <div className="w-7 h-7 rounded-full bg-gradient-to-tr from-[#1428A0] to-blue-500 flex items-center justify-center text-white font-black text-xs shadow-inner shadow-black/20">
                {user.name.charAt(0)}
              </div>
              <ChevronDown className="w-3.5 h-3.5 text-gray-400" />
            </button>
            
            {/* User Dropdown with Dev Mode Inside */}
            <div className="absolute right-0 top-full pt-2 opacity-0 translate-y-2 pointer-events-none group-hover:opacity-100 group-hover:translate-y-0 group-hover:pointer-events-auto transition-all duration-200 z-[100]">
              <div className="w-64 bg-white dark:bg-[#151515] border border-gray-100 dark:border-white/10 rounded-xl shadow-2xl overflow-hidden flex flex-col backdrop-blur-xl ring-1 ring-black/5 dark:ring-white/5">
                <div className="px-4 py-4 border-b border-gray-100 dark:border-white/10 bg-gray-50/50 dark:bg-white/[0.02]">
                  <p className="text-sm font-bold text-gray-900 dark:text-white truncate leading-tight">{user.name}</p>
                  <p className="text-xs font-medium text-gray-500 truncate mt-0.5">@{user.employeeId}</p>
                  {user.systemRole === 'Administrator' && (
                    <span className="inline-flex items-center mt-2 px-2 py-0.5 bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 text-[10px] font-bold rounded-sm uppercase tracking-wider">
                      <ShieldCheck className="w-3 h-3 mr-1" /> Admin
                    </span>
                  )}
                </div>

                {/* Dev Mode Options inside Profile */}
                <div className="px-4 py-3 border-b border-gray-100 dark:border-white/10 bg-yellow-50/30 dark:bg-yellow-900/10">
                  <div className="flex items-center space-x-1.5 mb-2">
                    <Wrench className="w-3.5 h-3.5 text-yellow-600 dark:text-yellow-500" />
                    <span className="text-xs font-bold text-yellow-700 dark:text-yellow-500 uppercase tracking-wider">Dev Simulation</span>
                  </div>
                  <div className="space-y-2">
                    <div>
                      <select 
                        value={user.systemRole} 
                        onChange={(e) => loginAs({ systemRole: e.target.value as SystemRole })}
                        className="w-full bg-white dark:bg-[#222] border border-gray-200 dark:border-white/10 rounded-md px-2 py-1 text-xs font-bold text-gray-700 dark:text-gray-300 outline-none"
                      >
                        <option value="Administrator">Role: Admin</option>
                        <option value="User">Role: User</option>
                      </select>
                    </div>
                    <div>
                      <select 
                        value={user.position} 
                        onChange={(e) => loginAs({ position: e.target.value as Role })}
                        className="w-full bg-white dark:bg-[#222] border border-gray-200 dark:border-white/10 rounded-md px-2 py-1 text-xs font-bold text-gray-700 dark:text-gray-300 outline-none"
                      >
                        <option value="Team Leader">Pos: Team Leader</option>
                        <option value="Group Leader">Pos: Group Leader</option>
                        <option value="Part Leader">Pos: Part Leader</option>
                        <option value="Cell Leader">Pos: Cell Leader</option>
                        <option value="Staff">Pos: Staff</option>
                      </select>
                    </div>
                  </div>
                </div>

                <div className="p-1">
                  <button 
                    onClick={() => {
                      localStorage.removeItem("token");
                      localStorage.removeItem("user");
                      router.push("/login");
                    }}
                    className="w-full flex items-center px-3 py-2 text-sm font-semibold text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-500/10 rounded-lg transition-colors"
                  >
                    <LogOut className="w-4 h-4 mr-2" />
                    Sign out
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </header>
    </>
  );
}

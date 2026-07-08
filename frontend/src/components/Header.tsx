"use client";

import { 
   Bell, Moon, Sun, MessageSquare, Sparkles, Command, Users, 
  CheckCircle, ShieldCheck, ChevronDown, Wrench, LogOut, Layers, Lock, 
  Eye, EyeOff, X, FileBadge, UserCog, HelpCircle, Award, BookOpen, LayoutDashboard, 
  ShieldAlert, Settings, PackageSearch, Hexagon, CheckSquare, Search
} from "lucide-react";
import { useTheme } from "next-themes";
import { useEffect, useState } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth, Role } from "@/lib/contexts/AuthContext";
import { useLanguage } from "@/lib/contexts/LanguageContext";
import { motion } from "framer-motion";

export function Header() {
  const { theme, setTheme } = useTheme();
  const [mounted, setMounted] = useState(false);
  const pathname = usePathname();
  const router = useRouter();
  
  const { user, loginAs, activeRoleLens, setRoleLens } = useAuth();
  const { locale, setLocale, t } = useLanguage();
  
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [oldPassword, setOldPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [passwordError, setPasswordError] = useState("");
  const [passwordSuccess, setPasswordSuccess] = useState("");
  const [isChangingPassword, setIsChangingPassword] = useState(false);

  const hasRoleLensAccess = user.systemRole === 'Administrator' || ['Group Leader', 'Part Leader'].includes(user.position);
  const availableLenses: Role[] = ['Group Leader', 'Part Leader', 'Cell Leader', 'Staff'];

  // Dynamic Navigation builder
  const getNavigation = () => {
    const supportSubItems = [
      { name: t.nav.faq, href: "/support/faq", icon: HelpCircle },
      { name: t.nav.qa, href: "/support/qa", icon: MessageSquare },
      { name: t.nav.chatWithAdmin, href: "#chat-with-admin", icon: ShieldAlert },
    ];

    if (user.systemRole === 'Administrator') {
      supportSubItems.push({ name: t.nav.userManagement, href: "/support/users", icon: UserCog });
      supportSubItems.push({ name: t.nav.auditLogs, href: "/support/audit-logs", icon: FileBadge });
    }

    return [
      { 
        name: t.nav.workforce, 
        href: "/hr", 
        icon: Users,
        subItems: [
          { name: t.nav.organization, href: "/hr", icon: LayoutDashboard },
          { name: t.nav.training, href: "/hr/training", icon: BookOpen },
          { name: t.nav.certificate, href: "/hr/certificate", icon: Award },
          { name: t.nav.test, href: "/hr/test", icon: CheckCircle }
        ]
      },
      { name: t.nav.inspections, href: "/inspections", icon: Wrench },
      { name: t.nav.newModels, href: "/new-model", icon: PackageSearch },
      { name: t.nav.compliance, href: "/standards", icon: FileBadge },
      { name: t.nav.tasks, href: "/tasks", icon: CheckSquare },
      { 
        name: t.nav.support, 
        href: "/settings", 
        icon: Settings,
        subItems: supportSubItems
      },
    ];
  };

  const mainNavigation = getNavigation();

  useEffect(() => {
    setMounted(true); // eslint-disable-line react-hooks/set-state-in-effect
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
  }, [setTheme, theme]);

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError("");
    setPasswordSuccess("");

    if (newPassword !== confirmPassword) {
      setPasswordError("Mật khẩu mới không khớp.");
      return;
    }
    if (newPassword.length < 6) {
      setPasswordError("Mật khẩu mới phải từ 6 ký tự trở lên.");
      return;
    }

    setIsChangingPassword(true);
    try {
      const res = await fetch("http://localhost:5000/api/auth/change-password", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          username: user.employeeId,
          oldPassword,
          newPassword
        })
      });
      const data = await res.json();
      if (!res.ok) {
        throw new Error(data.message || "Có lỗi xảy ra khi đổi mật khẩu.");
      }
      setPasswordSuccess("Đổi mật khẩu thành công!");
      setTimeout(() => {
        setIsPasswordModalOpen(false);
        setOldPassword("");
        setNewPassword("");
        setConfirmPassword("");
        setPasswordSuccess("");
      }, 1500);
    } catch (err: any /* eslint-disable-line @typescript-eslint/no-explicit-any */) {
      setPasswordError(err.message);
    } finally {
      setIsChangingPassword(false);
    }
  };

  return (
    <>
      <header className="w-full flex items-center justify-between h-14 sm:h-16 px-4 sm:px-8 bg-white/80 dark:bg-background/80 backdrop-blur-xl border-b border-border z-50 sticky top-0 transition-colors duration-500">
        
        {/* LEFT ZONE: Brand */}
        <div className="flex items-center flex-1 min-w-0">
          <Link href="/overview" className="flex items-center group relative overflow-hidden flex-shrink-0 mr-8">
            <div className="w-8 h-8 bg-gradient-to-br from-primary to-indigo-900 rounded-lg flex items-center justify-center shadow-md shadow-blue-900/20 relative ring-1 ring-white/10 transition-transform duration-300 group-hover:scale-105">
              <Hexagon className="w-5 h-5 text-white absolute" strokeWidth={1.5} />
              <Sparkles className="w-2.5 h-2.5 text-blue-300 absolute" />
            </div>
            <div className="flex flex-col ml-3 hidden sm:flex justify-center h-full">
              <h1 className="text-lg font-black tracking-tight text-gray-900 dark:text-white leading-none">
                IQC Nexus
              </h1>
              <span className="text-[10px] font-bold text-gray-500 dark:text-gray-400 mt-0.5 leading-none">
                Enterprise Quality Intelligence Platform
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
                  {item.subItems ? (
                    <button
                      className={`relative flex items-center h-10 px-3.5 text-sm font-semibold transition-colors duration-300 rounded-lg hover:bg-gray-100 dark:hover:bg-white/5 ${
                        isActive
                          ? "text-gray-900 dark:text-white"
                          : isSupport 
                            ? "text-gray-400 dark:text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
                            : "text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white"
                      }`}
                    >
                      {item.name}
                      <ChevronDown className="w-3.5 h-3.5 ml-1.5 opacity-50 group-hover:opacity-100 group-hover:rotate-180 transition-all duration-300" />
                      
                      {/* Animated Underline for Active State */}
                      {isActive && (
                        <motion.div 
                          layoutId="navbar-underline"
                          className="absolute bottom-0 left-3 right-3 h-[2px] bg-primary dark:bg-blue-400 rounded-t-full"
                          initial={false}
                          transition={{ type: "spring", stiffness: 300, damping: 30 }}
                        />
                      )}
                    </button>
                  ) : (
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
                      
                      {/* Animated Underline for Active State */}
                      {isActive && (
                        <motion.div 
                          layoutId="navbar-underline"
                          className="absolute bottom-0 left-3 right-3 h-[2px] bg-primary dark:bg-blue-400 rounded-t-full"
                          initial={false}
                          transition={{ type: "spring", stiffness: 300, damping: 30 }}
                        />
                      )}
                    </Link>
                  )}

                  {/* Dropdown Menu */}
                  {item.subItems && (
                    <div className="absolute top-full left-1/2 -translate-x-1/2 pt-2 opacity-0 translate-y-1 pointer-events-none group-hover:opacity-100 group-hover:translate-y-0 group-hover:pointer-events-auto transition-all duration-300 z-[100]">
                      <div className="w-56 bg-white dark:bg-popover border border-border rounded-xl shadow-xl overflow-hidden p-1 flex flex-col backdrop-blur-xl">
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
          <div className="hidden xl:flex relative group mr-3">
            <div className="absolute left-3 top-1/2 -translate-y-1/2 flex items-center space-x-1.5 pointer-events-none">
              <Search className="w-4 h-4 text-blue-500" />
            </div>
            <input 
              id="global-search-input"
              type="text" 
              placeholder={t.header.searchPlaceholder} 
              className="w-80 h-9 pl-9 pr-12 bg-gray-100/50 dark:bg-white/5 border border-transparent hover:border-gray-200 dark:hover:border-white/10 focus:border-primary/30 dark:focus:border-blue-500/30 focus:bg-white dark:focus:bg-[#1A1A1A] rounded-xl text-sm font-medium text-gray-900 dark:text-white outline-none transition-all duration-300 placeholder-gray-400 dark:placeholder-gray-500 focus:shadow-[0_0_0_3px_rgba(20,40,160,0.1)] dark:focus:shadow-[0_0_0_3px_rgba(59,130,246,0.1)]"
            />
            <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center">
              <kbd className="inline-flex items-center h-5 px-1.5 text-[10px] font-bold bg-white dark:bg-[#2A2A2A] border border-border rounded-md text-gray-400 shadow-sm">
                <Command className="w-3 h-3 mr-0.5" /> K
              </kbd>
            </div>
          </div>

          {/* Utility Cluster (Chat, Notif, Theme) */}
          <div className="flex items-center bg-gray-100/50 dark:bg-white/5 border border-gray-200/50 border-border rounded-xl p-0.5 mx-1 hidden sm:flex">
            {/* Chat */}
            <button className="relative p-1.5 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-white dark:hover:bg-white/10 transition-colors duration-300 rounded-lg group mx-0.5">
              <MessageSquare className="w-[18px] h-[18px]" />
            </button>

            {/* Notifications */}
            <button className="relative p-1.5 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-white dark:hover:bg-white/10 transition-colors duration-300 rounded-lg group mx-0.5">
              <Bell className="w-[18px] h-[18px] group-hover:animate-[wiggle_0.5s_ease-in-out_infinite]" />
              <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-rose-500 rounded-full ring-2 ring-white dark:ring-[#050505]"></span>
            </button>
            
            {/* Language Toggle */}
            {mounted && (
              <button 
                onClick={() => setLocale(locale === 'en' ? 'vi' : 'en')}
                className="px-2 font-bold text-[11px] text-gray-500 hover:text-gray-900 dark:hover:text-white hover:bg-white dark:hover:bg-white/10 transition-colors duration-300 rounded-lg mx-0.5 uppercase"
                title="Change Language"
              >
                {locale}
              </button>
            )}

            {/* Theme Toggle */}
            {mounted && (
              <button 
                onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
                className="p-1.5 text-gray-400 hover:text-gray-900 dark:hover:text-white hover:bg-white dark:hover:bg-white/10 transition-colors duration-300 rounded-lg group mx-0.5"
                title={t.header.toggleTheme}
              >
                {theme === 'dark' ? <Sun className="w-[18px] h-[18px] group-hover:rotate-45 transition-transform" /> : <Moon className="w-[18px] h-[18px] group-hover:-rotate-12 transition-transform" />}
              </button>
            )}
          </div>

          {/* Role Lens View Selector */}
          {hasRoleLensAccess && (
            <div className="relative group mx-1 hidden md:block">
              <button className="flex items-center space-x-1.5 px-3 py-1.5 bg-indigo-50 dark:bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 border border-indigo-100 dark:border-indigo-500/20 hover:bg-indigo-100 dark:hover:bg-indigo-500/20 transition-all rounded-lg font-bold text-xs">
                <Layers className="w-3.5 h-3.5" />
                <span>Lens: {activeRoleLens}</span>
                <ChevronDown className="w-3 h-3 ml-1 opacity-70" />
              </button>
              
              <div className="absolute right-0 top-full pt-2 opacity-0 translate-y-2 pointer-events-none group-hover:opacity-100 group-hover:translate-y-0 group-hover:pointer-events-auto transition-all duration-200 z-[100]">
                <div className="w-48 bg-white dark:bg-popover border border-border rounded-xl shadow-xl overflow-hidden flex flex-col backdrop-blur-xl">
                  <div className="px-3 py-2 border-b border-border bg-gray-50 dark:bg-white/5">
                    <span className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">{t.header.switchPerspective}</span>
                  </div>
                  <div className="p-1 space-y-0.5">
                    {availableLenses.map(lens => (
                      <button 
                        key={lens}
                        onClick={() => setRoleLens(lens)}
                        className={`w-full text-left px-3 py-2 text-sm font-medium rounded-lg transition-colors flex items-center justify-between ${activeRoleLens === lens ? 'bg-indigo-50 dark:bg-indigo-500/10 text-indigo-600 dark:text-indigo-400' : 'text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-white/5'}`}
                      >
                        {lens}
                        {activeRoleLens === lens && <CheckCircle className="w-3.5 h-3.5" />}
                      </button>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* User Profile */}
          <div className="relative group ml-1 hidden sm:block">
            <button className="flex items-center space-x-2 py-1.5 px-3 bg-transparent hover:bg-gray-100 dark:hover:bg-white/5 rounded-xl border border-transparent hover:border-gray-200 dark:hover:border-white/10 transition-all active:scale-95">
              <div className="flex flex-col items-end text-right">
                <span className="text-sm font-bold text-gray-900 dark:text-white leading-tight whitespace-nowrap">{user.name}</span>
                <span className="text-[10px] font-medium text-gray-500">@{user.employeeId}</span>
              </div>
              <ChevronDown className="w-3.5 h-3.5 text-gray-400" />
            </button>
            
            {/* User Dropdown with Dev Mode Inside */}
            <div className="absolute right-0 top-full pt-2 opacity-0 translate-y-2 pointer-events-none group-hover:opacity-100 group-hover:translate-y-0 group-hover:pointer-events-auto transition-all duration-200 z-[100]">
              <div className="w-64 bg-white dark:bg-popover border border-border rounded-xl shadow-2xl overflow-hidden flex flex-col backdrop-blur-xl ring-1 ring-black/5 dark:ring-white/5">
                <div className="px-4 py-4 border-b border-border bg-gray-50/50 dark:bg-white/[0.02]">
                  <p className="text-sm font-bold text-gray-900 dark:text-white truncate leading-tight">{user.name}</p>
                  <p className="text-xs font-medium text-gray-500 truncate mt-0.5">@{user.employeeId}</p>
                  {user.systemRole === 'Administrator' && (
                    <span className="inline-flex items-center mt-2 px-2 py-0.5 bg-rose-100 dark:bg-rose-900/30 text-rose-700 dark:text-rose-400 text-[10px] font-bold rounded-sm uppercase tracking-wider">
                      <ShieldCheck className="w-3 h-3 mr-1" /> Admin
                    </span>
                  )}
                </div>

                {/* Dev Mode Options inside Profile */}
                <div className="px-4 py-3 border-b border-border bg-yellow-50/30 dark:bg-yellow-900/10">
                  <div className="flex items-center space-x-1.5 mb-2">
                    <Wrench className="w-3.5 h-3.5 text-yellow-600 dark:text-yellow-500" />
                    <span className="text-xs font-bold text-yellow-700 dark:text-yellow-500 uppercase tracking-wider">{t.header.devOverride}</span>
                  </div>
                  <div className="space-y-2">
                    <div>
                      <select 
                        value={user.systemRole} 
                        onChange={(e) => loginAs({ systemRole: e.target.value as any /* eslint-disable-line @typescript-eslint/no-explicit-any */ })}
                        className="w-full bg-white dark:bg-secondary border border-border rounded-md px-2 py-1 text-xs font-bold text-gray-700 dark:text-gray-300 outline-none"
                      >
                        <option value="Administrator">Role: Admin</option>
                        <option value="User">Role: User</option>
                      </select>
                    </div>
                    <div>
                      <select 
                        value={user.position} 
                        onChange={(e) => loginAs({ position: e.target.value as any /* eslint-disable-line @typescript-eslint/no-explicit-any */ })}
                        className="w-full bg-white dark:bg-secondary border border-border rounded-md px-2 py-1 text-xs font-bold text-gray-700 dark:text-gray-300 outline-none"
                      >
                        <option value="Group Leader">Pos: Group Leader</option>
                        <option value="Part Leader">Pos: Part Leader</option>
                        <option value="Cell Leader">Pos: Cell Leader</option>
                        <option value="Staff">Pos: Staff</option>
                      </select>
                    </div>
                  </div>
                </div>

                <div className="p-1 border-b border-border">
                  <button 
                    onClick={() => setIsPasswordModalOpen(true)}
                    className="w-full flex items-center px-3 py-2 text-sm font-semibold text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-white/5 rounded-lg transition-colors"
                  >
                    <Lock className="w-4 h-4 mr-2" />
                    {t.header.changePassword}
                  </button>
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
                    {t.header.signOut}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Change Password Modal */}
      {isPasswordModalOpen && (
        <div className="fixed inset-0 z-[999] flex items-center justify-center">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => !isChangingPassword && setIsPasswordModalOpen(false)}></div>
          <motion.div 
            initial={{ opacity: 0, scale: 0.95, y: 10 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            className="relative w-full max-w-md bg-white dark:bg-popover rounded-2xl shadow-2xl border border-border overflow-hidden"
          >
            <div className="px-6 py-4 border-b border-border flex items-center justify-between">
              <h2 className="text-lg font-bold text-gray-900 dark:text-white flex items-center">
                <Lock className="w-5 h-5 mr-2 text-primary dark:text-blue-500" />
                Đổi Mật Khẩu
              </h2>
              <button 
                onClick={() => !isChangingPassword && setIsPasswordModalOpen(false)}
                className="p-1 rounded-md text-gray-400 hover:bg-gray-100 dark:hover:bg-white/10 transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            
            <form onSubmit={handleChangePassword} className="p-6 space-y-4">
              {passwordError && (
                <div className="p-3 bg-rose-50 text-rose-600 dark:bg-rose-900/20 dark:text-rose-400 rounded-lg text-sm font-medium">
                  {passwordError}
                </div>
              )}
              {passwordSuccess && (
                <div className="p-3 bg-emerald-50 text-emerald-600 dark:bg-emerald-900/20 dark:text-emerald-400 rounded-lg text-sm font-medium">
                  {passwordSuccess}
                </div>
              )}
              
              <div className="space-y-1">
                <label className="text-xs font-bold text-gray-700 dark:text-gray-300">Mật khẩu cũ</label>
                <div className="relative">
                  <input 
                    type={showPassword ? "text" : "password"} 
                    value={oldPassword}
                    onChange={e => setOldPassword(e.target.value)}
                    required
                    className="w-full pl-3 pr-10 py-2 bg-gray-50 dark:bg-secondary border border-border rounded-lg text-sm font-medium focus:border-primary dark:focus:border-blue-500 outline-none transition-colors"
                  />
                  <button type="button" onClick={() => setShowPassword(!showPassword)} className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>
              <div className="space-y-1">
                <label className="text-xs font-bold text-gray-700 dark:text-gray-300">Mật khẩu mới</label>
                <input 
                  type={showPassword ? "text" : "password"} 
                  value={newPassword}
                  onChange={e => setNewPassword(e.target.value)}
                  required
                  className="w-full pl-3 pr-10 py-2 bg-gray-50 dark:bg-secondary border border-border rounded-lg text-sm font-medium focus:border-primary dark:focus:border-blue-500 outline-none transition-colors"
                />
              </div>
              <div className="space-y-1">
                <label className="text-xs font-bold text-gray-700 dark:text-gray-300">Nhập lại mật khẩu mới</label>
                <input 
                  type={showPassword ? "text" : "password"} 
                  value={confirmPassword}
                  onChange={e => setConfirmPassword(e.target.value)}
                  required
                  className="w-full pl-3 pr-10 py-2 bg-gray-50 dark:bg-secondary border border-border rounded-lg text-sm font-medium focus:border-primary dark:focus:border-blue-500 outline-none transition-colors"
                />
              </div>
              
              <div className="pt-2 flex justify-end">
                <button 
                  type="submit" 
                  disabled={isChangingPassword}
                  className="px-4 py-2 bg-primary hover:bg-blue-700 dark:bg-blue-600 dark:hover:bg-blue-500 text-white rounded-lg text-sm font-bold shadow-md disabled:opacity-50 transition-all"
                >
                  {isChangingPassword ? "Đang xử lý..." : "Cập nhật mật khẩu"}
                </button>
              </div>
            </form>
          </motion.div>
        </div>
      )}
    </>
  );
}




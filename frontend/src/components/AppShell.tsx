"use client";

import { useAuth } from "@/lib/contexts/AuthContext";
import { navigationConfig } from "@/lib/navigation";
import {
  Bell,
  ChevronDown,
  Download,
  FileSpreadsheet,
  LayoutDashboard,
  Menu,
  PanelLeftClose,
  PanelLeftOpen,
  ShieldCheck,
  User,
  UserRound,
  X,
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import React, { useEffect, useRef, useState } from "react";

const iconMap = {
  dashboard: LayoutDashboard,
  imports: FileSpreadsheet,
  downloads: Download,
  audit: ShieldCheck,
  profile: User,
};

const routeLabels: Record<string, string> = {
  overview: "Dashboard",
  imports: "Import Center",
  downloads: "Download Center",
  audit: "Audit Trail",
  profile: "My Profile",
  unauthorized: "Unauthorized",
};

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { user, can, signOut } = useAuth();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement>(null);

  const segment = pathname.split("/").filter(Boolean)[0] || "overview";
  const title = routeLabels[segment] || "IQC Nexus";

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setProfileOpen(false);
        setMobileOpen(false);
      }
    }
    function handleClickOutside(event: MouseEvent) {
      if (!profileRef.current?.contains(event.target as Node)) {
        setProfileOpen(false);
      }
    }
    document.addEventListener("keydown", handleKeyDown);
    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  async function handleSignOut() {
    await signOut();
    router.replace("/login");
  }

  const visibleNavItems = navigationConfig.filter((item) =>
    item.permission ? can(item.permission) : true
  );

  return (
    <div className={`app-shell ${collapsed ? "nav-collapsed" : ""}`}>
      {mobileOpen && (
        <button
          className="nav-scrim"
          aria-label="Close navigation overlay"
          onClick={() => setMobileOpen(false)}
        />
      )}
      <aside className={`app-nav ${mobileOpen ? "mobile-open" : ""}`} aria-label="Main sidebar">
        <div className="nav-brand">
          <span className="brand-mark small" aria-hidden="true">
            IQ
          </span>
          <span className="nav-brand-copy">
            <strong>IQC Nexus</strong>
            <small>Quality Intelligence</small>
          </span>
          <button
            className="icon-button mobile-only"
            onClick={() => setMobileOpen(false)}
            aria-label="Close menu"
          >
            <X aria-hidden="true" />
          </button>
        </div>

        <nav aria-label="Primary navigation">
          {visibleNavItems.map((item) => {
            const IconComponent = iconMap[item.iconName];
            const active =
              pathname === item.href ||
              (item.href !== "/overview" && pathname.startsWith(`${item.href}`));
            return (
              <Link
                key={item.id}
                href={item.href}
                aria-current={active ? "page" : undefined}
                className={active ? "active" : ""}
                onClick={() => setMobileOpen(false)}
              >
                <IconComponent aria-hidden="true" />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>

        <button
          className="collapse-control"
          onClick={() => setCollapsed((val) => !val)}
          aria-label={collapsed ? "Expand navigation sidebar" : "Collapse navigation sidebar"}
        >
          {collapsed ? <PanelLeftOpen aria-hidden="true" /> : <PanelLeftClose aria-hidden="true" />}
          <span>{collapsed ? "Expand" : "Collapse navigation"}</span>
        </button>
      </aside>

      <div className="app-workspace">
        <header className="app-header">
          <button
            className="icon-button mobile-only"
            onClick={() => setMobileOpen(true)}
            aria-label="Open navigation menu"
          >
            <Menu aria-hidden="true" />
          </button>

          <div className="page-context">
            <p>
              <Link href="/overview">IQC NEXUS</Link> / {title.toUpperCase()}
            </p>
            <h1>{title}</h1>
          </div>

          <div className="header-tools">
            <span className="environment-chip">
              {process.env.NEXT_PUBLIC_ENVIRONMENT || "DEV"} · {process.env.NEXT_PUBLIC_APP_VERSION || "v1.0.0"}
            </span>

            <button className="icon-button" aria-label="Notifications" title="Notifications">
              <Bell aria-hidden="true" />
            </button>

            <div className="profile" ref={profileRef}>
              <button
                className="profile-trigger"
                onClick={() => setProfileOpen((val) => !val)}
                aria-expanded={profileOpen}
                aria-haspopup="menu"
                aria-label="User account menu"
              >
                <UserRound aria-hidden="true" />
                <span>{user.fullName || user.username || "User"}</span>
                <ChevronDown aria-hidden="true" />
              </button>

              {profileOpen && (
                <div className="profile-menu" role="menu" aria-label="User profile options">
                  <div>
                    <strong>{user.fullName || user.username}</strong>
                    <small className="block text-xs">{user.email || user.username}</small>
                  </div>

                  <div className="profile-meta">
                    <span>{user.position || "Staff"}</span>
                    <span>{user.organization || "IQC"}</span>
                  </div>

                  <Link
                    href="/profile"
                    role="menuitem"
                    onClick={() => setProfileOpen(false)}
                  >
                    My Profile
                  </Link>

                  <button role="menuitem" onClick={handleSignOut}>
                    Sign Out
                  </button>
                </div>
              )}
            </div>
          </div>
        </header>

        <main className="app-content">{children}</main>
      </div>
    </div>
  );
}
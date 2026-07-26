import { PlatformPermission } from "@/lib/auth/client";

export interface NavItemConfig {
  id: string;
  label: string;
  href: string;
  iconName: "dashboard" | "imports" | "downloads" | "audit" | "profile";
  permission?: PlatformPermission;
  badge?: string;
}

export const navigationConfig: NavItemConfig[] = [
  {
    id: "overview",
    label: "Dashboard",
    href: "/overview",
    iconName: "dashboard",
    permission: "dashboard.view",
  },
  {
    id: "imports",
    label: "Import Center",
    href: "/imports",
    iconName: "imports",
    permission: "import.view",
  },
  {
    id: "downloads",
    label: "Download Center",
    href: "/downloads",
    iconName: "downloads",
    permission: "download.view",
  },
  {
    id: "audit",
    label: "Audit Logs",
    href: "/audit",
    iconName: "audit",
    permission: "audit.view",
  },
  {
    id: "profile",
    label: "My Profile",
    href: "/profile",
    iconName: "profile",
  },
];

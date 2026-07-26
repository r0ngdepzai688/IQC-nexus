"use client";
import { PlatformPermission } from "@/lib/auth/client";
import { useAuth } from "@/lib/contexts/AuthContext";
import { ForbiddenState, LoadingState, ServiceErrorState } from "./states";
export function PermissionGate({ permission, children }: { permission: PlatformPermission; children: React.ReactNode }) {
  const { status, can } = useAuth();
  if (status === "loading") return <LoadingState label="Checking permission" />;
  if (status === "error") return <ServiceErrorState />;
  if (!can(permission)) return <ForbiddenState />;
  return children;
}
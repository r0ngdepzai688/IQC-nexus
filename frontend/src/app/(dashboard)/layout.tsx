"use client";

import { AppShell } from "@/components/AppShell";
import { LoadingState, ServiceErrorState } from "@/components/portal/states";
import { useAuth } from "@/lib/contexts/AuthContext";
import { useRouter } from "next/navigation";
import React, { useEffect } from "react";

function SessionBoundary({ children }: { children: React.ReactNode }) {
  const { status } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (status === "anonymous") {
      router.replace("/login");
    }
  }, [status, router]);

  if (status === "loading" || status === "anonymous") {
    return <LoadingState label="Verifying session" />;
  }

  if (status === "error") {
    return <ServiceErrorState retry={() => location.reload()} />;
  }

  return <AppShell>{children}</AppShell>;
}

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return <SessionBoundary>{children}</SessionBoundary>;
}
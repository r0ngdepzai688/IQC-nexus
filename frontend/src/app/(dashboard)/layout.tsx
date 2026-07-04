"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Sidebar } from "@/components/Sidebar";
import { Header } from "@/components/Header";
import { FloatingChat } from "@/components/FloatingChat";

import { AuthProvider } from "@/lib/contexts/AuthContext";

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (!token) {
      router.push("/login");
    } else {
      setIsAuthenticated(true);
    }
  }, [router]);

  if (!isAuthenticated) {
    return null; // or a loading spinner
  }

  return (
    <AuthProvider>
      <div className="flex flex-col h-screen overflow-hidden p-4 gap-4 relative z-10">
        <Header />
        <div className="flex-1 flex overflow-hidden gap-4">
          <Sidebar />
          <main className="flex-1 overflow-y-auto rounded-[2rem] pb-8 pr-1 custom-scrollbar">
            {children}
          </main>
        </div>
      </div>

      <FloatingChat />
    </AuthProvider>
  );
}
